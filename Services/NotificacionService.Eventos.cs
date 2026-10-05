using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed record ContadorNotificaciones(int Cantidad);

public partial class NotificacionService
{
    private async Task<int?> PersonaAutorizadaAsync(int usuarioId) =>
        await _permisos.TienePermisoAsync(usuarioId, "NOTIFICACION_VER")
            ? await _permisos.ObtenerPersonaActivaIdAsync(usuarioId) : null;

    public async Task<ServiceResult<ContadorNotificaciones>> ContarNoLeidasAsync(int usuarioId)
    {
        var persona = await PersonaAutorizadaAsync(usuarioId);
        return persona.HasValue ? ServiceResult<ContadorNotificaciones>.Ok(new ContadorNotificaciones(await _context.Notificaciones.AsNoTracking()
            .CountAsync(n => n.PersonaId == persona && !n.Leida))) : ServiceResult<ContadorNotificaciones>.Error("Acceso denegado.");
    }

    public async Task<ServiceResult<Notificacion>> ObtenerPropiaAsync(int id, int usuarioId)
    {
        var persona = await PersonaAutorizadaAsync(usuarioId);
        if (!persona.HasValue) return ServiceResult<Notificacion>.Error("Acceso denegado.");
        var n = await _context.Notificaciones.AsNoTracking().SingleOrDefaultAsync(n => n.Id == id && n.PersonaId == persona);
        return n == null ? ServiceResult<Notificacion>.Error("Notificación no encontrada.") : ServiceResult<Notificacion>.Ok(n);
    }

    // Sólo prepara entidades: SaveChanges y transacción pertenecen al caso de negocio.
    internal async Task AgregarAsync(int personaId, string titulo, string mensaje, TipoRecursoNotificacion tipo, int recursoId)
    {
        if (!await _context.Personas.AnyAsync(p => p.Id == personaId && p.Activo)) return;
        if (titulo.Length > 200 || mensaje.Length > 1000 || recursoId <= 0)
            throw new InvalidOperationException("Notificación interna inválida.");
        _context.Notificaciones.Add(new Notificacion { PersonaId = personaId, Titulo = titulo,
            Mensaje = mensaje, TipoRecurso = tipo, RecursoId = recursoId, Fecha = DateTime.Now });
    }

    internal async Task ClienteAsync(int ordenId, string titulo, TipoRecursoNotificacion tipo, int recursoId)
    {
        var datos = await _context.OrdenesTrabajo.Where(o => o.Id == ordenId)
            .Select(o => new { o.IngresoVehiculo.Turno.ClienteId, o.IngresoVehiculo.VehiculoPatenteSnapshot }).SingleAsync();
        await AgregarAsync(datos.ClienteId, titulo,
            $"{titulo}. Orden #{ordenId}; vehículo {datos.VehiculoPatenteSnapshot}; referencia #{recursoId}.", tipo, recursoId);
    }

    internal async Task OrdenFinalizadaAsync(int ordenId)
    {
        var datos = await _context.OrdenesTrabajo.Where(o => o.Id == ordenId)
            .Select(o => new { o.IngresoVehiculo.ClienteNombreSnapshot, o.IngresoVehiculo.VehiculoPatenteSnapshot }).SingleAsync();
        await FuncionAsync(RolesSistema.CAJA, "Orden lista para entrega",
            $"Orden #{ordenId}; cliente {datos.ClienteNombreSnapshot}; vehículo {datos.VehiculoPatenteSnapshot}. Trabajo finalizado.",
            TipoRecursoNotificacion.OrdenTrabajo, ordenId);
    }

    // Selección de destinatarios operativos; no concede autorización por rol ni por bypass ADMIN.
    internal async Task FuncionAsync(string funcion, string titulo, string mensaje, TipoRecursoNotificacion tipo, int id)
    {
        var personas = await _context.PersonaRoles.AsNoTracking().Where(pr => pr.FechaBaja == null && pr.Rol.Activo &&
            pr.Rol.Nombre == funcion && pr.Persona.Activo && pr.Persona.Usuario != null && pr.Persona.Usuario.Activo)
            .Select(pr => pr.PersonaId).Distinct().ToListAsync();
        foreach (var persona in personas) await AgregarAsync(persona, titulo, mensaje, tipo, id);
    }

    internal async Task TurnoAsync(Turno turno, DateTime anterior, bool reprogramado)
    {
        var hoy = DateTime.Today;
        bool Cercano(DateTime fecha) => fecha.Date == hoy || fecha.Date == hoy.AddDays(1);
        if (!Cercano(anterior) && (!reprogramado || !Cercano(turno.FechaInicio))) return;
        if (reprogramado && anterior == turno.FechaInicio) return;
        var titulo = reprogramado ? "Turno reprogramado" : "Turno cancelado";
        var datos = await _context.Turnos.Where(t => t.Id == turno.Id)
            .Select(t => new { t.Cliente.Nombre, t.Cliente.Apellido, Patente = t.Vehiculo == null ? null : t.Vehiculo.Patente }).SingleAsync();
        var mensaje = $"Turno #{turno.Id}; cliente {datos.Nombre} {datos.Apellido}; vehículo {datos.Patente ?? "sin identificar"}. {anterior:dd/MM/yyyy HH:mm}" +
            (reprogramado ? $" → {turno.FechaInicio:dd/MM/yyyy HH:mm}." : ".");
        await FuncionAsync(RolesSistema.CAJA, titulo, mensaje, TipoRecursoNotificacion.Turno, turno.Id);
    }

    internal async Task StockAsync(Repuesto repuesto, int anterior)
    {
        string? titulo = anterior > 0 && repuesto.StockActual == 0 ? "Repuesto sin stock" :
            anterior > repuesto.StockMinimo && repuesto.StockActual <= repuesto.StockMinimo ? "Stock bajo" : null;
        if (titulo == null) return;
        await FuncionAsync(RolesSistema.STOCK, titulo,
            $"{repuesto.Nombre}. Actual: {repuesto.StockActual} / Mínimo: {repuesto.StockMinimo}.",
            TipoRecursoNotificacion.Repuesto, repuesto.Id);
    }
}
