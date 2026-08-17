using MecaniCar360.Models;
using MecaniCar360.Patterns.Facade;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class PresupuestoController : Controller
    {
        private readonly PresupuestoService _presupuestoService;
        private readonly MecaniCarFacade _mecaniCarFacade;

        public PresupuestoController(
        PresupuestoService presupuestoService,
        MecaniCarFacade mecaniCarFacade)
        {
            _presupuestoService =
                presupuestoService;

            _mecaniCarFacade =
                mecaniCarFacade;
        }

        // =====================================
        // DETALLE
        // =====================================

        public async Task<IActionResult> Detalle(
            int ordenTrabajoId)
        {
            var resultado =
                await _presupuestoService
                    .ObtenerAsync(ordenTrabajoId);

            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;

                return RedirectToAction(
                    "Detalle",
                    "OrdenTrabajo",
                    new
                    {
                        id = ordenTrabajoId
                    });
            }

            return View(resultado.Data);
        }


        // =====================================
        // CREAR
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
     int ordenTrabajoId)
        {
            var mecanicoId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _mecaniCarFacade
                    .PrepararPresupuestoAsync(
                        ordenTrabajoId,
                        mecanicoId);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                "Detalle",
                "OrdenTrabajo",
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================
        // AGREGAR ITEM
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarItem(
            int presupuestoId,
            int ordenTrabajoId,
            string descripcion,
            int cantidad,
            decimal precioUnitario,
            int? repuestoId)
        {
            var mecanicoId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _presupuestoService
                    .AgregarItemAsync(
                        presupuestoId,
                        mecanicoId,
                        descripcion,
                        cantidad,
                        precioUnitario,
                        repuestoId);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                "Detalle",
                "OrdenTrabajo",
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================
        // ELIMINAR ITEM
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarItem(
            int presupuestoId,
            int itemId,
            int ordenTrabajoId)
        {
            var mecanicoId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _presupuestoService
                    .EliminarItemAsync(
                        presupuestoId,
                        itemId,
                        mecanicoId);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                "Detalle",
                "OrdenTrabajo",
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================
        // ENVIAR A APROBACIÓN
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarAprobacion(
            int presupuestoId,
            int ordenTrabajoId)
        {
            var mecanicoId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _presupuestoService
                    .EnviarAprobacionAsync(
                        presupuestoId,
                        mecanicoId);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                "Detalle",
                "OrdenTrabajo",
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================
        // APROBAR - CLIENTE
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(
            int presupuestoId,
            int ordenTrabajoId)
        {
            var clienteId =
                ObtenerUsuarioPersonaId();


            var usuarioId =
                ObtenerUsuarioId();


            var resultado =
                await _presupuestoService
                    .AprobarAsync(
                        presupuestoId,
                        clienteId,
                        usuarioId);


            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;


            return RedirectToAction(
                "Detalle",
                "OrdenTrabajo",
                new
                {
                    id = ordenTrabajoId
                });
        }

        // =====================================
        // RECHAZAR - CLIENTE
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(
            int presupuestoId,
            int ordenTrabajoId,
            string motivo)
        {
            var clienteId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _presupuestoService
                    .RechazarAsync(
                        presupuestoId,
                        clienteId,
                        motivo);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                "Detalle",
                "OrdenTrabajo",
                new
                {
                    id = ordenTrabajoId
                });
        }


        // =====================================
        // CONSULTA AJAX
        // =====================================

        [HttpGet]
        public async Task<JsonResult> Obtener(
            int ordenTrabajoId)
        {
            var resultado =
                await _presupuestoService
                    .ObtenerAsync(
                        ordenTrabajoId);

            if (!resultado.Exitoso)
            {
                return Json(new
                {
                    exitoso = false,
                    mensaje = resultado.Mensaje
                });
            }

            var presupuesto = resultado.Data!;

            return Json(new
            {
                exitoso = true,

                presupuesto = new
                {
                    id = presupuesto.Id,

                    estado = presupuesto.Estado
                        .ToString(),

                    total = presupuesto.Total,

                    fechaUltimaModificacion =
                        presupuesto.FechaUltimaModificacion,

                    motivoRechazo =
                        presupuesto.MotivoRechazo
                },

                items = presupuesto.Items
                    .Select(i => new
                    {
                        id = i.Id,

                        descripcion =
                            i.Descripcion,

                        cantidad =
                            i.Cantidad,

                        precioUnitario =
                            i.PrecioUnitario,

                        subtotal =
                            i.Subtotal,

                        repuestoId =
                            i.RepuestoId
                    })
                    .ToList(),

                historial = presupuesto.Historial
                    .OrderByDescending(h => h.Fecha)
                    .Select(h => new
                    {
                        totalAnterior =
                            h.TotalAnterior,

                        fecha =
                            h.Fecha,

                        motivo =
                            h.Motivo,

                        mecanico =
                            h.Mecanico == null
                                ? null
                                : $"{h.Mecanico.Apellido}, {h.Mecanico.Nombre}"
                    })
                    .ToList()
            });
        }


        // =====================================
        // MÉTODO PRIVADO
        // =====================================


        private int ObtenerUsuarioId()
        {
            var claim =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier);


            if (claim == null)
            {
                throw new InvalidOperationException(
                    "No se pudo identificar al usuario actual.");
            }


            if (!int.TryParse(
                claim.Value,
                out int usuarioId))
            {
                throw new InvalidOperationException(
                    "El identificador del usuario actual no es válido.");
            }


            return usuarioId;
        }
        private int ObtenerUsuarioPersonaId()
        {
            var claim =
                User.FindFirst("PersonaId");

            if (claim == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el PersonaId en la sesión.");
            }

            return int.Parse(claim.Value);
        }
    }
}