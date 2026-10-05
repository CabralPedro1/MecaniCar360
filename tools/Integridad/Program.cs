using MecaniCar360.Data;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var comando = args.FirstOrDefault();
string? Valor(string clave) { var i = Array.IndexOf(args, clave); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
if (comando is not ("verificar" or "inicializar"))
{
    Console.Error.WriteLine("Uso: verificar|inicializar --proyecto <raiz> [--backup <ruta SQL> --confirmar-linea-base --sin-escrituras]. Conexion: configuracion del proyecto/ConnectionStrings__DefaultConnection; nunca pasar secretos por argumentos.");
    return 2;
}
try
{
    var raiz = Path.GetFullPath(Valor("--proyecto") ?? Directory.GetCurrentDirectory());
    var entorno = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    var builder = new ConfigurationBuilder().SetBasePath(raiz).AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{entorno}.json", optional: true);
    if (entorno == "Development") builder.AddUserSecrets<MecaniCar360.Program>(optional: true);
    var configuration = builder.AddEnvironmentVariables().Build();
    await using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
        .UseSqlServer(configuration.GetConnectionString("DefaultConnection")).Options);
    var servicio = new IntegridadService(db);
    if (comando == "inicializar")
        await servicio.InicializarAsync(Valor("--backup") ?? "", args.Contains("--confirmar-linea-base"), args.Contains("--sin-escrituras"));
    var resultado = await servicio.VerificarAsync();
    Console.WriteLine("INTEGRIDAD=" + resultado.Estado);
    foreach (var e in resultado.Errores.Take(100)) Console.WriteLine($"{e.Entidad} | {e.Clave ?? "-"} | {e.Tipo}");
    return resultado.EsValida ? 0 : 1;
}
catch
{
    Console.Error.WriteLine("Operacion tecnica rechazada. Compruebe esquema, ausencia de linea base previa, confirmaciones, backup de esta base y permisos. No se reparan verificadores ni se imprimen excepciones sensibles.");
    return 1;
}
