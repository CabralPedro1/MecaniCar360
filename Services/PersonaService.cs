using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class PersonaService
    {
        private readonly MecaniCarContext _context;

        public PersonaService(MecaniCarContext context)
        {
            _context = context;
        }

        // =============================
        // CONSULTAS
        // =============================

        public async Task<ServiceResult<List<Persona>>> ObtenerTodasAsync()
        {
            var personas = await _context.Personas
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return ServiceResult<List<Persona>>.Ok(personas);
        }

        public async Task<ServiceResult<List<Persona>>> ObtenerActivasAsync()
        {
            var personas = await _context.Personas
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .Where(p => p.Activo)
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return ServiceResult<List<Persona>>.Ok(personas);
        }

        public async Task<ServiceResult<Persona>> ObtenerPorIdAsync(int id)
        {
            var persona = await ObtenerPersonaCompletaAsync(id);

            if (persona == null)
                return ServiceResult<Persona>.Error("Persona no encontrada.");

            return ServiceResult<Persona>.Ok(persona);
        }

        public async Task<ServiceResult<Persona>> ObtenerPorDniAsync(string dni)
        {
            var persona = await ObtenerPersonaPorDniAsync(dni);

            if (persona == null)
                return ServiceResult<Persona>.Error("Persona no encontrada.");

            return ServiceResult<Persona>.Ok(persona);
        }

        public async Task<ServiceResult<List<Persona>>> ObtenerPorRolAsync(string nombreRol)
        {
            nombreRol = nombreRol.Trim().ToUpper();

            var personas = await _context.Personas
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .Where(p =>
                    p.Activo &&
                    p.Roles.Any(r =>
                        r.FechaBaja == null &&
                        r.Rol.Activo &&
                        r.Rol.Nombre == nombreRol))
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return ServiceResult<List<Persona>>.Ok(personas);
        }

        public Task<ServiceResult<List<Persona>>> ObtenerMecanicosAsync()
            => ObtenerPorRolAsync("MECANICO");

        public Task<ServiceResult<List<Persona>>> ObtenerClientesAsync()
            => ObtenerPorRolAsync("CLIENTE");

        public Task<ServiceResult<List<Persona>>> ObtenerAdministrativosAsync()
            => ObtenerPorRolAsync("ADMIN");


        // =============================
        // ABM
        // =============================


        public async Task<ServiceResult> CrearAsync(Persona persona)
        {
            if (await ExisteDniAsync(persona.Dni))
                return ServiceResult.Error("Ya existe una persona con ese DNI.");

            persona.Dni = persona.Dni.Trim();

            _context.Personas.Add(persona);

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Persona creada correctamente.");
        }

        public async Task<ServiceResult> EditarAsync(Persona persona)
        {
            var existente = await ObtenerPersonaAsync(persona.Id);

            if (existente == null)
                return ServiceResult.Error("Persona no encontrada.");

            if (await ExisteDniAsync(persona.Dni, persona.Id))
                return ServiceResult.Error("Ya existe una persona con ese DNI.");

            existente.Nombre = persona.Nombre;
            existente.Apellido = persona.Apellido;
            existente.Dni = persona.Dni.Trim();
            existente.Telefono = persona.Telefono;
            existente.Email = persona.Email;
            existente.Activo = persona.Activo;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Persona actualizada correctamente.");
        }

        public async Task<ServiceResult> ActivarAsync(int id)
        {
            var persona = await ObtenerPersonaAsync(id);

            if (persona == null)
                return ServiceResult.Error("Persona no encontrada.");

            if (persona.Activo)
                return ServiceResult.Error("La persona ya se encuentra activa.");

            persona.Activo = true;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Persona activada correctamente.");
        }

        public async Task<ServiceResult> DesactivarAsync(int id)
        {
            var persona = await ObtenerPersonaAsync(id);

            if (persona == null)
                return ServiceResult.Error("Persona no encontrada.");

            if (!persona.Activo)
                return ServiceResult.Error("La persona ya se encuentra desactivada.");

            persona.Activo = false;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Persona desactivada correctamente.");
        }
        // =============================
        // ROLES
        // =============================

        public async Task<bool> TieneRolAsync(int personaId, string nombreRol)
        {
            nombreRol = nombreRol.Trim().ToUpper();

            return await _context.PersonaRoles.AnyAsync(pr =>
                pr.PersonaId == personaId &&
                pr.FechaBaja == null &&
                pr.Rol.Activo &&
                pr.Rol.Nombre == nombreRol);
        }

        public async Task<ServiceResult> AsignarRolAsync(
            int personaId,
            int rolId,
            int usuarioOtorgaId)
        {
            var persona = await _context.Personas.FindAsync(personaId);

            if (persona == null)
                return ServiceResult.Error("Persona no encontrada.");

            var rol = await _context.Roles.FindAsync(rolId);

            if (rol == null)
                return ServiceResult.Error("Rol no encontrado.");

            bool yaExiste = await _context.PersonaRoles.AnyAsync(pr =>
                pr.PersonaId == personaId &&
                pr.RolId == rolId &&
                pr.FechaBaja == null);

            if (yaExiste)
                return ServiceResult.Error("La persona ya posee ese rol.");

            _context.PersonaRoles.Add(new PersonaRol
            {
                PersonaId = personaId,
                RolId = rolId,
                FechaAlta = DateTime.Now,
                OtorgadoPorUsuarioId = usuarioOtorgaId
            });

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Rol asignado correctamente.");
        }

        public async Task<ServiceResult> QuitarRolAsync(
            int personaId,
            int rolId)
        {
            var relacion = await _context.PersonaRoles
                .FirstOrDefaultAsync(pr =>
                    pr.PersonaId == personaId &&
                    pr.RolId == rolId &&
                    pr.FechaBaja == null);

            if (relacion == null)
                return ServiceResult.Error("La persona no posee ese rol.");

            relacion.FechaBaja = DateTime.Now;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Rol removido correctamente.");
        }

        public async Task<ServiceResult<List<Rol>>> ObtenerRolesDisponiblesAsync(int personaId)
        {
            var asignados = await _context.PersonaRoles
                .Where(pr =>
                    pr.PersonaId == personaId &&
                    pr.FechaBaja == null)
                .Select(pr => pr.RolId)
                .ToListAsync();

            var roles = await _context.Roles
                .Where(r =>
                    r.Activo &&
                    !asignados.Contains(r.Id))
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return ServiceResult<List<Rol>>.Ok(roles);
        }

        public async Task<ServiceResult<List<Rol>>> ObtenerRolesPersonaAsync(int personaId)
        {
            var roles = await _context.PersonaRoles
                .Where(pr =>
                    pr.PersonaId == personaId &&
                    pr.FechaBaja == null)
                .Select(pr => pr.Rol)
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return ServiceResult<List<Rol>>.Ok(roles);
        }

        // =============================
        // MÉTODOS PRIVADOS
        // =============================

        private async Task<Persona?> ObtenerPersonaAsync(int id)
        {
            return await _context.Personas
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        private async Task<Persona?> ObtenerPersonaCompletaAsync(int id)
        {
            return await _context.Personas
                .Include(p => p.Usuario)
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        private async Task<Persona?> ObtenerPersonaPorDniAsync(string dni)
        {
            dni = dni.Trim();

            return await _context.Personas
                .Include(p => p.Usuario)
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .FirstOrDefaultAsync(p => p.Dni == dni);
        }

        private async Task<bool> ExisteDniAsync(string dni, int? excluirId = null)
        {
            dni = dni.Trim();

            return await _context.Personas.AnyAsync(p =>
                p.Dni == dni &&
                (!excluirId.HasValue || p.Id != excluirId.Value));
        }


    }
}