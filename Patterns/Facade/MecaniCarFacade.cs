using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Services;
using MecaniCar360.Data;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Patterns.Facade
{
    public class MecaniCarFacade
    {
        private readonly MecaniCarContext _context;
        private readonly DiagnosticoService
            _diagnosticoService;

        private readonly PresupuestoService
            _presupuestoService;

        private readonly OrdenTrabajoService
            _ordenTrabajoService;

        public MecaniCarFacade(
            DiagnosticoService diagnosticoService,
            PresupuestoService presupuestoService,
            OrdenTrabajoService ordenTrabajoService,
            MecaniCarContext context)
        {
            _context = context;
            _diagnosticoService =
                diagnosticoService;

            _presupuestoService =
                presupuestoService;

            _ordenTrabajoService =
                ordenTrabajoService;
        }


        // =====================================
        // PREPARAR PRESUPUESTO
        // =====================================

        public async Task<
            ServiceResult<Presupuesto>>
            PrepararPresupuestoAsync(
                int ordenTrabajoId,
                int usuarioSolicitanteId)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Id == usuarioSolicitanteId &&
                    u.Activo && u.Persona.Activo);

            if (usuario == null)
                return ServiceResult<Presupuesto>.Error("Usuario inactivo o inexistente.");

            // Los módulos auxiliares conservan por ahora sus contratos por persona.
            var mecanicoId = usuario.PersonaId;
            // =====================================
            // VERIFICAR ORDEN
            // =====================================

            var ordenResultado =
            await _ordenTrabajoService
                .ObtenerPorIdAsync(
                    ordenTrabajoId,
                    usuarioSolicitanteId);

            if (!ordenResultado.Exitoso)
            {
                return ServiceResult<
                    Presupuesto>.Error(
                    ordenResultado.Mensaje);
            }


            // =====================================
            // VERIFICAR DIAGNÓSTICO
            // =====================================

            var diagnosticoResultado =
                await _diagnosticoService
                    .ObtenerAsync(
                        ordenTrabajoId,
                        mecanicoId);

            if (!diagnosticoResultado.Exitoso)
            {
                return ServiceResult<
                    Presupuesto>.Error(
                    "La orden debe tener un diagnóstico antes de crear el presupuesto.");
            }


            // =====================================
            // CREAR PRESUPUESTO
            // =====================================

            return await _presupuestoService
                .CrearAsync(
                    ordenTrabajoId,
                    mecanicoId);
        }


        // =====================================
        // ENVIAR PRESUPUESTO A CLIENTE
        // =====================================

        public async Task<ServiceResult>
            EnviarPresupuestoAsync(
                int presupuestoId,
                int mecanicoId)
        {
            return await _presupuestoService
                .EnviarAprobacionAsync(
                    presupuestoId,
                    mecanicoId);
        }
    }
}
