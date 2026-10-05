using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models.ViewModels;

public sealed class AuditoriaConsultaViewModel
{
    public DateTime Fecha { get; set; }
    public string? Actor { get; set; }
    public string Accion { get; set; } = "";
    public string Entidad { get; set; } = "";
    public int? EntidadId { get; set; }
    public string? Descripcion { get; set; }
}

public sealed class CalificacionViewModel
{
    [Range(1, int.MaxValue)] public int OrdenTrabajoId { get; set; }
    [Range(1, 5)] public int Puntuacion { get; set; } = 5;
    [StringLength(1000)] public string? Comentario { get; set; }
}
