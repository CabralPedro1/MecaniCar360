using System.ComponentModel.DataAnnotations;
using MecaniCar360.Models;
using MecaniCar360.Models.ViewModels;

namespace MecaniCar360.Helpers;

public static class RegistroCompletoCliente
{
    public static bool DatosValidos(DatosRegistroClienteViewModel datos) =>
        Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), true)
        && DniPersona.Normalizar(datos.Dni) != null;

    public static bool PersonaCompleta(Persona p) => DatosValidos(new DatosRegistroClienteViewModel {
        Nombre = p.Nombre ?? "", Apellido = p.Apellido ?? "", Dni = p.Dni ?? "", Telefono = p.Telefono ?? "" });

    public static void Aplicar(Persona p, DatosRegistroClienteViewModel datos)
    {
        p.Nombre = datos.Nombre.Trim(); p.Apellido = datos.Apellido.Trim();
        p.Dni = DniPersona.Normalizar(datos.Dni); p.Telefono = datos.Telefono.Trim();
    }
}
