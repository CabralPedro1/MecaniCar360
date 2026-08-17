using MecaniCar360.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

public class VehiculoViewModel
{
    public int PersonaId { get; set; }

    public Vehiculo Vehiculo { get; set; } = new();

    public IEnumerable<SelectListItem> Marcas { get; set; } = [];

    public IEnumerable<SelectListItem> Modelos { get; set; } = [];
}