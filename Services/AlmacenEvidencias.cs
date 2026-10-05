using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace MecaniCar360.Services;

public sealed class AlmacenEvidencias
{
    public const int LimiteBytes = 10 * 1024 * 1024;
    private readonly string _raiz;
    private static readonly Dictionary<string, string> Tipos = new(StringComparer.OrdinalIgnoreCase)
    { [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp", [".pdf"] = "application/pdf" };

    public AlmacenEvidencias(IWebHostEnvironment entorno, IConfiguration configuracion)
    {
        _raiz = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, configuracion["Evidencias:Directorio"] ?? "App_Data/Evidencias"));
        var publica = Path.GetFullPath(entorno.WebRootPath ?? Path.Combine(entorno.ContentRootPath, "wwwroot"));
        if (_raiz.Equals(publica, StringComparison.OrdinalIgnoreCase) || _raiz.StartsWith(publica + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El almacenamiento de evidencias debe ser privado.");
    }

    public static string? Mime(string clave) => Tipos.GetValueOrDefault(Path.GetExtension(clave));

    private string Ruta(string clave)
    {
        if (!Regex.IsMatch(clave, @"\A[a-f0-9]{32}\.(jpg|jpeg|png|webp|pdf)\z"))
            throw new InvalidDataException("Referencia de archivo no válida.");
        // No seguir enlaces simbólicos/junctions configurados dentro del almacenamiento.
        for (var d = new DirectoryInfo(_raiz); d != null; d = d.Parent)
            if (d.Exists && (d.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Almacenamiento no disponible.");
        var ruta = Path.Combine(_raiz, clave);
        if (File.Exists(ruta) && (File.GetAttributes(ruta) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Archivo no disponible.");
        return ruta;
    }

    public async Task<(byte[] Datos, string Extension)> ValidarAsync(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!Tipos.TryGetValue(extension, out var mime) || archivo.Length <= 0 || archivo.Length > LimiteBytes)
            throw new InvalidDataException("Seleccione JPG, PNG, WEBP o PDF no vacío de hasta 10 MB.");
        if (!string.IsNullOrWhiteSpace(archivo.ContentType) && archivo.ContentType != "application/octet-stream" &&
            !string.Equals(archivo.ContentType, mime, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("El tipo declarado no coincide con la extensión.");
        await using var input = archivo.OpenReadStream();
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int n;
        while ((n = await input.ReadAsync(buffer)) > 0)
        {
            if (output.Length + n > LimiteBytes) throw new InvalidDataException("El archivo excede 10 MB.");
            output.Write(buffer, 0, n);
        }
        var bytes = output.ToArray();
        if (!FirmaValida(bytes, extension)) throw new InvalidDataException("El contenido no corresponde al formato permitido.");
        return (bytes, extension);
    }

    private static bool FirmaValida(byte[] b, string ext)
    {
        if (b.Length < 16) return false;
        return ext switch
        {
            ".jpg" or ".jpeg" => b[0] == 255 && b[1] == 216 && b[2] == 255 && b[^2] == 255 && b[^1] == 217,
            ".png" => b.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) && b.Length >= 45 &&
                Encoding.ASCII.GetString(b, 12, 4) == "IHDR" && Encoding.ASCII.GetString(b, b.Length - 8, 4) == "IEND",
            ".webp" => b.Length >= 30 && Encoding.ASCII.GetString(b,0,4) == "RIFF" && Encoding.ASCII.GetString(b,8,4) == "WEBP" &&
                BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(4,4)) == b.Length - 8 &&
                new[] {"VP8 ","VP8L","VP8X"}.Contains(Encoding.ASCII.GetString(b,12,4)),
            ".pdf" => Encoding.ASCII.GetString(b,0,5) == "%PDF-" && Encoding.ASCII.GetString(b, Math.Max(0,b.Length-1024), Math.Min(1024,b.Length)).Contains("%%EOF"),
            _ => false
        };
    }

    public async Task GuardarAsync(string clave, byte[] datos)
    {
        var ruta = Ruta(clave);
        Directory.CreateDirectory(_raiz);
        bool creado = false;
        try
        {
            await using var f = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            creado = true;
            await f.WriteAsync(datos);
        }
        catch
        {
            if (creado) File.Delete(ruta);
            throw;
        }
    }
    public void EliminarCreado(string clave) => File.Delete(Ruta(clave));
    public Stream Abrir(string clave) => new FileStream(Ruta(clave), FileMode.Open, FileAccess.Read, FileShare.Read);
}
