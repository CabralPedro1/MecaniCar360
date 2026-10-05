using System.ComponentModel.DataAnnotations;
namespace MecaniCar360.Models.ViewModels;

public sealed class CargarEvidenciaViewModel
{
    public int OrdenTrabajoId { get; set; }
    [Required, StringLength(500)] public string Descripcion { get; set; } = "";
    [Required] public IFormFile? Archivo { get; set; }
}
public sealed record EvidenciaResumen(int Id, DateTime Fecha, string Descripcion, string Tipo);
public sealed record EvidenciasViewModel(int OrdenTrabajoId, bool PuedeCargar, List<EvidenciaResumen> Evidencias);
public sealed record ArchivoEvidencia(Stream Contenido, string Mime, string Nombre);
