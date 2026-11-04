using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace MecaniCar360.Helpers;

public static class EmisionCorreoCuenta
{
    public const string PoliticaHttp = "CorreosCuentaPorIp";
    public static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(60);

    // Independiente de consumo/invalidación: también cuenta un intento con fallo SMTP.
    public static bool PuedeReemitir(DateTime? ultima, DateTime ahora) =>
        !ultima.HasValue || ahora - ultima.Value >= Intervalo;

    public static void ConfigurarLimiteHttp(RateLimiterOptions options) =>
        options.AddPolicy(PoliticaHttp, context =>
        {
            var ip = context.Connection.RemoteIpAddress;
            if (ip?.IsIPv4MappedToIPv6 == true) ip = ip.MapToIPv4();
            return RateLimitPartition.GetFixedWindowLimiter(ip?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5, Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0, AutoReplenishment = true
                });
        });
}

// Sólo orientación, que no tiene fila persistente. Una instancia por proceso;
// no reemplaza los intervalos persistentes de enlaces e invitaciones.
public sealed class LimiteOrientacionCorreo
{
    private readonly object _mutex = new();
    private readonly Dictionary<int, DateTime> _intentos = new();
    private const int Capacidad = 10000;

    public bool IntentarReservar(int usuarioId, DateTime ahora)
    {
        if (usuarioId <= 0) return false;
        lock (_mutex)
        {
            if (_intentos.TryGetValue(usuarioId, out var ultima) &&
                !EmisionCorreoCuenta.PuedeReemitir(ultima, ahora)) return false;
            if (_intentos.Count >= Capacidad)
            {
                foreach (var id in _intentos.Where(p => EmisionCorreoCuenta.PuedeReemitir(p.Value, ahora))
                    .Select(p => p.Key).ToArray()) _intentos.Remove(id);
                if (_intentos.Count >= Capacidad) return false;
            }
            _intentos[usuarioId] = ahora;
            return true;
        }
    }
}
