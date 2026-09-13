using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class NotificacionService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisos;

        public NotificacionService(
            MecaniCarContext context, PermisoService permisos)
        {
            _context = context;
            _permisos = permisos;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<List<Notificacion>>>
            ObtenerPorPersonaAsync(
                int usuarioSolicitanteId)
        {
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue) return ServiceResult<List<Notificacion>>.Error("Usuario inactivo o no encontrado.");

            var personaExiste =
                await _context.Personas
                    .AnyAsync(p =>
                        p.Id == personaId &&
                        p.Activo);

            if (!personaExiste)
            {
                return ServiceResult<List<Notificacion>>
                    .Error(
                        "La persona no existe o está inactiva.");
            }

            var notificaciones =
                await _context.Notificaciones

                    .Where(n =>
                        n.PersonaId == personaId)

                    .OrderBy(n => n.Leida)
                    .ThenByDescending(n => n.Fecha)

                    .ToListAsync();

            return ServiceResult<List<Notificacion>>
                .Ok(notificaciones);
        }


        public async Task<ServiceResult<List<Notificacion>>>
            ObtenerNoLeidasAsync(
                int usuarioSolicitanteId)
        {
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue) return ServiceResult<List<Notificacion>>.Error("Usuario inactivo o no encontrado.");

            var notificaciones =
                await _context.Notificaciones

                    .Where(n =>
                        n.PersonaId == personaId &&
                        !n.Leida)

                    .OrderByDescending(n => n.Fecha)

                    .ToListAsync();

            return ServiceResult<List<Notificacion>>
                .Ok(notificaciones);
        }


        // =====================================
        // CREAR NOTIFICACIÓN
        // =====================================

        internal async Task<ServiceResult>
            NotificarAsync(
                int personaId,
                string titulo,
                string mensaje)
        {
            if (string.IsNullOrWhiteSpace(
                titulo))
            {
                return ServiceResult.Error(
                    "Debe ingresar un título.");
            }

            if (titulo.Trim().Length > 200)
            {
                return ServiceResult.Error(
                    "El título no puede superar los 200 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(
                mensaje))
            {
                return ServiceResult.Error(
                    "Debe ingresar un mensaje.");
            }

            if (mensaje.Trim().Length > 1000)
            {
                return ServiceResult.Error(
                    "El mensaje no puede superar los 1000 caracteres.");
            }

            var personaExiste =
                await _context.Personas
                    .AnyAsync(p =>
                        p.Id == personaId &&
                        p.Activo);

            if (!personaExiste)
            {
                return ServiceResult.Error(
                    "La persona no existe o está inactiva.");
            }

            var notificacion =
                new Notificacion
                {
                    PersonaId =
                        personaId,

                    Titulo =
                        titulo.Trim(),

                    Mensaje =
                        mensaje.Trim(),

                    Fecha =
                        DateTime.Now,

                    Leida =
                        false
                };

            _context.Notificaciones.Add(
                notificacion);

            await GuardarCambiosAsync();

            return ServiceResult.Ok(
                "Notificación creada correctamente.");
        }


        // =====================================
        // OPERACIONES
        // =====================================

        public async Task<ServiceResult>
            MarcarComoLeidaAsync(
                int notificacionId,
                int usuarioSolicitanteId)
        {
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue) return ServiceResult.Error("Usuario inactivo o no encontrado.");

            var notificacion =
                await _context.Notificaciones
                    .FirstOrDefaultAsync(n =>
                        n.Id == notificacionId &&
                        n.PersonaId == personaId);

            if (notificacion == null)
            {
                return ServiceResult.Error(
                    "Notificación no encontrada.");
            }

            if (notificacion.Leida)
            {
                return ServiceResult.Ok(
                    "La notificación ya estaba marcada como leída.");
            }

            notificacion.Leida = true;

            await GuardarCambiosAsync();

            return ServiceResult.Ok(
                "Notificación marcada como leída.");
        }


        public async Task<ServiceResult>
            MarcarTodasComoLeidasAsync(
                int usuarioSolicitanteId)
        {
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue) return ServiceResult.Error("Usuario inactivo o no encontrado.");

            var notificaciones =
                await _context.Notificaciones

                    .Where(n =>
                        n.PersonaId == personaId &&
                        !n.Leida)

                    .ToListAsync();

            if (notificaciones.Count == 0)
            {
                return ServiceResult.Ok(
                    "No hay notificaciones pendientes.");
            }

            foreach (var notificacion in notificaciones)
            {
                notificacion.Leida = true;
            }

            await GuardarCambiosAsync();

            return ServiceResult.Ok(
                "Notificaciones marcadas como leídas.");
        }


        // =====================================
        // MÉTODOS PRIVADOS
        // =====================================

        private async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}