using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Patterns.Observer;
using MecaniCar360.Patterns.State;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class PresupuestoService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisos;
        private readonly StockService _stock;
        private readonly OrdenStateService _estados;
        private readonly OrdenSubject _observer;
        private readonly ILogger<PresupuestoService> _logger;

        public PresupuestoService(MecaniCarContext context, PermisoService permisos,
            StockService stock, OrdenStateService estados, OrdenSubject observer,
            ILogger<PresupuestoService> logger)
        {
            _context = context;
            _permisos = permisos;
            _stock = stock;
            _estados = estados;
            _observer = observer;
            _logger = logger;
        }

        public async Task<ServiceResult<Presupuesto>> ObtenerAsync(int ordenTrabajoId, int usuarioSolicitanteId)
        {
            var usuario = await UsuarioAutorizadoAsync(usuarioSolicitanteId, "PRESUPUESTO_VER");
            var orden = await OrdenConsultaAsync(ordenTrabajoId);
            if (usuario == null || orden == null || !await TecnicoAsync(orden, usuario))
                return ServiceResult<Presupuesto>.Error("No tiene acceso a este presupuesto.");
            var presupuesto = await _context.Presupuestos.AsNoTracking()
                .Include(p => p.Items)
                .Include(p => p.Historial).ThenInclude(h => h.Mecanico)
                .Include(p => p.Versiones).ThenInclude(v => v.Items)
                .Include(p => p.Versiones).ThenInclude(v => v.Evidencias
                    .Where(e => e.EvidenciaTrabajo.OrdenTrabajoId == ordenTrabajoId))
                    .ThenInclude(e => e.EvidenciaTrabajo)
                .FirstOrDefaultAsync(p => p.OrdenTrabajoId == ordenTrabajoId);
            if (presupuesto == null) return ServiceResult<Presupuesto>.Error("La orden no tiene presupuesto.");
            presupuesto.OrdenTrabajo = orden;
            orden.Evidencias = await _context.Evidencias.AsNoTracking()
                .Where(e => e.OrdenTrabajoId == orden.Id).OrderByDescending(e => e.Fecha).ToListAsync();
            return ServiceResult<Presupuesto>.Ok(presupuesto);
        }

        // Nunca devuelve el borrador ni los ítems editables al cliente.
        public async Task<ServiceResult<PresupuestoVersion>> ObtenerPropioAsync(int ordenTrabajoId, int usuarioSolicitanteId)
        {
            var usuario = await UsuarioAutorizadoAsync(usuarioSolicitanteId, "CLIENTE_PRESUPUESTO_VER");
            var orden = await OrdenConsultaAsync(ordenTrabajoId);
            if (usuario == null || orden == null || !await PropietarioAsync(orden, usuario))
                return ServiceResult<PresupuestoVersion>.Error("No tiene acceso a este presupuesto.");
            var version = await _context.PresupuestoVersiones.AsNoTracking()
                .Include(v => v.Items)
                .Include(v => v.Evidencias.Where(e => e.EvidenciaTrabajo.OrdenTrabajoId == ordenTrabajoId))
                    .ThenInclude(e => e.EvidenciaTrabajo)
                .Include(v => v.Presupuesto)
                .Where(v => v.Presupuesto.OrdenTrabajoId == ordenTrabajoId)
                .OrderByDescending(v => v.NumeroVersion).FirstOrDefaultAsync();
            if (version == null)
                return ServiceResult<PresupuestoVersion>.Error("Todavía no hay una versión enviada.");
            // Sólo contexto de vigencia; no se carga contenido editable.
            version.Presupuesto.OrdenTrabajo = orden;
            version.Presupuesto.Total = version.Total;
            return ServiceResult<PresupuestoVersion>.Ok(version);
        }

        // El resultado de las operaciones incluye la OT persistida para redirecciones seguras.
        public Task<ServiceResult<PresupuestoOperacion>> CrearAsync(int ordenTrabajoId, int usuarioSolicitanteId) =>
            EjecutarAsync(ordenTrabajoId, async orden =>
            {
                var usuario = await UsuarioAutorizadoAsync(usuarioSolicitanteId, "PRESUPUESTO_CREAR");
                if (usuario == null || !await TecnicoAsync(orden, usuario)) return ErrorAcceso();
                if (!Editable(orden) || orden.EstadoActual != EstadoOrden.Diagnostico)
                    return ErrorEstado();
                if (await _context.Presupuestos.AnyAsync(p => p.OrdenTrabajoId == orden.Id))
                    return ServiceResult<PresupuestoOperacion>.Error("La orden ya posee un presupuesto.");
                var descripcion = await _context.Diagnosticos.Where(d => d.OrdenTrabajoId == orden.Id)
                    .Select(d => d.DescripcionActual).FirstOrDefaultAsync();
                if (string.IsNullOrWhiteSpace(descripcion))
                    return ServiceResult<PresupuestoOperacion>.Error("Debe registrar un diagnóstico con contenido.");
                _context.Presupuestos.Add(new Presupuesto
                {
                    OrdenTrabajoId = orden.Id, MecanicoId = orden.MecanicoId,
                    Estado = EstadoPresupuesto.Modificado, Total = 0,
                    FechaUltimaModificacion = DateTime.Now
                });
                return ServiceResult<PresupuestoOperacion>.Ok(new PresupuestoOperacion(orden.Id), "Presupuesto creado.");
            });

        public async Task<ServiceResult<PresupuestoOperacion>> AgregarItemAsync(int presupuestoId, int usuarioSolicitanteId,
            string descripcion, int cantidad, decimal precioUnitario, int? repuestoId = null)
        {
            if (string.IsNullOrWhiteSpace(descripcion) || cantidad <= 0 || precioUnitario < 0 ||
                precioUnitario > 9999999999999999.99m || decimal.Round(precioUnitario, 2) != precioUnitario)
                return ServiceResult<PresupuestoOperacion>.Error("Ingrese descripción, cantidad positiva y precio válido con hasta dos decimales.");
            return await EditarAsync(presupuestoId, usuarioSolicitanteId, async presupuesto =>
            {
                if (repuestoId.HasValue && !await _context.Repuestos.AnyAsync(r => r.Id == repuestoId.Value))
                    return ServiceResult.Error("Repuesto no encontrado.");
                var subtotal = cantidad * precioUnitario;
                if (presupuesto.CalcularTotal() + subtotal > 9999999999999999.99m)
                    return ServiceResult.Error("El total excede el importe admitido.");
                presupuesto.Items.Add(new PresupuestoItem
                {
                    Descripcion = descripcion.Trim(), Cantidad = cantidad,
                    PrecioUnitario = precioUnitario, RepuestoId = repuestoId
                });
                return ServiceResult.Ok();
            });
        }

        public Task<ServiceResult<PresupuestoOperacion>> EliminarItemAsync(int presupuestoId, int itemId, int usuarioSolicitanteId) =>
            EditarAsync(presupuestoId, usuarioSolicitanteId, presupuesto =>
            {
                var item = presupuesto.Items.SingleOrDefault(i => i.Id == itemId);
                if (item == null) return Task.FromResult(ServiceResult.Error("El ítem no pertenece al presupuesto."));
                presupuesto.Items.Remove(item);
                return Task.FromResult(ServiceResult.Ok());
            });

        private async Task<ServiceResult<PresupuestoOperacion>> EditarAsync(int presupuestoId, int usuarioId,
            Func<Presupuesto, Task<ServiceResult>> editar)
        {
            var ordenId = await OrdenIdAsync(presupuestoId);
            return await EjecutarAsync(ordenId, async orden =>
            {
                var usuario = await UsuarioAutorizadoAsync(usuarioId, "PRESUPUESTO_MODIFICAR");
                if (usuario == null || !await TecnicoAsync(orden, usuario)) return ErrorAcceso();
                if (!Editable(orden)) return ErrorEstado();
                var presupuesto = await PresupuestoEditableAsync(presupuestoId);
                if (presupuesto == null || !Enum.IsDefined(presupuesto.Estado)) return ErrorEstado();
                var resultado = await editar(presupuesto);
                if (!resultado.Exitoso) return ServiceResult<PresupuestoOperacion>.Error(resultado.Mensaje);
                presupuesto.Total = presupuesto.CalcularTotal();
                presupuesto.Estado = EstadoPresupuesto.Modificado;
                presupuesto.FechaUltimaModificacion = DateTime.Now;
                return ServiceResult<PresupuestoOperacion>.Ok(new PresupuestoOperacion(orden.Id), "Borrador actualizado. Debe enviarse para una nueva decisión.");
            });
        }

        public async Task<ServiceResult<PresupuestoOperacion>> EnviarAprobacionAsync(int presupuestoId,
            int usuarioSolicitanteId, IEnumerable<int>? evidenciaIds = null)
        {
            var ids = evidenciaIds?.ToList() ?? new List<int>();
            if (ids.Any(id => id <= 0) || ids.Count != ids.Distinct().Count())
                return ServiceResult<PresupuestoOperacion>.Error("Las evidencias deben tener identificadores positivos y no repetidos.");
            var ordenId = await OrdenIdAsync(presupuestoId);
            return await EjecutarAsync(ordenId, async orden =>
            {
                var usuario = await UsuarioAutorizadoAsync(usuarioSolicitanteId, "PRESUPUESTO_ENVIAR");
                if (usuario == null || !await TecnicoAsync(orden, usuario)) return ErrorAcceso();
                if (!Editable(orden)) return ErrorEstado();
                var presupuesto = await PresupuestoEditableAsync(presupuestoId);
                if (presupuesto == null || presupuesto.Estado != EstadoPresupuesto.Modificado)
                    return ServiceResult<PresupuestoOperacion>.Error("Sólo puede enviarse un presupuesto modificado.");
                if (presupuesto.Items.Count == 0 || presupuesto.Items.Any(i =>
                    string.IsNullOrWhiteSpace(i.Descripcion) || i.Cantidad <= 0 || i.PrecioUnitario < 0))
                    return ServiceResult<PresupuestoOperacion>.Error("El presupuesto debe contener ítems válidos.");
                if (ids.Count > 0 && await _context.Evidencias.CountAsync(e =>
                    ids.Contains(e.Id) && e.OrdenTrabajoId == orden.Id) != ids.Count)
                    return ServiceResult<PresupuestoOperacion>.Error("Alguna evidencia no existe o pertenece a otra orden.");
                var numero = (await _context.PresupuestoVersiones
                    .Where(v => v.PresupuestoId == presupuesto.Id)
                    .MaxAsync(v => (int?)v.NumeroVersion) ?? 0) + 1;
                var ahora = DateTime.Now;
                presupuesto.Total = presupuesto.CalcularTotal();
                if (presupuesto.Total < 0 || presupuesto.Total > 9999999999999999.99m)
                    return ServiceResult<PresupuestoOperacion>.Error("El total excede el importe admitido.");
                var version = new PresupuestoVersion
                {
                    PresupuestoId = presupuesto.Id, NumeroVersion = numero, Total = presupuesto.Total,
                    Decision = EstadoPresupuestoVersion.Pendiente, FechaEnvio = ahora,
                    EnviadaPorUsuarioId = usuario.Id,
                    Items = presupuesto.Items.Select(i => new PresupuestoVersionItem
                    {
                        Descripcion = i.Descripcion, Cantidad = i.Cantidad,
                        PrecioUnitario = i.PrecioUnitario, RepuestoId = i.RepuestoId
                    }).ToList(),
                    Evidencias = ids.Select(id => new PresupuestoVersionEvidencia { EvidenciaTrabajoId = id }).ToList()
                };
                _context.PresupuestoVersiones.Add(version);
                presupuesto.Estado = EstadoPresupuesto.Pendiente;
                presupuesto.MotivoRechazo = null;
                presupuesto.FechaUltimaModificacion = ahora;
                await CambiarEstadoAsync(orden, new EstadoEsperandoAprobacionHandler(), usuario, true);
                return ServiceResult<PresupuestoOperacion>.Ok(new PresupuestoOperacion(orden.Id), "Nueva versión enviada al cliente.");
            }, "El presupuesto está disponible para su aprobación.");
        }

        public Task<ServiceResult<PresupuestoOperacion>> AprobarAsync(int presupuestoVersionId, int usuarioSolicitanteId) =>
            DecidirAsync(presupuestoVersionId, usuarioSolicitanteId, true, null);

        public Task<ServiceResult<PresupuestoOperacion>> RechazarAsync(int presupuestoVersionId, int usuarioSolicitanteId, string motivo) =>
            DecidirAsync(presupuestoVersionId, usuarioSolicitanteId, false, motivo);

        private async Task<ServiceResult<PresupuestoOperacion>> DecidirAsync(int versionId, int usuarioId, bool aprobar, string? motivo)
        {
            var solicitante = await UsuarioAutorizadoAsync(usuarioId,
                aprobar ? "CLIENTE_PRESUPUESTO_APROBAR" : "CLIENTE_PRESUPUESTO_RECHAZAR");
            if (solicitante == null) return ErrorAcceso();
            if (!await _permisos.EsAdministradorAsync(usuarioId) &&
                !await _context.PresupuestoVersiones.AnyAsync(v => v.Id == versionId &&
                    v.Presupuesto.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId == solicitante.PersonaId))
                return ErrorAcceso();
            if (!aprobar && (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 1000))
                return ServiceResult<PresupuestoOperacion>.Error("Indique un motivo de rechazo de hasta 1000 caracteres.");
            var ordenId = await _context.PresupuestoVersiones.Where(v => v.Id == versionId)
                .Select(v => (int?)v.Presupuesto.OrdenTrabajoId).FirstOrDefaultAsync();
            return await EjecutarAsync(ordenId, async orden =>
            {
                var usuario = await UsuarioAutorizadoAsync(usuarioId,
                    aprobar ? "CLIENTE_PRESUPUESTO_APROBAR" : "CLIENTE_PRESUPUESTO_RECHAZAR");
                if (usuario == null || !await PropietarioAsync(orden, usuario)) return ErrorAcceso();
                var version = await _context.PresupuestoVersiones
                    .Include(v => v.Items).Include(v => v.Presupuesto).FirstOrDefaultAsync(v => v.Id == versionId);
                if (version == null || !Editable(orden) || orden.EstadoActual != EstadoOrden.EsperandoAprobacion ||
                    version.Decision != EstadoPresupuestoVersion.Pendiente ||
                    version.Presupuesto.Estado != EstadoPresupuesto.Pendiente ||
                    await _context.PresupuestoVersiones.AnyAsync(v =>
                        v.PresupuestoId == version.PresupuestoId && v.NumeroVersion > version.NumeroVersion))
                    return ServiceResult<PresupuestoOperacion>.Error("La versión ya fue decidida o no está vigente para decisión.");
                var mensaje = aprobar ? "Versión aprobada." : "Versión rechazada. La orden queda rechazada.";
                if (aprobar)
                {
                    if (version.Items.Count == 0) return ServiceResult<PresupuestoOperacion>.Error("La versión no contiene ítems.");
                    var stock = await _stock.ProcesarAprobacionIncrementalAsync(orden.Id, usuario.Id, version.Id);
                    if (!stock.Exitoso) return ServiceResult<PresupuestoOperacion>.Error(stock.Mensaje);
                    if (!string.IsNullOrEmpty(stock.Mensaje)) mensaje += " " + stock.Mensaje;
                }
                var ahora = DateTime.Now;
                version.Decision = aprobar ? EstadoPresupuestoVersion.Aprobado : EstadoPresupuestoVersion.Rechazado;
                version.FechaDecision = ahora;
                version.DecididaPorUsuarioId = usuario.Id;
                version.MotivoRechazo = aprobar ? null : motivo!.Trim();
                version.Presupuesto.Estado = aprobar ? EstadoPresupuesto.Aprobado : EstadoPresupuesto.Rechazado;
                version.Presupuesto.MotivoRechazo = version.MotivoRechazo;
                version.Presupuesto.FechaUltimaModificacion = ahora;
                await CambiarEstadoAsync(orden,
                    aprobar ? new EstadoAprobadoHandler() : new EstadoRechazadoHandler(), usuario, false);
                return ServiceResult<PresupuestoOperacion>.Ok(new PresupuestoOperacion(orden.Id), mensaje);
            }, aprobar ? "El presupuesto fue aprobado." : "El presupuesto fue rechazado. Se detiene la reparación.");
        }

        private async Task<ServiceResult<PresupuestoOperacion>> EjecutarAsync(int? ordenId,
            Func<OrdenTrabajo, Task<ServiceResult<PresupuestoOperacion>>> accion, string? evento = null)
        {
            if (!ordenId.HasValue) return ServiceResult<PresupuestoOperacion>.Error("Orden o presupuesto no encontrado.");
            ServiceResult<PresupuestoOperacion> resultado;
            OrdenTrabajo? orden;
            await using (var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                try
                {
                    // Todas las mutaciones del módulo bloquean primero la misma OT.
                    // UPDLOCK evita que dos lectores intenten convertir simultáneamente su bloqueo.
                    orden = await _context.OrdenesTrabajo.FromSqlInterpolated(
                        $"SELECT * FROM [OrdenesTrabajo] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {ordenId.Value}")
                        .FirstOrDefaultAsync();
                    if (orden == null) return ServiceResult<PresupuestoOperacion>.Error("Orden no encontrada.");
                    await _context.Entry(orden).ReloadAsync();
                    await _context.Entry(orden).Reference(o => o.Factura).LoadAsync();
                    await _context.Entry(orden).Reference(o => o.IngresoVehiculo).LoadAsync();
                    await _context.Entry(orden.IngresoVehiculo).Reference(i => i.Turno).LoadAsync();
                    resultado = await accion(orden);
                    if (!resultado.Exitoso)
                    {
                        await tx.RollbackAsync();
                        _context.ChangeTracker.Clear();
                        return resultado;
                    }
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (Exception ex) when (EsConflicto(ex))
                {
                    await tx.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    return ServiceResult<PresupuestoOperacion>.Error("La operación coincidió con otro cambio. Actualice la pantalla y revise el estado antes de reintentar.");
                }
                catch
                {
                    await tx.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    throw;
                }
            }
            if (evento != null)
            {
                try { await _observer.NotificarAsync(orden, evento); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Operación confirmada en OT {OrdenId}; falló la notificación.", orden.Id);
                    resultado.Mensaje += " El cambio fue guardado, pero no pudo completarse la notificación.";
                }
            }
            return resultado;
        }

        private static bool EsConflicto(Exception ex) =>
            ex is SqlException sql && sql.Number is 1205 or 1222 or 2601 or 2627 ||
            ex.InnerException != null && EsConflicto(ex.InnerException);

        private Task<int?> OrdenIdAsync(int presupuestoId) => _context.Presupuestos
            .Where(p => p.Id == presupuestoId).Select(p => (int?)p.OrdenTrabajoId).FirstOrDefaultAsync();

        private Task<Presupuesto?> PresupuestoEditableAsync(int id) =>
            _context.Presupuestos.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);

        private Task<OrdenTrabajo?> OrdenConsultaAsync(int id) => _context.OrdenesTrabajo.AsNoTracking()
            .Include(o => o.IngresoVehiculo).ThenInclude(i => i.Turno).ThenInclude(t => t.Cliente)
            .Include(o => o.IngresoVehiculo).ThenInclude(i => i.Turno).ThenInclude(t => t.Vehiculo)
            .Include(o => o.Factura).FirstOrDefaultAsync(o => o.Id == id);

        private async Task<Usuario?> UsuarioAutorizadoAsync(int id, string patente)
        {
            if (!await _permisos.TienePermisoAsync(id, patente)) return null;
            return await _context.Usuarios.AsNoTracking().Include(u => u.Persona)
                .FirstOrDefaultAsync(u => u.Id == id && u.Activo && u.Persona.Activo);
        }

        private async Task<bool> TecnicoAsync(OrdenTrabajo orden, Usuario usuario) =>
            await _permisos.EsAdministradorAsync(usuario.Id) ||
            orden.MecanicoId == usuario.PersonaId && await _context.PersonaRoles.AnyAsync(pr =>
                pr.PersonaId == usuario.PersonaId && pr.Persona.Activo && pr.FechaBaja == null &&
                pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.MECANICO);

        private async Task<bool> PropietarioAsync(OrdenTrabajo orden, Usuario usuario) =>
            await _permisos.EsAdministradorAsync(usuario.Id) ||
            orden.IngresoVehiculo.Turno.ClienteId == usuario.PersonaId;

        private static bool Editable(OrdenTrabajo orden) =>
            !orden.FechaFin.HasValue && orden.Factura == null &&
            orden.EstadoActual is EstadoOrden.Diagnostico or EstadoOrden.EsperandoAprobacion or
                EstadoOrden.Aprobado or EstadoOrden.EnReparacion or EstadoOrden.Rechazado;

        private async Task CambiarEstadoAsync(OrdenTrabajo orden, IEstadoOrdenHandler handler, Usuario usuario, bool tecnico)
        {
            _estados.CambiarEstado(orden, handler);
            orden.HistorialEstados.Last().MecanicoId =
                tecnico && !await _permisos.EsAdministradorAsync(usuario.Id) ? usuario.PersonaId : null;
        }

        private static ServiceResult<PresupuestoOperacion> ErrorAcceso() => ServiceResult<PresupuestoOperacion>.Error("No tiene permisos sobre este presupuesto.");
        private static ServiceResult<PresupuestoOperacion> ErrorEstado() => ServiceResult<PresupuestoOperacion>.Error("La orden no admite esta operación: revise estado, cierre y factura.");
    }
}
