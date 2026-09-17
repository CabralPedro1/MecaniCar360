using System.Data;
using Microsoft.Data.SqlClient;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public class CalificacionTrabajoService
{
    private readonly MecaniCarContext _context;
    private readonly PermisoService _permisos;
    private readonly AuditoriaService _auditoria;

    public CalificacionTrabajoService(MecaniCarContext context, PermisoService permisos, AuditoriaService auditoria)
    {
        _context = context;
        _permisos = permisos;
        _auditoria = auditoria;
    }

    // Cada autorización habilita únicamente su alcance; no existe fallback a consulta global.
    private async Task<IQueryable<CalificacionTrabajo>?> ConsultaAutorizadaAsync(int usuarioId)
    {
        var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioId);
        if (!personaId.HasValue) return null;
        var consulta = _context.Calificaciones.AsNoTracking();
        if (await _permisos.TienePermisoAsync(usuarioId, "ORDEN_VER") && await _permisos.EsAdministradorAsync(usuarioId))
            return consulta;
        var propias = await _permisos.TienePermisoAsync(usuarioId, "CLIENTE_ORDEN_VER");
        var asignadas = await _permisos.TienePermisoAsync(usuarioId, "ORDEN_VER") &&
            await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == personaId && pr.FechaBaja == null &&
                pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.MECANICO);
        if (!propias && !asignadas) return null;
        return consulta.Where(c => (propias && c.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId == personaId) ||
            (asignadas && c.OrdenTrabajo.MecanicoId == personaId));
    }

    public async Task<ServiceResult<CalificacionTrabajo>> ObtenerPorOrdenTrabajoAsync(int ordenTrabajoId, int usuarioSolicitanteId)
    {
        var consulta = await ConsultaAutorizadaAsync(usuarioSolicitanteId);
        if (consulta == null) return ServiceResult<CalificacionTrabajo>.Error("Acceso denegado.");
        var calificacion = await consulta.Include(c => c.Cliente).Include(c => c.OrdenTrabajo)
            .FirstOrDefaultAsync(c => c.OrdenTrabajoId == ordenTrabajoId);
        return calificacion == null ? ServiceResult<CalificacionTrabajo>.Error("Calificación inexistente o no accesible.") :
            ServiceResult<CalificacionTrabajo>.Ok(calificacion);
    }

    public async Task<ServiceResult<List<CalificacionTrabajo>>> ObtenerTodasAsync(int usuarioSolicitanteId)
    {
        var consulta = await ConsultaAutorizadaAsync(usuarioSolicitanteId);
        if (consulta == null) return ServiceResult<List<CalificacionTrabajo>>.Error("Acceso denegado.");
        return ServiceResult<List<CalificacionTrabajo>>.Ok(await consulta.Include(c => c.Cliente)
            .Include(c => c.OrdenTrabajo).OrderByDescending(c => c.Fecha).ToListAsync());
    }

    public async Task<ServiceResult> CrearAsync(int ordenTrabajoId, int usuarioSolicitanteId, int puntuacion, string? comentario)
    {
        if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "CLIENTE_CALIFICACION_CREAR"))
            return ServiceResult.Error("Acceso denegado.");
        if (puntuacion < 1 || puntuacion > 5) return ServiceResult.Error("La puntuación debe estar entre 1 y 5.");
        if (comentario?.Length > 1000) return ServiceResult.Error("El comentario no puede superar los 1000 caracteres.");
        if (_context.Database.CurrentTransaction != null)
            return ServiceResult.Error("La creación de calificación requiere una transacción propia.");
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var orden = await _context.OrdenesTrabajo.FromSqlInterpolated(
                $"SELECT * FROM [OrdenesTrabajo] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {ordenTrabajoId}").FirstOrDefaultAsync();
            if (orden == null) return ServiceResult.Error("Orden de trabajo no encontrada.");
            await _context.Entry(orden).ReloadAsync();
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue || !await _permisos.TienePermisoAsync(usuarioSolicitanteId, "CLIENTE_CALIFICACION_CREAR"))
                return ServiceResult.Error("Acceso denegado.");
            // El cliente de la reparación histórica no cambia con la titularidad del vehículo.
            // Esta comprobación también se aplica a ADMIN.
            if (!await _context.OrdenesTrabajo.AnyAsync(o => o.Id == orden.Id &&
                o.IngresoVehiculo.Turno.ClienteId == personaId.Value))
                return ServiceResult.Error("La orden no pertenece al cliente histórico autenticado.");
            if (orden.EstadoActual != EstadoOrden.Entregado)
                return ServiceResult.Error("Sólo se puede calificar una orden entregada.");
            if (await _context.Calificaciones.AnyAsync(c => c.OrdenTrabajoId == orden.Id))
                return ServiceResult.Error("Esta orden ya fue calificada.");
            var calificacion = new CalificacionTrabajo
            {
                OrdenTrabajoId = orden.Id, ClienteId = personaId.Value, Puntuacion = puntuacion,
                Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim(), Fecha = DateTime.Now
            };
            _context.Calificaciones.Add(calificacion);
            await _context.SaveChangesAsync();
            _auditoria.RegistrarOperacion("CALIFICACION_CREADA", "CalificacionTrabajo", calificacion.Id, usuarioSolicitanteId);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Ok("Calificación registrada correctamente.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _context.ChangeTracker.Clear();
            if (Conflicto(ex)) return ServiceResult.Error("La calificación no pudo registrarse por un conflicto de datos o concurrencia.");
            throw;
        }
    }

    private static bool Conflicto(Exception ex) =>
        ex is SqlException sql && sql.Number is 1205 or 1222 or 2601 or 2627 or 547 or 8152 or 2628 ||
        ex.InnerException != null && Conflicto(ex.InnerException);
}
