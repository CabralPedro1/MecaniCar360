using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Patterns.Memento;
using MecaniCar360.Patterns.State;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class DiagnosticoService
    {
        private readonly MecaniCarContext _context;
        private readonly OrdenStateService _ordenStateService;
        private readonly PermisoService _permisoService;

        public DiagnosticoService(MecaniCarContext context,
            OrdenStateService ordenStateService, PermisoService permisoService)
        {
            _context = context;
            _ordenStateService = ordenStateService;
            _permisoService = permisoService;
        }

        public async Task<ServiceResult> IniciarAsync(int ordenTrabajoId, int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAsync(usuarioSolicitanteId);
            if (usuario == null || !await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "DIAGNOSTICO_CREAR"))
                return ServiceResult.Error("Usuario inactivo o sin permisos para iniciar el diagnóstico.");

            var orden = await _context.OrdenesTrabajo.FirstOrDefaultAsync(o => o.Id == ordenTrabajoId);
            if (orden == null)
                return ServiceResult.Error("Orden de trabajo no encontrada.");

            var acceso = await ValidarAccesoAsync(orden, usuario, trabajoTecnico: true);
            if (!acceso.Exitoso) return acceso;
            if (orden.FechaFin.HasValue || orden.EstadoActual != EstadoOrden.Pendiente)
                return ServiceResult.Error("La orden debe estar pendiente y sin finalizar para iniciar el diagnóstico.");
            if (await _context.Diagnosticos.AnyAsync(d => d.OrdenTrabajoId == ordenTrabajoId))
                return ServiceResult.Error("La orden ya tiene un diagnóstico.");

            _ordenStateService.CambiarEstado(orden, new EstadoDiagnosticoHandler());
            var historial = orden.HistorialEstados.LastOrDefault();
            if (historial != null)
                historial.MecanicoId = await MecanicoActorAsync(usuario);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Diagnóstico iniciado correctamente.");
        }

        public async Task<ServiceResult<Diagnostico>> ObtenerAsync(int ordenTrabajoId, int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAsync(usuarioSolicitanteId);
            if (usuario == null || !await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "DIAGNOSTICO_VER"))
                return ServiceResult<Diagnostico>.Error("Usuario inactivo o sin permisos para consultar el diagnóstico.");

            var orden = await _context.OrdenesTrabajo.AsNoTracking().FirstOrDefaultAsync(o => o.Id == ordenTrabajoId);
            if (orden == null) return ServiceResult<Diagnostico>.Error("Orden de trabajo no encontrada.");
            var acceso = await ValidarAccesoAsync(orden, usuario);
            if (!acceso.Exitoso) return ServiceResult<Diagnostico>.Error(acceso.Mensaje);

            // Proyección sin tracking: no expone historial ni navegaciones cargadas previamente.
            var diagnostico = await _context.Diagnosticos.AsNoTracking()
                .Where(d => d.OrdenTrabajoId == ordenTrabajoId)
                .Select(d => new Diagnostico
                {
                    Id = d.Id,
                    OrdenTrabajoId = d.OrdenTrabajoId,
                    DescripcionActual = d.DescripcionActual,
                    FechaUltimaModificacion = d.FechaUltimaModificacion
                }).FirstOrDefaultAsync();
            return diagnostico == null
                ? ServiceResult<Diagnostico>.Error("La orden todavía no tiene un diagnóstico.")
                : ServiceResult<Diagnostico>.Ok(diagnostico);
        }

        public async Task<ServiceResult> GuardarAsync(int ordenTrabajoId, int usuarioSolicitanteId, string descripcion)
        {
            var usuario = await ObtenerUsuarioAsync(usuarioSolicitanteId);
            if (usuario == null) return ServiceResult.Error("Usuario inactivo o inexistente.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orden = await _context.OrdenesTrabajo.FirstOrDefaultAsync(o => o.Id == ordenTrabajoId);
                if (orden == null) return ServiceResult.Error("Orden de trabajo no encontrada.");
                var diagnostico = await _context.Diagnosticos.FirstOrDefaultAsync(d => d.OrdenTrabajoId == ordenTrabajoId);
                var patente = diagnostico == null ? "DIAGNOSTICO_CREAR" : "DIAGNOSTICO_MODIFICAR";
                if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, patente))
                    return ServiceResult.Error("No tiene permisos para guardar este diagnóstico.");

                var acceso = await ValidarAccesoAsync(orden, usuario, trabajoTecnico: true);
                if (!acceso.Exitoso) return acceso;
                if (orden.FechaFin.HasValue || !AdmiteModificacion(orden.EstadoActual))
                    return ServiceResult.Error("La orden no admite modificaciones del diagnóstico.");
                if (diagnostico == null && orden.EstadoActual != EstadoOrden.Diagnostico)
                    return ServiceResult.Error("La orden debe estar en diagnóstico para registrar el diagnóstico inicial.");
                if (string.IsNullOrWhiteSpace(descripcion))
                    return ServiceResult.Error("Debe ingresar una descripción del diagnóstico.");
                if (descripcion.Length > 5000)
                    return ServiceResult.Error("La descripción del diagnóstico no puede superar los 5000 caracteres.");

                var ahora = DateTime.Now;
                var texto = descripcion.Trim();
                var mecanicoActorId = await MecanicoActorAsync(usuario);
                if (diagnostico == null)
                {
                    diagnostico = new Diagnostico
                    {
                        OrdenTrabajoId = ordenTrabajoId,
                        DescripcionActual = texto,
                        FechaUltimaModificacion = ahora
                    };
                    _context.Diagnosticos.Add(diagnostico);
                    await _context.SaveChangesAsync();
                    _context.DiagnosticoHistoriales.Add(new DiagnosticoHistorial
                    {
                        DiagnosticoId = diagnostico.Id,
                        Descripcion = texto,
                        Fecha = ahora,
                        MecanicoId = mecanicoActorId
                    });
                }
                else
                {
                    var originator = new DiagnosticoOriginator(diagnostico.DescripcionActual);
                    var caretaker = new DiagnosticoCaretaker();
                    caretaker.Guardar(originator.CrearMemento());
                    var estadoAnterior = caretaker.ObtenerAnterior();
                    if (estadoAnterior != null)
                    {
                        _context.DiagnosticoHistoriales.Add(new DiagnosticoHistorial
                        {
                            DiagnosticoId = diagnostico.Id,
                            Descripcion = estadoAnterior.Descripcion,
                            Fecha = estadoAnterior.Fecha,
                            MecanicoId = mecanicoActorId
                        });
                    }
                    originator.Actualizar(texto);
                    diagnostico.DescripcionActual = originator.Descripcion;
                    diagnostico.FechaUltimaModificacion = ahora;
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return ServiceResult.Ok("Diagnóstico guardado correctamente.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ServiceResult<List<DiagnosticoHistorial>>> ObtenerHistorialAsync(int ordenTrabajoId, int usuarioSolicitanteId)
        {
            var usuario = await ObtenerUsuarioAsync(usuarioSolicitanteId);
            if (usuario == null || !await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "DIAGNOSTICO_HISTORIAL"))
                return ServiceResult<List<DiagnosticoHistorial>>.Error("Usuario inactivo o sin permisos para consultar el historial.");
            var orden = await _context.OrdenesTrabajo.AsNoTracking().FirstOrDefaultAsync(o => o.Id == ordenTrabajoId);
            if (orden == null) return ServiceResult<List<DiagnosticoHistorial>>.Error("Orden de trabajo no encontrada.");
            var acceso = await ValidarAccesoAsync(orden, usuario);
            if (!acceso.Exitoso) return ServiceResult<List<DiagnosticoHistorial>>.Error(acceso.Mensaje);
            var diagnosticoId = await _context.Diagnosticos.Where(d => d.OrdenTrabajoId == ordenTrabajoId)
                .Select(d => (int?)d.Id).FirstOrDefaultAsync();
            if (!diagnosticoId.HasValue)
                return ServiceResult<List<DiagnosticoHistorial>>.Error("La orden todavía no tiene un diagnóstico.");
            var historial = await _context.DiagnosticoHistoriales.AsNoTracking()
                .Include(h => h.Mecanico)
                .Where(h => h.DiagnosticoId == diagnosticoId.Value)
                .OrderByDescending(h => h.Fecha).ToListAsync();
            return ServiceResult<List<DiagnosticoHistorial>>.Ok(historial);
        }

        private Task<Usuario?> ObtenerUsuarioAsync(int usuarioSolicitanteId) =>
            _context.Usuarios.Include(u => u.Persona).FirstOrDefaultAsync(u =>
                u.Id == usuarioSolicitanteId && u.Activo && u.Persona.Activo);

        private async Task<ServiceResult> ValidarAccesoAsync(OrdenTrabajo orden, Usuario usuario, bool trabajoTecnico = false)
        {
            if (trabajoTecnico && !orden.MecanicoId.HasValue)
                return ServiceResult.Error("La orden debe tener un mecánico asignado.");
            if (await _permisoService.EsAdministradorAsync(usuario.Id))
                return ServiceResult.Ok();
            var mecanicoElegible = await _context.PersonaRoles.AnyAsync(pr =>
                pr.PersonaId == usuario.PersonaId && pr.Persona.Activo &&
                pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.MECANICO);
            return mecanicoElegible && orden.MecanicoId == usuario.PersonaId
                ? ServiceResult.Ok()
                : ServiceResult.Error("No tiene acceso al diagnóstico de esta orden.");
        }

        private async Task<int?> MecanicoActorAsync(Usuario usuario) =>
            await _permisoService.EsAdministradorAsync(usuario.Id) ? null : usuario.PersonaId;

        private static bool AdmiteModificacion(EstadoOrden estado) => estado is
            EstadoOrden.Diagnostico or EstadoOrden.EsperandoAprobacion or
            EstadoOrden.Aprobado or EstadoOrden.EnReparacion or EstadoOrden.Rechazado;
    }
}
