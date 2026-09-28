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
        private readonly PermisoService _permisos;

        public OrdenTrabajoController(
            OrdenTrabajoService ordenTrabajoService, PermisoService permisos)
        {
            _ordenTrabajoService = ordenTrabajoService;
            _permisos = permisos;
        }


        // =====================================================
        // INDEX
        // =====================================================

        [HttpGet]
        [Permiso("ORDEN_VER")]
        public async Task<IActionResult> Index()
        {
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ObtenerTodasAsync(
                        usuarioSolicitanteId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            await PrepararNavegacionAsync(usuarioSolicitanteId.Value);
            return View(resultado.Data);
        }


        // =====================================================
        // DETALLE
        // =====================================================

        [HttpGet]
        [Permiso("ORDEN_VER_DETALLE")]
        public async Task<IActionResult> Detalle(
            int id)
        {
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ObtenerPorIdAsync(
                        id,
                        usuarioSolicitanteId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            await PrepararNavegacionAsync(usuarioSolicitanteId.Value);
            return View(resultado.Data);
        }


        // =====================================================
        // ÓRDENES PENDIENTES
        // =====================================================

        [HttpGet]
        [Permiso("ORDEN_VER")]
        public async Task<IActionResult> Pendientes()
        {
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ObtenerPendientesAsync(
                        usuarioSolicitanteId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            await PrepararNavegacionAsync(usuarioSolicitanteId.Value);
            return View(resultado.Data);
        }


        // =====================================================
        // ÓRDENES DEL MECÁNICO
        // =====================================================

        [HttpGet]
        [Permiso("ORDEN_VER")]
        public async Task<IActionResult> DeMecanico(
            int? mecanicoId = null)
        {
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ObtenerDeMecanicoAsync(
                        mecanicoId ?? await _permisos.ObtenerPersonaActivaIdAsync(usuarioSolicitanteId.Value) ?? 0,
                        usuarioSolicitanteId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            await PrepararNavegacionAsync(usuarioSolicitanteId.Value);
            return View(
                "DeMecanico",
                resultado.Data);
        }


        // =====================================================
        // MECÁNICOS DISPONIBLES
        //
        // SOLO ADMIN
        // =====================================================

        [HttpGet]
        [Permiso("ORDEN_ASIGNAR_MECANICO")]
        public async Task<IActionResult>
            MecanicosDisponibles(int? ordenTrabajoId = null)
        {
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            if (!ModelState.IsValid || ordenTrabajoId <= 0) return BadRequest("Orden invalida.");
            ViewData["OrdenTrabajoId"] = ordenTrabajoId;

            var resultado =
                await _ordenTrabajoService
                    .ObtenerMecanicosDisponiblesAsync(
                        usuarioSolicitanteId.Value);

            if (!resultado.Exitoso)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return RedirectToAction(
                    nameof(Index));
            }

            await PrepararNavegacionAsync(usuarioSolicitanteId.Value);
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
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .AsignarMecanicoAsync(
                        ordenTrabajoId,
                        mecanicoId,
                        usuarioSolicitanteId.Value);

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
        [Permiso("ORDEN_MODIFICAR")]
        public async Task<IActionResult>
            TomarOrden(
                int ordenTrabajoId)
        {
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .TomarOrdenAsync(
                        ordenTrabajoId,
                        usuarioSolicitanteId.Value);

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
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .IniciarReparacionAsync(
                        ordenTrabajoId,
                        usuarioSolicitanteId.Value);

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
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .FinalizarAsync(
                        ordenTrabajoId,
                        usuarioSolicitanteId.Value,
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
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .EntregarAsync(
                        ordenTrabajoId,
                        usuarioSolicitanteId.Value);

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
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .CambiarUrgenciaAsync(
                        ordenTrabajoId,
                        urgencia,
                        usuarioSolicitanteId.Value);

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
            var usuarioSolicitanteId = ObtenerUsuarioId();

            if (usuarioSolicitanteId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var resultado =
                await _ordenTrabajoService
                    .ActualizarObservacionesAsync(
                        ordenTrabajoId,
                        observaciones,
                        usuarioSolicitanteId.Value);

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
        // COSTO DE DIAGNÓSTICO / REVISIÓN
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("ORDEN_MODIFICAR")]
        public async Task<IActionResult> ActualizarCostoDiagnostico(int ordenTrabajoId, decimal costoDiagnostico)
        {
            if (ObtenerUsuarioId() is not int usuarioId) return Forbid();
            if (!ModelState.IsValid)
                TempData["Error"] = "Ingrese un costo de diagnóstico válido.";
            else
            {
                var resultado = await _ordenTrabajoService.ActualizarCostoDiagnosticoAsync(
                    ordenTrabajoId, costoDiagnostico, usuarioId);
                TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;
            }
            return RedirectToAction(nameof(Detalle), new { id = ordenTrabajoId });
        }

        [HttpGet, Permiso("CLIENTE_ORDEN_VER")]
        public async Task<IActionResult> MisOrdenes()
        {
            var resultado = await _ordenTrabajoService.ObtenerPropiasAsync(ObtenerUsuarioId() ?? 0);
            if (!resultado.Exitoso) return Forbid();
            return Json(resultado.Data!.Select(o => new { o.Id, o.EstadoActual, o.FechaInicio, o.FechaFin }));
        }

        [HttpGet, Permiso("CLIENTE_ORDEN_VER")]
        public async Task<IActionResult> Propia(int id)
        {
            var resultado = await _ordenTrabajoService.ObtenerPropiaAsync(id, ObtenerUsuarioId() ?? 0);
            if (!resultado.Exitoso) return NotFound();
            var o = resultado.Data!;
            return Json(new { o.Id, o.EstadoActual, o.FechaInicio, o.FechaFin });
        }

        private async Task PrepararNavegacionAsync(int usuarioId)
        {
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioId);
            if (personaId.HasValue && await _permisos.TienePermisoAsync(usuarioId, "ORDEN_VER"))
            {
                // Reutilizar la elegibilidad y alcance del service, sin roles desde Claims.
                var propias = await _ordenTrabajoService.ObtenerDeMecanicoAsync(personaId.Value, usuarioId);
                ViewData["EsMecanicoConsultable"] = propias.Exitoso;
                ViewData["PuedeTomar"] = propias.Exitoso &&
                    await _permisos.TienePermisoAsync(usuarioId, "ORDEN_MODIFICAR");
            }
        }

        private int? ObtenerUsuarioId()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(
                    claim))
            {
                return null;
            }

            if (int.TryParse(
                    claim,
                    out int usuarioSolicitanteId))
            {
                return usuarioSolicitanteId;
            }

            return null;
        }
    }
}
