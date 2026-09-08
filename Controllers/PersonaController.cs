using System.Security.Claims;
using MecaniCar360.Attributes;
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

        public PersonaController(
            PersonaService personaService)
        {
            _personaService = personaService;
        }


        // =====================================
        // INDEX
        // =====================================

        [Permiso("PERSONA_VER")]
        public async Task<IActionResult> Index()
        {
            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado =
                await _personaService.ObtenerTodasAsync(
                    usuarioId.Value);

            if (!resultado.Exitoso)
                return Forbid();

            return View(resultado.Data);
        }


        // =====================================
        // DETALLE
        // =====================================

        [Permiso("PERSONA_VER")]
        public async Task<IActionResult> Detalle(
            int id)
        {
            var usuarioId = ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado =
                await _personaService.ObtenerPorIdAsync(
                    id,
                    usuarioId.Value);

            if (!resultado.Exitoso)
                return NotFound();

            return View(resultado.Data);
        }


        // =====================================
        // CREAR
        // =====================================

        [Permiso("PERSONA_CREAR")]
        public IActionResult Crear()
        {
            return View(
                new PersonaFormViewModel());
        }


        [Permiso("PERSONA_CREAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            PersonaFormViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var usuarioId =
                ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var persona = new Persona
            {
                Nombre = vm.Nombre,
                Apellido = vm.Apellido,
                Dni = vm.Dni,
                Telefono = vm.Telefono,
                Email = vm.Email,
                Activo = true
            };

            var resultado =
                await _personaService.CrearAsync(
                    persona,
                    usuarioId.Value);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(
                    string.Empty,
                    resultado.Mensaje);

                return View(vm);
            }

            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                nameof(Index));
        }


        // =====================================
        // EDITAR
        // =====================================

        [Permiso("PERSONA_MODIFICAR")]
        public async Task<IActionResult> Editar(
            int id)
        {
            var usuarioId =
                ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado =
                await _personaService.ObtenerPorIdParaEditarAsync(
                    id,
                    usuarioId.Value);

            if (!resultado.Exitoso)
                return NotFound();

            var persona =
                resultado.Data!;

            var vm =
                new PersonaFormViewModel
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


        [Permiso("PERSONA_MODIFICAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            PersonaFormViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var usuarioId =
                ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var persona = new Persona
            {
                Id = vm.Id,
                Nombre = vm.Nombre,
                Apellido = vm.Apellido,
                Dni = vm.Dni,
                Telefono = vm.Telefono,
                Email = vm.Email
            };

            var resultado =
                await _personaService.EditarAsync(
                    persona,
                    usuarioId.Value);

            if (!resultado.Exitoso)
            {
                ModelState.AddModelError(
                    string.Empty,
                    resultado.Mensaje);

                return View(vm);
            }

            TempData["Ok"] =
                resultado.Mensaje;

            return RedirectToAction(
                nameof(Index));
        }


        // =====================================
        // ACTIVAR
        // =====================================

        [Permiso("PERSONA_DESACTIVAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(
            int id)
        {
            var usuarioId =
                ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado =
                await _personaService.ActivarAsync(
                    id,
                    usuarioId.Value);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Index));
        }


        // =====================================
        // DESACTIVAR
        // =====================================

        [Permiso("PERSONA_DESACTIVAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(
            int id)
        {
            var usuarioId =
                ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado =
                await _personaService.DesactivarAsync(
                    id,
                    usuarioId.Value);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(Index));
        }


        // =====================================
        // ADMINISTRAR ROLES
        // =====================================

        [Permiso("PERSONA_VER")]
        [Permiso("ROL_VER")]
        public async Task<IActionResult> AdministrarRoles(
            int id)
        {
            var usuarioId =
                ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var personaResult =
                await _personaService.ObtenerPorIdAsync(
                    id,
                    usuarioId.Value);

            if (!personaResult.Exitoso)
                return NotFound();

            var rolesActuales =
                await _personaService
                    .ObtenerRolesPersonaAsync(
                        id,
                        usuarioId.Value);

            var rolesDisponibles =
                await _personaService
                    .ObtenerRolesDisponiblesAsync(
                        id,
                        usuarioId.Value);

            if (!rolesActuales.Exitoso ||
                !rolesDisponibles.Exitoso)
            {
                return Forbid();
            }

            var vm =
                new AdministrarRolesViewModel
                {
                    Persona =
                        personaResult.Data!,

                    RolesActuales =
                        rolesActuales.Data!,

                    RolesDisponibles =
                        rolesDisponibles.Data!
                };

            return View(vm);
        }


        // =====================================
        // ASIGNAR ROL
        // =====================================

        [Permiso("ROL_MODIFICAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarRol(
            int personaId,
            int rolId)
        {
            var usuarioId =
                ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado =
                await _personaService.AsignarRolAsync(
                    personaId,
                    rolId,
                    usuarioId.Value);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(AdministrarRoles),
                new { id = personaId });
        }


        // =====================================
        // QUITAR ROL
        // =====================================

        [Permiso("ROL_MODIFICAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarRol(
            int personaId,
            int rolId)
        {
            var usuarioId =
                ObtenerUsuarioActualId();

            if (!usuarioId.HasValue)
                return Unauthorized();

            var resultado =
                await _personaService.QuitarRolAsync(
                    personaId,
                    rolId,
                    usuarioId.Value);

            TempData[
                resultado.Exitoso
                    ? "Ok"
                    : "Error"
            ] = resultado.Mensaje;

            return RedirectToAction(
                nameof(AdministrarRoles),
                new { id = personaId });
        }


        // =====================================
        // USUARIO ACTUAL
        // =====================================

        private int? ObtenerUsuarioActualId()
        {
            var claim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier);

            if (claim == null)
                return null;

            if (!int.TryParse(
                claim.Value,
                out int usuarioId))
            {
                return null;
            }

            return usuarioId;
        }
    }
}