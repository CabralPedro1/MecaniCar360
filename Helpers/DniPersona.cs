using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MecaniCar360.Data;

namespace MecaniCar360.Helpers;

public static class DniPersona
{
    public const string Error = "No se puede utilizar ese DNI. Revise el dato o solicite asistencia al taller.";
    // DNI numérico: separadores de presentación admitidos, sin ceros iniciales.
    public static string? Normalizar(string? valor)
    {
        if (valor == null) return null;
        var n = valor.Replace(".", "").Replace("-", "").Replace(" ", "").Trim();
        if (n.Length is < 1 or > 15 || n.Any(c => c < '0' || c > '9')) return null;
        n = n.TrimStart('0');
        return n.Length == 0 ? "0" : n;
    }
    public static async Task<bool> ExisteAsync(MecaniCarContext db, string? dni, int? excluir = null)
    {
        var normal = Normalizar(dni);
        if (normal == null) return true;
        // Incluye formatos históricos, sin modificar filas ni exponer datos personales.
        var valores = await db.Personas.AsNoTracking().Where(p => p.Id != excluir && p.Dni != null)
            .Select(p => p.Dni).ToListAsync();
        return valores.Any(v => Normalizar(v) == normal);
    }
    public static bool Conflicto(Exception ex)
    {
        if (ex is DbUpdateConcurrencyException) return true;
        for (Exception? e = ex; e != null; e = e.InnerException)
            if (e is SqlException { Number: 2601 or 2627 or 1205 }) return true;
        return false;
    }
}
