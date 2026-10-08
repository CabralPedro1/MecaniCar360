using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace MecaniCar360.Helpers;

public static class GoogleClienteConfiguracion
{
    public const string Externa = "GoogleClienteTemporal";
    public const string EmailVerificado = "google:email_verified";
    public static bool Disponible(IConfiguration config) =>
        !string.IsNullOrWhiteSpace(config["Authentication:Google:ClientId"]) &&
        !string.IsNullOrWhiteSpace(config["Authentication:Google:ClientSecret"]);

    public static void AgregarGoogleCliente(this IServiceCollection services, IConfiguration config)
    {
        if (!Disponible(config)) return;
        services.AddAuthentication().AddCookie(Externa, options =>
        {
            options.Cookie.Name = ".MecaniCar.GoogleTemporal";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            options.SlidingExpiration = false;
        }).AddGoogle(options =>
        {
            options.ClientId = config["Authentication:Google:ClientId"]!;
            options.ClientSecret = config["Authentication:Google:ClientSecret"]!;
            options.SignInScheme = Externa;
            options.SaveTokens = false;
            options.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
            options.ClaimActions.DeleteClaim(ClaimTypes.NameIdentifier);
            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
            options.ClaimActions.MapJsonKey(EmailVerificado, "email_verified");
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/GoogleCliente/NoDisponible");
                return Task.CompletedTask;
            };
        });
    }
}
