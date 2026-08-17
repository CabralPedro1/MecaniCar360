using System.Security.Claims;
using MecaniCar360.Models;
using MecaniCar360.Services;
using MecaniCar360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace MecaniCar360.Controllers
{
    [Authorize]
    public class PersonaController : Controller
    {
        private readonly PersonaService _personaService;

        public PersonaController(PersonaService personaService)
        {
            _personaService = personaService;
        }

        //=====================================
        // INDEX
        //=====================================

        public async Task<IActionResult> Index()
        {
            var resultado = await _personaService.ObtenerTodasAsync();

            return View(resultado.Data);
        }

        //=====================================
        // DETALLE
        //=====================================

        public async Task<IActionResult> Detalle(int id)
        {
            var resultado = await _personaService.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }

        //=====================================
        // CREAR
        //=====================================

        [Authorize(Roles = "ADMIN")]
        public IActionResult Crear()
        {
            return View(new PersonaFormViewModel());
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(PersonaFormViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var persona = new Persona
            {
                Nombre = vm.Nombre,
                Apellido = vm.Apellido,
                Dni = vm.Dni,
                Telefono = vm.Telefono,
                Email = vm.Email,
                Activo = vm.Activo
            };

            var resultado = await _personaService.CrearAsync(persona);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return View(vm);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        //=====================================
        // EDITAR
        //=====================================

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Editar(int id)
        {
            var resultado = await _personaService.ObtenerPorIdAsync(id);

            if (!resultado.Exitoso)
                return NotFound();

            var persona = resultado.Data!;

            var vm = new PersonaFormViewModel
            {
                Id = persona.Id,
                Nombre = persona.Nombre,
                Apellido = persona.Apellido,
                Dni = persona.Dni,
                Telefono = persona.Telefono,
                Email = persona.Email,
                Activo = persona.Activo
            };

            return View(vm);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(PersonaFormViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var persona = new Persona
            {
                Id = vm.Id,
                Nombre = vm.Nombre,
                Apellido = vm.Apellido,
                Dni = vm.Dni,
                Telefono = vm.Telefono,
                Email = vm.Email,
                Activo = vm.Activo
            };

            var resultado = await _personaService.EditarAsync(persona);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return View(vm);
            }

            TempData["Ok"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        //=====================================
        // ACTIVAR
        //=====================================

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(int id)
        {
            var resultado = await _personaService.ActivarAsync(id);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        //=====================================
        // DESACTIVAR
        //=====================================

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            var resultado = await _personaService.DesactivarAsync(id);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        //=====================================
        // ADMINISTRAR ROLES
        //=====================================

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AdministrarRoles(int id)
        {
            var personaResult = await _personaService.ObtenerPorIdAsync(id);

            if (!personaResult.Exitoso)
                return NotFound();

            var rolesActuales = await _personaService.ObtenerRolesPersonaAsync(id);
            var rolesDisponibles = await _personaService.ObtenerRolesDisponiblesAsync(id);

            var vm = new AdministrarRolesViewModel
            {
                Persona = personaResult.Data!,
                RolesActuales = rolesActuales.Data!,
                RolesDisponibles = rolesDisponibles.Data!
            };

            return View(vm);
        }

        //=====================================
        // ASIGNAR ROL
        //=====================================

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarRol(int personaId, int rolId)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                return Unauthorized();

            if (!int.TryParse(claim.Value, out int usuarioActual))
                return Unauthorized();

            var resultado = await _personaService.AsignarRolAsync(
                personaId,
                rolId,
                usuarioActual);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(AdministrarRoles), new { id = personaId });
        }

        //=====================================
        // QUITAR ROL
        //=====================================

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarRol(int personaId, int rolId)
        {
            var resultado = await _personaService.QuitarRolAsync(personaId, rolId);

            TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(AdministrarRoles), new { id = personaId });
        }
    }
}