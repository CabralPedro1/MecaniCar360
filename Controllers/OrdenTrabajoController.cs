using MecaniCar360.Attributes;
using MecaniCar360.Helpers;
using MecaniCar360.Models.Enums;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class OrdenTrabajoController : Controller
    {
        private readonly OrdenTrabajoService _ordenTrabajoService;

        public OrdenTrabajoController(
            OrdenTrabajoService ordenTrabajoService)
        {
            _ordenTrabajoService = ordenTrabajoService;
        }


        // =====================================================
        // INDEX
        // =====================================================

        [Permiso("ORDEN_VER")]
        public async Task<IActionResult> Index()
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ObtenerTodasAsync(
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            return View(resultado.Data);
        }


        // =====================================================
        // DETALLE
        // =====================================================

        [Permiso("ORDEN_VER_DETALLE")]
        public async Task<IActionResult> Detalle(
            int id)
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ObtenerPorIdAsync(
                        id,
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            return View(resultado.Data);
        }


        // =====================================================
        // ÓRDENES PENDIENTES
        // =====================================================

        [Permiso("ORDEN_VER")]
        public async Task<IActionResult> Pendientes()
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ObtenerPendientesAsync(
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            return View(resultado.Data);
        }


        // =====================================================
        // ÓRDENES DEL MECÁNICO
        // =====================================================

        [Permiso("ORDEN_VER")]
        public async Task<IActionResult> DeMecanico(
            int mecanicoId)
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ObtenerDeMecanicoAsync(
                        mecanicoId,
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            return View(
                "DeMecanico",
                resultado.Data);
        }


        // =====================================================
        // MECÁNICOS DISPONIBLES
        //
        // SOLO ADMIN
        // =====================================================

        [Permiso("ORDEN_ASIGNAR_MECANICO")]
        public async Task<IActionResult>
            MecanicosDisponibles()
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ObtenerMecanicosDisponiblesAsync(
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            return View(resultado.Data);
        }


        // =====================================================
        // ASIGNAR MECÁNICO
        //
        // SOLO ADMIN
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("ORDEN_ASIGNAR_MECANICO")]
        public async Task<IActionResult>
            AsignarMecanico(
                int ordenTrabajoId,
                int mecanicoId)
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .AsignarMecanicoAsync(
                        ordenTrabajoId,
                        mecanicoId,
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Detalle),
                    new
                    {
                        id = ordenTrabajoId
                    });
            }

            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================================
        // TOMAR ORDEN
        //
        // MECÁNICO
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("ORDEN_VER")]
        public async Task<IActionResult>
            TomarOrden(
                int ordenTrabajoId)
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .TomarOrdenAsync(
                        ordenTrabajoId,
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Detalle),
                    new
                    {
                        id = ordenTrabajoId
                    });
            }

            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================================
        // INICIAR REPARACIÓN
        //
        // MECÁNICO
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("ORDEN_CAMBIAR_ESTADO")]
        public async Task<IActionResult>
            IniciarReparacion(
                int ordenTrabajoId)
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .IniciarReparacionAsync(
                        ordenTrabajoId,
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Detalle),
                    new
                    {
                        id = ordenTrabajoId
                    });
            }

            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================================
        // FINALIZAR
        //
        // MECÁNICO
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("ORDEN_FINALIZAR")]
        public async Task<IActionResult>
            Finalizar(
                int ordenTrabajoId,
                int? horasReales)
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .FinalizarAsync(
                        ordenTrabajoId,
                        personaId.Value,
                        horasReales);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Detalle),
                    new
                    {
                        id = ordenTrabajoId
                    });
            }

            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================================
        // ENTREGAR VEHÍCULO
        //
        // ADMIN / CAJA
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("ORDEN_ENTREGAR")]
        public async Task<IActionResult>
            Entregar(
                int ordenTrabajoId)
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .EntregarAsync(
                        ordenTrabajoId,
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Detalle),
                    new
                    {
                        id = ordenTrabajoId
                    });
            }

            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================================
        // CAMBIAR URGENCIA
        //
        // ADMIN / MECÁNICO ASIGNADO
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("ORDEN_MODIFICAR")]
        public async Task<IActionResult>
            CambiarUrgencia(
                int ordenTrabajoId,
                NivelUrgencia urgencia)
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .CambiarUrgenciaAsync(
                        ordenTrabajoId,
                        urgencia,
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Detalle),
                    new
                    {
                        id = ordenTrabajoId
                    });
            }

            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================================
        // OBSERVACIONES
        //
        // ADMIN / MECÁNICO ASIGNADO
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("ORDEN_MODIFICAR")]
        public async Task<IActionResult>
            ActualizarObservaciones(
                int ordenTrabajoId,
                string? observaciones)
        {
            var personaId = ObtenerPersonaId();

            if (personaId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ActualizarObservacionesAsync(
                        ordenTrabajoId,
                        observaciones,
                        personaId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Detalle),
                    new
                    {
                        id = ordenTrabajoId
                    });
            }

            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                nameof(Detalle),
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================================
        // HELPER
        // =====================================================

        private int? ObtenerPersonaId()
        {
            var claim =
                User.FindFirstValue(
                    "PersonaId");

            if (string.IsNullOrWhiteSpace(
                    claim))
            {
                return null;
            }

            if (int.TryParse(
                    claim,
                    out int personaId))
            {
                return personaId;
            }

            return null;
        }
    }
}