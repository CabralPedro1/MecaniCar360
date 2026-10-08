using System.ComponentModel.DataAnnotations;
using MecaniCar360.Models;

namespace MecaniCar360.Models.ViewModels;

public sealed class VehiculoPropioNuevoViewModel
{
    [Required, MaxLength(10)] public string Patente { get; set; } = "";
    [MaxLength(17)] public string? Vin { get; set; }
    [Range(1, int.MaxValue)] public int ModeloId { get; set; }
    [Range(1900, 2100)] public int Anio { get; set; } = DateTime.Today.Year;
    [MaxLength(50)] public string? Color { get; set; }
    [Range(0, int.MaxValue)] public int? Kilometraje { get; set; }
}
