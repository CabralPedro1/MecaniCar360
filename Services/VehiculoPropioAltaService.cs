using System.ComponentModel.DataAnnotations;
using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class VehiculoPropioAltaService(MecaniCarContext db, PermisoService permisos,
    ClienteHabilitadoService habilitado, IdentidadClienteService identidad, VehiculoService vehiculos,
    AuditoriaService auditoria)
{
    private async Task<int?> PersonaAsync(int actor)
    {
        if (!await permisos.TienePermisoAsync(actor, "CLIENTE_VEHICULO_CREAR")) return null;
        var id = await db.Usuarios.Where(u => u.Id == actor && u.Activo && u.Persona.Activo)
            .Select(u => (int?)u.PersonaId).SingleOrDefaultAsync();
        return id.HasValue && await identidad.ExclusivamenteClienteAsync(id.Value) &&
            await habilitado.EstaHabilitadoAsync(id.Value) ? id : null;
    }

    public async Task<ServiceResult<List<CatalogoModelo>>> CatalogoAsync(int actor)
    {
        if (!(await PersonaAsync(actor)).HasValue)
            return ServiceResult<List<CatalogoModelo>>.Error("Acceso denegado.");
        return ServiceResult<List<CatalogoModelo>>.Ok(await db.Modelos.AsNoTracking()
            .Where(m => m.Activo && m.Marca.Activo).OrderBy(m => m.Marca.Nombre).ThenBy(m => m.Nombre)
            .Select(m => new CatalogoModelo(m.Id, m.Marca.Nombre + " - " + m.Nombre)).ToListAsync());
    }

    public async Task<ServiceResult> CrearAsync(VehiculoPropioNuevoViewModel model, int actor)
    {
        if (!Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true))
            return ServiceResult.Error("Revise los datos del vehículo.");
        if (db.Database.CurrentTransaction != null || db.ChangeTracker.HasChanges())
            return ServiceResult.Error("Hay otra operación pendiente.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var persona = await PersonaAsync(actor);
            if (!persona.HasValue) return ServiceResult.Error("Acceso denegado.");
            var modelo = await db.Modelos.AsNoTracking().SingleOrDefaultAsync(m => m.Id == model.ModeloId);
            if (modelo == null) return ServiceResult.Error("Modelo no disponible.");
            var vehiculo = new Vehiculo { Patente = model.Patente, Vin = model.Vin, MarcaId = modelo.MarcaId,
                ModeloId = modelo.Id, Anio = model.Anio, Color = model.Color, Kilometraje = model.Kilometraje };
            var r = await vehiculos.CrearValidadoAsync(vehiculo);
            if (!r.Exitoso) return r;
            db.DominiosVehiculares.Add(new DominioVehicular { PersonaId = persona.Value, VehiculoId = vehiculo.Id,
                FechaDesde = DateTime.Now });
            auditoria.RegistrarOperacion("VEHICULO_PROPIO_CREADO", "Vehiculo", vehiculo.Id, actor);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Ok("Vehículo registrado.");
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        {
            await tx.RollbackAsync();
            db.ChangeTracker.Clear();
            return ServiceResult.Error("No se pudo registrar. Verifique la patente y vuelva a intentar.");
        }
    }
}

public sealed record CatalogoModelo(int Id, string Nombre);
