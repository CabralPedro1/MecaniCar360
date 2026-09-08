using MecaniCar360.Data;
using MecaniCar360.Patterns.Facade;
using MecaniCar360.Patterns.Observer;
using MecaniCar360.Patterns.State;
using MecaniCar360.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // =====================================
            // MVC
            // =====================================

            builder.Services.AddControllersWithViews();


            // =====================================
            // DB CONTEXT
            // =====================================

            builder.Services.AddDbContext<MecaniCarContext>(
                options =>
                    options.UseSqlServer(
                        builder.Configuration
                            .GetConnectionString(
                                "DefaultConnection"))
            );


            // =====================================
            // AUTHENTICATION
            // =====================================

            builder.Services
                .AddAuthentication(
                    CookieAuthenticationDefaults
                        .AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath =
                        "/Account/Login";

                    options.AccessDeniedPath =
                        "/Account/Login";

                    options.ExpireTimeSpan =
                        TimeSpan.FromHours(8);

                    options.SlidingExpiration = true;

                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy =
                        CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite =
                        SameSiteMode.Lax;

                    options.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = async context =>
                        {
                            var claim = context.Principal?
                                .FindFirst(ClaimTypes.NameIdentifier);

                            if (claim == null ||
                                !int.TryParse(
                                    claim.Value,
                                    out int usuarioId))
                            {
                                context.RejectPrincipal();
                                await context.HttpContext.SignOutAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme);
                                return;
                            }

                            var db = context.HttpContext.RequestServices
                                .GetRequiredService<MecaniCarContext>();

                            var vigente = await db.Usuarios
                                .AnyAsync(u =>
                                    u.Id == usuarioId &&
                                    u.Activo &&
                                    u.Persona.Activo);

                            if (!vigente)
                            {
                                context.RejectPrincipal();
                                await context.HttpContext.SignOutAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme);
                            }
                        }
                    };
                });


            // =====================================
            // AUTHORIZATION
            // =====================================

            builder.Services.AddAuthorization();


            // =====================================
            // SESSION
            // =====================================

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout =
                    TimeSpan.FromMinutes(60);

                options.Cookie.HttpOnly =
                    true;

                options.Cookie.IsEssential =
                    true;
            });


            // =====================================
            // SERVICES
            // =====================================

            builder.Services.AddScoped<TurnoService>();

            builder.Services.AddScoped<
                IngresoVehiculoService>();

            builder.Services.AddScoped<
                OrdenTrabajoService>();

            builder.Services.AddScoped<
                OrdenStateService>();

            builder.Services.AddScoped<
                PresupuestoService>();

            builder.Services.AddScoped<
                FacturaService>();

            builder.Services.AddScoped<
                StockService>();

            builder.Services.AddScoped<
                ProveedorService>();

            builder.Services.AddScoped<
                MarcaService>();

            builder.Services.AddScoped<
                VehiculoService>();

            builder.Services.AddScoped<
                DominioVehicularService>();

            builder.Services.AddScoped<
                RolService>();

            builder.Services.AddScoped<
                PersonaService>();

            builder.Services.AddScoped<
                AccountService>();

            builder.Services.AddScoped<
                UsuarioService>();

            builder.Services.AddScoped<
                PermisoService>();

            builder.Services.AddScoped<
                EmailService>();

            builder.Services.AddScoped<
                NotificacionService>();

            builder.Services.AddScoped<
                DiagnosticoService>();

            builder.Services.AddScoped<
                GarantiaService>();


            // =====================================
            // FACADE PATTERN
            // =====================================

            builder.Services.AddScoped<
                MecaniCarFacade>();


            // =====================================
            // OBSERVER PATTERN
            // =====================================

            builder.Services.AddScoped<
                IOrdenObserver,
                EmailOrdenObserver>();

            builder.Services.AddScoped<
                IOrdenObserver,
                NotificacionOrdenObserver>();

            builder.Services.AddScoped<
                OrdenSubject>();


            // =====================================
            // BUILD
            // =====================================

            var app = builder.Build();


            // =====================================
            // INICIALIZADOR BD
            // =====================================

            using (var scope =
                   app.Services.CreateScope())
            {
                var context =
                    scope.ServiceProvider
                        .GetRequiredService<
                            MecaniCarContext>();

                InicializadorBD.Inicializar(
                    context);
            }


            // =====================================
            // MANEJO DE ERRORES
            // =====================================

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler(
                    "/Home/Error");

                app.UseHsts();
            }


            // =====================================
            // MIDDLEWARE
            // =====================================

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();


            // =====================================
            // SESSION
            // =====================================

            app.UseSession();


            // =====================================
            // AUTHENTICATION
            // =====================================

            app.UseAuthentication();

            app.UseAuthorization();


            // =====================================
            // RUTA PRINCIPAL
            // =====================================

            app.MapControllerRoute(
                name: "default",
                pattern:
                    "{controller=Account}/{action=Login}/{id?}");


            // =====================================
            // EJECUTAR
            // =====================================

            app.Run();
        }
    }
}