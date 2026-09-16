using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services;

public partial class StockService
{
    public async Task<ServiceResult<List<RepuestoTecnicoDto>>> ObtenerRepuestosTecnicosAsync(int usuarioSolicitanteId, int? id = null)
    {
        if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_VER"))
            return ServiceResult<List<RepuestoTecnicoDto>>.Error("Acceso denegado.");
        var datos = await _context.Repuestos.AsNoTracking()
            .Where(r => r.Activo && (!id.HasValue || r.Id == id.Value)).OrderBy(r => r.Nombre)
            .Select(r => new RepuestoTecnicoDto(r.Id, r.SKU, r.Nombre, r.Compatibilidad, r.StockActual, r.StockMinimo))
            .ToListAsync();
        return ServiceResult<List<RepuestoTecnicoDto>>.Ok(datos);
    }
}
