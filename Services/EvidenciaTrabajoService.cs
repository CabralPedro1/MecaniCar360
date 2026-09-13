using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class EvidenciaTrabajoService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisos;

        public EvidenciaTrabajoService(
            MecaniCarContext context, PermisoService permisos)
        {
            _context = context;
            _permisos = permisos;
        }

        // =====================================================
        // CONSULTAS
        // =====================================================

        public async Task<ServiceResult<List<EvidenciaTrabajo>>>
            ObtenerPorOrdenTrabajoAsync(
                int ordenTrabajoId,
                int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "DIAGNOSTICO_VER")) return ServiceResult<List<EvidenciaTrabajo>>.Error("Acceso denegado.");
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue) return ServiceResult<List<EvidenciaTrabajo>>.Error("Acceso denegado.");

            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult<List<EvidenciaTrabajo>>
                    .Error(
                        "Orden de trabajo no encontrada.");
            }

            var puedeConsultar =
                await PuedeAccederOrdenAsync(
                    orden,
                    usuarioSolicitanteId);

            if (!puedeConsultar)
            {
                return ServiceResult<List<EvidenciaTrabajo>>
                    .Error(
                        "No tiene permisos para consultar las evidencias de esta orden.");
            }

            var evidencias =
                await _context.Evidencias

                    .Include(e => e.SubidaPorUsuario)
                        .ThenInclude(u => u.Persona)

                    .Where(e =>
                        e.OrdenTrabajoId ==
                        ordenTrabajoId)

                    .OrderByDescending(e => e.Fecha)

                    .ToListAsync();

            return ServiceResult<List<EvidenciaTrabajo>>
                .Ok(evidencias);
        }


        // =====================================================
        // ABM
        // =====================================================

        public async Task<ServiceResult> CrearAsync(
            int ordenTrabajoId,
            int usuarioId,
            string descripcion,
            string rutaArchivo)
        {
            if (!await _permisos.TienePermisoAsync(usuarioId, "ORDEN_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");

            // =====================================
            // VALIDACIONES BÁSICAS
            // =====================================

            if (string.IsNullOrWhiteSpace(
                descripcion))
            {
                return ServiceResult.Error(
                    "La descripción de la evidencia es obligatoria.");
            }

            if (descripcion.Trim().Length > 500)
            {
                return ServiceResult.Error(
                    "La descripción no puede superar los 500 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(
                rutaArchivo))
            {
                return ServiceResult.Error(
                    "Debe indicar el archivo de evidencia.");
            }

            if (rutaArchivo.Trim().Length > 500)
            {
                return ServiceResult.Error(
                    "La ruta del archivo no puede superar los 500 caracteres.");
            }

            // =====================================
            // USUARIO
            // =====================================

            var usuario =
                await _context.Usuarios

                    .Include(u => u.Persona)

                    .FirstOrDefaultAsync(u =>
                        u.Id == usuarioId &&
                        u.Activo);

            if (usuario == null)
            {
                return ServiceResult.Error(
                    "Usuario no encontrado o inactivo.");
            }

            if (!usuario.Persona.Activo)
            {
                return ServiceResult.Error(
                    "La persona asociada al usuario está inactiva.");
            }

            // =====================================
            // ORDEN
            // =====================================

            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult.Error(
                    "Orden de trabajo no encontrada.");
            }

            // =====================================
            // PERMISOS
            // =====================================

            var puedeModificar =
                await PuedeAgregarEvidenciaAsync(
                    orden,
                    usuarioId);

            if (!puedeModificar)
            {
                return ServiceResult.Error(
                    "No tiene permisos para agregar evidencias a esta orden.");
            }

            // =====================================
            // ESTADO
            // =====================================

            if (orden.EstadoActual ==
                EstadoOrden.Entregado)
            {
                return ServiceResult.Error(
                    "No se pueden agregar evidencias a una orden entregada.");
            }

            // =====================================
            // CREAR EVIDENCIA
            // =====================================

            var evidencia =
                new EvidenciaTrabajo
                {
                    OrdenTrabajoId =
                        ordenTrabajoId,

                    Descripcion =
                        descripcion.Trim(),

                    RutaArchivo =
                        rutaArchivo.Trim(),

                    Fecha =
                        DateTime.Now,

                    SubidaPorUsuarioId =
                        usuarioId
                };

            _context.Evidencias.Add(
                evidencia);

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Evidencia agregada correctamente.");
        }


        // =====================================================
        // MÉTODOS PRIVADOS
        // =====================================================

        private Task<bool> PuedeAgregarEvidenciaAsync(OrdenTrabajo orden, int usuarioId) =>
            PuedeAccederOrdenAsync(orden, usuarioId);

        private async Task<bool> PuedeAccederOrdenAsync(OrdenTrabajo orden, int usuarioId)
        {
            if (await _permisos.EsAdministradorAsync(usuarioId)) return true;
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioId);
            return personaId.HasValue && orden.MecanicoId == personaId &&
                await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == personaId &&
                    pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.MECANICO);
        }
    }
}
