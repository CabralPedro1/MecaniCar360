using System.Security.Claims;
using MecaniCar360.Data;
using MecaniCar360.Models;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public sealed class AuditoriaService
    {
        private readonly MecaniCarContext _context;
        private readonly IHttpContextAccessor _accessor;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(MecaniCarContext context, IHttpContextAccessor accessor,
            IServiceScopeFactory scopes, ILogger<AuditoriaService> logger)
        {
            _context = context;
            _accessor = accessor;
            _scopes = scopes;
            _logger = logger;
        }

        // Sólo prepara el INSERT. El Service de negocio controla SaveChanges y la transacción.
        // El ID esperado se compara con Claims; nunca establece la identidad del auditor.
        internal Auditoria RegistrarOperacion(string accion, string entidad, int? entidadId,
            int usuarioEsperado, string? descripcion = null)
        {
            var actor = ObtenerActor();
            if (!actor.HasValue || actor != usuarioEsperado)
                throw new UnauthorizedAccessException("La identidad de auditoría no coincide con el solicitante.");

            var registro = Crear(accion, entidad, entidadId, actor, descripcion);
            _context.Auditorias.Add(registro);
            return registro;
        }

        public Task RegistrarLoginExitosoAsync() => RegistrarSesionAsync("LOGIN_EXITOSO", ObtenerActor());
        public Task RegistrarLoginFallidoAsync() => RegistrarSesionAsync("LOGIN_FALLIDO", null);
        public Task RegistrarLogoutAsync() => RegistrarSesionAsync("LOGOUT", ObtenerActor());

        private int? ObtenerActor()
        {
            var principal = _accessor.HttpContext?.User;
            return principal?.Identity?.IsAuthenticated == true &&
                int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
                    ? id : null;
        }

        private async Task RegistrarSesionAsync(string accion, int? actor)
        {
            try
            {
                if (accion != "LOGIN_FALLIDO" && !actor.HasValue)
                    throw new UnauthorizedAccessException("El evento requiere una identidad autenticada.");

                // Contexto independiente: no confirma entidades rastreadas por la operación HTTP.
                await using var scope = _scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<MecaniCarContext>();
                if (actor.HasValue && !await db.Usuarios.AsNoTracking().AnyAsync(u => u.Id == actor))
                    throw new InvalidOperationException("El actor ya no existe.");
                db.Auditorias.Add(Crear(accion, "Sesion", null, actor,
                    accion == "LOGIN_FALLIDO" ? "Intento de autenticación rechazado." : null));
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // No volcar excepciones SQL ni datos de la petición, que podrían contener secretos.
                _logger.LogError("No se pudo registrar {Accion} en auditoría. Tipo de error: {Tipo}.",
                    accion, ex.GetType().Name);
            }
        }

        private static Auditoria Crear(string accion, string entidad, int? entidadId,
            int? actor, string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(accion) || accion.Length > 100 ||
                string.IsNullOrWhiteSpace(entidad) || entidad.Length > 100 ||
                descripcion?.Length > 2000)
                throw new ArgumentException("Datos de auditoría inválidos.");
            return new Auditoria
            {
                UsuarioId = actor, Accion = accion, Entidad = entidad,
                EntidadId = entidadId, Descripcion = descripcion, Fecha = DateTime.Now
            };
        }
    }
}
