using System.ComponentModel.DataAnnotations;
using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class RegistroClienteService(MecaniCarContext db, IdentidadClienteService identidad,
    InvitacionClienteService invitaciones, AuditoriaService auditoria, ILogger<RegistroClienteService> logger,
    SeguridadClienteService seguridad)
{
    public const string Respuesta = "Solicitud recibida. Si corresponde y el envío puede completarse, recibirás instrucciones por correo. Si no llegan, puedes volver a intentarlo más tarde o contactar al taller. Si ya tienes cuenta, puedes iniciar sesión.";

    public async Task<ServiceResult> SolicitarAsync(RegistroClienteViewModel model)
    {
        model.Nombre = model.Nombre?.Trim() ?? "";
        model.Apellido = model.Apellido?.Trim() ?? "";
        model.Email = IdentificadorCuenta.Normalizar(model.Email);
        if (!Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true))
            return ServiceResult.Error("Revise nombre, apellido y email.");
        if (db.Database.CurrentTransaction != null || db.ChangeTracker.HasChanges())
            return ServiceResult.Error("Hay otra operación pendiente.");
        int? personaId = null;
        int? cuentaExistente = null;
        try
        {
            await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                await identidad.BloquearEmailAsync(model.Email);
                var email = model.Email.ToUpperInvariant();
                cuentaExistente = await db.Usuarios.Where(u => u.EmailLogin.Trim().ToUpper() == email)
                    .Select(u => (int?)u.Id).SingleOrDefaultAsync();
                if (!cuentaExistente.HasValue)
                {
                    var personas = await db.Personas.Where(p => p.Email != null && p.Email.Trim().ToUpper() == email).ToListAsync();
                    if (personas.Count > 1) return ServiceResult.Ok(Respuesta);
                    var persona = personas.SingleOrDefault();
                    if (persona != null)
                    {
                        if (!await identidad.ExclusivamenteClienteAsync(persona.Id) || await db.Usuarios.AnyAsync(u => u.PersonaId == persona.Id))
                            return ServiceResult.Ok(Respuesta);
                    }
                    else
                    {
                        var rol = await db.Roles.SingleOrDefaultAsync(r => r.Nombre == RolesSistema.CLIENTE && r.Activo);
                        if (rol == null) return ServiceResult.Ok(Respuesta);
                        persona = new Persona { Nombre = model.Nombre, Apellido = model.Apellido, Email = model.Email };
                        persona.Roles.Add(new PersonaRol { RolId = rol.Id });
                        db.Personas.Add(persona);
                        await db.SaveChangesAsync();
                        auditoria.RegistrarOnboarding("CLIENTE_CREADO", "Persona", persona.Id);
                    }
                    personaId = persona.Id;
                    auditoria.RegistrarOnboarding("AUTORREGISTRO_SOLICITADO", "Persona", persona.Id);
                    await db.SaveChangesAsync();
                }
                await tx.CommitAsync();
            }
            if (cuentaExistente.HasValue)
            {
                // El mensaje específico sólo se muestra después de demostrar acceso al correo.
                // No crear Persona, Usuario ni InvitacionCliente para una cuenta existente.
                await seguridad.EnviarOrientacionAsync(cuentaExistente.Value);
                return ServiceResult.Ok(Respuesta);
            }
            // La respuesta pública no distingue conflicto, entrega SMTP ni existencia de cuenta.
            if (!personaId.HasValue) return ServiceResult.Ok(Respuesta);
            var emision = await invitaciones.EmitirPublicaAsync(personaId.Value);
            if (!emision.Exitoso)
                logger.LogWarning(new EventId(4104, "AUTORREGISTRO_EMISION_NO_COMPLETADA"),
                    "No se completó la emisión del autorregistro. Consulte los eventos de InvitacionClienteService.");
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        {
            logger.LogWarning("Solicitud pública no completada. Tipo: {Tipo}", ex.GetType().Name);
        }
        return ServiceResult.Ok(Respuesta);
    }
}
