using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using MecaniCar360.Data;
using MecaniCar360.Services;

namespace MecaniCar360.Concurrency;

public sealed class ControlledReadGate(string table, CancellationToken stop = default) : DbCommandInterceptor
{
    public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int used;
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.Transaction != null && command.CommandText.Contains("[" + table + "]", StringComparison.Ordinal) &&
            Interlocked.CompareExchange(ref used, 1, 0) == 0)
        {
            Reached.TrySetResult();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(stop, cancellationToken);
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(5), linked.Token);
        }
        return result;
    }
}

public sealed class TransactionAttempt : DbTransactionInterceptor
{
    public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default)
    {
        Reached.TrySetResult();
        return ValueTask.FromResult(result);
    }
}

public sealed class InjectedPersistenceFailure : Exception;

// Cancellation at EF command boundaries; direct production ADO.NET cannot be
// interrupted by this interceptor. The runner must drain or abort the process.
public sealed class CancellationBoundary(CancellationToken stop) : DbCommandInterceptor
{
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { stop.ThrowIfCancellationRequested(); return ValueTask.FromResult(result); }
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { stop.ThrowIfCancellationRequested(); return ValueTask.FromResult(result); }
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    { stop.ThrowIfCancellationRequested(); return ValueTask.FromResult(result); }
}

// Both fault points share evidence bound to the actual transaction. No tracked
// values or callback flags substitute for reading persisted (uncommitted) rows.
public sealed class WriteEvidence : SaveChangesInterceptor
{
    private DbTransaction? transaction;
    private byte[]? before;
    private byte[]? verticalBefore;
    public bool Written { get; private set; }
    public bool IntegrityMaintained { get; private set; }
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var db = eventData.Context as MecaniCarContext ?? throw new InvalidOperationException("Missing operation context.");
        var current = db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("Write evidence requires an existing transaction.");
        if(transaction != null && !ReferenceEquals(transaction,current))
            throw new InvalidOperationException("Persistence evidence crossed transaction boundaries.");
        transaction = current;
        before = await ConcurrencyFixtures.SnapshotAsync(db, cancellationToken: cancellationToken);
        verticalBefore ??= await ConcurrencyFixtures.SnapshotAsync(db, true, cancellationToken);
        return result;
    }

    public async Task RequireWriteAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct)
    {
        var db = (MecaniCarContext)eventData.Context!;
        RequireTransaction(db.Database.CurrentTransaction?.GetDbTransaction());
        if(result <= 0 || before == null || before.SequenceEqual(await ConcurrencyFixtures.SnapshotAsync(db, cancellationToken: ct)))
            throw new InvalidOperationException("Fault point not reached: no persisted DML change in this transaction.");
        Written = true;
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        await RequireWriteAsync(eventData, result, cancellationToken);
        return result;
    }

    public async Task RequireIntegrityAsync(DbTransaction current, TransactionEventData eventData, CancellationToken ct)
    {
        RequireTransaction(current);
        var db = (MecaniCarContext)eventData.Context!;
        if(!Written || verticalBefore == null ||
            verticalBefore.SequenceEqual(await ConcurrencyFixtures.SnapshotAsync(db, true, ct)) ||
            !(await new IntegridadService(db).VerificarAsync(ct)).EsValida)
            throw new InvalidOperationException("Fault point not reached: DML and valid changed DVH/DVV are required before commit.");
        IntegrityMaintained = true;
    }

    private void RequireTransaction(DbTransaction? current)
    {
        if(transaction == null || !ReferenceEquals(transaction,current))
            throw new InvalidOperationException("Missing same-transaction persistence evidence.");
    }
}

public sealed class FailAfterWrite(WriteEvidence evidence) : SaveChangesInterceptor
{
    public bool Fired { get; private set; }
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        await evidence.RequireWriteAsync(eventData, result, cancellationToken);
        Fired = true;
        throw new InjectedPersistenceFailure();
    }
}

// A second injection point, after the coordinator has maintained DVH/DVV and
// immediately before SQL COMMIT. No production transaction interceptor is replaced.
public sealed class FailBeforeCommit(WriteEvidence evidence) : DbTransactionInterceptor
{
    public bool Fired { get; private set; }
    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
        TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        await evidence.RequireIntegrityAsync(transaction, eventData, cancellationToken);
        Fired = true;
        throw new InjectedPersistenceFailure();
    }
}
