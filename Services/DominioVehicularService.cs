using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;

public class DominioVehicularService
{
    private readonly MecaniCarContext _context;
    private readonly VehiculoService _vehiculoService;

    public DominioVehicularService(
        MecaniCarContext context,
        VehiculoService vehiculoService)
    {
        _context = context;
        _vehiculoService = vehiculoService;
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

    public async Task<ServiceResult> CrearVehiculoAsync(int personaId, Vehiculo vehiculo)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var persona = await _context.Personas.FindAsync(personaId);

            if (persona == null)
                return ServiceResult.Error("Persona no encontrada.");

            var resultado = await _vehiculoService.CrearAsync(vehiculo);

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

    public async Task<ServiceResult> TransferirVehiculoAsync(int vehiculoId, int nuevaPersonaId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var dominio = await _context.DominiosVehiculares
                .FirstOrDefaultAsync(d =>
                    d.VehiculoId == vehiculoId &&
                    d.FechaHasta == null);

            if (dominio == null)
                return ServiceResult.Error("No existe un titular activo.");

            if (dominio.PersonaId == nuevaPersonaId)
                return ServiceResult.Error("La persona ya es titular del vehículo.");

            dominio.FechaHasta = DateTime.Now;

            _context.DominiosVehiculares.Add(new DominioVehicular
            {
                VehiculoId = vehiculoId,
                PersonaId = nuevaPersonaId,
                FechaDesde = DateTime.Now
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

    public async Task<ServiceResult> FinalizarTitularidadAsync(int vehiculoId)
    {
        var dominio = await _context.DominiosVehiculares
            .FirstOrDefaultAsync(d =>
                d.VehiculoId == vehiculoId &&
                d.FechaHasta == null);

        if (dominio == null)
            return ServiceResult.Error("No existe un titular activo.");

        dominio.FechaHasta = DateTime.Now;

        await GuardarCambiosAsync();

        return ServiceResult.Ok("Titularidad finalizada correctamente.");
    }

    // MÉTODOS PRIVADOS
    private async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }

}