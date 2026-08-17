using MecaniCar360.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

public class ModeloViewModel
{
    public Modelo Modelo { get; set; } = new();

    public IEnumerable<SelectListItem> Marcas { get; set; } = Enumerable.Empty<SelectListItem>();
}