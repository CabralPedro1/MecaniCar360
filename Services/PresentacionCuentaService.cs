using MecaniCar360.Data;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed record PresentacionCuenta(string Nombre, bool PasswordLocal, bool PuedeVincularGoogle,
    bool PuedeAgregarPassword = false, bool PuedeGestionarPassword = false);
public sealed class PresentacionCuentaService(MecaniCarContext db, IdentidadClienteService identidad, ClienteHabilitadoService habilitado)
{
    public async Task<PresentacionCuenta> ObtenerAsync(int actor)
    {
        var u = await db.Usuarios.AsNoTracking().Where(u => u.Id == actor && u.Activo && u.Persona.Activo)
            .Select(u => new { u.Persona.Nombre, u.Persona.Apellido, u.PasswordHash, u.PersonaId,
                Google = u.IdentidadesExternas.Any(i => i.Proveedor == Models.Enums.ProveedorIdentidadExterna.Google) }).SingleOrDefaultAsync();
        if (u == null) return new("Cuenta", false, false);
        var nombre = $"{u.Nombre} {u.Apellido}".Trim();
        var password = ClienteHabilitadoService.CredencialEstablecida(u.PasswordHash);
        var cliente = await identidad.ExclusivamenteClienteAsync(u.PersonaId) && await habilitado.EstaHabilitadoAsync(u.PersonaId);
        return new(nombre.Length == 0 ? "Mi cuenta" : nombre, password,
            password && !u.Google && cliente, u.PasswordHash == null && u.Google && cliente, cliente);
    }
}
