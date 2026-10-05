using System.Data.Common;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MecaniCar360.Data.Integridad;

internal sealed record FilaIntegridad(object?[] Valores, object?[] Claves, string? Dvh)
{
    public string Clave => string.Join(",", Claves.Select(x => Convert.ToString(x, System.Globalization.CultureInfo.InvariantCulture)));
}
internal static class AlmacenIntegridad
{
    internal static DbCommand Comando(MecaniCarContext db, string sql, params (string Nombre, object? Valor)[] parametros)
    {
        var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction() ?? throw new InvalidOperationException("Integridad requiere transaccion.");
        cmd.CommandText = sql; cmd.CommandTimeout = 30;
        foreach (var (nombre, valor) in parametros) { var p = cmd.CreateParameter(); p.ParameterName = nombre; p.Value = valor ?? DBNull.Value; cmd.Parameters.Add(p); }
        return cmd;
    }
    internal static async Task<List<FilaIntegridad>> LeerAsync(MecaniCarContext db, EntidadProtegida entidad, CancellationToken ct)
    {
        // TABLOCK + HOLDLOCK protects existing rows AND insertions, even from non-cooperating SQL sessions.
        await using var cmd = Comando(db, $"SELECT {string.Join(",", entidad.Campos.Select(c => "[" + c + "]"))}, [DVH] FROM [dbo].[{entidad.Tabla}] WITH (TABLOCK,HOLDLOCK) ORDER BY {string.Join(",", entidad.Claves.Select(c => "[" + c + "]"))}");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rows = new List<FilaIntegridad>();
        while (await reader.ReadAsync(ct))
        {
            var values = new object?[entidad.Campos.Length];
            for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(new(values, entidad.Claves.Select(k => values[Array.IndexOf(entidad.Campos, k)]).ToArray(),
                reader.IsDBNull(values.Length) ? null : reader.GetString(values.Length)));
        }
        return rows;
    }
    internal static async Task<List<ErrorIntegridad>> VerificarAsync(MecaniCarContext db, IEnumerable<EntidadProtegida> entidades, CancellationToken ct)
    {
        var errors = new List<ErrorIntegridad>();
        var verticales = new Dictionary<string, (int Version, string Valor, long Cantidad)>(StringComparer.Ordinal);
        await using (var cmd = Comando(db, "SELECT NombreEntidad,VersionAlgoritmo,Valor,CantidadRegistros FROM dbo.DigitosVerificadoresVerticales WITH (TABLOCK,HOLDLOCK)"))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) verticales.Add(reader.GetString(0), (reader.GetInt32(1), reader.GetString(2), reader.GetInt64(3)));
        foreach (var extra in verticales.Keys.Except(RegistroEntidadesProtegidas.Todas.Select(e => e.Tabla)))
            errors.Add(new("DigitosVerificadoresVerticales", null, "VERSION_INCOMPATIBLE"));
        foreach (var e in RegistroEntidadesProtegidas.Todas)
            if (!verticales.ContainsKey(e.Tabla)) errors.Add(new(e.Tabla, null, "DVV_FALTANTE"));
        foreach (var e in entidades.OrderBy(x => x.Tabla, StringComparer.Ordinal))
        {
            var rows = await LeerAsync(db, e, ct);
            foreach (var row in rows)
            {
                var esperado = SerializadorCanonico.Dvh(e.Tabla, row.Valores);
                if (row.Dvh != esperado) errors.Add(new(e.Tabla, row.Clave, row.Dvh == null ? "DVH_FALTANTE" : "DVH_INVALIDO"));
            }
            if (!verticales.TryGetValue(e.Tabla, out var v)) continue;
            if (v.Version != RegistroEntidadesProtegidas.Version) errors.Add(new(e.Tabla, null, "VERSION_INCOMPATIBLE"));
            var dvv = SerializadorCanonico.Dvv(e.Tabla, rows.Count, rows.Select(r => (r.Claves, r.Dvh)));
            if (v.Valor != dvv || v.Cantidad != rows.Count) errors.Add(new(e.Tabla, null, "DVV_INVALIDO"));
        }
        return errors;
    }
    internal static async Task GuardarDvhAsync(MecaniCarContext db, EntidadProtegida e, FilaIntegridad row, string dvh, CancellationToken ct)
    {
        var parameters = row.Claves.Select((v, i) => ("@k" + i, v)).Append(("@dvh", (object?)dvh)).ToArray();
        await using var cmd = Comando(db, $"UPDATE dbo.[{e.Tabla}] SET DVH=@dvh WHERE {string.Join(" AND ", e.Claves.Select((k, i) => "[" + k + "]=@k" + i))}", parameters);
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new InvalidOperationException("No se pudo mantener el verificador de la fila.");
    }
    internal static async Task GuardarDvvAsync(MecaniCarContext db, EntidadProtegida e, List<FilaIntegridad> rows, bool inicial, CancellationToken ct)
    {
        var valor = SerializadorCanonico.Dvv(e.Tabla, rows.Count, rows.Select(r => (r.Claves, r.Dvh)));
        var sql = inicial
            ? "INSERT dbo.DigitosVerificadoresVerticales (NombreEntidad,VersionAlgoritmo,Valor,CantidadRegistros,FechaActualizacionUtc) VALUES (@n,1,@v,@c,@f)"
            : "UPDATE dbo.DigitosVerificadoresVerticales SET Valor=@v,CantidadRegistros=@c,FechaActualizacionUtc=@f WHERE NombreEntidad=@n AND VersionAlgoritmo=1";
        await using var cmd = Comando(db, sql, ("@n", e.Tabla), ("@v", valor), ("@c", (long)rows.Count), ("@f", DateTime.UtcNow));
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new InvalidOperationException("No se pudo mantener el verificador vertical.");
    }
}
