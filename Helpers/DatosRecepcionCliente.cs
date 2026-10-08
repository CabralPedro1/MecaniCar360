using MecaniCar360.Models;

namespace MecaniCar360.Helpers;

public static class DatosRecepcionCliente
{
    public static IReadOnlyList<string> Faltantes(Persona? persona)
    {
        var campos = new List<string>();
        if (string.IsNullOrWhiteSpace(persona?.Nombre)) campos.Add("Nombre");
        if (string.IsNullOrWhiteSpace(persona?.Apellido)) campos.Add("Apellido");
        if (string.IsNullOrWhiteSpace(persona?.Dni)) campos.Add("DNI");
        return campos;
    }
}
