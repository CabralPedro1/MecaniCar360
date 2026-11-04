using System.Security.Cryptography;
using System.Text.Json;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public partial class StockService
{
    // JSON tipado conserva límites de campos, null y números independientes de la cultura.
    private static byte[] HuellaMovimiento(string tipo, int repuestoId, int cantidad,
        string? observaciones, int? proveedor = null, decimal? precio = null,
        int? orden = null, int? lote = null) => SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Version = 1, Tipo = tipo, RepuestoId = repuestoId, Cantidad = cantidad,
            Observaciones = observaciones?.Trim(), Proveedor = proveedor,
            Precio = precio.HasValue ? precio.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : null,
            Orden = orden, Lote = lote
        }));

    private static ServiceResult ResultadoRepetido(OperacionStockIdempotente registro,
        int usuarioId, string tipo, byte[] huella) =>
        registro.UsuarioId == usuarioId && registro.TipoOperacion == tipo &&
        registro.HuellaSolicitud.AsSpan().SequenceEqual(huella)
            ? ServiceResult.Ok(registro.MensajeResultado)
            : ServiceResult.Error("La clave de operación ya fue utilizada con otra solicitud. Inicie una operación nueva.");

    private Task<ServiceResult> EjecutarMovimientoAsync(Guid clave, int usuarioId, string tipo,
        byte[] huella, Func<OperacionStockIdempotente, Task<ServiceResult>> operacion)
    {
        if (clave == Guid.Empty)
            return Task.FromResult(ServiceResult.Error("Falta una clave de operación válida. Vuelva a abrir el formulario."));
        return EjecutarStockAsync(async () =>
        {
            // El interceptor ya adquirió el bloqueo global de integridad. Esta clave
            // se bloquea antes de OT/inventario, incluso cuando aún no tiene una fila.
            if (!await UsuarioAutorizadoAsync(usuarioId, "STOCK_MOVIMIENTO"))
                return ServiceResult.Error("Acceso denegado.");
            var anterior = await _context.OperacionesStockIdempotentes.FromSqlInterpolated(
                $"SELECT * FROM [OperacionesStockIdempotentes] WITH (UPDLOCK, HOLDLOCK) WHERE [Clave] = {clave}")
                .AsNoTracking().SingleOrDefaultAsync();
            if (anterior != null) return ResultadoRepetido(anterior, usuarioId, tipo, huella);
            var registro = new OperacionStockIdempotente
            {
                Clave = clave, UsuarioId = usuarioId, TipoOperacion = tipo, HuellaSolicitud = huella
            };
            var resultado = await operacion(registro);
            if (!resultado.Exitoso) return resultado;
            registro.MensajeResultado = resultado.Mensaje;
            registro.FechaConfirmacionUtc = DateTime.UtcNow;
            _context.OperacionesStockIdempotentes.Add(registro);
            return resultado;
        });
    }
}
