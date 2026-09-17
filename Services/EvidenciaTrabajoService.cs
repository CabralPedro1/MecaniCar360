using System.Data;
using Microsoft.Data.SqlClient;
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
        private readonly AuditoriaService _auditoria;

        public EvidenciaTrabajoService(
            MecaniCarContext context, PermisoService permisos, AuditoriaService auditoria)
        {
            _context = context;
            _permisos = permisos;
            _auditoria = auditoria;
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
                await _context.Evidencias.AsNoTracking()

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

        public async Task<ServiceResult> CrearAsync(int ordenTrabajoId, int usuarioId, string descripcion, string rutaArchivo)
        {
            if (!await _permisos.TienePermisoAsync(usuarioId, "ORDEN_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");
            if (string.IsNullOrWhiteSpace(descripcion) || descripcion.Trim().Length > 500 ||
                string.IsNullOrWhiteSpace(rutaArchivo) || rutaArchivo.Trim().Length > 500)
                return ServiceResult.Error("Descripción y referencia son obligatorias y admiten hasta 500 caracteres.");
            if (_context.Database.CurrentTransaction != null)
                return ServiceResult.Error("La creación de evidencia requiere una transacción propia.");
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var orden = await _context.OrdenesTrabajo.FromSqlInterpolated(
                    $"SELECT * FROM [OrdenesTrabajo] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {ordenTrabajoId}")
                    .FirstOrDefaultAsync();
                if (orden == null) return ServiceResult.Error("Orden de trabajo no encontrada.");
                await _context.Entry(orden).ReloadAsync();
                if (!await _permisos.TienePermisoAsync(usuarioId, "ORDEN_MODIFICAR") ||
                    !(await _permisos.ObtenerPersonaActivaIdAsync(usuarioId)).HasValue ||
                    !await PuedeAgregarEvidenciaAsync(orden, usuarioId))
                    return ServiceResult.Error("No tiene permiso o asignación vigente para agregar evidencia.");
                if (orden.FechaFin.HasValue || orden.EstadoActual is not (EstadoOrden.Diagnostico or
                    EstadoOrden.EsperandoAprobacion or EstadoOrden.Aprobado or EstadoOrden.EnReparacion or EstadoOrden.Rechazado))
                    return ServiceResult.Error("La orden no admite nuevas evidencias: revise estado y fecha de cierre.");
                var evidencia = new EvidenciaTrabajo
                {
                    OrdenTrabajoId = orden.Id, SubidaPorUsuarioId = usuarioId,
                    Descripcion = descripcion.Trim(), RutaArchivo = rutaArchivo.Trim(), Fecha = DateTime.Now
                };
                _context.Evidencias.Add(evidencia);
                await _context.SaveChangesAsync();
                _auditoria.RegistrarOperacion("EVIDENCIA_CREADA", "EvidenciaTrabajo", evidencia.Id, usuarioId);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return ServiceResult.Ok("Evidencia agregada correctamente.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                if (Conflicto(ex)) return ServiceResult.Error("Conflicto de datos o concurrencia. Actualice antes de reintentar.");
                throw;
            }
        }

        private static bool Conflicto(Exception ex) =>
            ex is SqlException sql && sql.Number is 1205 or 1222 or 2601 or 2627 or 547 or 8152 or 2628 ||
            ex.InnerException != null && Conflicto(ex.InnerException);

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
