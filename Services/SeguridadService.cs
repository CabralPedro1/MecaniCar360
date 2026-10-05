using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class SeguridadService(MecaniCarContext db, PermisoService permisos, AuditoriaService auditoria)
{
    public const string Capacidad = "SEGURIDAD_ADMINISTRAR";

    public async Task<SeguridadViewModel?> ConsultarAsync(int actor)
    {
        if (!await permisos.TienePermisoAsync(actor, Capacidad)) return null;
        return new SeguridadViewModel
        {
            Familias = await db.Familias.AsNoTracking().Include(f => f.Patentes).Include(f => f.Roles).OrderBy(f => f.Nombre).ToListAsync(),
            Patentes = await db.Patentes.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync(),
            Roles = await db.Roles.AsNoTracking().Include(r => r.Familias).OrderBy(r => r.Nombre).ToListAsync()
        };
    }

    // Serializa las escrituras del grafo completo, incluida la comprobación de ciclos.
    private async Task<ServiceResult> EscribirAsync(int actor, Func<Task<ServiceResult>> cambio)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=N'MecaniCar360.Seguridad', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 51000, 'No se pudo bloquear la configuración de seguridad.', 1;");
        if (!await permisos.TienePermisoAsync(actor, Capacidad)) return ServiceResult.Error("Acceso denegado.");
        var resultado = await cambio();
        if (!resultado.Exitoso) return resultado;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return resultado;
    }

    public Task<ServiceResult> GuardarFamiliaAsync(FamiliaEdicionViewModel model, int actor) => EscribirAsync(actor, async () =>
    {
        var nombre = model.Nombre?.Trim() ?? "";
        if (nombre.Length is 0 or > 100 || model.Descripcion?.Length > 250)
            return ServiceResult.Error("Nombre o descripción inválidos.");
        if (await db.Familias.AnyAsync(f => f.Id != model.Id && f.Nombre.ToUpper() == nombre.ToUpper()))
            return ServiceResult.Error("Ya existe una familia con ese nombre.");
        var nueva = model.Id == 0;
        var familia = nueva ? new Familia() : await db.Familias.FindAsync(model.Id);
        if (familia == null) return ServiceResult.Error("Familia inexistente.");
        familia.Nombre = nombre;
        familia.Descripcion = model.Descripcion?.Trim();
        familia.Activo = model.Activo;
        if (nueva) db.Familias.Add(familia);
        await db.SaveChangesAsync();
        auditoria.RegistrarOperacion(nueva ? "FAMILIA_CREADA" : "FAMILIA_MODIFICADA", "Familia", familia.Id, actor);
        return ServiceResult.Ok("Familia guardada.");
    });

    public Task<ServiceResult> PatenteAsync(int familiaId, int patenteId, bool agregar, int actor) => EscribirAsync(actor, async () =>
    {
        if (!await db.Familias.AnyAsync(f => f.Id == familiaId) || !await db.Patentes.AnyAsync(p => p.Id == patenteId))
            return ServiceResult.Error("Familia o patente inexistente.");
        var relacion = await db.FamiliaPatentes.FindAsync(familiaId, patenteId);
        if (agregar == (relacion != null)) return ServiceResult.Error(agregar ? "Asociación duplicada." : "Asociación inexistente.");
        if (agregar) db.FamiliaPatentes.Add(new FamiliaPatente { FamiliaId = familiaId, PatenteId = patenteId });
        else db.FamiliaPatentes.Remove(relacion!);
        auditoria.RegistrarOperacion(agregar ? "FAMILIA_PATENTE_AGREGADA" : "FAMILIA_PATENTE_QUITADA", "Familia", familiaId, actor, $"PatenteId: {patenteId}");
        return ServiceResult.Ok("Composición actualizada.");
    });

    public static bool FormaCiclo(int padre, int hija, IReadOnlyDictionary<int, int?> padres)
    {
        var visitados = new HashSet<int>();
        int? actual = padre;
        while (actual.HasValue)
        {
            if (actual == hija || !visitados.Add(actual.Value)) return true;
            actual = padres.TryGetValue(actual.Value, out var siguiente) ? siguiente : null;
        }
        return false;
    }

    public Task<ServiceResult> HijaAsync(int familiaId, int hijaId, bool agregar, int actor) => EscribirAsync(actor, async () =>
    {
        var padres = await db.Familias.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.FamiliaPadreId);
        if (!padres.ContainsKey(familiaId) || !padres.ContainsKey(hijaId)) return ServiceResult.Error("Familia inexistente.");
        if (agregar && FormaCiclo(familiaId, hijaId, padres)) return ServiceResult.Error("La asociación crearía un ciclo.");
        if (agregar && padres[hijaId].HasValue) return ServiceResult.Error("La familia ya tiene padre. Quite primero esa asociación.");
        if (!agregar && padres[hijaId] != familiaId) return ServiceResult.Error("Asociación inexistente.");
        var hija = await db.Familias.FindAsync(hijaId);
        hija!.FamiliaPadreId = agregar ? familiaId : null;
        auditoria.RegistrarOperacion(agregar ? "FAMILIA_HIJA_AGREGADA" : "FAMILIA_HIJA_QUITADA", "Familia", familiaId, actor, $"HijaId: {hijaId}");
        return ServiceResult.Ok("Jerarquía actualizada.");
    });

    public Task<ServiceResult> RolAsync(int rolId, int familiaId, bool agregar, int actor) => EscribirAsync(actor, async () =>
    {
        if (!await db.Roles.AnyAsync(r => r.Id == rolId) || !await db.Familias.AnyAsync(f => f.Id == familiaId))
            return ServiceResult.Error("Rol o familia inexistente.");
        var relacion = await db.RolFamilias.FindAsync(rolId, familiaId);
        if (agregar == (relacion != null)) return ServiceResult.Error(agregar ? "Asociación duplicada." : "Asociación inexistente.");
        if (agregar) db.RolFamilias.Add(new RolFamilia { RolId = rolId, FamiliaId = familiaId });
        else db.RolFamilias.Remove(relacion!);
        auditoria.RegistrarOperacion(agregar ? "ROL_FAMILIA_AGREGADA" : "ROL_FAMILIA_QUITADA", "Rol", rolId, actor, $"FamiliaId: {familiaId}");
        return ServiceResult.Ok("Familias del rol actualizadas.");
    });
}
