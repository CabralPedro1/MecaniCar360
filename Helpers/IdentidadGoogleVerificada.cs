using System.Security.Claims;

namespace MecaniCar360.Helpers;

public sealed record IdentidadGoogleVerificada(string Subject, string Email)
{
    public static IdentidadGoogleVerificada? Leer(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = IdentificadorCuenta.Normalizar(principal.FindFirstValue(ClaimTypes.Email));
        return principal.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(subject) && subject.Length <= 255 &&
            bool.TryParse(principal.FindFirstValue(GoogleClienteConfiguracion.EmailVerificado), out var verificado) && verificado &&
            IdentificadorCuenta.EmailValido(email) ? new(subject, email) : null;
    }
}
