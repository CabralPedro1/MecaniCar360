using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MecaniCar360.Models.ViewModels;

public class DatosRegistroClienteViewModel
{
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Required, StringLength(100)] public string Apellido { get; set; } = "";
    [Required, StringLength(30)] public string Dni { get; set; } = "";
    [Required, RegularExpression(@"^[0-9]{6,15}$")] public string Telefono { get; set; } = "";
    [BindNever, ValidateNever] public string Email { get; set; } = "";
}

public sealed class RegistroCompletoClienteViewModel : DatosRegistroClienteViewModel
{
}

public sealed class ContactoClienteViewModel
{
    [Required, RegularExpression(@"^[0-9]{6,15}$")] public string Telefono { get; set; } = "";
    [BindNever, ValidateNever] public string Nombre { get; set; } = "";
    [BindNever, ValidateNever] public string Apellido { get; set; } = "";
    [BindNever, ValidateNever] public string Dni { get; set; } = "";
    [BindNever, ValidateNever] public string Email { get; set; } = "";
}
