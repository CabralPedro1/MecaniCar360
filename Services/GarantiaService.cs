using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class GarantiaService
    {
        private readonly MecaniCarContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly PermisoService _permisos;

        public GarantiaService(MecaniCarContext context, PermisoService permisos, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _permisos = permisos;
        }

        public async Task<ServiceResult<Garantia>> CrearAsync(
            int ordenTrabajoId, int usuarioId, List<GarantiaItemDto> items)
        {
            if (!await UsuarioAutorizadoAsync(usuarioId, "GARANTIA_CREAR"))
                return ServiceResult<Garantia>.Error("Acceso denegado.");
            if (ordenTrabajoId <= 0)
                return ServiceResult<Garantia>.Error("La orden de trabajo no es válida.");
            if (items == null || items.Count == 0)
                return ServiceResult<Garantia>.Error("Debe seleccionar al menos un ítem.");
            if (items.Any(i => i == null || i.FacturaItemId <= 0))
                return ServiceResult<Garantia>.Error("Los identificadores de ítems deben ser positivos.");
            if (items.Select(i => i.FacturaItemId).Distinct().Count() != items.Count)
                return ServiceResult<Garantia>.Error("No se puede repetir un ítem de factura.");
            if (items.Any(i => i.MesesGarantia < 1 || i.MesesGarantia > 120))
                return ServiceResult<Garantia>.Error("La garantía debe estar entre 1 y 120 meses.");
            if (items.Any(i => i.Observaciones?.Length > 1000))
                return ServiceResult<Garantia>.Error("Las observaciones no pueden superar los 1000 caracteres.");

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                // Emisión, pago, entrega y garantía bloquean primero la misma OT.
                var orden = await _context.OrdenesTrabajo.FromSqlInterpolated(
                    $"SELECT * FROM [OrdenesTrabajo] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {ordenTrabajoId}")
                    .FirstOrDefaultAsync();
                if (orden == null) return ServiceResult<Garantia>.Error("Orden de trabajo no encontrada.");
                await _context.Entry(orden).ReloadAsync();
                if (await _context.Garantias.AnyAsync(g => g.OrdenTrabajoId == orden.Id))
                    return ServiceResult<Garantia>.Error("La orden ya posee una garantía, aunque esté anulada.");
                if (orden.EstadoActual != EstadoOrden.Entregado || !orden.FechaFin.HasValue)
                    return ServiceResult<Garantia>.Error("La reparación debe estar finalizada y entregada.");
                var estadoPrevio = await _context.Set<OrdenTrabajoEstadoHistorial>().AsNoTracking()
                    .Where(h => h.OrdenTrabajoId == orden.Id && h.Estado != EstadoOrden.Entregado)
                    .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Id)
                    .Select(h => (EstadoOrden?)h.Estado).FirstOrDefaultAsync();
                if (estadoPrevio != EstadoOrden.Finalizado)
                    return ServiceResult<Garantia>.Error("La entrega no corresponde a una reparación finalizada.");
                var ingreso = await _context.IngresosVehiculo.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == orden.IngresoVehiculoId);
                if (ingreso?.FechaEgreso == null)
                    return ServiceResult<Garantia>.Error("La orden no tiene una entrega efectiva registrada.");
                var factura = await _context.Facturas.AsNoTracking()
                    .Include(f => f.Items).Include(f => f.Pagos)
                    .Include(f => f.PresupuestoVersionOrigen).ThenInclude(v => v!.Presupuesto)
                    .FirstOrDefaultAsync(f => f.OrdenTrabajoId == orden.Id);
                if (factura == null || factura.Estado != EstadoFactura.Pagada || factura.Total <= 0 ||
                    factura.Pagos.Where(p => p.Estado == EstadoPago.Pagado).Sum(p => p.Monto) < factura.Total)
                    return ServiceResult<Garantia>.Error("La factura debe estar completamente pagada.");
                var version = factura.PresupuestoVersionOrigen;
                if (!factura.PresupuestoVersionOrigenId.HasValue || version == null ||
                    version.Decision != EstadoPresupuestoVersion.Aprobado || version.Presupuesto.OrdenTrabajoId != orden.Id)
                    return ServiceResult<Garantia>.Error("No corresponde garantía para el cobro de diagnóstico/rechazo.");
                var idsFactura = factura.Items.Select(i => i.Id).ToHashSet();
                if (items.Any(i => !idsFactura.Contains(i.FacturaItemId)))
                    return ServiceResult<Garantia>.Error("Los ítems deben pertenecer a la factura de esta orden.");
                var inicio = ingreso.FechaEgreso.Value;
                DateTime fin;
                try { fin = items.Max(i => inicio.AddMonths(i.MesesGarantia)); }
                catch (ArgumentOutOfRangeException)
                {
                    return ServiceResult<Garantia>.Error("La fecha de cobertura excede el rango permitido.");
                }
                var garantia = new Garantia
                {
                    OrdenTrabajoId = orden.Id, CreadaPorUsuarioId = usuarioId,
                    FechaInicio = inicio, FechaFin = fin, Activa = true,
                    Items = items.Select(i => new GarantiaItem
                    {
                        FacturaItemId = i.FacturaItemId, MesesGarantia = i.MesesGarantia,
                        Observaciones = string.IsNullOrWhiteSpace(i.Observaciones) ? null : i.Observaciones.Trim()
                    }).ToList()
                };
                _context.Garantias.Add(garantia);
                await _context.SaveChangesAsync();
                _auditoria.RegistrarOperacion("GARANTIA_CREADA", "Garantia", garantia.Id, usuarioId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return ServiceResult<Garantia>.Ok(garantia);
            }
            catch (Exception ex) when (EsConflicto(ex))
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                return ServiceResult<Garantia>.Error("La operación coincidió con otro cambio. Revise si la garantía ya existe.");
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        public async Task<ServiceResult> AnularAsync(int garantiaId, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "GARANTIA_ANULAR"))
                return ServiceResult.Error("Acceso denegado.");
            if (garantiaId <= 0) return ServiceResult.Error("Identificador de garantía inválido.");
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var garantia = await _context.Garantias.FromSqlInterpolated(
                    $"SELECT * FROM [Garantias] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {garantiaId}")
                    .FirstOrDefaultAsync();
                if (garantia == null) return ServiceResult.Error("Garantía no encontrada.");
                await _context.Entry(garantia).ReloadAsync();
                if (!garantia.Activa) return ServiceResult.Error("La garantía ya se encuentra anulada.");
                garantia.Activa = false;
                _auditoria.RegistrarOperacion("GARANTIA_ANULADA", "Garantia", garantia.Id, usuarioSolicitanteId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return ServiceResult.Ok("Garantía anulada correctamente.");
            }
            catch (Exception ex) when (EsConflicto(ex))
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                return ServiceResult.Error("La operación coincidió con otro cambio. Revise si la garantía ya fue anulada.");
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        public async Task<ServiceResult<Garantia>> ObtenerAsync(int garantiaId, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "GARANTIA_VER"))
                return ServiceResult<Garantia>.Error("Acceso denegado.");
            var garantia = await Consulta().FirstOrDefaultAsync(g => g.Id == garantiaId);
            return garantia == null ? ServiceResult<Garantia>.Error("Garantía no encontrada.") : ServiceResult<Garantia>.Ok(garantia);
        }

        public async Task<ServiceResult<List<Garantia>>> ObtenerTodasAsync(int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "GARANTIA_VER"))
                return ServiceResult<List<Garantia>>.Error("Acceso denegado.");
            return ServiceResult<List<Garantia>>.Ok(await Consulta().OrderByDescending(g => g.FechaInicio).ToListAsync());
        }

        public async Task<ServiceResult<List<Garantia>>> ObtenerPorVehiculoAsync(int vehiculoId, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "GARANTIA_VER"))
                return ServiceResult<List<Garantia>>.Error("Acceso denegado.");
            return ServiceResult<List<Garantia>>.Ok(await Consulta()
                .Where(g => g.OrdenTrabajo.IngresoVehiculo.Turno.VehiculoId == vehiculoId)
                .OrderByDescending(g => g.FechaInicio).ToListAsync());
        }

        public async Task<ServiceResult<List<Garantia>>> ObtenerPorClienteAsync(int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "GARANTIA_VER_PROPIA"))
                return ServiceResult<List<Garantia>>.Error("Acceso denegado.");
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue) return ServiceResult<List<Garantia>>.Error("Acceso denegado.");
            return ServiceResult<List<Garantia>>.Ok(await Consulta()
                .Where(g => g.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId == personaId.Value)
                .OrderByDescending(g => g.FechaInicio).ToListAsync());
        }

        public async Task<ServiceResult<Garantia>> ObtenerPropiaAsync(int garantiaId, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "GARANTIA_VER_PROPIA"))
                return ServiceResult<Garantia>.Error("Acceso denegado.");
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue) return ServiceResult<Garantia>.Error("Acceso denegado.");
            var garantia = await Consulta().FirstOrDefaultAsync(g => g.Id == garantiaId &&
                g.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId == personaId.Value);
            return garantia == null ? ServiceResult<Garantia>.Error("Garantía no encontrada.") : ServiceResult<Garantia>.Ok(garantia);
        }

        public async Task<ServiceResult> EstaVigenteAsync(int garantiaId, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "GARANTIA_VER"))
                return ServiceResult.Error("Acceso denegado.");
            var garantia = await _context.Garantias.AsNoTracking().FirstOrDefaultAsync(g => g.Id == garantiaId);
            if (garantia == null) return ServiceResult.Error("Garantía no encontrada.");
            return Vigencia(garantia, garantia.FechaFin);
        }

        public async Task<ServiceResult> ItemEstaCubiertoAsync(int garantiaItemId, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "GARANTIA_VER"))
                return ServiceResult.Error("Acceso denegado.");
            var item = await _context.GarantiaItems.AsNoTracking().Include(i => i.Garantia)
                .Include(i => i.FacturaItem).ThenInclude(i => i.Factura)
                .FirstOrDefaultAsync(i => i.Id == garantiaItemId);
            if (item == null) return ServiceResult.Error("Ítem de garantía no encontrado.");
            if (item.FacturaItem.Factura.OrdenTrabajoId != item.Garantia.OrdenTrabajoId ||
                !item.FacturaItem.Factura.PresupuestoVersionOrigenId.HasValue ||
                item.MesesGarantia < 1 || item.MesesGarantia > 120)
                return ServiceResult.Error("El ítem no tiene una cobertura válida.");
            try
            {
                var fin = item.Garantia.FechaInicio.AddMonths(item.MesesGarantia);
                return Vigencia(item.Garantia, fin < item.Garantia.FechaFin ? fin : item.Garantia.FechaFin);
            }
            catch (ArgumentOutOfRangeException)
            {
                return ServiceResult.Error("La fecha de cobertura excede el rango permitido.");
            }
        }

        private static ServiceResult Vigencia(Garantia garantia, DateTime fin)
        {
            var ahora = DateTime.Now;
            if (!garantia.Activa) return ServiceResult.Error("La garantía se encuentra anulada.");
            if (ahora < garantia.FechaInicio) return ServiceResult.Error("La cobertura todavía no comenzó.");
            if (ahora > fin) return ServiceResult.Error("La cobertura se encuentra vencida.");
            return ServiceResult.Ok("La cobertura se encuentra vigente.");
        }

        private IQueryable<Garantia> Consulta() => _context.Garantias.AsNoTracking()
            .Include(g => g.Items).ThenInclude(i => i.FacturaItem)
            .Include(g => g.CreadaPorUsuario).ThenInclude(u => u.Persona)
            .Include(g => g.OrdenTrabajo).ThenInclude(o => o.IngresoVehiculo).ThenInclude(i => i.Turno).ThenInclude(t => t.Cliente)
            .Include(g => g.OrdenTrabajo).ThenInclude(o => o.IngresoVehiculo).ThenInclude(i => i.Turno).ThenInclude(t => t.Vehiculo);

        private async Task<bool> UsuarioAutorizadoAsync(int id, string patente) => id > 0 &&
            await _permisos.TienePermisoAsync(id, patente) &&
            await _context.Usuarios.AsNoTracking().AnyAsync(u => u.Id == id && u.Activo && u.Persona.Activo);

        private static bool EsConflicto(Exception ex) =>
            ex is SqlException sql && sql.Number is 1205 or 1222 or 2601 or 2627 ||
            ex.InnerException != null && EsConflicto(ex.InnerException);
    }
}
