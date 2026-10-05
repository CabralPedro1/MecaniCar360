using System.Text.Json;
using MecaniCar360.Models.DTOs;

namespace MecaniCar360.Services;

public sealed class EstadoIntegridad
{
    private readonly object _sync = new();
    private readonly string _directorio;
    private ResultadoIntegridad _resultado = new(CondicionIntegridad.NoVerificada, DateTime.UtcNow, 1, null, Array.Empty<ErrorIntegridad>());
    public EstadoIntegridad(string directorio, string webRoot)
    {
        _directorio = Path.GetFullPath(directorio);
        var publico = Path.GetFullPath(webRoot).TrimEnd(Path.DirectorySeparatorChar);
        if (_directorio.Equals(publico, StringComparison.OrdinalIgnoreCase) || _directorio.StartsWith(publico + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El registro de integridad debe ser privado.");
    }
    public ResultadoIntegridad Resultado { get { lock (_sync) return _resultado; } }
    public void ConfirmarValida()
    {
        lock (_sync)
            if (_resultado.Estado != CondicionIntegridad.Comprometida)
                _resultado = new(CondicionIntegridad.Valida, DateTime.UtcNow, 1, null, Array.Empty<ErrorIntegridad>());
    }
    public ResultadoIntegridad Comprometer(IReadOnlyList<ErrorIntegridad> errores)
    {
        lock (_sync)
        {
            if (_resultado.Estado == CondicionIntegridad.Comprometida) return _resultado;
            _resultado = new(CondicionIntegridad.Comprometida, DateTime.UtcNow, 1, Guid.NewGuid().ToString("N"), errores.ToArray());
            try
            {
                for (DirectoryInfo? d = new(_directorio); d != null; d = d.Parent)
                    if (d.Exists && d.Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new IOException();
                Directory.CreateDirectory(_directorio);
                // Unique file, no overwrite or followed file link. No expected/found hashes or business values.
                using var file = new FileStream(Path.Combine(_directorio, _resultado.IncidenteId + ".json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(file, _resultado);
            }
            catch
            {
                Console.Error.WriteLine("No se pudo persistir el incidente de integridad " + _resultado.IncidenteId + ". El sistema permanece bloqueado.");
            }
            return _resultado;
        }
    }
}
