using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Patterns.Strategy;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class FacturaService
    {
        private readonly MecaniCarContext _context;

        public FacturaService(MecaniCarContext context)
        {
            _context = context;
        }


        // =====================================
        // REGISTRAR PAGO
        // =====================================

        public async Task<ServiceResult<Factura>> RegistrarPagoAsync(
            int ordenTrabajoId,
            MetodoPago metodoPago,
            int usuarioId)
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                // =====================================
                // BUSCAR ORDEN
                // =====================================

                var orden = await _context.OrdenesTrabajo
                    .Include(o => o.Presupuesto)
                        .ThenInclude(p => p!.Items)

                    .Include(o => o.Factura)
                        .ThenInclude(f => f!.Pagos)

                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

                if (orden == null)
                {
                    return ServiceResult<Factura>.Error(
                        "Orden de trabajo no encontrada.");
                }


                // =====================================
                // VERIFICAR ESTADO DE LA ORDEN
                // =====================================

                if (orden.EstadoActual != EstadoOrden.Finalizado &&
                    orden.EstadoActual != EstadoOrden.Rechazado)
                {
                    return ServiceResult<Factura>.Error(
                        "La orden todavía no está lista para ser cobrada.");
                }


                // =====================================
                // EVITAR FACTURA DUPLICADA
                // =====================================

                if (orden.Factura != null)
                {
                    return ServiceResult<Factura>.Error(
                        "La orden ya posee una factura.");
                }


                decimal total;


                // =====================================
                // ORDEN RECHAZADA
                // COBRAR DIAGNÓSTICO
                // =====================================

                if (orden.EstadoActual ==
                    EstadoOrden.Rechazado)
                {
                    if (!orden.CostoDiagnostico.HasValue ||
                        orden.CostoDiagnostico.Value <= 0)
                    {
                        return ServiceResult<Factura>.Error(
                            "La orden no tiene un costo de diagnóstico válido.");
                    }

                    total =
                        orden.CostoDiagnostico.Value;
                }


                // =====================================
                // ORDEN FINALIZADA
                // COBRAR REPARACIÓN
                // =====================================

                else
                {
                    if (orden.Presupuesto == null)
                    {
                        return ServiceResult<Factura>.Error(
                            "La orden no tiene un presupuesto asociado.");
                    }

                    if (orden.Presupuesto.Estado !=
                        EstadoPresupuesto.Aprobado)
                    {
                        return ServiceResult<Factura>.Error(
                            "El presupuesto no está aprobado.");
                    }

                    total =
                        orden.Presupuesto.Total;

                    if (total <= 0)
                    {
                        return ServiceResult<Factura>.Error(
                            "El presupuesto no tiene un total válido.");
                    }
                }


                // =====================================
                // CREAR FACTURA
                // =====================================

                var factura = new Factura
                {
                    OrdenTrabajoId =
                        orden.Id,

                    Total =
                        total,

                    FechaEmision =
                        DateTime.Now,

                    Estado =
                        EstadoFactura.Emitida,

                    NumeroFactura =
                        GenerarNumeroFactura(),

                    Observaciones =
                        orden.EstadoActual ==
                        EstadoOrden.Rechazado
                            ? "Cobro correspondiente al diagnóstico."
                            : "Cobro correspondiente a la reparación."
                };


                // =====================================
                // PRESUPUESTO ORIGEN
                // =====================================

                if (orden.EstadoActual ==
                    EstadoOrden.Finalizado)
                {
                    factura.PresupuestoOrigenId =
                        orden.Presupuesto!.Id;
                }
                else
                {
                    factura.PresupuestoOrigenId =
                        null;
                }


                // =====================================
                // ITEMS DE FACTURA
                // =====================================

                if (orden.EstadoActual ==
                    EstadoOrden.Rechazado)
                {
                    factura.Items.Add(
                        new FacturaItem
                        {
                            Descripcion =
                                "Diagnóstico del vehículo",

                            Cantidad =
                                1,

                            PrecioUnitario =
                                total
                        });
                }
                else
                {
                    foreach (var item in
                        orden.Presupuesto!.Items)
                    {
                        factura.Items.Add(
                            new FacturaItem
                            {
                                Descripcion =
                                    item.Descripcion,

                                Cantidad =
                                    item.Cantidad,

                                PrecioUnitario =
                                    item.PrecioUnitario
                            });
                    }
                }


                // =====================================
                // GUARDAR FACTURA
                // =====================================

                _context.Facturas.Add(
                    factura);

                await _context.SaveChangesAsync();


                // =====================================
                // CREAR PAGO
                // =====================================

                var pago = new Pago
                {
                    FacturaId =
                        factura.Id,

                    Monto =
                        total,

                    FechaPago =
                        DateTime.Now,

                    MetodoPago =
                        metodoPago,

                    Estado =
                        EstadoPago.Pendiente,

                    RegistradoPorUsuarioId =
                        usuarioId
                };


                // =====================================
                // STRATEGY
                // =====================================

                var estrategia =
                    PagoStrategyFactory.Crear(
                        metodoPago);

                await estrategia.Procesar(
                    pago);


                // =====================================
                // VERIFICAR RESULTADO DEL PAGO
                // =====================================

                if (pago.Estado !=
                    EstadoPago.Pagado)
                {
                    return ServiceResult<Factura>.Error(
                        "El pago no pudo ser aprobado.");
                }


                // =====================================
                // AGREGAR PAGO
                // =====================================

                _context.Pagos.Add(
                    pago);


                // =====================================
                // MARCAR FACTURA COMO PAGADA
                // =====================================

                factura.Estado =
                    EstadoFactura.Pagada;


                await _context.SaveChangesAsync();


                // =====================================
                // CONFIRMAR TRANSACCIÓN
                // =====================================

                await transaction.CommitAsync();

                return ServiceResult<Factura>.Ok(
                    factura,
                    $"Pago de {total:C} registrado y factura emitida correctamente.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // =====================================
        // OBTENER FACTURA POR ORDEN
        // =====================================

        public async Task<ServiceResult<Factura>> ObtenerAsync(
            int ordenTrabajoId)
        {
            var factura =
                await _context.Facturas

                    .Include(f => f.Items)

                    .Include(f => f.Pagos)
                        .ThenInclude(p =>
                            p.RegistradoPorUsuario)

                    .Include(f => f.OrdenTrabajo)
                        .ThenInclude(o => o.Turno)
                            .ThenInclude(t => t.Cliente)

                    .FirstOrDefaultAsync(f =>
                        f.OrdenTrabajoId ==
                        ordenTrabajoId);

            if (factura == null)
            {
                return ServiceResult<Factura>.Error(
                    "La orden todavía no tiene una factura.");
            }

            return ServiceResult<Factura>.Ok(
                factura);
        }


        // =====================================
        // OBTENER FACTURA POR ID
        // =====================================

        public async Task<ServiceResult<Factura>> ObtenerPorIdAsync(
            int id)
        {
            var factura =
                await _context.Facturas

                    .Include(f => f.Items)

                    .Include(f => f.Pagos)
                        .ThenInclude(p =>
                            p.RegistradoPorUsuario)

                    .Include(f => f.OrdenTrabajo)
                        .ThenInclude(o => o.Turno)
                            .ThenInclude(t => t.Cliente)

                    .FirstOrDefaultAsync(f =>
                        f.Id == id);

            if (factura == null)
            {
                return ServiceResult<Factura>.Error(
                    "Factura no encontrada.");
            }

            return ServiceResult<Factura>.Ok(
                factura);
        }


        // =====================================
        // OBTENER PAGOS
        // =====================================

        public async Task<ServiceResult<List<Pago>>> ObtenerPagosAsync(
            int facturaId)
        {
            var pagos = await _context.Pagos
                .Include(p =>
                    p.RegistradoPorUsuario)

                .Where(p =>
                    p.FacturaId == facturaId)

                .OrderByDescending(p =>
                    p.FechaPago)

                .ToListAsync();

            return ServiceResult<List<Pago>>.Ok(
                pagos);
        }


        // =====================================
        // GENERAR NÚMERO DE FACTURA
        // =====================================

        private string GenerarNumeroFactura()
        {
            return
                $"FAC-{DateTime.Now:yyyyMMddHHmmssfff}";
        }
    }
}