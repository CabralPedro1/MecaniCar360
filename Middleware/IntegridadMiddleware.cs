using MecaniCar360.Models.DTOs;
using MecaniCar360.Services;

namespace MecaniCar360.Middleware;

public sealed class IntegridadMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, EstadoIntegridad estado)
    {
        var resultado = estado.Resultado;
        if (resultado.Estado == CondicionIntegridad.Valida) return next(context);
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        // No MVC layout, authentication, session, script, style, or database dependency.
        var incidente = resultado.IncidenteId == null ? "" : "<p>Incidente: " + System.Net.WebUtility.HtmlEncode(resultado.IncidenteId) + "</p>";
        return context.Response.WriteAsync("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><title>Sistema no disponible</title></head><body><main><h1>Sistema no disponible</h1><p>El sistema no está disponible debido a una verificación de integridad.</p>" + incidente + "</main></body></html>");
    }
}
