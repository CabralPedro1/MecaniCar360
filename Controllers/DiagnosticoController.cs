using MecaniCar360.Attributes;
using MecaniCar360.Helpers;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class DiagnosticoController : Controller
    {
        private readonly DiagnosticoService _diagnosticoService;

        public DiagnosticoController(
            DiagnosticoService diagnosticoService)
        {
            _diagnosticoService =
                diagnosticoService;
        }


        // =====================================
        // OBTENER DIAGNÓSTICO
        //
        // ADMIN / MECÁNICO ASIGNADO
        // =====================================

        [HttpGet]
        [Permiso("DIAGNOSTICO_VER")]
        public async Task<JsonResult> Obtener(
            int id)
        {
            var personaId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _diagnosticoService
                    .ObtenerAsync(
                        id,
                        personaId);

            if (!resultado.Exitoso)
            {
                return Json(new
                {
                    exitoso = false,
                    mensaje = resultado.Mensaje
                });
            }

            return Json(new
            {
                exitoso = true,

                diagnostico = new
                {
                    id =
                        resultado.Data!.Id,

                    descripcion =
                        resultado.Data
                            .DescripcionActual,

                    fecha =
                        resultado.Data
                            .FechaUltimaModificacion
                },

                historial =
                    resultado.Data.Historial
                        .OrderByDescending(
                            h => h.Fecha)
                        .Select(h => new
                        {
                            descripcion =
                                h.Descripcion,

                            fecha =
                                h.Fecha,

                            mecanico =
                                h.Mecanico == null
                                    ? null
                                    : $"{h.Mecanico.Apellido}, {h.Mecanico.Nombre}"
                        })
                        .ToList()
            });
        }


        // =====================================
        // INICIAR DIAGNÓSTICO
        //
        // MECÁNICO
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("DIAGNOSTICO_CREAR")]
        public async Task<IActionResult> Iniciar(
            int id)
        {
            var mecanicoId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _diagnosticoService
                    .IniciarAsync(
                        id,
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
                    id
                });
        }


        // =====================================
        // GUARDAR DIAGNÓSTICO
        //
        // MECÁNICO
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("DIAGNOSTICO_MODIFICAR")]
        public async Task<IActionResult> Guardar(
            int id,
            string descripcion)
        {
            var mecanicoId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _diagnosticoService
                    .GuardarAsync(
                        id,
                        mecanicoId,
                        descripcion);

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
                    id
                });
        }


        // =====================================
        // FINALIZAR DIAGNÓSTICO
        //
        // MECÁNICO
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("DIAGNOSTICO_FINALIZAR")]
        public async Task<IActionResult> Finalizar(
            int id)
        {
            var mecanicoId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _diagnosticoService
                    .FinalizarAsync(
                        id,
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
                    id
                });
        }


        // =====================================
        // HISTORIAL
        //
        // ADMIN / MECÁNICO ASIGNADO
        // =====================================

        [HttpGet]
        [Permiso("DIAGNOSTICO_HISTORIAL")]
        public async Task<JsonResult> Historial(
            int id)
        {
            var personaId =
                ObtenerUsuarioPersonaId();

            var resultado =
                await _diagnosticoService
                    .ObtenerHistorialAsync(
                        id,
                        personaId);

            if (!resultado.Exitoso)
            {
                return Json(new
                {
                    exitoso = false,
                    mensaje = resultado.Mensaje
                });
            }

            return Json(new
            {
                exitoso = true,

                historial =
                    resultado.Data!
                        .Select(h => new
                        {
                            descripcion =
                                h.Descripcion,

                            fecha =
                                h.Fecha,

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

        private int ObtenerUsuarioPersonaId()
        {
            var claim =
                User.FindFirst(
                    "PersonaId");

            if (claim == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el PersonaId en la sesión.");
            }

            if (!int.TryParse(
                    claim.Value,
                    out int personaId))
            {
                throw new InvalidOperationException(
                    "El PersonaId de la sesión no es válido.");
            }

            return personaId;
        }
    }
}