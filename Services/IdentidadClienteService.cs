using MecaniCar360.Data;
using MecaniCar360.Models;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

// CLIENTE es elegibilidad de dominio, no sustitución de las patentes funcionales.
public sealed class IdentidadClienteService(MecaniCarContext db)
{
    public Task<bool> ExclusivamenteClienteAsync(int personaId) => db.Personas.AnyAsync(p => p.Id == personaId && p.Activo &&
        p.Roles.Any(r => r.FechaBaja == null && r.Rol.Activo && r.Rol.Nombre == RolesSistema.CLIENTE) &&
        !p.Roles.Any(r => r.FechaBaja == null && r.Rol.Nombre != RolesSistema.CLIENTE));

    public Task BloquearEmailAsync(string email)
    {
        // Un mutex por email normalizado, sin datos personales en el nombre del lock.
        var recurso = "MecaniCar360.Onboarding." + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(email.Trim().ToUpperInvariant())));
        return db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource={recurso}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 51001, 'Operacion ocupada.', 1;");
    }
}
