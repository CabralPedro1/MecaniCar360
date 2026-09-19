using System.Security.Claims;
using MecaniCar360.Attributes;
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
        private readonly PersonaService _personaService;

        public VehiculoController(
            VehiculoService vehiculoService,
            DominioVehicularService dominioVehicularService,
            MarcaService marcaService,
            PersonaService personaService)
        {
            _vehiculoService = vehiculoService;
            _dominioVehicularService = dominioVehicularService;
            _marcaService = marcaService;
            _personaService = personaService;
        }

        // =====================================
        // INDEX
        // =====================================

        [Permiso("VEHICULO_VER")]
        public async Task<IActionResult> Index([FromQuery] int? personaId = null)
        {
            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            if (!ModelState.IsValid ||
                (Request.Query.ContainsKey(nameof(personaId)) && !personaId.HasValue) ||
                (personaId.HasValue && personaId.Value <= 0))
                return BadRequest("La persona indicada no es válida.");

            if (personaId.HasValue)
            {
                var persona = await _personaService.ObtenerPorIdAsync(personaId.Value, usuarioId.Value);
                if (!persona.Exitoso || persona.Data == null)
                    return NotFound();
                if (!persona.Data.Activo)
                    return BadRequest("La persona indicada está inactiva.");
            }

            var resultado = personaId.HasValue
                ? await _dominioVehicularService.ObtenerVehiculosDePersonaAsync(personaId.Value, usuarioId.Value)
                : await _vehiculoService.ObtenerTodosAsync(usuarioId.Value);

            ViewData["PersonaId"] = personaId;
            if (!resultado.Exitoso)
            {
                ViewData["Error"] = resultado.Mensaje;
                return View(new List<Vehiculo>());
            }

            return View(resultado.Data);
        }

        // =====================================
        // DETALLE
        // =====================================

        [Permiso("VEHICULO_VER")]
        public async Task<IActionResult> Detalle(int id, int? personaId = null)
        {
            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _vehiculoService
                .ObtenerPorIdAsync(id, usuarioId.Value);

            if (!resultado.Exitoso)
                return NotFound();

            ViewData["PersonaId"] = personaId > 0 ? personaId : null;
            return View(resultado.Data);
        }

        // =====================================
        // CREAR
        // =====================================

        [Permiso("VEHICULO_CREAR")]
        public async Task<IActionResult> Crear(int personaId)
        {
            if (personaId <= 0)
                return BadRequest("Seleccione una persona para registrar su vehículo.");
            await CargarMarcasAsync();
            await CargarModelosAsync();

            return View(new VehiculoViewModel
            {
                PersonaId = personaId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("VEHICULO_CREAR")]
        public async Task<IActionResult> Crear(VehiculoViewModel model)
        {
            PrepararValidacionFormulario();
            if (model.PersonaId <= 0)
                ModelState.AddModelError(nameof(model.PersonaId), "Seleccione una persona válida.");
            if (!ModelState.IsValid)
            {
                await CargarMarcasAsync(model.Vehiculo.MarcaId);
                await CargarModelosAsync(
                    model.Vehiculo.MarcaId,
                    model.Vehiculo.ModeloId);

                return View(model);
            }

            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _dominioVehicularService
                .CrearVehiculoAsync(
                    model.PersonaId,
                    model.Vehiculo,
                    usuarioId.Value);

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

        [Permiso("VEHICULO_MODIFICAR")]
        public async Task<IActionResult> Editar(int id, int personaId)
        {
            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _vehiculoService
                .ObtenerPorIdParaEditarAsync(id, usuarioId.Value);

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
        [Permiso("VEHICULO_MODIFICAR")]
        public async Task<IActionResult> Editar(VehiculoViewModel model)
        {
            PrepararValidacionFormulario();
            if (!ModelState.IsValid)
            {
                await CargarMarcasAsync(model.Vehiculo.MarcaId);
                await CargarModelosAsync(
                    model.Vehiculo.MarcaId,
                    model.Vehiculo.ModeloId);

                return View(model);
            }

            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _vehiculoService
                .EditarAsync(model.Vehiculo, usuarioId.Value);

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

            return RedirectToAction(nameof(Detalle), new { id = model.Vehiculo.Id, personaId = model.PersonaId > 0 ? (int?)model.PersonaId : null });
        }

        // =====================================
        // CAMBIAR ESTADO
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("VEHICULO_MODIFICAR")]
        public async Task<IActionResult> CambiarEstado(int id, int personaId)
        {
            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado = await _vehiculoService
                .CambiarEstadoAsync(id, usuarioId.Value);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Detalle), new { id, personaId = personaId > 0 ? (int?)personaId : null });
        }

        // =====================================
        // AJAX
        // =====================================

        [HttpGet]
        [Permiso("VEHICULO_VER", "VEHICULO_CREAR", "VEHICULO_MODIFICAR")]
        public async Task<IActionResult> ObtenerModelos(int marcaId)
        {
            var resultado = await _vehiculoService.ObtenerModelosPorMarcaAsync(marcaId, ObtenerUsuarioActualId() ?? 0);

            if (!resultado.Exitoso)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No se pudieron cargar los modelos." });

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
            var resultado = await _marcaService.ObtenerTodosAsync(ObtenerUsuarioActualId() ?? 0);
            if (!resultado.Exitoso)
                ModelState.AddModelError(string.Empty, resultado.Mensaje);

            ViewBag.Marcas = new SelectList(
                (resultado.Data ?? new List<Marca>())
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
                    .ObtenerModelosPorMarcaAsync(marcaId.Value, ObtenerUsuarioActualId() ?? 0);

                if (resultado.Exitoso)
                    modelos = resultado.Data!;
                else
                    ModelState.AddModelError(string.Empty, resultado.Mensaje);
            }

            ViewBag.Modelos = new SelectList(
                modelos,
                "Id",
                "Nombre",
                seleccionado);
        }

        private void PrepararValidacionFormulario()
        {
            // El formulario envía las FK; el service valida existencia y relación del catálogo.
            // Estas navegaciones EF no son campos editables ni datos requeridos del cliente.
            ModelState.Remove("Vehiculo.Marca");
            ModelState.Remove("Vehiculo.Modelo");
        }

        [HttpGet, Permiso("CLIENTE_VEHICULO_VER")]
        public async Task<IActionResult> MisVehiculos()
        {
            var resultado = await _vehiculoService.ObtenerVehiculosPropiosAsync(ObtenerUsuarioActualId() ?? 0);
            if (!resultado.Exitoso) return Forbid();
            return Json(resultado.Data!.Select(v => new { v.Id, v.Patente, v.Anio, Marca = v.Marca.Nombre, Modelo = v.Modelo.Nombre }));
        }

        [HttpGet, Permiso("CLIENTE_VEHICULO_VER")]
        public async Task<IActionResult> Propio(int id)
        {
            var resultado = await _vehiculoService.ObtenerVehiculoPropioAsync(ObtenerUsuarioActualId() ?? 0, id);
            if (!resultado.Exitoso) return NotFound();
            var v = resultado.Data!;
            return Json(new { v.Id, v.Patente, v.Anio, v.Color, Marca = v.Marca.Nombre, Modelo = v.Modelo.Nombre });
        }

        private int? ObtenerUsuarioActualId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null || !int.TryParse(claim.Value, out int usuarioId))
                return null;

            return usuarioId;
        }
    }
}
