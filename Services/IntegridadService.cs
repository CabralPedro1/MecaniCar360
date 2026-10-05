using MecaniCar360.Data;
using MecaniCar360.Data.Integridad;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class IntegridadService(MecaniCarContext db)
{
    public async Task<ResultadoIntegridad> VerificarAsync(CancellationToken ct = default)
    {
        var propia = db.Database.CurrentTransaction == null;
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try
        {
            if (propia) tx = await db.Database.BeginTransactionAsync(ct);
            var errores = await AlmacenIntegridad.VerificarAsync(db, RegistroEntidadesProtegidas.Todas, ct);
            if (propia) await tx!.RollbackAsync(ct); // Verification never writes.
            return new(errores.Count == 0 ? CondicionIntegridad.Valida : CondicionIntegridad.Comprometida,
                DateTime.UtcNow, 1, null, errores);
        }
        catch
        {
            if (tx != null) try { await tx.RollbackAsync(CancellationToken.None); } catch { }
            return new(CondicionIntegridad.Comprometida, DateTime.UtcNow, 1, null,
                new[] { new ErrorIntegridad("Esquema", null, "VERIFICACION_NO_DISPONIBLE") });
        }
        finally { if (tx != null) await tx.DisposeAsync(); }
    }

    // Technical operation, deliberately NOT registered in any HTTP controller.
    public async Task InicializarAsync(string archivoBackupServidor, bool confirmarLineaBase, bool confirmarVentanaSinEscrituras, CancellationToken ct = default)
    {
        if (!confirmarLineaBase || !confirmarVentanaSinEscrituras || db.Database.CurrentTransaction != null || db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Inicializacion requiere confirmacion explicita, ventana sin escrituras y contexto nuevo.");
        await VerificarBackupAsync(archivoBackupServidor, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await using (var cmd = AlmacenIntegridad.Comando(db, "SELECT COUNT_BIG(*) FROM dbo.DigitosVerificadoresVerticales WITH(TABLOCKX,HOLDLOCK)"))
            if (Convert.ToInt64(await cmd.ExecuteScalarAsync(ct)) != 0)
                throw new InvalidOperationException("Ya existe una linea base. No se sobrescribe ni repara.");
        // Read/validate every table before changing any row; database transaction rolls back on failure.
        var tablas = new List<(EntidadProtegida Entidad, List<FilaIntegridad> Filas)>();
        foreach (var e in RegistroEntidadesProtegidas.Todas.OrderBy(e => e.Tabla, StringComparer.Ordinal))
        {
            var rows = await AlmacenIntegridad.LeerAsync(db, e, ct);
            if (rows.Any(r => r.Dvh != null)) throw new InvalidOperationException("Existen DVH previos sin una linea base completa. Requiere investigacion, no reinicializacion.");
            tablas.Add((e, rows));
        }
        foreach (var (e, rows) in tablas)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var dvh = SerializadorCanonico.Dvh(e.Tabla, rows[i].Valores);
                await AlmacenIntegridad.GuardarDvhAsync(db, e, rows[i], dvh, ct);
                rows[i] = rows[i] with { Dvh = dvh };
            }
            await AlmacenIntegridad.GuardarDvvAsync(db, e, rows, true, ct);
        }
        var errores = await AlmacenIntegridad.VerificarAsync(db, RegistroEntidadesProtegidas.Todas, ct);
        if (errores.Count != 0) throw new IntegridadException(errores);
        await tx.CommitAsync(ct);
    }

    private async Task VerificarBackupAsync(string archivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(archivo)) throw new InvalidOperationException("Se requiere backup previo verificado.");
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandTimeout = 1800;
            var p = cmd.CreateParameter(); p.ParameterName = "@archivo"; p.Value = archivo; cmd.Parameters.Add(p);
            cmd.CommandText = "RESTORE HEADERONLY FROM DISK=@archivo";
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                if (!await reader.ReadAsync(ct) || Convert.ToInt32(reader["BackupType"]) != 1 ||
                    !Convert.ToBoolean(reader["HasBackupChecksums"]) ||
                    !string.Equals(Convert.ToString(reader["DatabaseName"]), db.Database.GetDbConnection().Database, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("El backup debe ser FULL con CHECKSUM de esta base.");
                if (await reader.ReadAsync(ct)) throw new InvalidOperationException("Se requiere un archivo con un unico backup.");
            }
            cmd.CommandText = "RESTORE VERIFYONLY FROM DISK=@archivo WITH CHECKSUM, STOP_ON_ERROR";
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch { throw new InvalidOperationException("No se pudo verificar el backup previo de esta base. Revise archivo y permisos SQL."); }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
