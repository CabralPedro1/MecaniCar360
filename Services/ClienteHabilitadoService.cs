using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
namespace MecaniCar360.Services;

public sealed class ClienteHabilitadoService(MecaniCarContext context)
{
    internal static bool CredencialEstablecida(string? hash) => hash != null &&
        Regex.IsMatch(hash, @"^\$2[aby]\$(0[4-9]|[12][0-9]|3[01])\$[./A-Za-z0-9]{53}$");

    public async Task<bool> EstaHabilitadoAsync(int personaId)
    {
        var usuario = await context.Usuarios.AsNoTracking()
            .Where(u => u.PersonaId == personaId && u.Activo && u.Persona.Activo &&
                u.Persona.Roles.Any(pr => pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.CLIENTE))
            .Select(u => new { u.PasswordHash, Externa = u.IdentidadesExternas.Any(i =>
                i.Proveedor == ProveedorIdentidadExterna.Google && i.IdentificadorExterno.Trim() != "") })
            .SingleOrDefaultAsync();
        return usuario != null && (CredencialEstablecida(usuario.PasswordHash) || usuario.Externa);
    }
}
