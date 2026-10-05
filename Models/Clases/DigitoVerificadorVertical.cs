namespace MecaniCar360.Models;

public sealed class DigitoVerificadorVertical
{
    public string NombreEntidad { get; set; } = "";
    public int VersionAlgoritmo { get; set; }
    public string Valor { get; set; } = "";
    public long CantidadRegistros { get; set; }
    public DateTime FechaActualizacionUtc { get; set; }
}
