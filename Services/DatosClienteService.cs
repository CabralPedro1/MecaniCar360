using MecaniCar360.Data;
using MecaniCar360.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class DatosClienteService(MecaniCarContext db)
{
    public async Task<IReadOnlyList<string>> PendientesRecepcionAsync(int actor) => DatosRecepcionCliente.Faltantes(
        await db.Usuarios.AsNoTracking().Where(u => u.Id == actor && u.Activo && u.Persona.Activo)
            .Select(u => u.Persona).SingleOrDefaultAsync());
}
