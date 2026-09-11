using MecaniCar360.Models.DTOs;
using MecaniCar360.Services;

namespace MecaniCar360.Patterns.Facade
{
    public class MecaniCarFacade
    {
        private readonly PresupuestoService _presupuestoService;
        public MecaniCarFacade(PresupuestoService presupuestoService)
        {
            _presupuestoService = presupuestoService;
        }

        // La validación completa pertenece al service, también para llamadas directas.
        public Task<ServiceResult<PresupuestoOperacion>> PrepararPresupuestoAsync(int ordenTrabajoId, int usuarioSolicitanteId) =>
            _presupuestoService.CrearAsync(ordenTrabajoId, usuarioSolicitanteId);

        public Task<ServiceResult<PresupuestoOperacion>> EnviarPresupuestoAsync(int presupuestoId,
            int usuarioSolicitanteId, IEnumerable<int>? evidenciaIds = null) =>
            _presupuestoService.EnviarAprobacionAsync(presupuestoId, usuarioSolicitanteId, evidenciaIds);
    }
}