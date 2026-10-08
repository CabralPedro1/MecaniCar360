using System.Security.Claims;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MecaniCar360.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GoogleClienteController(IConfiguration config, GoogleClienteService google, SessionManager sesion,
    AuditoriaService auditoria, AccountService account, PresentacionCuentaService presentacion) : Controller
{
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(Program.LoginRateLimitPolicy)]
    public IActionResult Ingresar() => Iniciar(false);

    [Authorize, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(Program.LoginRateLimitPolicy)]
    public async Task<IActionResult> Vincular(string password)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ||
            !(await presentacion.ObtenerAsync(actor)).PuedeVincularGoogle)
            return RedirectToAction("Index", "PortalCliente");
        if (string.IsNullOrWhiteSpace(password)) return RedirectToAction(nameof(NoDisponible));
        // Reautenticación local: una sesión Google no basta para agregar otro método.
        var local = await account.LoginAsync(User.Identity!.Name!, password);
        if (!local.Exitoso || local.Usuario!.Id.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            await auditoria.RegistrarVinculacionGoogleRechazadaAsync();
            return RedirectToAction(nameof(NoDisponible));
        }
        return Iniciar(true);
    }

    private IActionResult Iniciar(bool vincular)
    {
        if (!GoogleClienteConfiguracion.Disponible(config)) return RedirectToAction(nameof(NoDisponible));
        var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(Callback)) };
        if (vincular)
        {
            properties.Items["usuario"] = User.FindFirstValue(ClaimTypes.NameIdentifier);
            properties.Items["stamp"] = User.FindFirstValue(Usuario.SecurityStampClaim);
            properties.Items["verificada"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Callback()
    {
        if (!GoogleClienteConfiguracion.Disponible(config)) return RedirectToAction(nameof(NoDisponible));
        var resultado = await HttpContext.AuthenticateAsync(GoogleClienteConfiguracion.Externa);
        await HttpContext.SignOutAsync(GoogleClienteConfiguracion.Externa);
        if (!resultado.Succeeded || resultado.Principal == null) return RedirectToAction(nameof(NoDisponible));
        int? vincular = null;
        string? stampVinculacion = null;
        if (resultado.Properties!.Items.TryGetValue("usuario", out var usuario))
        {
            if (User.Identity?.IsAuthenticated != true || usuario != User.FindFirstValue(ClaimTypes.NameIdentifier) ||
                !resultado.Properties.Items.TryGetValue("stamp", out var stamp) ||
                stamp != User.FindFirstValue(Usuario.SecurityStampClaim) || !int.TryParse(usuario, out var id) ||
                !resultado.Properties.Items.TryGetValue("verificada", out var instante) ||
                !ReautenticacionVigente(instante, DateTimeOffset.UtcNow))
                return RedirectToAction(nameof(NoDisponible));
            vincular = id;
            stampVinculacion = stamp;
        }
        var r = await google.ResolverAsync(resultado.Principal, vincular, stampVinculacion);
        if (!r.Exitoso)
        {
            if (vincular.HasValue) await auditoria.RegistrarVinculacionGoogleRechazadaAsync();
            else await auditoria.RegistrarLoginGoogleFallidoAsync();
            return RedirectToAction(nameof(NoDisponible));
        }
        var principal = new ClaimsPrincipal(r.Data!);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        HttpContext.User = principal;
        sesion.IniciarSesion();
        return RedirectToAction("Index", "PortalCliente");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult NoDisponible() => View();

    internal static bool ReautenticacionVigente(string? instante, DateTimeOffset ahora) =>
        long.TryParse(instante, out var segundos) && segundos <= ahora.ToUnixTimeSeconds() &&
        segundos >= ahora.ToUnixTimeSeconds() - 300;
}
