using System.Security.Claims;
using MecaniCar360.Attributes;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MecaniCar360.Controllers;

[Authorize]
public sealed class InvitacionClienteController(InvitacionClienteService invitaciones) : Controller
{
    [HttpPost, ValidateAntiForgeryToken, Permiso("USUARIO_CREAR"), Permiso("PERSONA_VER")]
    public async Task<IActionResult> Emitir(int personaId)
    {
        if (!ModelState.IsValid || personaId <= 0) return BadRequest();
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) || actor <= 0) return Unauthorized();
        var resultado = await invitaciones.EmitirAsync(personaId, actor);
        TempData[resultado.Exitoso ? "Ok" : "Error"] = resultado.Mensaje;
        return RedirectToAction("Cliente", "Persona", new { id = personaId });
    }
}
