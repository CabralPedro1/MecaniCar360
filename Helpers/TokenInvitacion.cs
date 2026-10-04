using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
namespace MecaniCar360.Helpers;

public static class TokenInvitacion
{
    public static string Generar() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    public static bool FormatoValido(string? token) => token?.Length == 43 &&
        token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
