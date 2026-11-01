using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class CalificacionTrabajoService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisos;

        public CalificacionTrabajoService(
            MecaniCarContext context, PermisoService permisos)
        {
            _context = context;
            _permisos = permisos;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<CalificacionTrabajo>>
            ObtenerPorOrdenTrabajoAsync(
                int ordenTrabajoId, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "CLIENTE_ORDEN_VER")) return ServiceResult<CalificacionTrabajo>.Error("Acceso denegado.");
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue || (!await _permisos.EsAdministradorAsync(usuarioSolicitanteId) &&
                !await _context.OrdenesTrabajo.AnyAsync(o => o.Id == ordenTrabajoId && o.IngresoVehiculo.Turno.ClienteId == personaId)))
                return ServiceResult<CalificacionTrabajo>.Error("Acceso denegado.");

            var calificacion =
                await _context.Calificaciones
                    .Include(c => c.Cliente)
                    .Include(c => c.OrdenTrabajo)
                    .FirstOrDefaultAsync(
                        c => c.OrdenTrabajoId == ordenTrabajoId);

            if (calificacion == null)
            {
                return ServiceResult<CalificacionTrabajo>
                    .Error(
                        "La orden de trabajo no tiene una calificación.");
            }

            return ServiceResult<CalificacionTrabajo>
                .Ok(calificacion);
        }

        public async Task<ServiceResult<List<CalificacionTrabajo>>>
            ObtenerTodasAsync(int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "ORDEN_VER")) return ServiceResult<List<CalificacionTrabajo>>.Error("Acceso denegado.");
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            var limitarAlMecanico = !await _permisos.EsAdministradorAsync(usuarioSolicitanteId) &&
                await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == personaId &&
                    pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.MECANICO);

            var calificaciones =
                await _context.Calificaciones
                    .Include(c => c.Cliente)
                    .Include(c => c.OrdenTrabajo)
                    .Where(c => !limitarAlMecanico || c.OrdenTrabajo.MecanicoId == personaId)
                    .OrderByDescending(c => c.Fecha)
                    .ToListAsync();

            return ServiceResult<List<CalificacionTrabajo>>
                .Ok(calificaciones);
        }

        // =====================================
        // ABM
        // =====================================

        // Consultas frescas: no reutilizan entidades rastreadas antes de esperar el bloqueo.
        private static IQueryable<Usuario> ClientesElegibles(IQueryable<Usuario> usuarios) =>
            usuarios.Where(u => u.Activo && u.Persona.Activo && !u.PrimerLogin &&
                u.Persona.Roles.Any(r => r.FechaBaja == null && r.Rol.Activo && r.Rol.Nombre == RolesSistema.CLIENTE) &&
                !u.Persona.Roles.Any(r => r.FechaBaja == null && r.Rol.Activo && r.Rol.Nombre != RolesSistema.CLIENTE));

        private async Task<int?> ClienteAutorizadoAsync(int usuarioId)
        {
            var persona = await ClientesElegibles(_context.Usuarios.AsNoTracking())
                .Where(u => u.Id == usuarioId).Select(u => (int?)u.PersonaId).SingleOrDefaultAsync();
            return persona.HasValue &&
                await new ClienteHabilitadoService(_context).EstaHabilitadoAsync(persona.Value) &&
                await _permisos.TienePermisoAsync(usuarioId, "CLIENTE_CALIFICACION_CREAR")
                ? persona : null;
        }

        private static string? ValidarOrden(EstadoOrden estado, int propietario, int cliente,
            bool titularActual, bool existente)
        {
            if (propietario != cliente) return "La orden no pertenece al cliente autenticado.";
            if (estado != EstadoOrden.Entregado) return "Solo se puede calificar una orden de trabajo entregada.";
            if (existente) return "Esta orden de trabajo ya fue calificada.";
            if (!titularActual) return "El cliente no puede calificar esta orden de trabajo.";
            return null;
        }

        private async Task<string?> ValidarOrdenAsync(int ordenId, int clienteId)
        {
            var orden = await _context.OrdenesTrabajo.AsNoTracking().Where(o => o.Id == ordenId)
                .Select(o => new { o.EstadoActual, o.IngresoVehiculo.Turno.ClienteId,
                    Titular = o.IngresoVehiculo.Vehiculo.DominiosVehiculares.Any(d =>
                        d.PersonaId == clienteId && d.FechaHasta == null),
                    Existente = o.Calificacion != null }).SingleOrDefaultAsync();
            return orden == null ? "La orden de trabajo no existe." :
                ValidarOrden(orden.EstadoActual, orden.ClienteId, clienteId, orden.Titular, orden.Existente);
        }

        public async Task<bool> PuedeCalificarAsync(int ordenTrabajoId, int usuarioId)
        {
            var cliente = await ClienteAutorizadoAsync(usuarioId);
            return cliente.HasValue && await ValidarOrdenAsync(ordenTrabajoId, cliente.Value) == null;
        }

        private static string? ValidarEntrada(int puntuacion, string? comentario)
        {
            if (puntuacion < 1 || puntuacion > 5) return "La puntuación debe estar entre 1 y 5.";
            if (comentario?.Length > 1000) return "El comentario no puede superar los 1000 caracteres.";
            return null;
        }

        public async Task<ServiceResult> CrearAsync(
            int ordenTrabajoId, int usuarioSolicitanteId, int puntuacion, string? comentario)
        {
            var error = ValidarEntrada(puntuacion, comentario);
            if (error != null) return ServiceResult.Error(error);

            // BeginTransaction adquiere primero el mutex global DVH/DVV mediante el interceptor.
            // Serializable protege también la ausencia de calificación; se conserva el índice único.
            await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var cliente = await ClienteAutorizadoAsync(usuarioSolicitanteId);
                if (!cliente.HasValue) return ServiceResult.Error("Acceso denegado.");
                error = await ValidarOrdenAsync(ordenTrabajoId, cliente.Value);
                if (error != null) return ServiceResult.Error(error);

                _context.Calificaciones.Add(new CalificacionTrabajo
                {
                    OrdenTrabajoId = ordenTrabajoId,
                    ClienteId = cliente.Value,
                    Puntuacion = puntuacion,
                    Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim(),
                    Fecha = DateTime.Now
                });
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return ServiceResult.Ok("Calificación registrada correctamente.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
