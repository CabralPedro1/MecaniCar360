using System.ComponentModel.DataAnnotations;
using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class AltaClienteService
{
    private readonly MecaniCarContext _db;
    private readonly PermisoService _permisos;
    private readonly AuditoriaService _auditoria;

    public AltaClienteService(MecaniCarContext db, PermisoService permisos, AuditoriaService auditoria)
        => (_db, _permisos, _auditoria) = (db, permisos, auditoria);

    public async Task<ServiceResult<ClienteOperativoViewModel>> CrearAsync(NuevoClienteViewModel model, int actorId)
    {
        if (!await _permisos.TienePermisoAsync(actorId, "PERSONA_CREAR"))
            return ServiceResult<ClienteOperativoViewModel>.Error("No posee permisos para crear clientes.");
        var datos = new NuevoClienteViewModel
        {
            Nombre = model.Nombre?.Trim() ?? "", Apellido = model.Apellido?.Trim() ?? "",
            Dni = MecaniCar360.Helpers.DniPersona.Normalizar(model.Dni) ?? "",
            Telefono = string.IsNullOrWhiteSpace(model.Telefono) ? null : model.Telefono.Trim(),
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim()
        };
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), errores, true))
            return ServiceResult<ClienteOperativoViewModel>.Error("Revise los campos obligatorios, sus longitudes y el email.");
        if (_db.Database.CurrentTransaction != null || _db.ChangeTracker.HasChanges())
            return ServiceResult<ClienteOperativoViewModel>.Error("No se puede iniciar el alta con otra operación pendiente.");

        var previas = _db.ChangeTracker.Entries().Select(e => e.Entity).ToHashSet(ReferenceEqualityComparer.Instance);
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var confirmado = false;
        try
        {
            if (!await _permisos.TienePermisoAsync(actorId, "PERSONA_CREAR"))
                return ServiceResult<ClienteOperativoViewModel>.Error("No posee permisos para crear clientes.");
            var rolId = await _db.Roles.AsNoTracking()
                .Where(r => r.Nombre == RolesSistema.CLIENTE && r.Activo)
                .Select(r => (int?)r.Id).SingleOrDefaultAsync();
            if (!rolId.HasValue)
                return ServiceResult<ClienteOperativoViewModel>.Error("El rol CLIENTE no está disponible o está inactivo.");
            if (await MecaniCar360.Helpers.DniPersona.ExisteAsync(_db, datos.Dni))
                return ServiceResult<ClienteOperativoViewModel>.Error("Ya existe una persona con ese DNI. Revise el registro existente.");
            if (datos.Email != null)
            {
                var email = datos.Email.ToUpperInvariant();
                if (await _db.Personas.AnyAsync(p => p.Email != null && p.Email.Trim().ToUpper() == email) ||
                    await _db.Usuarios.AnyAsync(u => u.EmailLogin.Trim().ToUpper() == email))
                    return ServiceResult<ClienteOperativoViewModel>.Error("El email ya está registrado. Revise el registro existente.");
            }
            var persona = new Persona
            {
                Nombre = datos.Nombre, Apellido = datos.Apellido, Dni = datos.Dni,
                Telefono = datos.Telefono, Email = datos.Email, Activo = true
            };
            persona.Roles.Add(new PersonaRol
            {
                RolId = rolId.Value, FechaAlta = DateTime.Now, OtorgadoPorUsuarioId = actorId
            });
            _db.Personas.Add(persona);
            await _db.SaveChangesAsync();
            _auditoria.RegistrarOperacion("PERSONA_CREADA", "Persona", persona.Id, actorId);
            _auditoria.RegistrarOperacion("ROL_ASIGNADO", "Persona", persona.Id, actorId, $"Rol #{rolId.Value}.");
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            confirmado = true;
            return ServiceResult<ClienteOperativoViewModel>.Ok(new ClienteOperativoViewModel
            {
                Id = persona.Id, Nombre = persona.Nombre, Apellido = persona.Apellido,
                Dni = persona.Dni, Telefono = persona.Telefono, Email = persona.Email, Activo = persona.Activo
            }, "Cliente creado correctamente.");
        }
        catch
        {
            return ServiceResult<ClienteOperativoViewModel>.Error("No se pudo completar el alta. Revise los datos o intente nuevamente.");
        }
        finally
        {
            if (!confirmado)
            {
                try { await transaction.RollbackAsync(); }
                finally
                {
                    foreach (var entry in _db.ChangeTracker.Entries().Where(e => !previas.Contains(e.Entity)).ToList())
                        entry.State = EntityState.Detached;
                }
            }
        }
    }
}
