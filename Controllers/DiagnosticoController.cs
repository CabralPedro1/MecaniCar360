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
        private readonly OrdenTrabajoService _ordenes;
        public DiagnosticoController(DiagnosticoService diagnosticoService, OrdenTrabajoService ordenes)
        {
            _diagnosticoService = diagnosticoService;
            _ordenes = ordenes;
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
        [Permiso("DIAGNOSTICO_CREAR", "DIAGNOSTICO_MODIFICAR")]
        public async Task<IActionResult> Guardar(int id, string descripcion, IEnumerable<int>? evidenciaIds = null)
        {
            if (!ObtenerUsuarioId(out var usuarioSolicitanteId)) return Forbid();
            if (!ModelState.IsValid)
            {
                return await MostrarRechazo(id, usuarioSolicitanteId);
            }
            var resultado = await _diagnosticoService.GuardarAsync(id, usuarioSolicitanteId, descripcion, evidenciaIds);
            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return await MostrarRechazo(id, usuarioSolicitanteId);
            }
            TempData["Ok"] = resultado.Mensaje;
            return RedirectToAction("Detalle", "OrdenTrabajo", new { id });
        }

        private async Task<IActionResult> MostrarRechazo(int id, int usuarioId)
        {
            // La consulta existente exige permiso de detalle y alcance de la OT.
            var orden = await _ordenes.ObtenerPorIdAsync(id, usuarioId);
            if (!orden.Exitoso) return NotFound();
            return View("Guardar", orden.Data);
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
                    id = h.Id,
                    descripcion = h.Descripcion,
                    fecha = h.Fecha,
                    tipoRegistro = h.TipoRegistro.ToString(),
                    registradoPorUsuarioId = h.RegistradoPorUsuarioId,
                    evidencias = h.Evidencias.Select(e => new
                    {
                        id = e.EvidenciaTrabajoId,
                        descripcion = e.EvidenciaTrabajo.Descripcion,
                        fecha = e.EvidenciaTrabajo.Fecha
                    }).ToList(),
                    mecanico = h.Mecanico == null ? null : $"{h.Mecanico.Apellido}, {h.Mecanico.Nombre}"
                }).ToList()
            });
        }

        private bool ObtenerUsuarioId(out int usuarioSolicitanteId) =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioSolicitanteId);
    }
}
