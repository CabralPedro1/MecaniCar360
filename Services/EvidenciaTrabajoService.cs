using System.Data;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public sealed class EvidenciaTrabajoService(MecaniCarContext context, PermisoService permisos,
    AlmacenEvidencias almacen, AuditoriaService auditoria, ILogger<EvidenciaTrabajoService> logger)
{
    private static bool EstadoPermite(OrdenTrabajo o) => o.EstadoActual is EstadoOrden.Diagnostico or
        EstadoOrden.EsperandoAprobacion or EstadoOrden.Aprobado or EstadoOrden.EnReparacion or EstadoOrden.Rechazado or EstadoOrden.Finalizado;

    private async Task<bool> AccesoAsync(OrdenTrabajo orden, int usuarioId, bool cargar)
    {
        if (!await permisos.TienePermisoAsync(usuarioId, cargar ? "EVIDENCIA_CREAR" : "EVIDENCIA_VER")) return false;
        var persona = await permisos.ObtenerPersonaActivaIdAsync(usuarioId);
        if (!persona.HasValue) return false;
        if (await permisos.EsAdministradorAsync(usuarioId)) return true;
        var tecnico = orden.MecanicoId == persona && await context.PersonaRoles.AnyAsync(pr =>
            pr.PersonaId == persona && pr.FechaBaja == null && pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.MECANICO);
        if (tecnico) return true;
        return !cargar && await permisos.TienePermisoAsync(usuarioId,"CLIENTE_ORDEN_VER") &&
            await context.OrdenesTrabajo.AnyAsync(o => o.Id == orden.Id && o.IngresoVehiculo.Turno.ClienteId == persona);
    }

    public async Task<ServiceResult<EvidenciasViewModel>> ObtenerPorOrdenTrabajoAsync(int ordenTrabajoId, int usuarioId)
    {
        var orden = await context.OrdenesTrabajo.AsNoTracking().SingleOrDefaultAsync(o => o.Id == ordenTrabajoId);
        if (orden == null || !await AccesoAsync(orden,usuarioId,false)) return ServiceResult<EvidenciasViewModel>.Error("Acceso denegado.");
        var datos = await context.Evidencias.AsNoTracking().Where(e => e.OrdenTrabajoId == ordenTrabajoId)
            .OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id)
            .Select(e => new {e.Id,e.Fecha,e.Descripcion,e.RutaArchivo}).ToListAsync();
        return ServiceResult<EvidenciasViewModel>.Ok(new(ordenTrabajoId,EstadoPermite(orden) && await AccesoAsync(orden,usuarioId,true),
            datos.Select(e => new EvidenciaResumen(e.Id,e.Fecha,e.Descripcion,AlmacenEvidencias.Mime(e.RutaArchivo) ?? "Archivo anterior")).ToList()));
    }

    public async Task<ServiceResult> CrearAsync(int ordenTrabajoId,int usuarioId,string descripcion,IFormFile? archivo)
    {
        if (archivo == null || string.IsNullOrWhiteSpace(descripcion) || descripcion.Trim().Length > 500)
            return ServiceResult.Error("Ingrese una descripción de hasta 500 caracteres y un archivo.");
        if (context.Database.CurrentTransaction != null) return ServiceResult.Error("La carga requiere una operación independiente.");
        var previa = await context.OrdenesTrabajo.AsNoTracking().SingleOrDefaultAsync(o=>o.Id==ordenTrabajoId);
        if (previa == null || !await AccesoAsync(previa,usuarioId,true)) return ServiceResult.Error("Acceso denegado.");
        (byte[] Datos,string Extension) validado;
        try { validado = await almacen.ValidarAsync(archivo); }
        catch (InvalidDataException ex) { return ServiceResult.Error(ex.Message); }
        catch (IOException) { return ServiceResult.Error("No se pudo leer el archivo recibido."); }
        var clave = Guid.NewGuid().ToString("N") + validado.Extension;
        bool guardado=false, confirmado=false;
        await using var tx=await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var orden=await context.OrdenesTrabajo.FromSqlInterpolated(
                $"SELECT * FROM [OrdenesTrabajo] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={ordenTrabajoId}").SingleOrDefaultAsync();
            if(orden!=null) await context.Entry(orden).ReloadAsync();
            if(orden==null || !await AccesoAsync(orden,usuarioId,true) || !EstadoPermite(orden))
                return ServiceResult.Error("La orden no admite esta carga.");
            await almacen.GuardarAsync(clave,validado.Datos);guardado=true;
            var evidencia=new EvidenciaTrabajo {OrdenTrabajoId=orden.Id,Descripcion=descripcion.Trim(),RutaArchivo=clave,SubidaPorUsuarioId=usuarioId,Fecha=DateTime.Now};
            context.Evidencias.Add(evidencia);
            await context.SaveChangesAsync();
            auditoria.RegistrarOperacion("EVIDENCIA_CREADA","EvidenciaTrabajo",evidencia.Id,usuarioId,$"Orden #{orden.Id}.");
            await context.SaveChangesAsync();
            await tx.CommitAsync();confirmado=true;
            return ServiceResult.Ok("Evidencia agregada correctamente.");
        }
        catch(Exception ex)
        {
            await tx.RollbackAsync();context.ChangeTracker.Clear();
            logger.LogWarning("No se pudo guardar evidencia en OT {OrdenId}. Tipo de error: {Tipo}",ordenTrabajoId,ex.GetType().Name);
            return ServiceResult.Error("No se pudo guardar la evidencia. Intente nuevamente.");
        }
        finally { if(guardado && !confirmado) almacen.EliminarCreado(clave); }
    }

    public async Task<ServiceResult<ArchivoEvidencia>> ArchivoAsync(int id,int usuarioId)
    {
        var evidencia=await context.Evidencias.AsNoTracking().Include(e=>e.OrdenTrabajo).SingleOrDefaultAsync(e=>e.Id==id);
        if(evidencia==null || !await AccesoAsync(evidencia.OrdenTrabajo,usuarioId,false)) return ServiceResult<ArchivoEvidencia>.Error("Archivo no disponible.");
        try
        {
            var mime=AlmacenEvidencias.Mime(evidencia.RutaArchivo);
            if(mime==null) return ServiceResult<ArchivoEvidencia>.Error("Archivo no disponible.");
            var stream=almacen.Abrir(evidencia.RutaArchivo);
            return ServiceResult<ArchivoEvidencia>.Ok(new(stream,mime,$"evidencia-{id}{Path.GetExtension(evidencia.RutaArchivo)}"));
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException)
        { return ServiceResult<ArchivoEvidencia>.Error("Archivo no disponible."); }
    }
}
