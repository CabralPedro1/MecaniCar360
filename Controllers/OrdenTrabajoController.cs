using MecaniCar360.Models;
using MecaniCar360.Models.Enums;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class OrdenTrabajoController : Controller
    {
        private readonly OrdenTrabajoService _ordenTrabajoService;

        public OrdenTrabajoController(
            OrdenTrabajoService ordenTrabajoService)
        {
            _ordenTrabajoService =
                ordenTrabajoService;
        }


        // =====================================
        // INDEX
        // =====================================

        public async Task<IActionResult> Index()
        {
            var resultado =
                await _ordenTrabajoService
                    .ObtenerTodasAsync();

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return View(
                    new List<OrdenTrabajo>());
            }

            return View(
                resultado.Data);
        }


        // =====================================
        // DETALLE
        // =====================================

        public async Task<IActionResult> Detalle(
            int id)
        {
            var resultado =
                await _ordenTrabajoService
                    .ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            return View(
                resultado.Data);
        }


        // =====================================
        // PENDIENTES
        // =====================================

        public async Task<IActionResult> Pendientes()
        {
            var resultado =
                await _ordenTrabajoService
                    .ObtenerPendientesAsync();

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return View(
                    new List<OrdenTrabajo>());
            }

            return View(
                resultado.Data);
        }


        // =====================================
        // ÓRDENES DE UN MECÁNICO
        // =====================================

        public async Task<IActionResult> DeMecanico(
            int mecanicoId)
        {
            var resultado =
                await _ordenTrabajoService
                    .ObtenerDeMecanicoAsync(
                        mecanicoId);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return View(
                    new List<OrdenTrabajo>());
            }

            return View(
                resultado.Data);
        }


        // =====================================
        // MECÁNICOS DISPONIBLES
        // =====================================

        [HttpGet]
        public async Task<JsonResult>
            MecanicosDisponibles()
        {
            var resultado =
                await _ordenTrabajoService
                    .ObtenerMecanicosDisponiblesAsync();

            if (!resultado.Exitoso)
            {
                return Json(new
                {
                    exitoso = false,

                    mensaje =
                        resultado.Mensaje,

                    mecanicos =
                        new List<object>()
                });
            }

            return Json(new
            {
                exitoso = true,

                mecanicos =
                    resultado.Data!
                        .Select(m => new
                        {
                            id =
                                m.Id,

                            nombre =
                                $"{m.Apellido}, {m.Nombre}"
                        })
                        .ToList()
            });
        }


        // =====================================
        // ASIGNAR MECÁNICO
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            AsignarMecanico(
                int id,
                int mecanicoId)
        {
            var resultado =
                await _ordenTrabajoService
                    .AsignarMecanicoAsync(
                        id,
                        mecanicoId);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id
                });
        }


        // =====================================
        // TOMAR ORDEN
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tomar(
            int id)
        {
            var mecanicoId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _ordenTrabajoService
                    .TomarOrdenAsync(
                        id,
                        mecanicoId);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id
                });
        }


        // =====================================
        // INICIAR REPARACIÓN
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            IniciarReparacion(
                int id)
        {
            var mecanicoId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _ordenTrabajoService
                    .IniciarReparacionAsync(
                        id,
                        mecanicoId);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id
                });
        }


        // =====================================
        // FINALIZAR REPARACIÓN
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            Finalizar(
                int id,
                int? horasReales)
        {
            var mecanicoId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _ordenTrabajoService
                    .FinalizarAsync(
                        id,
                        mecanicoId,
                        horasReales);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id
                });
        }


        // =====================================
        // ENTREGAR VEHÍCULO
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            Entregar(
                int id)
        {
            var resultado =
                await _ordenTrabajoService
                    .EntregarAsync(id);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id
                });
        }


        // =====================================
        // CAMBIAR URGENCIA
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            CambiarUrgencia(
                int id,
                NivelUrgencia urgencia)
        {
            var resultado =
                await _ordenTrabajoService
                    .CambiarUrgenciaAsync(
                        id,
                        urgencia);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id
                });
        }


        // =====================================
        // OBSERVACIONES
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            ActualizarObservaciones(
                int id,
                string? observaciones)
        {
            var resultado =
                await _ordenTrabajoService
                    .ActualizarObservacionesAsync(
                        id,
                        observaciones);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id
                });
        }


        // =====================================
        // MÉTODOS PRIVADOS
        // =====================================

        private int ObtenerUsuarioPersonaId()
        {
            var claim =
                User.FindFirst("PersonaId");

            if (claim == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el PersonaId en la sesión.");
            }

            return int.Parse(
                claim.Value);
        }
    }
}