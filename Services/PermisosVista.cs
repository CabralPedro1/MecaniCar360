namespace MecaniCar360.Services;

// Sólo presentación: nunca sustituye las verificaciones de acciones o servicios.
public sealed class PermisosVista(
    Func<int, Task<PermisosVisuales>> resolver, IHttpContextAccessor contexto)
{
    private readonly object clave = new();

    private Task<PermisosVisuales> ObtenerAsync(int usuarioId)
    {
        var http = contexto.HttpContext;
        if (http == null) return resolver(usuarioId);
        if (!http.Items.TryGetValue(clave, out var valor))
            http.Items[clave] = valor = new Dictionary<int, Task<PermisosVisuales>>();
        var cache = (Dictionary<int, Task<PermisosVisuales>>)valor!;
        lock (cache)
        {
            if (!cache.TryGetValue(usuarioId, out var tarea))
                cache[usuarioId] = tarea = resolver(usuarioId);
            return tarea;
        }
    }

    public async Task<bool> TienePermisoAsync(int usuarioId, string patente)
    {
        var permisos = await ObtenerAsync(usuarioId);
        return permisos.Administrador || permisos.Patentes.Contains(patente, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> TieneAlgunoAsync(int usuarioId, params string[] patentes)
    {
        foreach (var patente in patentes)
            if (await TienePermisoAsync(usuarioId, patente)) return true;
        return false;
    }
}

public sealed record PermisosVisuales(bool Administrador, IReadOnlyList<string> Patentes);
