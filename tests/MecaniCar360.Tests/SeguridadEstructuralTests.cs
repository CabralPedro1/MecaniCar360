using System.Reflection;
using System.Security.Claims;
using MecaniCar360.Controllers;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MecaniCar360.Tests;

public class SeguridadEstructuralTests
{
    [Fact]
    public void AsignacionOperativa_ConservaRutasYNoDependeDePantallasTecnicas()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "MecaniCar360.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        string Read(string path) => File.ReadAllText(Path.Combine(root!.FullName, path));
        var controller = Read("Controllers/PersonaController.cs");
        var service = Read("Services/PersonaService.cs");
        Assert.Contains("_personaService.ObtenerAdministracionRolesAsync", controller);
        Assert.Contains("ObtenerRolesDisponiblesAsync(personaId, actorId)", service);
        Assert.DoesNotContain("PoliticaSeguridadEstructural", controller + service);
        var view = Read("Views/Persona/AdministrarRoles.cshtml");
        Assert.Contains("asp-action=\"AsignarRol\"", view);
        Assert.Contains("asp-action=\"QuitarRol\"", view);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root!.FullName, "Views"), "*.cshtml", SearchOption.AllDirectories))
        {
            if (path.Contains(Path.DirectorySeparatorChar + "Rol" + Path.DirectorySeparatorChar) ||
                path.Contains(Path.DirectorySeparatorChar + "Seguridad" + Path.DirectorySeparatorChar)) continue;
            var source = File.ReadAllText(path);
            Assert.DoesNotContain("asp-controller=\"Rol\"", source);
            Assert.DoesNotContain("asp-controller=\"Seguridad\"", source);
        }
    }

    // Null dependencies deliberately fail if rejection attempts any persistence,
    // audit, authorization lookup or transaction. No SQL/provider is used.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(0)]
    public async Task Servicios_RechazanSinConsultarDependenciasNiMutarEntrada(int actor)
    {
        var roles = new RolService(null!, null!, null!);
        var rol = new Rol { Id = 17, Nombre = "Original", Activo = true };
        var seguridad = new SeguridadService(null!, null!, null!);
        var familia = new FamiliaEdicionViewModel { Id = 23, Nombre = "Original" };
        var resultados = new[] {
            await roles.CrearAsync(rol, actor), await roles.EditarAsync(rol, actor),
            await roles.CambiarEstadoAsync(17, actor),
            await seguridad.GuardarFamiliaAsync(familia, actor),
            await seguridad.PatenteAsync(23, 1, true, actor),
            await seguridad.PatenteAsync(23, 1, false, actor),
            await seguridad.HijaAsync(23, 2, true, actor),
            await seguridad.HijaAsync(23, 2, false, actor),
            await seguridad.RolAsync(17, 23, true, actor),
            await seguridad.RolAsync(17, 23, false, actor)
        };
        Assert.All(resultados, r => {
            Assert.False(r.Exitoso);
            Assert.Equal(PoliticaSeguridadEstructural.Mensaje, r.Mensaje);
        });
        Assert.Equal(17, rol.Id);
        Assert.Equal("Original", rol.Nombre);
        Assert.True(rol.Activo);
        Assert.Equal("Original", familia.Nombre);
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("CAJA")]
    [InlineData("MECANICO")]
    [InlineData("STOCK")]
    [InlineData("CLIENTE")]
    public async Task AccionesGetPost_RechazanInclusoConIdentidadAdmin(string perfil)
    {
        foreach (var controller in new Controller[] { new RolController(null!), new SeguridadController(null!) })
        {
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, perfil)
                }, "Test"))
            } };
            var names = controller is RolController ? new[] { "Index", "Detalle", "Crear", "Editar", "CambiarEstado" } : new[] { "Index", "Familia", "Patentes", "Rol", "Editar", "Patente", "Hija", "FamiliaRol" };
            foreach (var method in controller.GetType().GetMethods().Where(m => names.Contains(m.Name)))
            {
                var args = method.GetParameters().Select(p => p.ParameterType == typeof(int) ? (object)1 :
                    p.ParameterType == typeof(bool) ? true : Activator.CreateInstance(p.ParameterType)).ToArray();
                var result = method.Invoke(controller, args);
                if (result is Task<IActionResult> task) result = await task;
                Assert.IsType<ForbidResult>(result);
            }
        }
    }

    [Fact]
    public void Politica_NoTieneInterruptorDeConfiguracion()
    {
        Assert.False(PoliticaSeguridadEstructural.PermiteModificar);
        Assert.False(PoliticaSeguridadEstructural.PermiteConsultarPantallasTecnicas);
        Assert.Null(typeof(PoliticaSeguridadEstructural).GetProperty(nameof(PoliticaSeguridadEstructural.PermiteModificar))!.SetMethod);
    }
}
