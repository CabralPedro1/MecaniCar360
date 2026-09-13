using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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


        // =====================================
        // CREAR GARANTÍA
        // =====================================

        public async Task<ServiceResult<Garantia>>
            CrearAsync(
                int ordenTrabajoId,
                int usuarioId,
                DateTime fechaInicio,
                List<GarantiaItemDto> items)
        {
            if (!await _permisos.TienePermisoAsync(usuarioId, "GARANTIA_CREAR")) return ServiceResult<Garantia>.Error("Acceso denegado.");

            // =====================================
            // VALIDACIONES GENERALES
            // =====================================

            if (items == null || !items.Any())
            {
                return ServiceResult<Garantia>.Error(
                    "Debe seleccionar al menos un ítem para generar la garantía.");
            }

            if (fechaInicio == default)
            {
                fechaInicio = DateTime.Now;
            }


            // =====================================
            // BUSCAR ORDEN
            // =====================================

            var orden =
                await _context.OrdenesTrabajo
                    .Include(o => o.Presupuesto)
                        .ThenInclude(p => p.Items)
                    .Include(o => o.IngresoVehiculo)
                        .ThenInclude(i => i.Turno)
                            .ThenInclude(t => t.Vehiculo)
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult<Garantia>.Error(
                    "Orden de trabajo no encontrada.");
            }


            // =====================================
            // VERIFICAR PRESUPUESTO
            // =====================================

            if (orden.Presupuesto == null)
            {
                return ServiceResult<Garantia>.Error(
                    "La orden no tiene un presupuesto asociado.");
            }


            // =====================================
            // VERIFICAR SI YA EXISTE GARANTÍA
            // =====================================

            var garantiaExistente =
                await _context.Garantias
                    .AnyAsync(g =>
                        g.OrdenTrabajoId ==
                        ordenTrabajoId &&
                        g.Activa);

            if (garantiaExistente)
            {
                return ServiceResult<Garantia>.Error(
                    "La orden de trabajo ya posee una garantía activa.");
            }


            // =====================================
            // VALIDAR USUARIO
            // =====================================

            var usuario =
                await _context.Usuarios
                    .Include(u => u.Persona)
                    .FirstOrDefaultAsync(u =>
                        u.Id == usuarioId &&
                        u.Activo &&
                        u.Persona.Activo);

            if (usuario == null)
            {
                return ServiceResult<Garantia>.Error(
                    "El usuario no es válido o se encuentra inactivo.");
            }


            // =====================================
            // VALIDAR ÍTEMS
            // =====================================

            var idsItems =
                items
                    .Select(i => i.PresupuestoItemId)
                    .Distinct()
                    .ToList();

            var itemsPresupuesto =
                orden.Presupuesto.Items
                    .Where(i =>
                        idsItems.Contains(i.Id))
                    .ToList();

            if (itemsPresupuesto.Count !=
                idsItems.Count)
            {
                return ServiceResult<Garantia>.Error(
                    "Uno o más ítems seleccionados no pertenecen al presupuesto de la orden.");
            }


            // =====================================
            // VALIDAR MESES
            // =====================================

            foreach (var item in items)
            {
                if (item.MesesGarantia < 0 ||
                    item.MesesGarantia > 120)
                {
                    return ServiceResult<Garantia>.Error(
                        "La garantía debe estar entre 0 y 120 meses.");
                }
            }


            // =====================================
            // CREAR GARANTÍA
            // =====================================

            var garantia =
                new Garantia
                {
                    OrdenTrabajoId =
                        ordenTrabajoId,

                    FechaInicio =
                        fechaInicio,

                    FechaFin =
                        fechaInicio,

                    Activa = true,

                    CreadaPorUsuarioId =
                        usuarioId
                };


            // =====================================
            // CREAR ÍTEMS
            // =====================================

            foreach (var itemDto in items)
            {
                var presupuestoItem =
                    itemsPresupuesto
                        .First(i =>
                            i.Id ==
                            itemDto.PresupuestoItemId);

                var fechaFinItem =
                    fechaInicio.AddMonths(
                        itemDto.MesesGarantia);

                if (fechaFinItem >
                    garantia.FechaFin)
                {
                    garantia.FechaFin =
                        fechaFinItem;
                }

                garantia.Items.Add(
                    new GarantiaItem
                    {
                        PresupuestoItemId =
                            presupuestoItem.Id,

                        MesesGarantia =
                            itemDto.MesesGarantia,

                        Observaciones =
                            string.IsNullOrWhiteSpace(
                                itemDto.Observaciones)
                                ? null
                                : itemDto.Observaciones.Trim()
                    });
            }


            // =====================================
            // GUARDAR
            // =====================================

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.Garantias.Add(
                garantia);

            await _context.SaveChangesAsync();
            _auditoria.RegistrarOperacion("GARANTIA_CREADA", "Garantia", garantia.Id, usuarioId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<Garantia>.Ok(
                garantia);
        }


        // =====================================
        // OBTENER GARANTÍA
        // =====================================

        public async Task<ServiceResult<Garantia>>
            ObtenerAsync(
                int garantiaId, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "GARANTIA_VER")) return ServiceResult<Garantia>.Error("Acceso denegado.");

            var garantia =
                await _context.Garantias
                    .Include(g => g.OrdenTrabajo)
                        .ThenInclude(o => o.IngresoVehiculo)
                            .ThenInclude(i => i.Turno)
                                .ThenInclude(t => t.Vehiculo)

                    .Include(g => g.OrdenTrabajo)
                        .ThenInclude(o => o.IngresoVehiculo)
                            .ThenInclude(i => i.Turno)
                                .ThenInclude(t => t.Cliente)

                    .Include(g => g.Items)
                        .ThenInclude(i =>
                            i.PresupuestoItem)

                    .Include(g => g.CreadaPorUsuario)
                        .ThenInclude(u => u.Persona)

                    .FirstOrDefaultAsync(g =>
                        g.Id == garantiaId);

            if (garantia == null)
            {
                return ServiceResult<Garantia>.Error(
                    "Garantía no encontrada.");
            }

            ActualizarEstadoInterno(
                garantia);

            return ServiceResult<Garantia>.Ok(
                garantia);
        }


        // =====================================
        // LISTAR GARANTÍAS
        // =====================================

        public async Task<
            ServiceResult<List<Garantia>>>
            ObtenerTodasAsync(int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "GARANTIA_VER")) return ServiceResult<List<Garantia>>.Error("Acceso denegado.");

            var garantias =
                await _context.Garantias

                    .Include(g =>
                        g.OrdenTrabajo)

                        .ThenInclude(o =>
                            o.IngresoVehiculo)

                            .ThenInclude(t =>
                            t.Turno.Vehiculo)

                    .Include(g =>
                        g.OrdenTrabajo)

                        .ThenInclude(o =>
                            o.IngresoVehiculo)

                            .ThenInclude(t =>
                            t.Turno.Cliente)

                    .Include(g =>
                        g.Items)

                        .ThenInclude(i =>
                            i.PresupuestoItem)

                    .OrderByDescending(g =>
                        g.FechaInicio)

                    .ToListAsync();


            foreach (var garantia in garantias)
            {
                ActualizarEstadoInterno(
                    garantia);
            }

            return ServiceResult<
                List<Garantia>>.Ok(
                garantias);
        }


        // =====================================
        // GARANTÍAS DE UN CLIENTE
        // =====================================

        public async Task<
            ServiceResult<List<Garantia>>>
            ObtenerPorClienteAsync(
                int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "GARANTIA_VER_PROPIA")) return ServiceResult<List<Garantia>>.Error("Acceso denegado.");
            var clienteId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!clienteId.HasValue) return ServiceResult<List<Garantia>>.Error("Acceso denegado.");

            var garantias =
                await _context.Garantias

                    .Include(g =>
                        g.OrdenTrabajo)

                        .ThenInclude(o =>
                            o.IngresoVehiculo)

                            .ThenInclude(t =>
                            t.Turno.Vehiculo)

                    .Include(g =>
                        g.OrdenTrabajo)

                        .ThenInclude(o =>
                            o.IngresoVehiculo)

                            .ThenInclude(t =>
                            t.Turno.Cliente)

                    .Include(g =>
                        g.Items)

                        .ThenInclude(i =>
                            i.PresupuestoItem)

                    .Where(g =>
                        g.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId ==
                        clienteId)

                    .OrderByDescending(g =>
                        g.FechaInicio)

                    .ToListAsync();


            foreach (var garantia in garantias)
            {
                ActualizarEstadoInterno(
                    garantia);
            }

            return ServiceResult<
                List<Garantia>>.Ok(
                garantias);
        }


        // =====================================
        // GARANTÍAS DE UN VEHÍCULO
        // =====================================

        public async Task<
            ServiceResult<List<Garantia>>>
            ObtenerPorVehiculoAsync(
                int vehiculoId, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "GARANTIA_VER")) return ServiceResult<List<Garantia>>.Error("Acceso denegado.");

            var garantias =
                await _context.Garantias

                    .Include(g =>
                        g.OrdenTrabajo)

                        .ThenInclude(o =>
                            o.IngresoVehiculo)

                            .ThenInclude(t =>
                            t.Turno.Vehiculo)

                    .Include(g =>
                        g.OrdenTrabajo)

                        .ThenInclude(o =>
                            o.IngresoVehiculo)

                            .ThenInclude(t =>
                            t.Turno.Cliente)

                    .Include(g =>
                        g.Items)

                        .ThenInclude(i =>
                            i.PresupuestoItem)

                    .Where(g =>
                        g.OrdenTrabajo.IngresoVehiculo.Turno.VehiculoId ==
                        vehiculoId)

                    .OrderByDescending(g =>
                        g.FechaInicio)

                    .ToListAsync();


            foreach (var garantia in garantias)
            {
                ActualizarEstadoInterno(
                    garantia);
            }

            return ServiceResult<
                List<Garantia>>.Ok(
                garantias);
        }


        // =====================================
        // VERIFICAR GARANTÍA VIGENTE
        // =====================================

        public async Task<ServiceResult> EstaVigenteAsync(
        int garantiaId, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "GARANTIA_VER")) return ServiceResult.Error("Acceso denegado.");

            var garantia =
                await _context.Garantias
                    .FirstOrDefaultAsync(g =>
                        g.Id == garantiaId);

            if (garantia == null)
            {
                return ServiceResult.Error(
                    "Garantía no encontrada.");
            }

            if (!garantia.Activa)
            {
                return ServiceResult.Error(
                    "La garantía se encuentra inactiva.");
            }

            if (DateTime.Now.Date >
                garantia.FechaFin.Date)
            {
                return ServiceResult.Error(
                    "La garantía se encuentra vencida.");
            }

            return ServiceResult.Ok(
                "La garantía se encuentra vigente.");
        }


        // =====================================
        // VERIFICAR COBERTURA DE UN ÍTEM
        // =====================================

        public async Task<ServiceResult> ItemEstaCubiertoAsync(
        int garantiaItemId, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "GARANTIA_VER")) return ServiceResult.Error("Acceso denegado.");

            var item =
                await _context.GarantiaItems
                    .Include(i => i.Garantia)
                    .FirstOrDefaultAsync(i =>
                        i.Id == garantiaItemId);

            if (item == null)
            {
                return ServiceResult.Error(
                    "Ítem de garantía no encontrado.");
            }

            if (!item.Garantia.Activa)
            {
                return ServiceResult.Error(
                    "La garantía se encuentra inactiva.");
            }

            var fechaFin =
                item.Garantia.FechaInicio
                    .AddMonths(
                        item.MesesGarantia);

            if (DateTime.Now.Date >
                fechaFin.Date)
            {
                return ServiceResult.Error(
                    "El ítem no se encuentra cubierto por la garantía.");
            }

            return ServiceResult.Ok(
                "El ítem se encuentra cubierto por la garantía.");
        }

        // =====================================
        // ANULAR GARANTÍA
        // =====================================

        public async Task<ServiceResult>
            AnularAsync(
                int garantiaId, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "GARANTIA_ANULAR")) return ServiceResult.Error("Acceso denegado.");

            var garantia =
                await _context.Garantias
                    .FirstOrDefaultAsync(g =>
                        g.Id == garantiaId);

            if (garantia == null)
            {
                return ServiceResult.Error(
                    "Garantía no encontrada.");
            }

            if (!garantia.Activa)
            {
                return ServiceResult.Error(
                    "La garantía ya se encuentra inactiva.");
            }

            garantia.Activa = false;

            _auditoria.RegistrarOperacion("GARANTIA_ANULADA", "Garantia", garantia.Id, usuarioSolicitanteId);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Garantía anulada correctamente.");
        }

        public async Task<ServiceResult<Garantia>> ObtenerPropiaAsync(int garantiaId, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "GARANTIA_VER_PROPIA"))
                return ServiceResult<Garantia>.Error("Acceso denegado.");
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId);
            if (!personaId.HasValue) return ServiceResult<Garantia>.Error("Acceso denegado.");
            var garantia = await _context.Garantias.AsNoTracking().Include(g => g.Items)
                .FirstOrDefaultAsync(g => g.Id == garantiaId && g.OrdenTrabajo.IngresoVehiculo.Turno.ClienteId == personaId);
            if (garantia == null) return ServiceResult<Garantia>.Error("Garantía no encontrada.");
            ActualizarEstadoInterno(garantia);
            return ServiceResult<Garantia>.Ok(garantia);
        }

        private void ActualizarEstadoInterno(
    Garantia garantia)
        {
            // Una garantía anulada permanece inactiva.
            if (!garantia.Activa)
                return;

            // Si venció, se marca como inactiva.
            if (DateTime.Now.Date >
                garantia.FechaFin.Date)
            {
                garantia.Activa = false;
            }
        }
    }
}