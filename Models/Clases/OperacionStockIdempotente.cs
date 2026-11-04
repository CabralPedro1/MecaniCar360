namespace MecaniCar360.Models;

// Sólo resultados confirmados; se guarda en la misma transacción que el movimiento.
public sealed class OperacionStockIdempotente
{
    public Guid Clave { get; set; }
    public int UsuarioId { get; set; }
    public string TipoOperacion { get; set; } = string.Empty;
    public byte[] HuellaSolicitud { get; set; } = Array.Empty<byte>();
    public int MovimientoStockId { get; set; }
    public string MensajeResultado { get; set; } = string.Empty;
    public DateTime FechaConfirmacionUtc { get; set; }
}
