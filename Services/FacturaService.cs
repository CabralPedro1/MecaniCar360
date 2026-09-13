using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Patterns.Strategy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class FacturaService
    {
        private readonly MecaniCarContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly PermisoService _permisos;
        public FacturaService(MecaniCarContext context, PermisoService permisos, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _permisos = permisos;
        }

        public async Task<ServiceResult<List<Factura>>> ListarAsync(int usuarioId)
        {
            if (await UsuarioAsync(usuarioId, "FACTURA_VER") == null)
                return ServiceResult<List<Factura>>.Error("No tiene permiso para consultar facturas.");
            return ServiceResult<List<Factura>>.Ok(await Consulta()
                .OrderByDescending(f => f.FechaEmision).ThenByDescending(f => f.Id).ToListAsync());
        }

        public async Task<ServiceResult<List<Factura>>> MisFacturasAsync(int usuarioId)
        {
            var usuario = await UsuarioAsync(usuarioId, "CLIENTE_FACTURA_VER");
            if (usuario == null) return ServiceResult<List<Factura>>.Error("No tiene permiso para consultar sus facturas.");
            return ServiceResult<List<Factura>>.Ok(await Consulta()
                .Where(f => f.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId == usuario.PersonaId)
                .OrderByDescending(f => f.FechaEmision).ThenByDescending(f => f.Id).ToListAsync());
        }

        public Task<ServiceResult<Factura>> ObtenerAsync(int ordenTrabajoId, int usuarioId) =>
            ConsultarAsync(Consulta().Where(f => f.OrdenTrabajoId == ordenTrabajoId), usuarioId, "FACTURA_VER");

        public Task<ServiceResult<Factura>> ObtenerPorIdAsync(int facturaId, int usuarioId) =>
            ConsultarAsync(Consulta().Where(f => f.Id == facturaId), usuarioId, "FACTURA_VER");

        public Task<ServiceResult<Factura>> ObtenerPropiaAsync(int facturaId, int usuarioId) =>
            ConsultarAsync(Consulta().Where(f => f.Id == facturaId), usuarioId, "CLIENTE_FACTURA_VER", true);

        public Task<ServiceResult<Factura>> ObtenerParaPagoAsync(int facturaId, int usuarioId) =>
            ConsultarAsync(Consulta().Where(f => f.Id == facturaId), usuarioId, "PAGO_REGISTRAR");

        public Task<ServiceResult<Factura>> ObtenerPagosAsync(int facturaId, int usuarioId) =>
            ConsultarAsync(Consulta().Where(f => f.Id == facturaId), usuarioId, "PAGO_VER");

        private async Task<ServiceResult<Factura>> ConsultarAsync(IQueryable<Factura> consulta,
            int usuarioId, string patente, bool propia = false)
        {
            var usuario = await UsuarioAsync(usuarioId, patente);
            if (usuario == null) return ServiceResult<Factura>.Error("No tiene acceso a esta factura.");
            if (propia)
                consulta = consulta.Where(f => f.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId == usuario.PersonaId);
            // El cliente ve los pagos de su factura; el personal requiere permiso de pagos.
            if (propia || patente is "PAGO_VER" or "PAGO_REGISTRAR" ||
                await _permisos.TienePermisoAsync(usuarioId, "PAGO_VER") ||
                await _permisos.TienePermisoAsync(usuarioId, "PAGO_REGISTRAR"))
                consulta = consulta.Include(f => f.Pagos).ThenInclude(p => p.RegistradoPorUsuario);
            var factura = await consulta.Include(f => f.Items).FirstOrDefaultAsync();
            return factura == null ? ServiceResult<Factura>.Error("No tiene acceso a esta factura o no existe.")
                : ServiceResult<Factura>.Ok(factura);
        }

        public async Task<ServiceResult<Factura>> EmitirAsync(int ordenTrabajoId, int usuarioId)
        {
            if (await UsuarioAsync(usuarioId, "FACTURA_CREAR") == null)
                return ServiceResult<Factura>.Error("No tiene permiso para emitir facturas.");
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var orden = await _context.OrdenesTrabajo.FromSqlInterpolated(
                    $"SELECT * FROM [OrdenesTrabajo] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {ordenTrabajoId}")
                    .FirstOrDefaultAsync();
                if (orden == null) return ServiceResult<Factura>.Error("Orden no encontrada.");
                await _context.Entry(orden).ReloadAsync();
                if (orden.EstadoActual is not (EstadoOrden.Finalizado or EstadoOrden.Rechazado))
                    return ServiceResult<Factura>.Error("La orden debe estar finalizada o rechazada.");
                if (await _context.Facturas.AnyAsync(f => f.OrdenTrabajoId == orden.Id))
                    return ServiceResult<Factura>.Error("La orden ya tiene una factura.");
                var factura = new Factura
                {
                    OrdenTrabajoId = orden.Id, FechaEmision = DateTime.Now,
                    Estado = EstadoFactura.Emitida,
                    NumeroFactura = $"FAC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"
                };
                if (orden.EstadoActual == EstadoOrden.Rechazado)
                {
                    if (!orden.CostoDiagnostico.HasValue || !ImporteValido(orden.CostoDiagnostico.Value))
                        return ServiceResult<Factura>.Error("Debe establecer un costo de diagnóstico/revisión válido desde la orden.");
                    factura.Total = orden.CostoDiagnostico.Value;
                    factura.Observaciones = "Cobro correspondiente al diagnóstico/revisión.";
                    factura.Items.Add(new FacturaItem
                    {
                        Descripcion = "Diagnóstico/revisión del vehículo", Cantidad = 1,
                        PrecioUnitario = factura.Total
                    });
                }
                else
                {
                    var presupuesto = await _context.Presupuestos.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.OrdenTrabajoId == orden.Id);
                    if (presupuesto == null || presupuesto.Estado != EstadoPresupuesto.Aprobado)
                        return ServiceResult<Factura>.Error("El presupuesto vigente no está aprobado.");
                    var version = await _context.PresupuestoVersiones.AsNoTracking().Include(v => v.Items)
                        .Where(v => v.PresupuestoId == presupuesto.Id)
                        .OrderByDescending(v => v.NumeroVersion).FirstOrDefaultAsync();
                    if (version == null || version.Decision != EstadoPresupuestoVersion.Aprobado)
                        return ServiceResult<Factura>.Error("La última versión enviada debe estar aprobada.");
                    if (!ImporteValido(version.Total) || version.Items.Count == 0 ||
                        version.Items.Any(i => i.Cantidad <= 0 || i.PrecioUnitario < 0 ||
                            string.IsNullOrWhiteSpace(i.Descripcion)) ||
                        version.Items.Sum(i => i.Subtotal) != version.Total)
                        return ServiceResult<Factura>.Error("La versión aprobada tiene importes o conceptos inconsistentes.");
                    factura.Total = version.Total;
                    factura.PresupuestoOrigenId = presupuesto.Id;
                    factura.PresupuestoVersionOrigenId = version.Id;
                    factura.Observaciones = $"Reparación según presupuesto versión {version.NumeroVersion}.";
                    factura.Items = version.Items.Select(i => new FacturaItem
                    {
                        Descripcion = i.Descripcion, Cantidad = i.Cantidad, PrecioUnitario = i.PrecioUnitario
                    }).ToList();
                }
                _context.Facturas.Add(factura);
                await _context.SaveChangesAsync();
                _auditoria.RegistrarOperacion("FACTURA_EMITIDA", "Factura", factura.Id, usuarioId, $"Orden #{orden.Id}; factura #{factura.Id}.");
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return ServiceResult<Factura>.Ok(factura, "Factura emitida. Puede registrar el pago.");
            }
            catch (Exception ex) when (Conflicto(ex))
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                return ServiceResult<Factura>.Error("La orden fue modificada o ya se emitió la factura. Actualice la pantalla.");
            }
            catch
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        public async Task<ServiceResult<Factura>> RegistrarPagoAsync(int facturaId, decimal monto,
            MetodoPago metodoPago, int usuarioId, decimal saldoEsperado)
        {
            if (await UsuarioAsync(usuarioId, "PAGO_REGISTRAR") == null)
                return ServiceResult<Factura>.Error("No tiene permiso para registrar pagos.");
            if (!Enum.IsDefined(metodoPago)) return ServiceResult<Factura>.Error("Método de pago inválido.");
            if (!ImporteValido(monto)) return ServiceResult<Factura>.Error("Ingrese un monto positivo con hasta dos decimales.");
            var ordenId = await _context.Facturas.Where(f => f.Id == facturaId)
                .Select(f => (int?)f.OrdenTrabajoId).FirstOrDefaultAsync();
            if (!ordenId.HasValue) return ServiceResult<Factura>.Error("Factura no encontrada.");
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                // Mismo orden de bloqueo que emisión/Presupuesto; la factura queda bloqueada
                // hasta confirmar el pago, impidiendo sobrepagos concurrentes.
                var orden = await _context.OrdenesTrabajo.FromSqlInterpolated(
                    $"SELECT * FROM [OrdenesTrabajo] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {ordenId.Value}")
                    .FirstAsync();
                var factura = await _context.Facturas.FromSqlInterpolated(
                    $"SELECT * FROM [Facturas] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {facturaId}")
                    .FirstAsync();
                await _context.Entry(factura).ReloadAsync();
                if (factura.Estado == EstadoFactura.Anulada)
                    return ServiceResult<Factura>.Error("No se puede cobrar una factura anulada.");
                var pagado = await _context.Pagos.Where(p => p.FacturaId == facturaId && p.Estado == EstadoPago.Pagado)
                    .SumAsync(p => (decimal?)p.Monto) ?? 0;
                var saldo = factura.Total - pagado;
                if (saldo <= 0 || factura.Estado == EstadoFactura.Pagada)
                    return ServiceResult<Factura>.Error("La factura ya está pagada.");
                if (saldoEsperado != saldo)
                    return ServiceResult<Factura>.Error("El saldo cambió. Revise los pagos antes de volver a registrar.");
                if (monto > saldo) return ServiceResult<Factura>.Error("El monto supera el saldo pendiente.");
                var pago = new Pago
                {
                    FacturaId = factura.Id, Monto = monto, MetodoPago = metodoPago,
                    FechaPago = DateTime.Now, Estado = EstadoPago.Pendiente,
                    RegistradoPorUsuarioId = usuarioId
                };
                // Proyecto académico: las estrategias no procesan cobros externos.
                await PagoStrategyFactory.Crear(metodoPago).Procesar(pago);
                if (pago.Estado != EstadoPago.Pagado)
                    return ServiceResult<Factura>.Error("El pago no fue confirmado.");
                _context.Pagos.Add(pago);
                factura.Estado = pagado + monto >= factura.Total ? EstadoFactura.Pagada : EstadoFactura.Emitida;
                await _context.SaveChangesAsync();
                _auditoria.RegistrarOperacion("PAGO_REGISTRADO", "Pago", pago.Id, usuarioId, $"Orden #{orden.Id}; factura #{factura.Id}.");
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return ServiceResult<Factura>.Ok(factura,
                    factura.Estado == EstadoFactura.Pagada ? "Pago registrado. Factura pagada." : "Pago parcial registrado.");
            }
            catch (Exception ex) when (Conflicto(ex))
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                return ServiceResult<Factura>.Error("Otro usuario actualizó la factura. Revise el saldo y el historial antes de reintentar.");
            }
            catch
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        private IQueryable<Factura> Consulta() => _context.Facturas.AsNoTracking()
            .Include(f => f.OrdenTrabajo).ThenInclude(o => o.IngresoVehiculo).ThenInclude(i => i.Turno).ThenInclude(t => t.Cliente)
            .Include(f => f.OrdenTrabajo).ThenInclude(o => o.IngresoVehiculo).ThenInclude(i => i.Turno).ThenInclude(t => t.Vehiculo);

        private async Task<Usuario?> UsuarioAsync(int id, string patente)
        {
            if (!await _permisos.TienePermisoAsync(id, patente)) return null;
            return await _context.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && u.Activo && u.Persona.Activo);
        }

        private static bool ImporteValido(decimal monto) =>
            monto > 0 && monto <= 9999999999999999.99m && decimal.Round(monto, 2) == monto;

        private static bool Conflicto(Exception ex) =>
            ex is SqlException sql && sql.Number is 1205 or 1222 or 2601 or 2627 ||
            ex.InnerException != null && Conflicto(ex.InnerException);
    }
}
