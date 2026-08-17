using MecaniCar360.Models;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MecaniCar360.Controllers
{
    [Authorize]
    public class VehiculoController : Controller
    {
        private readonly VehiculoService _vehiculoService;
        private readonly DominioVehicularService _dominioVehicularService;
        private readonly MarcaService _marcaService;

        public VehiculoController(
            VehiculoService vehiculoService,
            DominioVehicularService dominioVehicularService,
            MarcaService marcaService)
        {
            _vehiculoService = vehiculoService;
            _dominioVehicularService = dominioVehicularService;
            _marcaService = marcaService;
        }

        // =====================================
        // INDEX
        // =====================================

        public async Task<IActionResult> Index()
        {
            var resultado = await _vehiculoService.ObtenerTodosAsync();

            return View(resultado.Data);
        }

        // =====================================
        // DETALLE
        // =====================================

        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _vehiculoService.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        // =====================================
        // CREAR
        // =====================================

        public async Task<IActionResult> Crear(int personaId)
        {
            await CargarMarcasAsync();
            await CargarModelosAsync();

            return View(new VehiculoViewModel
            {
                PersonaId = personaId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(VehiculoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarMarcasAsync(model.Vehiculo.MarcaId);
                await CargarModelosAsync(
                    model.Vehiculo.MarcaId,
                    model.Vehiculo.ModeloId);

                return View(model);
            }

            var resultado = await _dominioVehicularService
                .CrearVehiculoAsync(
                    model.PersonaId,
                    model.Vehiculo);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);

                await CargarMarcasAsync(model.Vehiculo.MarcaId);
                await CargarModelosAsync(
                    model.Vehiculo.MarcaId,
                    model.Vehiculo.ModeloId);

                return View(model);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(
                "Detalle",
                "Persona",
                new { id = model.PersonaId });
        }

        // =====================================
        // EDITAR
        // =====================================

        public async Task<IActionResult> Editar(int id, int personaId)
        {
            var resultado = await _vehiculoService.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            await CargarMarcasAsync(resultado.Data!.MarcaId);
            await CargarModelosAsync(
                resultado.Data.MarcaId,
                resultado.Data.ModeloId);

            return View(new VehiculoViewModel
            {
                PersonaId = personaId,
                Vehiculo = resultado.Data
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(VehiculoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarMarcasAsync(model.Vehiculo.MarcaId);
                await CargarModelosAsync(
                    model.Vehiculo.MarcaId,
                    model.Vehiculo.ModeloId);

                return View(model);
            }

            var resultado = await _vehiculoService
                .EditarAsync(model.Vehiculo);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);

                await CargarMarcasAsync(model.Vehiculo.MarcaId);
                await CargarModelosAsync(
                    model.Vehiculo.MarcaId,
                    model.Vehiculo.ModeloId);

                return View(model);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(
                "Detalle",
                "Persona",
                new { id = model.PersonaId });
        }

        // =====================================
        // CAMBIAR ESTADO
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, int personaId)
        {
            var resultado = await _vehiculoService.CambiarEstadoAsync(id);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(
                "Detalle",
                "Persona",
                new { id = personaId });
        }

        // =====================================
        // AJAX
        // =====================================

        [HttpGet]
        public async Task<JsonResult> ObtenerModelos(int marcaId)
        {
            var resultado = await _vehiculoService.ObtenerModelosPorMarcaAsync(marcaId);

            if (!resultado.Exitoso)
                return Json(new List<object>());

            return Json(resultado.Data!.Select(m => new
            {
                id = m.Id,
                nombre = m.Nombre
            }));
        }

        // =====================================
        // MÉTODOS PRIVADOS
        // =====================================

        private async Task CargarMarcasAsync(int? seleccionada = null)
        {
            var resultado = await _marcaService.ObtenerTodosAsync();

            ViewBag.Marcas = new SelectList(
                resultado.Data!
                    .Where(m => m.Activo)
                    .OrderBy(m => m.Nombre),
                "Id",
                "Nombre",
                seleccionada);
        }

        private async Task CargarModelosAsync(int? marcaId = null, int? seleccionado = null)
        {
            List<Modelo> modelos = new();

            if (marcaId.HasValue)
            {
                var resultado = await _vehiculoService
                    .ObtenerModelosPorMarcaAsync(marcaId.Value);

                if (resultado.Exitoso)
                    modelos = resultado.Data!;
            }

            ViewBag.Modelos = new SelectList(
                modelos,
                "Id",
                "Nombre",
                seleccionado);
        }
    }
}