using BCrypt.Net;
using MecaniCar360.Models;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Data
{
    public static class InicializadorBD
    {
        public static void Inicializar(MecaniCarContext context)
        {
            context.Database.Migrate();

            // =====================
            // ROLES
            // =====================
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Rol
                    {
                        Nombre = "ADMIN",
                        FechaCreacion = DateTime.Now
                    },
                    new Rol
                    {
                        Nombre = "MECANICO",
                        FechaCreacion = DateTime.Now
                    },
                    new Rol
                    {
                        Nombre = "STOCK",
                        FechaCreacion = DateTime.Now
                    },
                    new Rol
                    {
                        Nombre = "CAJA",
                        FechaCreacion = DateTime.Now
                    },
                    new Rol
                    {
                        Nombre = "CLIENTE",
                        EsRolCliente = true,
                        FechaCreacion = DateTime.Now
                    }
                );

                context.SaveChanges();
            }

            // =====================
            // OBTENER ROLES
            // =====================
            var roles = context.Roles
                .Where(r => r.Nombre == "ADMIN" ||
                            r.Nombre == "MECANICO" ||
                            r.Nombre == "STOCK" ||
                            r.Nombre == "CAJA")
                .ToList();

            var rolAdmin = roles.First(r => r.Nombre == "ADMIN");
            var rolMecanico = roles.First(r => r.Nombre == "MECANICO");
            var rolStock = roles.First(r => r.Nombre == "STOCK");
            var rolCaja = roles.First(r => r.Nombre == "CAJA");

            // =====================
            // USUARIO ADMIN PRINCIPAL
            // =====================
            if (!context.Usuarios.Any(u => u.Username == "admin"))
            {
                var persona = new Persona
                {
                    Nombre = "Administrador",
                    Apellido = "Principal",
                    Dni = "00000000",
                    Telefono = "1111111111",
                    Email = "360.mecanicar@gmail.com",
                    Activo = true
                };

                persona.Roles.Add(new PersonaRol
                {
                    Rol = rolAdmin,
                    FechaAlta = DateTime.Now
                });

                persona.Roles.Add(new PersonaRol
                {
                    Rol = rolMecanico,
                    FechaAlta = DateTime.Now
                });

                persona.Roles.Add(new PersonaRol
                {
                    Rol = rolStock,
                    FechaAlta = DateTime.Now
                });

                persona.Roles.Add(new PersonaRol
                {
                    Rol = rolCaja,
                    FechaAlta = DateTime.Now
                });

                var usuario = new Usuario
                {
                    Username = "admin",
                    EmailLogin = "360.mecanicar@gmail.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    PrimerLogin = true,
                    Activo = true,
                    Persona = persona
                };

                context.Usuarios.Add(usuario);
                context.SaveChanges();
            }
        }
    }
}