using System.Security.Principal;
using MecaniCar360.Data;
using MecaniCar360.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MecaniCar360.Helpers;

internal static class BootstrapAdminLocal
{
    internal const string Comando = "--bootstrap-admin-enviar";

    // Constructor privado: ningún Controller puede fabricar una autorización con IDs del request.
    internal sealed class Autorizacion
    {
        private readonly string servidor;
        private readonly string baseDatos;
        private Autorizacion(string servidor, string baseDatos)
        { this.servidor = servidor; this.baseDatos = baseDatos; }

        internal static Autorizacion Verificar(IConfiguration config, string servidor, string baseDatos)
        {
            if (!OperatingSystem.IsWindows()) throw new UnauthorizedAccessException();
            using var identidad = WindowsIdentity.GetCurrent();
            if (!OperadorPermitido(identidad.User?.Value, config["BootstrapAdmin:OperadorSid"],
                new WindowsPrincipal(identidad).IsInRole(WindowsBuiltInRole.Administrator)))
                throw new UnauthorizedAccessException();
            var permiso = new Autorizacion(servidor, baseDatos);
            permiso.ComprobarConexion(config.GetConnectionString("DefaultConnection"));
            return permiso;
        }

        private void ComprobarConexion(string? cadena)
        {
            var c = new SqlConnectionStringBuilder(cadena);
            if (!c.IntegratedSecurity || string.IsNullOrWhiteSpace(servidor) || string.IsNullOrWhiteSpace(baseDatos) ||
                !string.Equals(c.DataSource, servidor, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(c.InitialCatalog, baseDatos, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException();
        }

        internal void ComprobarDestino(MecaniCarContext db) => ComprobarConexion(db.Database.GetConnectionString());

        internal async Task VerificarServidorAsync(MecaniCarContext db)
        {
            ComprobarDestino(db);
            await db.Database.OpenConnectionAsync();
            try
            {
                using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')), DB_NAME()";
                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync() || !string.Equals(reader.GetString(0), servidor, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(reader.GetString(1), baseDatos, StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException();
            }
            finally { await db.Database.CloseConnectionAsync(); }
        }
    }

    internal static bool OperadorPermitido(string? efectivo, string? configurado, bool elevado) =>
        elevado && !string.IsNullOrWhiteSpace(efectivo) && !string.IsNullOrWhiteSpace(configurado) &&
        string.Equals(efectivo, configurado, StringComparison.Ordinal);

    internal static async Task<int> EjecutarAsync(string[] args)
    {
        // Entrada deliberadamente estricta; no aceptar este modo desde configuración del host web.
        if (args.Length != 5 || args[0] != Comando || args[1] != "--confirmar-servidor" || args[3] != "--confirmar-base")
        {
            Console.Error.WriteLine("Uso: --bootstrap-admin-enviar --confirmar-servidor <instancia exacta> --confirmar-base <base exacta>.");
            return 2;
        }
        try
        {
            var entorno = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").AddJsonFile($"appsettings.{entorno}.json", optional: true);
            if (entorno == "Development") builder.AddUserSecrets<Program>(optional: true);
            var config = builder.AddEnvironmentVariables().Build();
            var permiso = Autorizacion.Verificar(config, args[2], args[4]);
            _ = ActivacionAdminInicial.EmailConfigurado(config);
            await using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
                .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
            await permiso.VerificarServidorAsync(db);
            var integridad = new IntegridadService(db);
            if (!(await integridad.VerificarAsync()).EsValida) throw new InvalidOperationException();
            var servicio = new ActivacionAdminService(db, new EmailService(config), config,
                TimeProvider.System, NullLogger<ActivacionAdminService>.Instance);
            var resultado = await servicio.EmitirLocalAsync(permiso);
            Console.WriteLine(resultado.Mensaje);
            if (!(await integridad.VerificarAsync()).EsValida) throw new InvalidOperationException();
            return resultado.Exitoso ? 0 : 1;
        }
        catch
        {
            // No imprimir excepciones, cadenas, destinatario ni material de activación.
            Console.Error.WriteLine("Emisión local rechazada. Compruebe operador Windows elevado/SID, destino, configuración, esquema e integridad. No se ejecutan migraciones ni seed.");
            return 1;
        }
    }
}
