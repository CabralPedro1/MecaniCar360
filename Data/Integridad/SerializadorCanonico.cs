using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MecaniCar360.Data.Integridad;

public static class SerializadorCanonico
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;
    public static string Token(object? valor)
    {
        if (valor is null or DBNull) return "N;";
        var texto = valor switch
        {
            string s => s,
            bool b => b ? "1" : "0",
            decimal d => (d == 0 ? 0m : d).ToString("0.00", Cultura),
            DateTime d => d.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", Cultura),
            Enum e => Convert.ToInt64(e, Cultura).ToString(Cultura),
            int or long or short or byte => Convert.ToString(valor, Cultura)!,
            _ => throw new InvalidOperationException("Tipo no admitido en representacion canonica.")
        };
        return "V" + Encoding.UTF8.GetByteCount(texto).ToString(Cultura) + ":" + texto + ";";
    }
    public static string Dvh(string entidad, IEnumerable<object?> valores) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            "MC360|DVH|1|" + Token(entidad) + "|" + string.Join("|", valores.Select(Token)))));

    // Rows MUST arrive in numeric SQL primary-key order. Compound key is a framed token per component.
    public static string Dvv(string entidad, long cantidad, IEnumerable<(object?[] Claves, string? Dvh)> filas)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void Add(string s) => hash.AppendData(Encoding.UTF8.GetBytes(s));
        Add("MC360|DVV|1|" + Token(entidad) + "|" + Token(cantidad));
        foreach (var fila in filas)
        {
            foreach (var key in fila.Claves) Add("|" + Token(key));
            Add("|" + Token(fila.Dvh));
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
