using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Helpers;

public static class IdentificadorCuenta
{
    // SQL Server aplica la misma comparacion en consultas e indices UNIQUE.
    public const string Collation = "Latin1_General_100_CI_AS";
    public static string Normalizar(string? valor) => valor?.Trim() ?? string.Empty;
    // Clave en memoria para flujos que ya usan comparacion invariante.
    // No usar dentro de predicados EF ni para sustituir ToUpper dependiente de cultura.
    internal static string ClaveEmailInvariante(string valor) => valor.Trim().ToUpperInvariant();
    public static bool UsernameValido(string? valor)
    {
        var normalizado = Normalizar(valor);
        return normalizado.Length is > 0 and <= 50 && !normalizado.Contains('@');
    }
    public static bool EmailValido(string? valor)
    {
        var normalizado = Normalizar(valor);
        return normalizado.Length is > 0 and <= 100 && new EmailAddressAttribute().IsValid(normalizado);
    }
}
