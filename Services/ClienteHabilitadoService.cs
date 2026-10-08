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
        var usuario = await context.Usuarios.AsNoTracking().Include(u => u.Persona)
            .Where(u => u.PersonaId == personaId && u.Activo && u.Persona.Activo &&
                u.Persona.Roles.Any(pr => pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.CLIENTE))
            .SingleOrDefaultAsync();
        return usuario != null && !usuario.PrimerLogin && (CredencialEstablecida(usuario.PasswordHash) ||
            await context.IdentidadesExternas.AnyAsync(i => i.UsuarioId == usuario.Id &&
                i.Proveedor == ProveedorIdentidadExterna.Google && i.IdentificadorExterno.Trim() != ""))
            && Helpers.RegistroCompletoCliente.PersonaCompleta(usuario.Persona)
            && await CorreoVerificadoAsync(usuario);
    }

    public async Task<bool> CorreoVerificadoAsync(Usuario usuario) => await context.IdentidadesExternas.AnyAsync(i =>
        i.UsuarioId == usuario.Id && i.Proveedor == ProveedorIdentidadExterna.Google && i.IdentificadorExterno.Trim() != "")
        || await context.InvitacionesCliente.AnyAsync(i => i.PersonaId == usuario.PersonaId
            && i.FechaConsumida != null && i.EmailDestino == usuario.EmailLogin);
}
