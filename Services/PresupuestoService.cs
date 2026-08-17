using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Patterns.Memento;
using MecaniCar360.Patterns.Observer;
using MecaniCar360.Patterns.State;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class PresupuestoService
    {

        private readonly MecaniCarContext _context;
        private readonly OrdenSubject _ordenSubject;
        private readonly OrdenStateService _ordenStateService;
        private readonly StockService _stockService;

        public PresupuestoService(
    MecaniCarContext context,
    OrdenSubject ordenSubject,
    OrdenStateService ordenStateService,
    StockService stockService)
        {
            _context = context;
            _ordenSubject = ordenSubject;
            _ordenStateService = ordenStateService;
            _stockService = stockService;
        }


        // =====================================
        // OBTENER PRESUPUESTO
        // =====================================

        public async Task<ServiceResult<Presupuesto>>
            ObtenerAsync(int ordenTrabajoId)
        {
            var presupuesto =
                await _context.Presupuestos
                    .Include(p => p.Items)
                        .ThenInclude(i => i.Repuesto)

                    .Include(p => p.Mecanico)

                    .Include(p => p.Historial)
                        .ThenInclude(h => h.Mecanico)

                    .FirstOrDefaultAsync(p =>
                        p.OrdenTrabajoId ==
                        ordenTrabajoId);

            if (presupuesto == null)
            {
                return ServiceResult<Presupuesto>.Error(
                    "La orden todavía no tiene un presupuesto.");
            }

            return ServiceResult<Presupuesto>.Ok(
                presupuesto);
        }


        // =====================================
        // CREAR PRESUPUESTO
        // =====================================

        public async Task<ServiceResult<Presupuesto>>
            CrearAsync(
                int ordenTrabajoId,
                int mecanicoId)
        {
            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult<Presupuesto>.Error(
                    "Orden de trabajo no encontrada.");
            }

            if (orden.MecanicoId != mecanicoId)
            {
                return ServiceResult<Presupuesto>.Error(
                    "La orden no está asignada a este mecánico.");
            }

            if (orden.EstadoActual !=
                EstadoOrden.Diagnostico)
            {
                return ServiceResult<Presupuesto>.Error(
                    "El diagnóstico debe estar finalizado para crear el presupuesto.");
            }

            var existente =
                await _context.Presupuestos
                    .AnyAsync(p =>
                        p.OrdenTrabajoId ==
                        ordenTrabajoId);

            if (existente)
            {
                return ServiceResult<Presupuesto>.Error(
                    "La orden ya posee un presupuesto.");
            }

            var presupuesto = new Presupuesto
            {
                OrdenTrabajoId =
                    ordenTrabajoId,

                MecanicoId =
                    mecanicoId,

                Estado =
                    EstadoPresupuesto.Modificado,

                Total =
                    0,

                FechaUltimaModificacion =
                    DateTime.Now
            };

            _context.Presupuestos.Add(
                presupuesto);

            await _context.SaveChangesAsync();

            return ServiceResult<Presupuesto>.Ok(
                presupuesto,
                "Presupuesto creado correctamente.");
        }


        // =====================================
        // AGREGAR ITEM
        // =====================================

        public async Task<ServiceResult> AgregarItemAsync(
            int presupuestoId,
            int mecanicoId,
            string descripcion,
            int cantidad,
            decimal precioUnitario,
            int? repuestoId = null)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                return ServiceResult.Error(
                    "Debe ingresar una descripción.");
            }

            if (cantidad <= 0)
            {
                return ServiceResult.Error(
                    "La cantidad debe ser mayor a cero.");
            }

            if (precioUnitario < 0)
            {
                return ServiceResult.Error(
                    "El precio no puede ser negativo.");
            }


            var presupuesto =
                await _context.Presupuestos
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p =>
                        p.Id == presupuestoId);

            if (presupuesto == null)
            {
                return ServiceResult.Error(
                    "Presupuesto no encontrado.");
            }


            if (presupuesto.MecanicoId !=
                mecanicoId)
            {
                return ServiceResult.Error(
                    "El presupuesto no está asignado a este mecánico.");
            }


            if (presupuesto.Estado ==
                EstadoPresupuesto.Aprobado)
            {
                return ServiceResult.Error(
                    "No se puede modificar un presupuesto aprobado.");
            }


            if (presupuesto.Estado ==
                EstadoPresupuesto.Rechazado)
            {
                return ServiceResult.Error(
                    "No se puede modificar un presupuesto rechazado.");
            }


            // =====================================
            // MEMENTO
            // =====================================

            var memento =
                CrearMementoPresupuesto(
                    presupuesto);

            var caretaker =
                new PresupuestoCaretaker();

            caretaker.Guardar(
                memento);

            GuardarMementoEnHistorial(
                presupuesto,
                caretaker.ObtenerAnterior(),
                mecanicoId,
                "Se agregó un ítem al presupuesto.");


            // =====================================
            // AGREGAR ITEM
            // =====================================

            presupuesto.Items.Add(
                new PresupuestoItem
                {
                    Descripcion =
                        descripcion.Trim(),

                    Cantidad =
                        cantidad,

                    PrecioUnitario =
                        precioUnitario,

                    RepuestoId =
                        repuestoId
                });


            presupuesto.Total =
                presupuesto.CalcularTotal();

            presupuesto.FechaUltimaModificacion =
                DateTime.Now;

            presupuesto.Estado =
                EstadoPresupuesto.Modificado;


            await _context.SaveChangesAsync();


            // =====================================
            // ADVERTENCIA DE STOCK
            // =====================================

            if (repuestoId.HasValue)
            {
                var disponibilidad =
                    await _stockService
                        .AnalizarDisponibilidadAsync(
                            repuestoId.Value,
                            cantidad);


                if (!disponibilidad.Existe)
                {
                    return ServiceResult.Ok(
                        "Ítem agregado correctamente. " +
                        "No se pudo consultar la disponibilidad del repuesto.");
                }


                if (disponibilidad.CantidadFaltante > 0)
                {
                    return ServiceResult.Ok(
                        $"Ítem agregado correctamente. " +
                        $"ADVERTENCIA: hay {disponibilidad.StockActual} " +
                        $"unidad(es) disponibles y se necesitan {cantidad}. " +
                        $"Faltan {disponibilidad.CantidadFaltante} unidad(es). " +
                        $"El presupuesto puede continuar normalmente.");
                }


                if (disponibilidad.QuedaBajoMinimo)
                {
                    var stockPosterior =
                        disponibilidad.StockActual -
                        cantidad;

                    return ServiceResult.Ok(
                        $"Ítem agregado correctamente. " +
                        $"ADVERTENCIA: el stock quedaría en " +
                        $"{stockPosterior} unidad(es), " +
                        $"igual o por debajo del mínimo " +
                        $"({disponibilidad.StockMinimo}).");
                }
            }


            return ServiceResult.Ok(
                "Ítem agregado correctamente.");
        }

        // =====================================
        // ELIMINAR ITEM
        // =====================================

        public async Task<ServiceResult>
            EliminarItemAsync(
                int presupuestoId,
                int itemId,
                int mecanicoId)
        {
            var presupuesto =
                await _context.Presupuestos
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p =>
                        p.Id == presupuestoId);

            if (presupuesto == null)
            {
                return ServiceResult.Error(
                    "Presupuesto no encontrado.");
            }

            if (presupuesto.MecanicoId !=
                mecanicoId)
            {
                return ServiceResult.Error(
                    "El presupuesto no está asignado a este mecánico.");
            }

            if (presupuesto.Estado ==
                EstadoPresupuesto.Aprobado)
            {
                return ServiceResult.Error(
                    "No se puede modificar un presupuesto aprobado.");
            }

            if (presupuesto.Estado ==
                EstadoPresupuesto.Rechazado)
            {
                return ServiceResult.Error(
                    "No se puede modificar un presupuesto rechazado.");
            }

            var item =
                presupuesto.Items
                    .FirstOrDefault(i =>
                        i.Id == itemId);

            if (item == null)
            {
                return ServiceResult.Error(
                    "Ítem no encontrado.");
            }


            // =====================================
            // MEMENTO
            // =====================================

            var memento =
                CrearMementoPresupuesto(
                    presupuesto);

            var caretaker =
                new PresupuestoCaretaker();

            caretaker.Guardar(
                memento);

            GuardarMementoEnHistorial(
                presupuesto,
                caretaker.ObtenerAnterior(),
                mecanicoId,
                "Se eliminó un ítem del presupuesto.");


            // =====================================
            // ELIMINAR ITEM
            // =====================================

            presupuesto.Items.Remove(item);

            presupuesto.Total =
                presupuesto.CalcularTotal();

            presupuesto.FechaUltimaModificacion =
                DateTime.Now;

            presupuesto.Estado =
                EstadoPresupuesto.Modificado;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Ítem eliminado correctamente.");
        }


        // =====================================
        // ENVIAR A APROBACIÓN
        // =====================================

        public async Task<ServiceResult>
            EnviarAprobacionAsync(
                int presupuestoId,
                int mecanicoId)
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var presupuesto =
                    await _context.Presupuestos

                        .Include(p => p.Items)

                        .Include(p => p.OrdenTrabajo)
                            .ThenInclude(o => o.Turno)

                        .FirstOrDefaultAsync(p =>
                            p.Id == presupuestoId);

                if (presupuesto == null)
                {
                    return ServiceResult.Error(
                        "Presupuesto no encontrado.");
                }

                if (presupuesto.OrdenTrabajo == null ||
                    presupuesto.OrdenTrabajo.Turno == null)
                {
                    return ServiceResult.Error(
                        "No se pudo determinar la orden y el cliente.");
                }

                if (presupuesto.MecanicoId !=
                    mecanicoId)
                {
                    return ServiceResult.Error(
                        "El presupuesto no está asignado a este mecánico.");
                }

                if (presupuesto.OrdenTrabajo.MecanicoId !=
                    mecanicoId)
                {
                    return ServiceResult.Error(
                        "La orden no está asignada a este mecánico.");
                }

                if (presupuesto.Estado ==
                    EstadoPresupuesto.Aprobado)
                {
                    return ServiceResult.Error(
                        "El presupuesto ya fue aprobado.");
                }

                if (presupuesto.Estado ==
                    EstadoPresupuesto.Rechazado)
                {
                    return ServiceResult.Error(
                        "El presupuesto fue rechazado y no puede enviarse nuevamente.");
                }

                if (!presupuesto.Items.Any())
                {
                    return ServiceResult.Error(
                        "El presupuesto debe tener al menos un ítem.");
                }

                var orden =
                    presupuesto.OrdenTrabajo;

                if (orden.EstadoActual !=
                    EstadoOrden.Diagnostico)
                {
                    return ServiceResult.Error(
                        "La orden debe encontrarse en diagnóstico para enviar el presupuesto.");
                }

                presupuesto.Total =
                    presupuesto.CalcularTotal();

                presupuesto.Estado =
                    EstadoPresupuesto.Pendiente;

                presupuesto.FechaUltimaModificacion =
                    DateTime.Now;


                // =====================================
                // STATE PATTERN
                // =====================================

                _ordenStateService.CambiarEstado(
                    orden,
                    new EstadoEsperandoAprobacionHandler());

                var historial =
                    orden.HistorialEstados.LastOrDefault();

                if (historial != null)
                {
                    historial.MecanicoId =
                        mecanicoId;
                }


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                // =====================================
                // OBSERVER
                // =====================================

                await _ordenSubject.NotificarAsync(
                    orden,
                    "El presupuesto está disponible para su aprobación.");

                return ServiceResult.Ok(
                    "Presupuesto enviado al cliente para su aprobación.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // =====================================
        // APROBAR - CLIENTE
        // =====================================

        public async Task<ServiceResult> AprobarAsync(
            int presupuestoId,
            int clienteId,
            int usuarioId)
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var presupuesto =
                    await _context.Presupuestos

                        .Include(p => p.Items)
                            .ThenInclude(i => i.Repuesto)

                        .Include(p => p.OrdenTrabajo)
                            .ThenInclude(o => o.Turno)

                        .Include(p => p.OrdenTrabajo)
                            .ThenInclude(o => o.Factura)

                        .FirstOrDefaultAsync(p =>
                            p.Id == presupuestoId);


                if (presupuesto == null)
                {
                    return ServiceResult.Error(
                        "Presupuesto no encontrado.");
                }


                if (presupuesto.OrdenTrabajo == null ||
                    presupuesto.OrdenTrabajo.Turno == null)
                {
                    return ServiceResult.Error(
                        "No se pudo determinar la orden y el cliente.");
                }


                var orden =
                    presupuesto.OrdenTrabajo;


                // =====================================
                // VERIFICAR CLIENTE
                // =====================================

                if (orden.Turno.ClienteId !=
                    clienteId)
                {
                    return ServiceResult.Error(
                        "No tiene autorización para aprobar este presupuesto.");
                }


                // =====================================
                // VERIFICAR FACTURA
                // =====================================

                if (orden.Factura != null)
                {
                    return ServiceResult.Error(
                        "La orden ya posee una factura y no puede volver a modificarse.");
                }


                // =====================================
                // VERIFICAR ESTADO
                // =====================================

                if (presupuesto.Estado ==
                    EstadoPresupuesto.Pendiente)
                {
                    if (orden.EstadoActual !=
                        EstadoOrden.EsperandoAprobacion)
                    {
                        return ServiceResult.Error(
                            "La orden no está esperando la aprobación del cliente.");
                    }
                }
                else if (
                    presupuesto.Estado ==
                    EstadoPresupuesto.Rechazado)
                {
                    if (orden.EstadoActual !=
                        EstadoOrden.Rechazado)
                    {
                        return ServiceResult.Error(
                            "La orden no se encuentra rechazada.");
                    }

                    // El cliente se arrepintió.
                }
                else
                {
                    return ServiceResult.Error(
                        "El presupuesto no puede ser aprobado en su estado actual.");
                }


                // =====================================
                // VERIFICAR ITEMS
                // =====================================

                if (!presupuesto.Items.Any())
                {
                    return ServiceResult.Error(
                        "El presupuesto no tiene ítems.");
                }


                // =====================================
                // MEMENTO
                // =====================================

                var memento =
                    CrearMementoPresupuesto(
                        presupuesto);

                var caretaker =
                    new PresupuestoCaretaker();

                caretaker.Guardar(
                    memento);

                GuardarMementoEnHistorial(
                    presupuesto,
                    caretaker.ObtenerAnterior(),
                    presupuesto.MecanicoId ?? 0,
                    "El presupuesto fue aprobado.");


                // =====================================
                // RESERVAR / DESCONTAR STOCK
                // =====================================

                foreach (var item in presupuesto.Items)
                {
                    // Los servicios o trabajos
                    // sin repuesto no afectan stock.

                    if (!item.RepuestoId.HasValue)
                    {
                        continue;
                    }


                    var resultadoStock =
                        await _stockService
                            .ReservarParaOrdenAsync(
                                item.RepuestoId.Value,
                                item.Cantidad,
                                usuarioId,
                                orden.Id);


                    if (!resultadoStock.Exitoso)
                    {
                        return ServiceResult.Error(
                            resultadoStock.Mensaje);
                    }
                }


                // =====================================
                // APROBAR PRESUPUESTO
                // =====================================

                presupuesto.Estado =
                    EstadoPresupuesto.Aprobado;

                presupuesto.FechaUltimaModificacion =
                    DateTime.Now;


                // =====================================
                // STATE PATTERN
                // =====================================

                _ordenStateService.CambiarEstado(
                    orden,
                    new EstadoAprobadoHandler());


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                // =====================================
                // OBSERVER
                // =====================================

                await _ordenSubject.NotificarAsync(
                    orden,
                    "El presupuesto fue aprobado. La reparación puede continuar.");


                // =====================================
                // MENSAJE AL CLIENTE
                // =====================================

                return ServiceResult.Ok(
                    "Presupuesto aprobado correctamente. " +
                    "La orden puede continuar con la reparación.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
            
        // =====================================
        // RECHAZAR - CLIENTE
        // =====================================

        public async Task<ServiceResult> RechazarAsync(
            int presupuestoId,
            int clienteId,
            string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return ServiceResult.Error(
                    "Debe indicar el motivo del rechazo.");
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var presupuesto =
                    await _context.Presupuestos

                        .Include(p => p.OrdenTrabajo)
                            .ThenInclude(o => o.Turno)

                        .Include(p => p.OrdenTrabajo)
                            .ThenInclude(o => o.Factura)

                        .FirstOrDefaultAsync(p =>
                            p.Id == presupuestoId);

                if (presupuesto == null)
                {
                    return ServiceResult.Error(
                        "Presupuesto no encontrado.");
                }

                if (presupuesto.OrdenTrabajo == null ||
                    presupuesto.OrdenTrabajo.Turno == null)
                {
                    return ServiceResult.Error(
                        "No se pudo determinar la orden y el cliente.");
                }

                var orden =
                    presupuesto.OrdenTrabajo;


                // =====================================
                // VERIFICAR CLIENTE
                // =====================================

                if (orden.Turno.ClienteId != clienteId)
                {
                    return ServiceResult.Error(
                        "No tiene autorización para rechazar este presupuesto.");
                }


                // =====================================
                // NO SE PUEDE RECHAZAR SI HAY FACTURA
                // =====================================

                if (orden.Factura != null)
                {
                    return ServiceResult.Error(
                        "La orden ya posee una factura y no puede rechazarse nuevamente.");
                }


                // =====================================
                // VERIFICAR ESTADO
                // =====================================

                if (presupuesto.Estado !=
                    EstadoPresupuesto.Pendiente)
                {
                    return ServiceResult.Error(
                        "El presupuesto no está pendiente de aprobación.");
                }

                if (orden.EstadoActual !=
                    EstadoOrden.EsperandoAprobacion)
                {
                    return ServiceResult.Error(
                        "La orden no está esperando la aprobación del cliente.");
                }


                // =====================================
                // MEMENTO
                // Guardamos el estado anterior.
                // =====================================

                var memento =
                    CrearMementoPresupuesto(
                        presupuesto);

                var caretaker =
                    new PresupuestoCaretaker();

                caretaker.Guardar(
                    memento);

                GuardarMementoEnHistorial(
                    presupuesto,
                    caretaker.ObtenerAnterior(),
                    presupuesto.MecanicoId ?? 0,
                    "El presupuesto fue rechazado por el cliente.");


                // =====================================
                // RECHAZAR PRESUPUESTO
                // =====================================

                presupuesto.Estado =
                    EstadoPresupuesto.Rechazado;

                presupuesto.MotivoRechazo =
                    motivo.Trim();

                presupuesto.FechaUltimaModificacion =
                    DateTime.Now;


                // =====================================
                // STATE PATTERN
                // =====================================

                _ordenStateService.CambiarEstado(
                    orden,
                    new EstadoRechazadoHandler());


                // =====================================
                // NO SE CREA FACTURA ACÁ
                // =====================================

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                // =====================================
                // OBSERVER
                // =====================================

                await _ordenSubject.NotificarAsync(
                    orden,
                    "El presupuesto fue rechazado por el cliente.");

                return ServiceResult.Ok(
                    "Presupuesto rechazado. " +
                    "La reparación no será realizada por el momento. " +
                    "El cliente puede aprobar nuevamente el presupuesto antes de solicitar la entrega del vehículo.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // =====================================
        // MEMENTO
        // CREAR SNAPSHOT
        // =====================================

        private PresupuestoMemento
            CrearMementoPresupuesto(
                Presupuesto presupuesto)
        {
            var originator =
                new PresupuestoOriginator(
                    presupuesto.Total,
                    presupuesto.Estado.ToString());

            return originator.CrearMemento();
        }


        // =====================================
        // MEMENTO
        // PERSISTIR SNAPSHOT
        // =====================================

        private void GuardarMementoEnHistorial(
            Presupuesto presupuesto,
            PresupuestoMemento? memento,
            int mecanicoId,
            string motivo)
        {
            if (memento == null)
            {
                return;
            }

            presupuesto.Historial.Add(
                new PresupuestoHistorial
                {
                    PresupuestoId =
                        presupuesto.Id,

                    TotalAnterior =
                        memento.Total,

                    Fecha =
                        memento.Fecha,

                    Motivo =
                        $"{motivo} Estado anterior: {memento.Estado}",

                    MecanicoId =
                        mecanicoId
                });
        }
    }
}