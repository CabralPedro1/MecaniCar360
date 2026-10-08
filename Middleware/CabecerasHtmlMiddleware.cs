namespace MecaniCar360.Middleware;

// No altera CSP, cookies ni respuestas de recursos estáticos/OAuth.
public sealed class CabecerasHtmlMiddleware(RequestDelegate siguiente)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
            {
                var headers = context.Response.Headers;
                if (!headers.ContainsKey("X-Content-Type-Options")) headers["X-Content-Type-Options"] = "nosniff";
                if (!headers.ContainsKey("Referrer-Policy")) headers["Referrer-Policy"] = "no-referrer";
                // Evitar que el navegador/proxy guarde formularios o páginas privadas.
                headers.CacheControl = "no-store";
            }
            return Task.CompletedTask;
        });
        return siguiente(context);
    }
}
