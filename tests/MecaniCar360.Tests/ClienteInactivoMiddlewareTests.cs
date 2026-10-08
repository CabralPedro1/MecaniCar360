using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using MecaniCar360.Data;
using MecaniCar360.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MecaniCar360.Tests;

public class ClienteInactivoMiddlewareTests
{
    [Theory]
    [InlineData("GET", "/PortalCliente/Index")]
    [InlineData("POST", "/PortalCliente/MisDatos")]
    [InlineData("GET", "/RegistroCompletoCliente/Index")]
    [InlineData("GET", "/Account/AccesoDenegado")]
    [InlineData("GET", "/Account/Login")]
    public async Task ClienteConRolInactivo_RevocaCookieYTerminaSinRedireccion(string metodo, string ruta)
    {
        using var conexion = new ConexionLectura(Usuario(false), Cliente(false));
        using var db = Contexto(conexion);
        var auth = new AutenticacionPrueba();
        using var provider = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = provider, User = Principal() };
        http.Request.Method = metodo; http.Request.Path = ruta;
        var next = 0;
        var middleware = new PrimerLoginMiddleware(_ => { next++; return Task.CompletedTask; });
        await middleware.InvokeAsync(http, db);
        Assert.Equal(403, http.Response.StatusCode);
        Assert.False(http.Response.Headers.ContainsKey("Location"));
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, Assert.Single(auth.Salidas));
        Assert.False(http.User.Identity!.IsAuthenticated);
        Assert.Equal(0, next);
        Assert.Equal(2, conexion.Lecturas);

        // Sin la cookie rechazada, la siguiente petición es anónima: no vuelve a completar registro.
        var siguiente = new DefaultHttpContext { RequestServices = provider };
        siguiente.Request.Path = "/Account/Login";
        await middleware.InvokeAsync(siguiente, db);
        Assert.Equal(1, next);
        Assert.Equal(2, conexion.Lecturas);
        Assert.False(siguiente.Response.Headers.ContainsKey("Location"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PerfilInterno_ConservaSuFlujoDePrimerLogin(bool primerLogin)
    {
        using var conexion = new ConexionLectura(Usuario(primerLogin), Cliente(null));
        using var db = Contexto(conexion);
        var next = false;
        var http = new DefaultHttpContext { User = Principal() };
        http.Request.Method = "GET";
        await new PrimerLoginMiddleware(_ => { next = true; return Task.CompletedTask; }).InvokeAsync(http, db);
        Assert.Equal(!primerLogin, next);
        Assert.Equal(primerLogin ? "/Account/CompletarDatos" : "", http.Response.Headers.Location.ToString());
        Assert.True(http.User.Identity!.IsAuthenticated);
    }

    private static ClaimsPrincipal Principal() => new(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, "123") }, CookieAuthenticationDefaults.AuthenticationScheme));
    private static MecaniCarContext Contexto(DbConnection conexion) => new(new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(conexion).Options);
    private static DataTable Usuario(bool primerLogin)
    {
        var tabla = new DataTable(); tabla.Columns.Add("PrimerLogin", typeof(bool));
        tabla.Columns.Add("Activo", typeof(bool)); tabla.Columns.Add("PersonaActiva", typeof(bool));
        tabla.Rows.Add(primerLogin, true, true); return tabla;
    }
    private static DataTable Cliente(bool? activo)
    {
        var tabla = new DataTable(); tabla.Columns.Add("PersonaId", typeof(int)); tabla.Columns.Add("RolActivo", typeof(bool));
        if (activo.HasValue) tabla.Rows.Add(321, activo.Value); return tabla;
    }

    // Doble ADO de sólo lectura: ejecuta el middleware y la materialización EF sin abrir SQL Server.
    private sealed class ConexionLectura(params DataTable[] resultados) : DbConnection
    {
        private ConnectionState estado;
        public int Lecturas { get; private set; }
        private DbDataReader Leer() => resultados[Lecturas++].CreateDataReader();
        [AllowNull] public override string ConnectionString { get; set; } = "";
        public override string Database => "PruebaSinBase";
        public override string DataSource => "Prueba";
        public override string ServerVersion => "16.0";
        public override ConnectionState State => estado;
        public override void Open() => estado = ConnectionState.Open;
        public override void Close() => estado = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new Comando(this);
        private sealed class Comando(ConexionLectura conexion) : DbCommand
        {
            private readonly Microsoft.Data.SqlClient.SqlCommand parametros = new();
            [AllowNull] public override string CommandText { get; set; } = "";
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; } = conexion;
            protected override DbTransaction? DbTransaction { get; set; }
            protected override DbParameterCollection DbParameterCollection => parametros.Parameters;
            protected override DbParameter CreateDbParameter() => parametros.CreateParameter();
            public override void Cancel() { }
            public override void Prepare() { }
            public override int ExecuteNonQuery() => throw new NotSupportedException("No se permiten escrituras.");
            public override object ExecuteScalar() => throw new NotSupportedException();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
                conexion.Leer();
        }
    }
    private sealed class AutenticacionPrueba : IAuthenticationService
    {
        public List<string?> Salidas { get; } = new();
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) { Salidas.Add(scheme); return Task.CompletedTask; }
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => throw new NotSupportedException();
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => throw new NotSupportedException();
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => throw new NotSupportedException();
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => throw new NotSupportedException();
    }
}
