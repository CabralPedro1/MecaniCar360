using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace MecaniCar360.Filters
{
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _rol;

        public AuthorizeRoleAttribute(string rol)
        {
            _rol = rol;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var rolesJson = context.HttpContext.Session.GetString("Roles");

            if (string.IsNullOrEmpty(rolesJson))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var roles = JsonSerializer.Deserialize<List<string>>(rolesJson);

            if (roles == null || !roles.Contains(_rol))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}