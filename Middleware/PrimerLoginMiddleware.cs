using System.Security.Claims;
using MecaniCar360.Controllers;
using MecaniCar360.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Middleware;

public sealed class PrimerLoginMiddleware
{
    private readonly RequestDelegate _next;

    public PrimerLoginMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, MecaniCarContext db)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();

        // Sólo la reejecución interna del manejador de errores puede omitir la consulta.
        // Así un fallo de BD no provoca una segunda consulta para presentar el error.
        if (action?.ControllerTypeInfo.AsType() == typeof(HomeController) &&
            action.ActionName == nameof(HomeController.Error) &&
            context.Features.Get<IExceptionHandlerPathFeature>() != null)
        {
            await _next(context);
            return;
        }

        if (!int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            await RechazarIdentidadAsync(context);
            return;
        }

        // No capturar fallos de consulta como si el primer ingreso estuviera completo.
        var usuario = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new { u.PrimerLogin, u.Activo, PersonaActiva = u.Persona.Activo })
            .SingleOrDefaultAsync(context.RequestAborted);

        if (usuario == null || !usuario.Activo || !usuario.PersonaActiva)
        {
            await RechazarIdentidadAsync(context);
            return;
        }

        if (!usuario.PrimerLogin || EsAccionPermitida(context, action))
        {
            await _next(context);
            return;
        }

        Redirigir(context, "/Account/CompletarDatos");
    }

    private static bool EsAccionPermitida(HttpContext context, ControllerActionDescriptor? action)
    {
        if (action?.ControllerTypeInfo.AsType() != typeof(AccountController)) return false;

        return (action.ActionName == nameof(AccountController.CompletarDatos) &&
                (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsPost(context.Request.Method))) ||
               (action.ActionName == nameof(AccountController.Logout) &&
                HttpMethods.IsPost(context.Request.Method));
    }

    private static async Task RechazarIdentidadAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        Redirigir(context, "/Account/Login");
    }

    private static void Redirigir(HttpContext context, string path)
    {
        context.Response.StatusCode = HttpMethods.IsGet(context.Request.Method)
            ? StatusCodes.Status302Found
            : StatusCodes.Status303SeeOther;
        context.Response.Headers.Location = context.Request.PathBase.Add(new PathString(path)).Value;
    }
}
