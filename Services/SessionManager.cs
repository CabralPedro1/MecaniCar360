using System.Security.Claims;

namespace MecaniCar360.Services
{
    public sealed class SessionManager
    {
        private const string UsuarioKey = "SessionManager.UsuarioId";
        private const string PersonaKey = "SessionManager.PersonaId";
        private const string UsernameKey = "SessionManager.Username";
        private readonly IHttpContextAccessor _accessor;

        public SessionManager(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        // Se llama después de SignInAsync y de actualizar HttpContext.User.
        // No recibe IDs ni objetos de usuario que puedan provenir del formulario.
        public void IniciarSesion()
        {
            var datos = LeerIdentidad();
            if (datos == null)
                throw new InvalidOperationException("Se requiere una identidad autenticada válida.");

            _accessor.HttpContext!.Session.Clear();
            Guardar(datos.Value);
        }

        // La eliminación de la cookie continúa siendo responsabilidad de SignOutAsync.
        public void CerrarSesion()
        {
            var context = _accessor.HttpContext;
            if (context == null) return;
            context.Session.Clear();
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        public bool EstaAutenticado() => ObtenerDatos() != null;
        public int? ObtenerUsuarioId() => ObtenerDatos()?.UsuarioId;
        public int? ObtenerPersonaId() => ObtenerDatos()?.PersonaId;
        public string? ObtenerUsername() => ObtenerDatos()?.Username;

        private (int UsuarioId, int PersonaId, string Username)? LeerIdentidad()
        {
            var identity = _accessor.HttpContext?.User.Identity as ClaimsIdentity;
            if (identity?.IsAuthenticated != true ||
                !int.TryParse(identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var usuarioId) || usuarioId <= 0 ||
                !int.TryParse(identity.FindFirst("PersonaId")?.Value, out var personaId) || personaId <= 0 ||
                string.IsNullOrWhiteSpace(identity.Name))
                return null;

            return (usuarioId, personaId, identity.Name);
        }

        private (int UsuarioId, int PersonaId, string Username)? ObtenerDatos()
        {
            var datos = LeerIdentidad();
            var session = _accessor.HttpContext?.Session;
            if (session == null) return null;

            if (datos == null)
            {
                session.Remove(UsuarioKey);
                session.Remove(PersonaKey);
                session.Remove(UsernameKey);
                return null;
            }

            // Session es sólo una copia mínima: nunca autentica ni prevalece sobre Claims.
            // También se reconstruye si Session expira antes que la cookie.
            Guardar(datos.Value);
            return datos;
        }

        private void Guardar((int UsuarioId, int PersonaId, string Username) datos)
        {
            var session = _accessor.HttpContext!.Session;
            if (session.GetInt32(UsuarioKey) != datos.UsuarioId)
                session.SetInt32(UsuarioKey, datos.UsuarioId);
            if (session.GetInt32(PersonaKey) != datos.PersonaId)
                session.SetInt32(PersonaKey, datos.PersonaId);
            if (session.GetString(UsernameKey) != datos.Username)
                session.SetString(UsernameKey, datos.Username);
        }
    }
}
