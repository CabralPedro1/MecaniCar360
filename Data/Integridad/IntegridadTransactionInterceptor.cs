using System.Data.Common;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MecaniCar360.Data.Integridad;

public sealed class IntegridadTransactionInterceptor : DbTransactionInterceptor
{
    public const string Recurso = "MecaniCar360:Integridad:Escritura:v1";
    private sealed class Marca { public bool Fallida; }
    private static readonly ConditionalWeakTable<DbTransaction, Marca> Transacciones = new();
    public static readonly IntegridadTransactionInterceptor Instancia = new();

    public static void Exigir(DbTransaction transaction)
    {
        if (!Transacciones.TryGetValue(transaction, out var m) || m.Fallida)
            throw new InvalidOperationException("Transaccion no habilitada por integridad o abortada. Descarte el contexto.");
    }
    internal static void MarcarFallida(DbTransaction transaction)
    {
        if (Transacciones.TryGetValue(transaction, out var m)) m.Fallida = true;
    }
    private static async Task TomarAsync(DbTransaction transaction, CancellationToken ct)
    {
        try
        {
            await using var cmd = transaction.Connection!.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandTimeout = 15;
            cmd.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'MecaniCar360:Integridad:Escritura:v1', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; SELECT @r;";
            var code = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            if (code < 0) throw new InvalidOperationException("No se pudo obtener el bloqueo de integridad. Reintente la operacion completa.");
            Transacciones.Add(transaction, new Marca());
        }
        catch
        {
            try { await transaction.RollbackAsync(CancellationToken.None); } finally { await transaction.DisposeAsync(); }
            throw;
        }
    }
    public override DbTransaction TransactionStarted(DbConnection connection, TransactionEndEventData eventData, DbTransaction result)
    { TomarAsync(result, CancellationToken.None).GetAwaiter().GetResult(); return result; }
    public override async ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
    { await TomarAsync(result, cancellationToken).ConfigureAwait(false); return result; }
    public override DbTransaction TransactionUsed(DbConnection connection, TransactionEventData eventData, DbTransaction result)
    { Exigir(result); return result; }
    public override ValueTask<DbTransaction> TransactionUsedAsync(DbConnection connection, TransactionEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
    { Exigir(result); return ValueTask.FromResult(result); }
    public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
    { ValidarCommit(transaction, eventData); return result; }
    public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    { ValidarCommit(transaction, eventData); return ValueTask.FromResult(result); }
    private static void ValidarCommit(DbTransaction transaction, TransactionEventData eventData)
    {
        Exigir(transaction);
        if (eventData.Context is MecaniCarContext db && (db.IntegridadFallida ||
            db.EstadoIntegridad?.Resultado.Estado == Models.DTOs.CondicionIntegridad.Comprometida))
            throw new InvalidOperationException("No se puede confirmar una transaccion con integridad comprometida.");
    }
}
