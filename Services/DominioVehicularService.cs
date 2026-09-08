using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;

public class DominioVehicularService
{
    private readonly MecaniCarContext _context;
    private readonly VehiculoService _vehiculoService;
    private readonly PermisoService _permisoService;

    public DominioVehicularService(
        MecaniCarContext context,
        VehiculoService vehiculoService,
        PermisoService permisoService)
    {
        _context = context;
        _vehiculoService = vehiculoService;
        _permisoService = permisoService;
    }

    // CONSULTAS

    public async Task<ServiceResult<List<Vehiculo>>> ObtenerVehiculosDePersonaAsync(int personaId)
    {
        var vehiculos = await _context.DominiosVehiculares
            .Include(d => d.Vehiculo)
                .ThenInclude(v => v.Marca)
            .Include(d => d.Vehiculo)
                .ThenInclude(v => v.Modelo)
            .Where(d =>
                d.PersonaId == personaId &&
                d.FechaHasta == null &&
                d.Vehiculo.Activo)
            .Select(d => d.Vehiculo)
            .OrderBy(v => v.Patente)
            .ToListAsync();

        return ServiceResult<List<Vehiculo>>.Ok(vehiculos);
    }

    public async Task<ServiceResult<Persona>> ObtenerTitularActualAsync(int vehiculoId)
    {
        var dominio = await _context.DominiosVehiculares
            .Include(d => d.Persona)
            .FirstOrDefaultAsync(d =>
                d.VehiculoId == vehiculoId &&
                d.FechaHasta == null);

        if (dominio == null)
            return ServiceResult<Persona>.Error("El vehículo no posee un titular.");

        return ServiceResult<Persona>.Ok(dominio.Persona);
    }

    public Task<bool> EsTitularActualAsync(
        int personaId,
        int vehiculoId)
    {
        return _context.DominiosVehiculares.AnyAsync(d =>
            d.PersonaId == personaId &&
            d.VehiculoId == vehiculoId &&
            d.FechaHasta == null);
    }

    public async Task<ServiceResult<List<DominioVehicular>>> ObtenerHistorialAsync(int vehiculoId)
    {
        var historial = await _context.DominiosVehiculares
            .Include(d => d.Persona)
            .Where(d => d.VehiculoId == vehiculoId)
            .OrderByDescending(d => d.FechaDesde)
            .ToListAsync();

        return ServiceResult<List<DominioVehicular>>.Ok(historial);
    }

    // ABM

    public async Task<ServiceResult> CrearVehiculoAsync(
        int personaId,
        Vehiculo vehiculo,
        int usuarioSolicitanteId)
    {
        if (!await _permisoService.TienePermisoAsync(
            usuarioSolicitanteId,
            "VEHICULO_CREAR"))
        {
            return ServiceResult.Error(
                "No posee permisos para crear vehículos.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var persona = await _context.Personas.FindAsync(personaId);

            if (persona == null || !persona.Activo)
                return ServiceResult.Error("La persona no existe o está inactiva.");

            var resultado = await _vehiculoService
                .CrearAsync(vehiculo, usuarioSolicitanteId);

            if (!resultado.Exitoso)
                return resultado;

            _context.DominiosVehiculares.Add(new DominioVehicular
            {
                PersonaId = personaId,
                VehiculoId = vehiculo.Id,
                FechaDesde = DateTime.Now
            });

            await GuardarCambiosAsync();

            await transaction.CommitAsync();

            return ServiceResult.Ok("Vehículo registrado correctamente.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResult> TransferirVehiculoAsync(
        int vehiculoId,
        int nuevaPersonaId,
        int usuarioSolicitanteId)
    {
        if (!await _permisoService.TienePermisoAsync(
            usuarioSolicitanteId,
            "VEHICULO_MODIFICAR"))
        {
            return ServiceResult.Error(
                "No posee permisos para modificar vehículos.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var vehiculo = await _context.Vehiculos
                .FirstOrDefaultAsync(v => v.Id == vehiculoId);

            if (vehiculo == null)
                return ServiceResult.Error("Vehículo no encontrado.");

            var nuevaPersona = await _context.Personas
                .FirstOrDefaultAsync(p => p.Id == nuevaPersonaId);

            if (nuevaPersona == null || !nuevaPersona.Activo)
                return ServiceResult.Error("La persona destino no existe o está inactiva.");

            var dominiosActivos = await _context.DominiosVehiculares
                .Where(d =>
                    d.VehiculoId == vehiculoId &&
                    d.FechaHasta == null)
                .ToListAsync();

            if (dominiosActivos.Count == 0)
                return ServiceResult.Error("No existe un titular activo.");

            if (dominiosActivos.Count > 1)
            {
                return ServiceResult.Error(
                    "El vehículo posee múltiples titularidades activas y la operación no puede continuar.");
            }

            if (dominiosActivos.Any(d => d.PersonaId == nuevaPersonaId))
                return ServiceResult.Error("La persona ya es titular del vehículo.");

            var ahora = DateTime.Now;

            foreach (var dominio in dominiosActivos)
                dominio.FechaHasta = ahora;

            _context.DominiosVehiculares.Add(new DominioVehicular
            {
                VehiculoId = vehiculoId,
                PersonaId = nuevaPersonaId,
                FechaDesde = ahora
            });

            await GuardarCambiosAsync();

            await transaction.CommitAsync();

            return ServiceResult.Ok("Vehículo transferido correctamente.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResult> FinalizarTitularidadAsync(
        int vehiculoId,
        int usuarioSolicitanteId)
    {
        if (!await _permisoService.TienePermisoAsync(
            usuarioSolicitanteId,
            "VEHICULO_MODIFICAR"))
        {
            return ServiceResult.Error(
                "No posee permisos para modificar vehículos.");
        }

        var dominiosActivos = await _context.DominiosVehiculares
            .Where(d =>
                d.VehiculoId == vehiculoId &&
                d.FechaHasta == null)
            .ToListAsync();

        if (dominiosActivos.Count == 0)
            return ServiceResult.Error("No existe un titular activo.");

        if (dominiosActivos.Count > 1)
        {
            return ServiceResult.Error(
                "El vehículo posee múltiples titularidades activas y la operación no puede continuar.");
        }

        var ahora = DateTime.Now;

        foreach (var dominio in dominiosActivos)
            dominio.FechaHasta = ahora;

        await GuardarCambiosAsync();

        return ServiceResult.Ok("Titularidad finalizada correctamente.");
    }

    // MÉTODOS PRIVADOS
    private async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }

}