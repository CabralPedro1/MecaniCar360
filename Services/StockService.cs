using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public partial class StockService
    {
        private readonly MecaniCarContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly PermisoService _permisos;

        public StockService(MecaniCarContext context, PermisoService permisos, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _permisos = permisos;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<List<Repuesto>>> ObtenerRepuestosAsync(int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_VER")) return ServiceResult<List<Repuesto>>.Error("Acceso denegado.");

            var repuestos = await _context.Repuestos
                .Include(r => r.Proveedores)
                    .ThenInclude(pr => pr.Proveedor)
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return ServiceResult<List<Repuesto>>.Ok(repuestos);
        }

        public async Task<ServiceResult<Repuesto>> ObtenerRepuestoAsync(int id, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_VER")) return ServiceResult<Repuesto>.Error("Acceso denegado.");

            var repuesto = await ObtenerRepuestoCompletoAsync(id);

            if (repuesto == null)
                return ServiceResult<Repuesto>.Error("Repuesto no encontrado.");

            return ServiceResult<Repuesto>.Ok(repuesto);
        }

        public async Task<ServiceResult<List<MovimientoStock>>> ObtenerMovimientosAsync(int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_MOVIMIENTO")) return ServiceResult<List<MovimientoStock>>.Error("Acceso denegado.");

            var movimientos = await _context.MovimientosStock
                .Include(m => m.Repuesto)
                .Include(m => m.ProveedorRepuesto)
                    .ThenInclude(pr => pr.Proveedor)
                .Include(m => m.RealizadoPorUsuario)
                    .ThenInclude(u => u.Persona)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            return ServiceResult<List<MovimientoStock>>.Ok(movimientos);
        }

        public async Task<ServiceResult<List<ProveedorRepuesto>>> ObtenerProveedoresDeRepuestoAsync(int repuestoId, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_MODIFICAR")) return ServiceResult<List<ProveedorRepuesto>>.Error("Acceso denegado.");

            var proveedores = await _context.ProveedorRepuestos
                .Include(pr => pr.Proveedor)
                .Where(pr => pr.RepuestoId == repuestoId)
                .OrderByDescending(pr => pr.Principal)
                .ThenBy(pr => pr.Proveedor.Nombre)
                .ToListAsync();

            return ServiceResult<List<ProveedorRepuesto>>.Ok(proveedores);
        }

        public async Task<ServiceResult<ProveedorRepuesto>> ObtenerRelacionProveedorAsync(int proveedorRepuestoId, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_MODIFICAR")) return ServiceResult<ProveedorRepuesto>.Error("Acceso denegado.");

            var relacion = await ObtenerRelacionProveedorCompletaAsync(proveedorRepuestoId);

            if (relacion == null)
                return ServiceResult<ProveedorRepuesto>.Error("Relación no encontrada.");

            return ServiceResult<ProveedorRepuesto>.Ok(relacion);
        }

        public async Task<bool> HayStockAsync(int repuestoId, int cantidad, int usuarioSolicitanteId)
        {
            if (repuestoId <= 0 || cantidad <= 0) return false;
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_VER")) return false;

            var repuesto = await ObtenerRepuestoActivoAsync(repuestoId);

            return repuesto != null && repuesto.StockActual >= cantidad;
        }

        // =====================================
        // ASOCIACIONES CON PROVEEDORES
        // =====================================

        public async Task<ServiceResult> AgregarProveedorARepuestoAsync(
            int repuestoId,
            int proveedorId,
            decimal precioCompra,
            string? codigoProveedor,
            bool principal, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");

            if (!PrecioValido(precioCompra) || codigoProveedor?.Length > 50 || repuestoId <= 0 || proveedorId <= 0)
                return ServiceResult.Error("El precio de compra debe ser mayor a cero.");

            var repuesto = await ObtenerRepuestoActivoAsync(repuestoId);

            if (repuesto == null)
                return ServiceResult.Error("Repuesto no encontrado.");

            var proveedor = await ObtenerProveedorActivoAsync(proveedorId);

            if (proveedor == null)
                return ServiceResult.Error("Proveedor no encontrado.");

            bool existe = await _context.ProveedorRepuestos.AnyAsync(pr =>
                pr.RepuestoId == repuestoId &&
                pr.ProveedorId == proveedorId);

            if (existe)
                return ServiceResult.Error("Ese proveedor ya está asociado al repuesto.");

            if (principal)
                await QuitarProveedorPrincipalAsync(repuestoId);

            _context.ProveedorRepuestos.Add(new ProveedorRepuesto
            {
                RepuestoId = repuestoId,
                ProveedorId = proveedorId,
                PrecioCompraActual = precioCompra,
                CodigoProveedor = codigoProveedor?.Trim(),
                Principal = principal
            });

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Proveedor agregado correctamente.");
        }

        public async Task<ServiceResult> ActualizarPrecioCompraAsync(
            int proveedorRepuestoId,
            decimal precio, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");

            if (!PrecioValido(precio) || proveedorRepuestoId <= 0)
                return ServiceResult.Error("El precio debe ser mayor a cero.");

            var relacion = await ObtenerRelacionProveedorCompletaAsync(proveedorRepuestoId);

            if (relacion == null)
                return ServiceResult.Error("Relación no encontrada.");

            relacion.PrecioCompraActual = precio;

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Precio actualizado correctamente.");
        }

        public async Task<ServiceResult> CambiarProveedorPrincipalAsync(
            int proveedorRepuestoId, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");

            var relacion = await ObtenerRelacionProveedorCompletaAsync(proveedorRepuestoId);

            if (relacion == null)
                return ServiceResult.Error("Relación no encontrada.");

            await QuitarProveedorPrincipalAsync(relacion.RepuestoId);

            relacion.Principal = true;

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Proveedor principal actualizado.");
        }

        public async Task<ServiceResult> EliminarProveedorDelRepuestoAsync(
            int proveedorRepuestoId, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");

            var relacion = await ObtenerRelacionProveedorCompletaAsync(proveedorRepuestoId);

            if (relacion == null)
                return ServiceResult.Error("Relación no encontrada.");

            var relaciones = await _context.ProveedorRepuestos
                .Where(pr => pr.RepuestoId == relacion.RepuestoId)
                .OrderBy(pr => pr.Id)
                .ToListAsync();

            if (relaciones.Count == 1)
                return ServiceResult.Error("El repuesto debe tener al menos un proveedor.");

            bool eraPrincipal = relacion.Principal;

            _context.ProveedorRepuestos.Remove(relacion);

            if (eraPrincipal)
            {
                var nuevoPrincipal = relaciones
                    .FirstOrDefault(p => p.Id != relacion.Id);

                if (nuevoPrincipal != null)
                    nuevoPrincipal.Principal = true;
            }

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Proveedor desvinculado correctamente.");
        }


        // =====================================
        // CONSULTA DE DISPONIBILIDAD
        // =====================================

        public async Task<(
            bool Existe,
            int StockActual,
            int StockMinimo,
            int CantidadNecesaria,
            int CantidadFaltante,
            bool QuedaBajoMinimo)>
            AnalizarDisponibilidadAsync(
                int repuestoId,
                int cantidadNecesaria, int usuarioSolicitanteId)
        {
            if (repuestoId <= 0 || cantidadNecesaria <= 0 || !await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_VER")) return (false, 0, 0, cantidadNecesaria, cantidadNecesaria, false);

            var repuesto =
                await _context.Repuestos
                    .FirstOrDefaultAsync(r =>
                        r.Id == repuestoId &&
                        r.Activo);

            if (repuesto == null)
            {
                return (
                    false,
                    0,
                    0,
                    cantidadNecesaria,
                    cantidadNecesaria,
                    false);
            }

            var cantidadFaltante =
                Math.Max(
                    0,
                    cantidadNecesaria -
                    repuesto.StockActual);

            var stockDespues =
                Math.Max(
                    0,
                    repuesto.StockActual -
                    cantidadNecesaria);

            var quedaBajoMinimo =
                stockDespues <=
                repuesto.StockMinimo;

            return (
                true,
                repuesto.StockActual,
                repuesto.StockMinimo,
                cantidadNecesaria,
                cantidadFaltante,
                quedaBajoMinimo);
        }


        // =====================================
        // NOTIFICACIONES DE STOCK
        // =====================================

        private async Task NotificarResponsableStockAsync(
            Repuesto repuesto,
            int ordenTrabajoId,
            int cantidadFaltante)
        {
            var personasStock =
                await _context.PersonaRoles
                    .Include(pr => pr.Rol)
                    .Where(pr =>
                        pr.FechaBaja == null &&
                        pr.Rol.Activo &&
                        pr.Persona.Activo && pr.Persona.Usuario != null && pr.Persona.Usuario.Activo &&
                        pr.Rol.Nombre ==
                            RolesSistema.STOCK)
                    .Select(pr => pr.PersonaId)
                    .Distinct()
                    .ToListAsync();


            string mensaje;

            if (cantidadFaltante > 0)
            {
                mensaje =
                    $"FALTANTE DE STOCK - Orden #{ordenTrabajoId}. " +
                    $"El repuesto '{repuesto.Nombre}' " +
                    $"no tiene cantidad suficiente. " +
                    $"Faltan {cantidadFaltante} unidad(es). " +
                    $"Stock actual: {repuesto.StockActual}.";
            }
            else
            {
                mensaje =
                    $"ALERTA DE STOCK - Orden #{ordenTrabajoId}. " +
                    $"El repuesto '{repuesto.Nombre}' " +
                    $"quedó en {repuesto.StockActual} unidad(es), " +
                    $"igual o por debajo del stock mínimo " +
                    $"({repuesto.StockMinimo}).";
            }


            foreach (var personaId in personasStock)
            {
                _context.Notificaciones.Add(
                    new Notificacion
                    {
                        PersonaId =
                            personaId,

                        Mensaje =
                            mensaje,

                        Fecha =
                            DateTime.Now,

                        Leida =
                            false
                    });
            }
        }



        // =====================================
        // MÉTODOS PRIVADOS
        // =====================================



        private async Task<Repuesto?> ObtenerRepuestoActivoAsync(int id)
        {
            return await _context.Repuestos
                .FirstOrDefaultAsync(r => r.Id == id && r.Activo);
        }

        private async Task<Repuesto?> ObtenerRepuestoCompletoAsync(int id)
        {
            return await _context.Repuestos
                .Include(r => r.Proveedores)
                    .ThenInclude(pr => pr.Proveedor)
                .Include(r => r.Movimientos)
                    .ThenInclude(m => m.RealizadoPorUsuario)
                        .ThenInclude(u => u.Persona)
                .FirstOrDefaultAsync(r => r.Id == id && r.Activo);
        }

        private async Task<Proveedor?> ObtenerProveedorActivoAsync(int id)
        {
            return await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == id && p.Activo);
        }

        private async Task<ProveedorRepuesto?> ObtenerRelacionProveedorCompletaAsync(int id)
        {
            return await _context.ProveedorRepuestos
                .Include(pr => pr.Proveedor)
                .Include(pr => pr.Repuesto)
                .FirstOrDefaultAsync(pr => pr.Id == id);
        }


        private async Task<bool> ExisteSkuAsync(string sku, int? excluirId = null)
        {
            sku = sku.Trim().ToUpper();

            return await _context.Repuestos.AnyAsync(r =>
                r.SKU == sku &&
                (!excluirId.HasValue || r.Id != excluirId.Value));
        }

        private async Task<ServiceResult> ValidarRepuestoAsync(
            Repuesto repuesto,
            int? excluirId = null)
        {
            if (string.IsNullOrWhiteSpace(repuesto.SKU))
                return ServiceResult.Error("Debe ingresar un SKU.");

            if (string.IsNullOrWhiteSpace(repuesto.Nombre))
                return ServiceResult.Error("Debe ingresar un nombre.");

            if (!PrecioValido(repuesto.PrecioVenta))
                return ServiceResult.Error("El precio de venta debe ser mayor a cero.");

            if (repuesto.StockActual < 0)
                return ServiceResult.Error("El stock no puede ser negativo.");

            if (repuesto.StockMinimo < 0)
                return ServiceResult.Error("El stock mínimo no puede ser negativo.");

            if (repuesto.SKU.Length > 30 || repuesto.Nombre.Length > 150 || repuesto.Marca?.Length > 100 ||
                repuesto.Modelo?.Length > 100 || repuesto.Compatibilidad?.Length > 300)
                return ServiceResult.Error("Los datos del repuesto exceden las longitudes permitidas.");

            if (await ExisteSkuAsync(repuesto.SKU, excluirId))
                return ServiceResult.Error("Ya existe un repuesto con ese SKU.");

            return ServiceResult.Ok();
        }

        


        private async Task QuitarProveedorPrincipalAsync(int repuestoId)
        {
            var principales = await _context.ProveedorRepuestos
                .Where(pr => pr.RepuestoId == repuestoId && pr.Principal)
                .ToListAsync();

            foreach (var proveedor in principales)
                proveedor.Principal = false;
        }

        private async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
}
