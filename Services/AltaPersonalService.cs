using System.ComponentModel.DataAnnotations;
using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class AltaPersonalService
{
    private readonly MecaniCarContext _db;
    private readonly PermisoService _permisos;
    private readonly PersonaService _personas;
    private readonly UsuarioService _usuarios;
    private readonly AuditoriaService _auditoria;

    public AltaPersonalService(MecaniCarContext db, PermisoService permisos, PersonaService personas,
        UsuarioService usuarios, AuditoriaService auditoria)
        => (_db, _permisos, _personas, _usuarios, _auditoria) = (db, permisos, personas, usuarios, auditoria);

    public async Task<bool> PuedeCrearAsync(int actorId) =>
        await _permisos.TienePermisoAsync(actorId, "PERSONA_CREAR") &&
        await _permisos.TienePermisoAsync(actorId, "USUARIO_CREAR") &&
        await _permisos.TienePermisoAsync(actorId, "ROL_MODIFICAR") &&
        // UsuarioService reserva actualmente las altas internas al administrador efectivo.
        await _permisos.EsAdministradorAsync(actorId);

    private IQueryable<Rol> RolesInternos() => _db.Roles.Where(r => r.Activo &&
        (r.Nombre == RolesSistema.ADMIN || r.Nombre == RolesSistema.MECANICO ||
         r.Nombre == RolesSistema.CAJA || r.Nombre == RolesSistema.STOCK));

    public async Task<ServiceResult<List<Rol>>> ObtenerRolesAsync(int actorId)
    {
        if (!await PuedeCrearAsync(actorId)) return ServiceResult<List<Rol>>.Error("Acceso denegado.");
        return ServiceResult<List<Rol>>.Ok(await RolesInternos().AsNoTracking().OrderBy(r => r.Nombre).ToListAsync());
    }

    public async Task<ServiceResult> CrearAsync(NuevoPersonalViewModel model, int actorId)
    {
        if (!await PuedeCrearAsync(actorId)) return ServiceResult.Error("No puede crear personal.");
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), errores, true))
            return ServiceResult.Error("Revise los datos obligatorios, sus longitudes y los emails.");

        // Este coordinador es dueño exclusivo de la transacción y de sus cambios.
        if (_db.Database.CurrentTransaction != null || _db.ChangeTracker.HasChanges())
            return ServiceResult.Error("No se puede iniciar el alta con otra operación pendiente.");

        var entidadesPrevias = _db.ChangeTracker.Entries().Select(e => e.Entity).ToHashSet(ReferenceEqualityComparer.Instance);
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var confirmado = false;
        try
        {
            if (!await PuedeCrearAsync(actorId)) return ServiceResult.Error("No puede crear personal.");
            var rol = await RolesInternos().SingleOrDefaultAsync(r => r.Id == model.RolId);
            if (rol == null) return ServiceResult.Error("Seleccione un rol interno activo válido.");
            if (await _personas.ExisteDniAsync(model.Dni)) return ServiceResult.Error("Ya existe una persona con ese DNI.");
            var username = model.Username.Trim();
            var emailLogin = model.EmailLogin.Trim();
            if (await _usuarios.ExisteUsernameAsync(username)) return ServiceResult.Error("Ya existe un usuario con ese nombre.");
            if (await _usuarios.ExisteEmailAsync(emailLogin)) return ServiceResult.Error("Ya existe un usuario con ese email.");

            var persona = new Persona
            {
                Nombre = model.Nombre.Trim(), Apellido = model.Apellido.Trim(), Dni = model.Dni.Trim(),
                Telefono = model.Telefono?.Trim(), Email = model.Email?.Trim(), Activo = true
            };
            var password = UsuarioService.GenerarContraseñaTemporal();
            var usuario = new Usuario
            {
                Persona = persona, Username = username, EmailLogin = emailLogin,
                ProveedorAutenticacion = ProveedorAutenticacion.Credenciales, IdentificadorExterno = null,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), PrimerLogin = true,
                SecurityStamp = Guid.NewGuid().ToString("N"), Activo = true, FechaCreacion = DateTime.Now
            };
            persona.Roles.Add(new PersonaRol { Rol = rol, FechaAlta = DateTime.Now, OtorgadoPorUsuarioId = actorId });
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();
            _auditoria.RegistrarOperacion("PERSONA_CREADA", "Persona", persona.Id, actorId);
            _auditoria.RegistrarOperacion("ROL_ASIGNADO", "Persona", persona.Id, actorId, $"Rol #{rol.Id}.");
            _auditoria.RegistrarOperacion("USUARIO_CREADO", "Usuario", usuario.Id, actorId);
            await _db.SaveChangesAsync();
            await _usuarios.EnviarCredencialesAsync(emailLogin, username, password);
            await transaction.CommitAsync();
            confirmado = true;
            return ServiceResult.Ok("El personal fue creado correctamente y se enviaron las credenciales de acceso.");
        }
        catch
        {
            return ServiceResult.Error("No se pudo completar el alta y envío de credenciales. Revise los datos o intente nuevamente.");
        }
        finally
        {
            if (!confirmado)
            {
                try { await transaction.RollbackAsync(); }
                finally
                {
                    foreach (var entry in _db.ChangeTracker.Entries().Where(e => !entidadesPrevias.Contains(e.Entity)).ToList())
                        entry.State = EntityState.Detached;
                }
            }
        }
    }
}
