namespace MecaniCar360.Middleware;

// Conserva las políticas CSP existentes y añade una restricción de framing para HTML.
public sealed class CabecerasHtmlMiddleware(RequestDelegate siguiente)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
            {
                var headers = context.Response.Headers;
                headers["X-Frame-Options"] = "DENY";
                const string framing = "frame-ancestors 'none'";
                var policies = headers["Content-Security-Policy"].ToString();
                // Las políticas CSP múltiples se aplican conjuntamente. No reemplazar
                // directivas de scripts/estilos ni añadir dos frame-ancestors a una política.
                var deniesFraming = policies.Split(',').Any(policy =>
                {
                    var directive = policy.Split(';').Select(d => d.Split(
                        (char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                        .FirstOrDefault(parts => parts.Length > 0 &&
                            parts[0].Equals("frame-ancestors", StringComparison.OrdinalIgnoreCase));
                    return directive is { Length: 2 } && directive[1] == "'none'";
                });
                if (!deniesFraming)
                    headers["Content-Security-Policy"] = string.IsNullOrWhiteSpace(policies)
                        ? framing : policies + ", " + framing;
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
