using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class PersonaService
    {
        private readonly MecaniCarContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly PermisoService _permisoService;

        public PersonaService(
            MecaniCarContext context,
            PermisoService permisoService, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _permisoService = permisoService;
        }


        // =============================
        // CONSULTAS
        // =============================

        public async Task<ServiceResult<List<Persona>>> ObtenerTodasAsync(
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_VER"))
            {
                return ServiceResult<List<Persona>>.Error(
                    "No posee permisos para consultar personas.");
            }

            var personas = await _context.Personas
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return ServiceResult<List<Persona>>.Ok(personas);
        }


        public async Task<ServiceResult<List<Persona>>> ObtenerActivasAsync(int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "PERSONA_VER")) return ServiceResult<List<Persona>>.Error("Acceso denegado.");

            var personas = await _context.Personas
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .Where(p => p.Activo)
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return ServiceResult<List<Persona>>.Ok(personas);
        }


        public async Task<ServiceResult<Persona>> ObtenerPorIdAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_VER"))
            {
                return ServiceResult<Persona>.Error(
                    "No posee permisos para consultar personas.");
            }

            var persona = await ObtenerPersonaCompletaAsync(id);

            if (persona == null)
                return ServiceResult<Persona>.Error(
                    "Persona no encontrada.");

            return ServiceResult<Persona>.Ok(persona);
        }

        public async Task<ServiceResult<Persona>> ObtenerPorIdParaEditarAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_MODIFICAR"))
            {
                return ServiceResult<Persona>.Error(
                    "No posee permisos para modificar personas.");
            }

            var persona = await ObtenerPersonaCompletaAsync(id);

            if (persona == null)
                return ServiceResult<Persona>.Error(
                    "Persona no encontrada.");

            return ServiceResult<Persona>.Ok(persona);
        }


        public async Task<ServiceResult<Persona>> ObtenerPorDniAsync(
            string dni, int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "PERSONA_VER")) return ServiceResult<Persona>.Error("Acceso denegado.");

            var persona = await ObtenerPersonaPorDniAsync(dni);

            if (persona == null)
                return ServiceResult<Persona>.Error(
                    "Persona no encontrada.");

            return ServiceResult<Persona>.Ok(persona);
        }


        public async Task<ServiceResult<List<Persona>>> ObtenerPorRolAsync(
            string nombreRol, int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "PERSONA_VER")) return ServiceResult<List<Persona>>.Error("Acceso denegado.");

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


        public Task<ServiceResult<List<Persona>>> ObtenerMecanicosAsync(int usuarioSolicitanteId)
            => ObtenerPorRolAsync(RolesSistema.MECANICO, usuarioSolicitanteId);


        public Task<ServiceResult<List<Persona>>> ObtenerClientesAsync(int usuarioSolicitanteId)
            => ObtenerPorRolAsync(RolesSistema.CLIENTE, usuarioSolicitanteId);


        public Task<ServiceResult<List<Persona>>> ObtenerAdministrativosAsync(int usuarioSolicitanteId)
            => ObtenerPorRolAsync(RolesSistema.ADMIN, usuarioSolicitanteId);


        // =============================
        // ABM
        // =============================

        public async Task<ServiceResult> CrearAsync(
            Persona persona,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_CREAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para crear personas.");
            }

            if (await ExisteDniAsync(persona.Dni))
                return ServiceResult.Error(
                    "Ya existe una persona con ese DNI.");

            persona.Dni = persona.Dni.Trim();

            persona.Id = 0;
            persona.Usuario = null;
            persona.Roles = new();
            _context.Personas.Add(persona);

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Persona creada correctamente.");
        }


        public async Task<ServiceResult> EditarAsync(
            Persona persona,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.EsAdministradorAsync(usuarioSolicitanteId) &&
                await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == persona.Id && pr.FechaBaja == null && pr.Rol.Nombre == RolesSistema.ADMIN))
                return ServiceResult.Error("Sólo ADMIN puede administrar esta persona.");

            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar personas.");
            }

            var existente = await ObtenerPersonaAsync(persona.Id);

            if (existente == null)
                return ServiceResult.Error(
                    "Persona no encontrada.");

            if (await ExisteDniAsync(
                persona.Dni,
                persona.Id))
            {
                return ServiceResult.Error(
                    "Ya existe una persona con ese DNI.");
            }

            existente.Nombre = persona.Nombre;
            existente.Apellido = persona.Apellido;
            existente.Dni = persona.Dni.Trim();
            existente.Telefono = persona.Telefono;
            existente.Email = persona.Email;

            // El estado se administra mediante
            // ActivarAsync / DesactivarAsync.
            // No lo modificamos desde la edición general.

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Persona actualizada correctamente.");
        }


        public async Task<ServiceResult> ActivarAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.EsAdministradorAsync(usuarioSolicitanteId) &&
                await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == id && pr.FechaBaja == null && pr.Rol.Nombre == RolesSistema.ADMIN))
                return ServiceResult.Error("Sólo ADMIN puede administrar esta persona.");

            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_DESACTIVAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar el estado de una persona.");
            }

            var persona = await ObtenerPersonaAsync(id);

            if (persona == null)
                return ServiceResult.Error(
                    "Persona no encontrada.");

            if (persona.Activo)
                return ServiceResult.Error(
                    "La persona ya se encuentra activa.");

            persona.Activo = true;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Persona activada correctamente.");
        }


        public async Task<ServiceResult> DesactivarAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.EsAdministradorAsync(usuarioSolicitanteId) &&
                await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == id && pr.FechaBaja == null && pr.Rol.Nombre == RolesSistema.ADMIN))
                return ServiceResult.Error("Sólo ADMIN puede administrar esta persona.");

            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_DESACTIVAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar el estado de una persona.");
            }

            var persona = await ObtenerPersonaAsync(id);

            if (persona == null)
                return ServiceResult.Error(
                    "Persona no encontrada.");

            if (!persona.Activo)
                return ServiceResult.Error(
                    "La persona ya se encuentra desactivada.");

            var esAdministrador = await _permisoService
                .EsAdministradorEfectivoPersonaAsync(id);

            if (esAdministrador)
            {
                if (!await _permisoService.EsAdministradorAsync(
                    usuarioSolicitanteId))
                {
                    return ServiceResult.Error(
                        "Sólo un administrador puede desactivar a otro administrador.");
                }

                var esSolicitante = await _context.Usuarios
                    .AnyAsync(u =>
                        u.Id == usuarioSolicitanteId &&
                        u.PersonaId == id);

                if (esSolicitante)
                {
                    return ServiceResult.Error(
                        "No puede desactivarse a sí mismo como administrador.");
                }

                if (await _permisoService
                    .ContarAdministradoresEfectivosAsync() <= 1)
                {
                    return ServiceResult.Error(
                        "No se puede desactivar al último administrador efectivo.");
                }
            }

            persona.Activo = false;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Persona desactivada correctamente.");
        }


        // =============================
        // ROLES
        // =============================

        public async Task<bool> TieneRolAsync(
            int personaId,
            string nombreRol, int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "ROL_VER")) return false;

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
            if (!await _permisoService.TienePermisoAsync(
                usuarioOtorgaId,
                "ROL_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para asignar roles.");
            }

            var persona = await _context.Personas
                .FirstOrDefaultAsync(p =>
                    p.Id == personaId);

            if (persona == null)
                return ServiceResult.Error(
                    "Persona no encontrada.");

            var rol = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.Id == rolId &&
                    r.Activo);

            if (rol == null)
                return ServiceResult.Error(
                    "Rol no encontrado o inactivo.");

            if (rol.Nombre.Equals(
                RolesSistema.ADMIN,
                StringComparison.OrdinalIgnoreCase) &&
                !await _permisoService.EsAdministradorAsync(
                    usuarioOtorgaId))
            {
                return ServiceResult.Error(
                    "Sólo un administrador puede asignar el rol ADMIN.");
            }

            bool yaExiste = await _context.PersonaRoles.AnyAsync(pr =>
                pr.PersonaId == personaId &&
                pr.RolId == rolId &&
                pr.FechaBaja == null);

            if (yaExiste)
                return ServiceResult.Error(
                    "La persona ya posee ese rol.");

            _context.PersonaRoles.Add(
                new PersonaRol
                {
                    PersonaId = personaId,
                    RolId = rolId,
                    FechaAlta = DateTime.Now,
                    OtorgadoPorUsuarioId = usuarioOtorgaId
                });

            _auditoria.RegistrarOperacion("ROL_ASIGNADO", "Persona", personaId, usuarioOtorgaId, $"Rol #{rolId}.");
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Rol asignado correctamente.");
        }


        public async Task<ServiceResult> QuitarRolAsync(
            int personaId,
            int rolId,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para quitar roles.");
            }

            var relacion = await _context.PersonaRoles
                .Include(pr => pr.Rol)
                .FirstOrDefaultAsync(pr =>
                    pr.PersonaId == personaId &&
                    pr.RolId == rolId &&
                    pr.FechaBaja == null);

            if (relacion == null)
                return ServiceResult.Error(
                    "La persona no posee ese rol.");

            if (relacion.Rol.Nombre.Equals(
                RolesSistema.ADMIN,
                StringComparison.OrdinalIgnoreCase))
            {
                if (!await _permisoService.EsAdministradorAsync(
                    usuarioSolicitanteId))
                {
                    return ServiceResult.Error(
                        "Sólo un administrador puede quitar el rol ADMIN.");
                }

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.Id == usuarioSolicitanteId);

                if (usuario?.PersonaId == personaId)
                {
                    return ServiceResult.Error(
                        "No puede quitarse su propia asignación ADMIN.");
                }

                if (await _permisoService
                    .EsAdministradorEfectivoPersonaAsync(personaId) &&
                    await _permisoService
                        .ContarAdministradoresEfectivosAsync() <= 1)
                {
                    return ServiceResult.Error(
                        "No se puede quitar la última asignación ADMIN activa.");
                }
            }

            relacion.FechaBaja = DateTime.Now;

            _auditoria.RegistrarOperacion("ROL_QUITADO", "Persona", personaId, usuarioSolicitanteId, $"Rol #{rolId}.");
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Rol removido correctamente.");
        }


        public async Task<ServiceResult<List<Rol>>>
            ObtenerRolesDisponiblesAsync(
                int personaId,
                int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_VER"))
            {
                return ServiceResult<List<Rol>>.Error(
                    "No posee permisos para consultar roles.");
            }

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


        public async Task<ServiceResult<List<Rol>>>
            ObtenerRolesPersonaAsync(
                int personaId,
                int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_VER"))
            {
                return ServiceResult<List<Rol>>.Error(
                    "No posee permisos para consultar roles.");
            }

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

        private async Task<Persona?> ObtenerPersonaAsync(
            int id)
        {
            return await _context.Personas
                .FirstOrDefaultAsync(p =>
                    p.Id == id);
        }


        private async Task<Persona?> ObtenerPersonaCompletaAsync(
            int id)
        {
            return await _context.Personas
                .Include(p => p.Usuario)
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .FirstOrDefaultAsync(p =>
                    p.Id == id);
        }


        private async Task<Persona?> ObtenerPersonaPorDniAsync(
            string dni)
        {
            dni = dni.Trim();

            return await _context.Personas
                .Include(p => p.Usuario)
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .FirstOrDefaultAsync(p =>
                    p.Dni == dni);
        }


        private async Task<bool> ExisteDniAsync(
            string dni,
            int? excluirId = null)
        {
            dni = dni.Trim();

            return await _context.Personas.AnyAsync(p =>
                p.Dni == dni &&
                (!excluirId.HasValue ||
                 p.Id != excluirId.Value));
        }
    }
}
