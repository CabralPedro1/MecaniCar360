using System.Text.Json;

namespace MecaniCar360.Helpers
{
    public static class AuthorizationHelper
    {
        public static bool TieneRol(HttpContext context, string rol)
        {
            var rolesJson = context.Session.GetString("Roles");
            if (string.IsNullOrEmpty(rolesJson))
                return false;

            var roles = JsonSerializer.Deserialize<List<string>>(rolesJson);
            return roles != null && roles.Contains(rol);
        }
    }
}