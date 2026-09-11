using MecaniCar360.Attributes;
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
        public DiagnosticoController(DiagnosticoService diagnosticoService)
        {
            _diagnosticoService = diagnosticoService;
        }

        [HttpGet]
        [Permiso("DIAGNOSTICO_VER")]
        public async Task<IActionResult> Obtener(int id)
        {
            if (!ObtenerUsuarioId(out var usuarioSolicitanteId)) return Forbid();
            var resultado = await _diagnosticoService.ObtenerAsync(id, usuarioSolicitanteId);
            if (!resultado.Exitoso)
                return Json(new { exitoso = false, mensaje = resultado.Mensaje });
            return Json(new
            {
                exitoso = true,
                diagnostico = new
                {
                    id = resultado.Data!.Id,
                    descripcion = resultado.Data.DescripcionActual,
                    fecha = resultado.Data.FechaUltimaModificacion
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("DIAGNOSTICO_CREAR")]
        public async Task<IActionResult> Iniciar(int id)
        {
            if (!ObtenerUsuarioId(out var usuarioSolicitanteId)) return Forbid();
            var resultado = await _diagnosticoService.IniciarAsync(id, usuarioSolicitanteId);
            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Detalle", "OrdenTrabajo", new { id });
        }

        // La patente se decide en el service según exista o no el diagnóstico.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(int id, string descripcion)
        {
            if (!ObtenerUsuarioId(out var usuarioSolicitanteId)) return Forbid();
            var resultado = await _diagnosticoService.GuardarAsync(id, usuarioSolicitanteId, descripcion);
            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Detalle", "OrdenTrabajo", new { id });
        }

        [HttpGet]
        [Permiso("DIAGNOSTICO_HISTORIAL")]
        public async Task<IActionResult> Historial(int id)
        {
            if (!ObtenerUsuarioId(out var usuarioSolicitanteId)) return Forbid();
            var resultado = await _diagnosticoService.ObtenerHistorialAsync(id, usuarioSolicitanteId);
            if (!resultado.Exitoso)
                return Json(new { exitoso = false, mensaje = resultado.Mensaje });
            return Json(new
            {
                exitoso = true,
                historial = resultado.Data!.Select(h => new
                {
                    descripcion = h.Descripcion,
                    fecha = h.Fecha,
                    mecanico = h.Mecanico == null ? null : $"{h.Mecanico.Apellido}, {h.Mecanico.Nombre}"
                }).ToList()
            });
        }

        private bool ObtenerUsuarioId(out int usuarioSolicitanteId) =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioSolicitanteId);
    }
}
