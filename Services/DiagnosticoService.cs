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

        public DiagnosticoService(
            MecaniCarContext context,
            OrdenStateService ordenStateService)
        {
            _context = context;
            _ordenStateService = ordenStateService;
        }


        // =====================================
        // INICIAR DIAGNÓSTICO
        // MECÁNICO
        // =====================================

        public async Task<ServiceResult> IniciarAsync(
            int ordenTrabajoId,
            int mecanicoId)
        {
            if (!await EsMecanicoActivoAsync(mecanicoId))
            {
                return ServiceResult.Error(
                    "El mecánico no está activo.");
            }

            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult.Error(
                    "Orden de trabajo no encontrada.");
            }

            if (orden.FechaFin.HasValue)
            {
                return ServiceResult.Error(
                    "La orden ya fue finalizada.");
            }

            if (orden.MecanicoId != mecanicoId)
            {
                return ServiceResult.Error(
                    "La orden no está asignada a este mecánico.");
            }

            if (orden.EstadoActual !=
                EstadoOrden.Pendiente)
            {
                return ServiceResult.Error(
                    "La orden debe estar pendiente para iniciar el diagnóstico.");
            }


            // =====================================
            // STATE PATTERN
            // =====================================

            _ordenStateService.CambiarEstado(
                orden,
                new EstadoDiagnosticoHandler());


            // El handler creó el historial.
            // Completamos quién realizó el cambio.

            var historial =
                orden.HistorialEstados.LastOrDefault();

            if (historial != null)
            {
                historial.MecanicoId =
                    mecanicoId;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Diagnóstico iniciado correctamente.");
        }


        // =====================================
        // OBTENER DIAGNÓSTICO
        //
        // ADMIN:
        // puede consultar cualquiera.
        //
        // MECÁNICO:
        // solamente el de sus órdenes.
        // =====================================

        public async Task<
            ServiceResult<Diagnostico>>
            ObtenerAsync(
                int ordenTrabajoId,
                int personaId)
        {
            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult<Diagnostico>.Error(
                    "Orden de trabajo no encontrada.");
            }

            var acceso =
                await ValidarAccesoAsync(
                    orden,
                    personaId);

            if (!acceso.Exitoso)
            {
                return ServiceResult<Diagnostico>.Error(
                    acceso.Mensaje);
            }

            var diagnostico =
                await _context.Diagnosticos

                    .Include(d => d.Historial)
                        .ThenInclude(h => h.Mecanico)

                    .FirstOrDefaultAsync(d =>
                        d.OrdenTrabajoId ==
                        ordenTrabajoId);

            if (diagnostico == null)
            {
                return ServiceResult<Diagnostico>.Error(
                    "La orden todavía no tiene un diagnóstico.");
            }

            return ServiceResult<Diagnostico>.Ok(
                diagnostico);
        }


        // =====================================
        // GUARDAR / ACTUALIZAR DIAGNÓSTICO
        //
        // SOLO MECÁNICO ASIGNADO
        // =====================================

        public async Task<ServiceResult> GuardarAsync(
            int ordenTrabajoId,
            int mecanicoId,
            string descripcion)
        {
            if (!await EsMecanicoActivoAsync(
                    mecanicoId))
            {
                return ServiceResult.Error(
                    "El mecánico no está activo.");
            }

            if (string.IsNullOrWhiteSpace(
                    descripcion))
            {
                return ServiceResult.Error(
                    "Debe ingresar una descripción del diagnóstico.");
            }

            // Evitamos descripciones excesivamente grandes.
            if (descripcion.Length > 5000)
            {
                return ServiceResult.Error(
                    "La descripción del diagnóstico no puede superar los 5000 caracteres.");
            }


            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var orden =
                    await _context.OrdenesTrabajo
                        .FirstOrDefaultAsync(o =>
                            o.Id == ordenTrabajoId);

                if (orden == null)
                {
                    return ServiceResult.Error(
                        "Orden de trabajo no encontrada.");
                }

                if (orden.FechaFin.HasValue)
                {
                    return ServiceResult.Error(
                        "La orden ya fue finalizada.");
                }

                if (orden.MecanicoId !=
                    mecanicoId)
                {
                    return ServiceResult.Error(
                        "La orden no está asignada a este mecánico.");
                }

                if (orden.EstadoActual !=
                    EstadoOrden.Diagnostico)
                {
                    return ServiceResult.Error(
                        "La orden debe encontrarse en diagnóstico.");
                }

                var ahora =
                    DateTime.Now;

                var texto =
                    descripcion.Trim();

                var diagnostico =
                    await _context.Diagnosticos
                        .FirstOrDefaultAsync(d =>
                            d.OrdenTrabajoId ==
                            ordenTrabajoId);


                // =====================================
                // CREAR DIAGNÓSTICO
                // =====================================

                if (diagnostico == null)
                {
                    diagnostico =
                        new Diagnostico
                        {
                            OrdenTrabajoId =
                                ordenTrabajoId,

                            DescripcionActual =
                                texto,

                            FechaUltimaModificacion =
                                ahora
                        };

                    _context.Diagnosticos.Add(
                        diagnostico);

                    await _context.SaveChangesAsync();


                    // Primer registro:
                    // no existe un estado anterior.

                    _context.DiagnosticoHistoriales.Add(
                        new DiagnosticoHistorial
                        {
                            DiagnosticoId =
                                diagnostico.Id,

                            Descripcion =
                                texto,

                            Fecha =
                                ahora,

                            MecanicoId =
                                mecanicoId
                        });
                }


                // =====================================
                // ACTUALIZAR DIAGNÓSTICO
                // =====================================

                else
                {
                    // =================================
                    // MEMENTO
                    // =================================

                    var originator =
                        new DiagnosticoOriginator(
                            diagnostico
                                .DescripcionActual);

                    var caretaker =
                        new DiagnosticoCaretaker();

                    var memento =
                        originator
                            .CrearMemento();

                    caretaker.Guardar(
                        memento);


                    // =================================
                    // GUARDAR ESTADO ANTERIOR
                    // =================================

                    var estadoAnterior =
                        caretaker
                            .ObtenerAnterior();

                    if (estadoAnterior != null)
                    {
                        _context
                            .DiagnosticoHistoriales
                            .Add(
                                new DiagnosticoHistorial
                                {
                                    DiagnosticoId =
                                        diagnostico.Id,

                                    Descripcion =
                                        estadoAnterior
                                            .Descripcion,

                                    Fecha =
                                        estadoAnterior
                                            .Fecha,

                                    MecanicoId =
                                        mecanicoId
                                });
                    }


                    // =================================
                    // ACTUALIZAR ORIGINATOR
                    // =================================

                    originator.Actualizar(
                        texto);


                    // =================================
                    // ACTUALIZAR ESTADO ACTUAL
                    // =================================

                    diagnostico
                        .DescripcionActual =
                        originator
                            .Descripcion;

                    diagnostico
                        .FechaUltimaModificacion =
                        ahora;
                }


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return ServiceResult.Ok(
                    "Diagnóstico guardado correctamente.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // =====================================
        // FINALIZAR DIAGNÓSTICO
        //
        // SOLO MECÁNICO ASIGNADO
        // =====================================

        public async Task<ServiceResult> FinalizarAsync(
            int ordenTrabajoId,
            int mecanicoId)
        {
            if (!await EsMecanicoActivoAsync(
                    mecanicoId))
            {
                return ServiceResult.Error(
                    "El mecánico no está activo.");
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var orden =
                    await _context.OrdenesTrabajo
                        .FirstOrDefaultAsync(o =>
                            o.Id == ordenTrabajoId);

                if (orden == null)
                {
                    return ServiceResult.Error(
                        "Orden de trabajo no encontrada.");
                }

                if (orden.FechaFin.HasValue)
                {
                    return ServiceResult.Error(
                        "La orden ya fue finalizada.");
                }

                if (orden.MecanicoId !=
                    mecanicoId)
                {
                    return ServiceResult.Error(
                        "La orden no está asignada a este mecánico.");
                }

                if (orden.EstadoActual !=
                    EstadoOrden.Diagnostico)
                {
                    return ServiceResult.Error(
                        "La orden debe encontrarse en diagnóstico.");
                }

                var diagnostico =
                    await _context.Diagnosticos
                        .FirstOrDefaultAsync(d =>
                            d.OrdenTrabajoId ==
                            ordenTrabajoId);

                if (diagnostico == null ||
                    string.IsNullOrWhiteSpace(
                        diagnostico
                            .DescripcionActual))
                {
                    return ServiceResult.Error(
                        "Debe registrar un diagnóstico antes de finalizarlo.");
                }


                /*
                 * El diagnóstico termina acá.
                 *
                 * La OT permanece en Diagnostico.
                 *
                 * El siguiente paso es crear y completar
                 * el presupuesto.
                 *
                 * Cuando el presupuesto se envía al cliente:
                 *
                 * Diagnostico → EsperandoAprobacion
                 */

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return ServiceResult.Ok(
                    "Diagnóstico finalizado correctamente. " +
                    "Ya puede confeccionarse el presupuesto.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // =====================================
        // HISTORIAL
        //
        // ADMIN:
        // puede consultar cualquiera.
        //
        // MECÁNICO:
        // solamente el de sus órdenes.
        // =====================================

        public async Task<
            ServiceResult<List<DiagnosticoHistorial>>>
            ObtenerHistorialAsync(
                int ordenTrabajoId,
                int personaId)
        {
            var orden =
                await _context.OrdenesTrabajo
                    .FirstOrDefaultAsync(o =>
                        o.Id == ordenTrabajoId);

            if (orden == null)
            {
                return ServiceResult<
                    List<DiagnosticoHistorial>>.Error(
                    "Orden de trabajo no encontrada.");
            }

            var acceso =
                await ValidarAccesoAsync(
                    orden,
                    personaId);

            if (!acceso.Exitoso)
            {
                return ServiceResult<
                    List<DiagnosticoHistorial>>.Error(
                    acceso.Mensaje);
            }

            var diagnostico =
                await _context.Diagnosticos
                    .FirstOrDefaultAsync(d =>
                        d.OrdenTrabajoId ==
                        ordenTrabajoId);

            if (diagnostico == null)
            {
                return ServiceResult<
                    List<DiagnosticoHistorial>>.Error(
                    "La orden todavía no tiene un diagnóstico.");
            }

            var historial =
                await _context
                    .DiagnosticoHistoriales

                    .Include(h =>
                        h.Mecanico)

                    .Where(h =>
                        h.DiagnosticoId ==
                        diagnostico.Id)

                    .OrderByDescending(h =>
                        h.Fecha)

                    .ToListAsync();

            return ServiceResult<
                List<DiagnosticoHistorial>>.Ok(
                historial);
        }


        // =====================================
        // VALIDAR ACCESO
        // =====================================

        private async Task<ServiceResult>
            ValidarAccesoAsync(
                OrdenTrabajo orden,
                int personaId)
        {
            // -------------------------------------
            // ADMIN
            // -------------------------------------

            if (await EsAdministradorAsync(
                    personaId))
            {
                return ServiceResult.Ok();
            }


            // -------------------------------------
            // MECÁNICO
            // -------------------------------------

            if (await EsMecanicoActivoAsync(
                    personaId))
            {
                if (orden.MecanicoId ==
                    personaId)
                {
                    return ServiceResult.Ok();
                }

                return ServiceResult.Error(
                    "No tiene acceso al diagnóstico de esta orden.");
            }


            // -------------------------------------
            // OTROS ROLES
            // -------------------------------------

            return ServiceResult.Error(
                "No tiene permisos para consultar este diagnóstico.");
        }


        // =====================================
        // VERIFICAR ADMIN
        // =====================================

        private async Task<bool>
            EsAdministradorAsync(
                int personaId)
        {
            return await _context.PersonaRoles
                .Include(pr => pr.Rol)
                .AnyAsync(pr =>
                    pr.PersonaId ==
                        personaId &&

                    pr.Rol.Nombre ==
                        "ADMIN" &&

                    pr.Rol.Activo &&

                    pr.FechaBaja == null);
        }


        // =====================================
        // VERIFICAR MECÁNICO
        // =====================================

        private async Task<bool>
            EsMecanicoActivoAsync(
                int personaId)
        {
            return await _context.PersonaRoles

                .Include(pr => pr.Rol)
                .Include(pr => pr.Persona)

                .AnyAsync(pr =>
                    pr.PersonaId ==
                        personaId &&

                    pr.Persona.Activo &&

                    pr.Rol.Nombre ==
                        "MECANICO" &&

                    pr.Rol.Activo &&

                    pr.FechaBaja == null);
        }
    }
}