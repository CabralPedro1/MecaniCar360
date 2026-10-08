using System.ComponentModel.DataAnnotations;
using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class RegistroCompletoClienteService(MecaniCarContext db, IdentidadClienteService identidad,
    ClienteHabilitadoService habilitado, AuditoriaService auditoria)
{
    public async Task<Usuario?> ObtenerAsync(int actor) {
        var u = await db.Usuarios.AsNoTracking().Include(u => u.Persona).SingleOrDefaultAsync(u => u.Id == actor && u.Activo && u.Persona.Activo);
        return u != null && await identidad.ExclusivamenteClienteAsync(u.PersonaId) ? u : null;
    }

    public async Task<ServiceResult> CompletarAsync(int actor, RegistroCompletoClienteViewModel vm)
    {
        if (!RegistroCompletoCliente.DatosValidos(vm)) return ServiceResult.Error("Revise los datos personales.");
        if (db.Database.CurrentTransaction != null || db.ChangeTracker.HasChanges()) return ServiceResult.Error("Hay otra operación pendiente.");
        var previo = await ObtenerAsync(actor);
        if (previo == null) return ServiceResult.Error("Cuenta no disponible.");
        var entradas = db.ChangeTracker.Entries().ToDictionary(e => e.Entity, e => e.CurrentValues.Clone(), ReferenceEqualityComparer.Instance);
        var confirmado = false;
        try {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            // La misma disciplina de invitaciones y edición administrativa: Persona -> Usuario.
            var p = await db.Personas.FromSqlInterpolated($"SELECT * FROM [Personas] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={previo.PersonaId}").SingleAsync();
            await db.Entry(p).ReloadAsync();
            var u = await db.Usuarios.SingleAsync(u => u.Id == actor);
            await db.Entry(u).ReloadAsync();
            if (!u.Activo || !await identidad.ExclusivamenteClienteAsync(p.Id) || !await habilitado.CorreoVerificadoAsync(u))
                return ServiceResult.Error("Cuenta no disponible. Solicite asistencia al taller.");
            if (await habilitado.EstaHabilitadoAsync(p.Id)) return ServiceResult.Error("El registro ya está completo.");
            if (await DniPersona.ExisteAsync(db, vm.Dni, p.Id)) return ServiceResult.Error(DniPersona.Error);
            RegistroCompletoCliente.Aplicar(p, vm);
            u.PrimerLogin = false; u.SecurityStamp = Guid.NewGuid().ToString("N");
            auditoria.RegistrarOperacion("REGISTRO_CLIENTE_COMPLETADO", "Usuario", u.Id, actor);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            confirmado = true;
            return ServiceResult.Ok("Registro completado. Inicie sesión con su método habitual.");
        }
        catch (Exception ex) when (DniPersona.Conflicto(ex)) { return ServiceResult.Error(DniPersona.Error); }
        finally {
            // No dejar escrituras pendientes tras rollback ni desenganchar entidades del caller.
            foreach (var e in db.ChangeTracker.Entries().Where(_ => !confirmado).ToList()) {
                if (!entradas.TryGetValue(e.Entity, out var valores)) e.State = EntityState.Detached;
                else { e.CurrentValues.SetValues(valores); e.OriginalValues.SetValues(valores); e.State = EntityState.Unchanged; }
            }
        }
    }

    public async Task<ServiceResult> ActualizarContactoAsync(int actor, ContactoClienteViewModel vm)
    {
        if (!Validator.TryValidateObject(vm, new(vm), new List<ValidationResult>(), true)) return ServiceResult.Error("Teléfono no válido.");
        var u = await ObtenerAsync(actor);
        if (u == null || !await habilitado.EstaHabilitadoAsync(u.PersonaId)) return ServiceResult.Error("Acceso denegado.");
        if (!await new PermisoService(db).TieneAlgunoAsync(actor, "CLIENTE_VEHICULO_VER", "CLIENTE_TURNO_VER", "CLIENTE_ORDEN_VER",
            "CLIENTE_PRESUPUESTO_VER", "CLIENTE_FACTURA_VER", "GARANTIA_VER_PROPIA")) return ServiceResult.Error("Acceso denegado.");
        if (db.Database.CurrentTransaction != null || db.ChangeTracker.HasChanges()) return ServiceResult.Error("Hay otra operación pendiente.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var p = await db.Personas.FromSqlInterpolated($"SELECT * FROM [Personas] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={u.PersonaId}").SingleAsync();
        await db.Entry(p).ReloadAsync();
        if (await ObtenerAsync(actor) == null || !await habilitado.EstaHabilitadoAsync(p.Id)) return ServiceResult.Error("Acceso denegado.");
        var anterior = db.Entry(p).CurrentValues.Clone();
        var auditorias = db.ChangeTracker.Entries<Auditoria>().Select(e => e.Entity).ToHashSet();
        try {
            p.Telefono = vm.Telefono.Trim();
            auditoria.RegistrarOperacion("CLIENTE_CONTACTO_MODIFICADO", "Persona", p.Id, actor);
            await db.SaveChangesAsync(); await tx.CommitAsync(); return ServiceResult.Ok("Datos actualizados.");
        } catch (Exception ex) {
            try { await tx.RollbackAsync(); } catch { /* El coordinador puede haber revertido. */ }
            db.Entry(p).CurrentValues.SetValues(anterior); db.Entry(p).OriginalValues.SetValues(anterior); db.Entry(p).State = EntityState.Unchanged;
            foreach (var e in db.ChangeTracker.Entries<Auditoria>().Where(e => !auditorias.Contains(e.Entity)).ToList()) e.State = EntityState.Detached;
            if (DniPersona.Conflicto(ex)) return ServiceResult.Error("Los datos cambiaron. Recargue e intente nuevamente.");
            throw;
        }
    }
}
