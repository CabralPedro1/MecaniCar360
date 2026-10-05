using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MecaniCar360.Data.Integridad;

internal static class CoordinadorIntegridad
{
    internal static async Task<int> GuardarAsync(MecaniCarContext db, Func<Task<int>> guardarBase, bool aceptar, CancellationToken ct)
    {
        if (db.IntegridadFallida || db.EstadoIntegridad?.Resultado.Estado == CondicionIntegridad.Comprometida)
            throw new InvalidOperationException("El contexto o el sistema estan bloqueados por integridad.");
        if (System.Transactions.Transaction.Current != null)
            throw new InvalidOperationException("Integridad no admite transacciones ambientales; use la transaccion EF controlada.");
        db.ChangeTracker.DetectChanges();
        var entries = db.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        if (entries.Any(e => e.Entity is DigitoVerificadorVertical)) throw new InvalidOperationException("DVV solo se mantiene por el coordinador tecnico.");
        var affected = RegistroEntidadesProtegidas.Todas.Where(t => entries.Any(e => e.Metadata.ClrType == t.Tipo)).ToHashSet();
        // Include database cascades, even when dependents are not tracked.
        var deleting = entries.Where(e => e.State == EntityState.Deleted).Select(e => e.Metadata).ToHashSet();
        var queue = new Queue<Microsoft.EntityFrameworkCore.Metadata.IEntityType>(deleting);
        while (queue.TryDequeue(out var type))
            foreach (var fk in type.GetReferencingForeignKeys().Where(f => f.DeleteBehavior == DeleteBehavior.Cascade))
                if (deleting.Add(fk.DeclaringEntityType)) queue.Enqueue(fk.DeclaringEntityType);
        foreach (var t in RegistroEntidadesProtegidas.Todas.Where(t => deleting.Any(x => x.ClrType == t.Tipo))) affected.Add(t);
        var own = db.Database.CurrentTransaction == null;
        var tx = own ? await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false) : db.Database.CurrentTransaction!;
        try
        {
            IntegridadTransactionInterceptor.Exigir(tx.GetDbTransaction());
            if (affected.Count > 0)
            {
                List<ErrorIntegridad> errors;
                try { errors = await AlmacenIntegridad.VerificarAsync(db, affected, ct).ConfigureAwait(false); }
                catch (System.Data.Common.DbException)
                {
                    throw new IntegridadException(new[] { new ErrorIntegridad("Esquema", null, "VERIFICACION_NO_DISPONIBLE") });
                }
                if (errors.Count > 0) throw new IntegridadException(errors);
                // Reject stale tracked writes (a legitimate concurrent commit is not corruption).
                foreach (var t in affected)
                {
                    var rows = await AlmacenIntegridad.LeerAsync(db, t, ct).ConfigureAwait(false);
                    foreach (var entry in entries.Where(e => e.Metadata.ClrType == t.Tipo && e.State != EntityState.Added))
                    {
                        var key = t.Claves.Select(k => entry.Property(k).CurrentValue).ToArray();
                        var row = rows.SingleOrDefault(r => r.Claves.SequenceEqual(key));
                        var original = entry.Property("DVH").OriginalValue as string;
                        if (row == null || original == null || original != row.Dvh) throw new DbUpdateConcurrencyException("La fila cambio. Recargue y reintente la operacion completa.");
                    }
                }
            }
            var count = await guardarBase().ConfigureAwait(false);
            foreach (var t in affected.OrderBy(t => t.Tabla, StringComparer.Ordinal))
            {
                var rows = await AlmacenIntegridad.LeerAsync(db, t, ct).ConfigureAwait(false);
                foreach (var entry in entries.Where(e => e.Metadata.ClrType == t.Tipo && e.State != EntityState.Deleted))
                {
                    var key = t.Claves.Select(k => entry.Property(k).CurrentValue).ToArray();
                    var index = rows.FindIndex(r => r.Claves.SequenceEqual(key));
                    if (index < 0) throw new InvalidOperationException("Fila persistida no disponible para integridad.");
                    var row = rows[index];
                    var dvh = SerializadorCanonico.Dvh(t.Tabla, row.Valores);
                    await AlmacenIntegridad.GuardarDvhAsync(db, t, row, dvh, ct).ConfigureAwait(false);
                    entry.Property("DVH").CurrentValue = dvh;
                    rows[index] = row with { Dvh = dvh };
                }
                await AlmacenIntegridad.GuardarDvvAsync(db, t, rows, false, ct).ConfigureAwait(false);
            }
            if (own) await tx.CommitAsync(ct).ConfigureAwait(false);
            if (aceptar) db.ChangeTracker.AcceptAllChanges();
            return count;
        }
        catch (Exception ex)
        {
            db.IntegridadFallida = true;
            IntegridadTransactionInterceptor.MarcarFallida(tx.GetDbTransaction());
            if (ex is IntegridadException integridad) db.EstadoIntegridad?.Comprometer(integridad.Errores);
            try { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); } catch { /* transaction may already be aborted by SQL Server */ }
            throw;
        }
        finally { if (own) await tx.DisposeAsync().ConfigureAwait(false); }
    }
}
