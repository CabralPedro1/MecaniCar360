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
        private readonly PermisoService _permisos;

        public AuditoriaService(MecaniCarContext context, IHttpContextAccessor accessor,
            IServiceScopeFactory scopes, ILogger<AuditoriaService> logger, PermisoService permisos)
        {
            _context = context;
            _accessor = accessor;
            _scopes = scopes;
            _logger = logger;
            _permisos = permisos;
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

        // Evento limitado: el destinatario no es el actor autenticado.
        internal void RegistrarActivacionCliente(int usuarioId)
        {
            _context.Auditorias.Add(Crear("CLIENTE_ACTIVADO", "Usuario", usuarioId, null,
                "Cuenta activada mediante invitacion."));
        }

        public Task RegistrarLoginExitosoAsync() => RegistrarSesionAsync("LOGIN_EXITOSO", ObtenerActor());
        public Task RegistrarLoginFallidoAsync() => RegistrarSesionAsync("LOGIN_FALLIDO", null);
        public Task RegistrarLoginGoogleFallidoAsync() => RegistrarSesionAsync("LOGIN_GOOGLE_FALLIDO", null);
        public Task RegistrarVinculacionGoogleRechazadaAsync() => RegistrarSesionAsync("GOOGLE_VINCULACION_RECHAZADA", ObtenerActor());
        public Task RegistrarLogoutAsync() => RegistrarSesionAsync("LOGOUT", ObtenerActor());

        private int? ObtenerActor()
        {
            var principal = _accessor.HttpContext?.User;
            return principal?.Identity?.IsAuthenticated == true &&
                int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
                    ? id : null;
        }

        internal void RegistrarPasswordCliente(int usuarioId, bool agregada)
        {
            // La prueba de titularidad es el enlace; no atribuir el evento a una cookie ajena.
            _context.Auditorias.Add(Crear(agregada ? "PASSWORD_CLIENTE_AGREGADA" : "PASSWORD_CLIENTE_RESTABLECIDA",
                "Usuario", usuarioId, null, "Credencial establecida mediante enlace de correo verificado."));
        }

        internal void RegistrarOnboarding(string accion, string entidad, int entidadId, int? actor = null)
        {
            if (accion is not ("AUTORREGISTRO_SOLICITADO" or "CLIENTE_CREADO" or "GOOGLE_VINCULADO" or
                "LOGIN_GOOGLE_EXITOSO" or "GOOGLE_VINCULACION_RECHAZADA" or "INVITACION_PUBLICA_EMITIDA" or "INVITACION_PUBLICA_ENVIO_FALLIDO"))
                throw new ArgumentException("Evento de onboarding no permitido.");
            _context.Auditorias.Add(Crear(accion, entidad, entidadId, actor, null));
        }

        public async Task<Models.DTOs.ServiceResult<List<Models.ViewModels.AuditoriaConsultaViewModel>>> ConsultarAsync(int usuarioId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioId, "AUDITORIA_VER"))
                return Models.DTOs.ServiceResult<List<Models.ViewModels.AuditoriaConsultaViewModel>>.Error("Acceso denegado.");
            var registros = await _context.Auditorias.AsNoTracking()
                .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id)
                .Select(a => new Models.ViewModels.AuditoriaConsultaViewModel
                {
                    Fecha = a.Fecha, Actor = a.Usuario == null ? null : a.Usuario.Username,
                    Accion = a.Accion, Entidad = a.Entidad, EntidadId = a.EntidadId, Descripcion = a.Descripcion
                }).ToListAsync();
            return Models.DTOs.ServiceResult<List<Models.ViewModels.AuditoriaConsultaViewModel>>.Ok(registros);
        }

        private async Task RegistrarSesionAsync(string accion, int? actor)
        {
            try
            {
                if (accion is not ("LOGIN_FALLIDO" or "LOGIN_GOOGLE_FALLIDO") && !actor.HasValue)
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
