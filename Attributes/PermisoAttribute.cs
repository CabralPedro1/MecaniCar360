using MecaniCar360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace MecaniCar360.Attributes
{
    public class PermisoAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _patente;

        public PermisoAttribute(string patente)
        {
            _patente = patente;
        }

        public async Task OnAuthorizationAsync(
            AuthorizationFilterContext context)
        {
            // =====================================
            // VERIFICAR AUTENTICACIÓN
            // =====================================

            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                context.Result =
                    new ChallengeResult();

                return;
            }


            // =====================================
            // OBTENER USUARIO
            // =====================================

            var claim =
                context.HttpContext.User
                    .FindFirst(
                        ClaimTypes.NameIdentifier);

            if (claim == null ||
                !int.TryParse(
                    claim.Value,
                    out int usuarioId))
            {
                context.Result =
                    new ForbidResult();

                return;
            }


            // =====================================
            // OBTENER SERVICIO
            // =====================================

            var permisoService =
                context.HttpContext
                    .RequestServices
                    .GetRequiredService<PermisoService>();


            // =====================================
            // VERIFICAR PATENTE
            // =====================================

            var tienePermiso =
                await permisoService
                    .TienePermisoAsync(
                        usuarioId,
                        _patente);


            if (!tienePermiso)
            {
                context.Result =
                    new ForbidResult();

                return;
            }
        }
    }
}