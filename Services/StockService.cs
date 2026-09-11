using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class StockService
    {
        private readonly MecaniCarContext _context;

        public StockService(MecaniCarContext context)
        {
            _context = context;
        }

        // =====================================
        // CONSULTAS
        // =====================================

        public async Task<ServiceResult<List<Repuesto>>> ObtenerRepuestosAsync()
        {
            var repuestos = await _context.Repuestos
                .Include(r => r.Proveedores)
                    .ThenInclude(pr => pr.Proveedor)
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return ServiceResult<List<Repuesto>>.Ok(repuestos);
        }

        public async Task<ServiceResult<Repuesto>> ObtenerRepuestoAsync(int id)
        {
            var repuesto = await ObtenerRepuestoCompletoAsync(id);

            if (repuesto == null)
                return ServiceResult<Repuesto>.Error("Repuesto no encontrado.");

            return ServiceResult<Repuesto>.Ok(repuesto);
        }

        public async Task<ServiceResult<List<MovimientoStock>>> ObtenerMovimientosAsync()
        {
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

        public async Task<ServiceResult<List<ProveedorRepuesto>>> ObtenerProveedoresDeRepuestoAsync(int repuestoId)
        {
            var proveedores = await _context.ProveedorRepuestos
                .Include(pr => pr.Proveedor)
                .Where(pr => pr.RepuestoId == repuestoId)
                .OrderByDescending(pr => pr.Principal)
                .ThenBy(pr => pr.Proveedor.Nombre)
                .ToListAsync();

            return ServiceResult<List<ProveedorRepuesto>>.Ok(proveedores);
        }

        public async Task<ServiceResult<ProveedorRepuesto>> ObtenerRelacionProveedorAsync(int proveedorRepuestoId)
        {
            var relacion = await ObtenerRelacionProveedorCompletaAsync(proveedorRepuestoId);

            if (relacion == null)
                return ServiceResult<ProveedorRepuesto>.Error("Relación no encontrada.");

            return ServiceResult<ProveedorRepuesto>.Ok(relacion);
        }

        public async Task<bool> HayStockAsync(int repuestoId, int cantidad)
        {
            var repuesto = await ObtenerRepuestoActivoAsync(repuestoId);

            return repuesto != null && repuesto.StockActual >= cantidad;
        }

        // =====================================
        // ABM REPUESTOS
        // =====================================

        public async Task<ServiceResult> CrearRepuestoAsync(Repuesto repuesto)
        {
            var validacion = await ValidarRepuestoAsync(repuesto);

            if (!validacion.Exitoso)
                return validacion;

            repuesto.SKU = repuesto.SKU.Trim().ToUpper();
            repuesto.Nombre = repuesto.Nombre.Trim();
            repuesto.FechaCreacion = DateTime.UtcNow;
            repuesto.Activo = true;

            _context.Repuestos.Add(repuesto);

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Repuesto creado correctamente.");
        }

        public async Task<ServiceResult> EditarRepuestoAsync(Repuesto repuesto)
        {
            var existente = await _context.Repuestos.FindAsync(repuesto.Id);

            if (existente == null)
                return ServiceResult.Error("Repuesto no encontrado.");

            if (!existente.Activo)
                return ServiceResult.Error("No se puede editar un repuesto desactivado.");

            var validacion = await ValidarRepuestoAsync(repuesto, repuesto.Id);

            if (!validacion.Exitoso)
                return validacion;

            existente.SKU = repuesto.SKU.Trim().ToUpper();
            existente.Nombre = repuesto.Nombre.Trim();
            existente.Marca = repuesto.Marca;
            existente.Modelo = repuesto.Modelo;
            existente.Compatibilidad = repuesto.Compatibilidad;
            existente.PrecioVenta = repuesto.PrecioVenta;
            existente.StockMinimo = repuesto.StockMinimo;

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Repuesto actualizado correctamente.");
        }

        public async Task<ServiceResult> CambiarEstadoAsync(int id)
        {
            var repuesto = await _context.Repuestos
                .FirstOrDefaultAsync(r => r.Id == id);

            if (repuesto == null)
                return ServiceResult.Error("Repuesto no encontrado.");

            repuesto.Activo = !repuesto.Activo;

            await GuardarCambiosAsync();

            return ServiceResult.Ok(
                repuesto.Activo
                    ? "Repuesto activado correctamente."
                    : "Repuesto desactivado correctamente.");
        }
        // =====================================
        // MOVIMIENTOS
        // =====================================

        public async Task<ServiceResult> RegistrarIngresoAsync(
            int repuestoId,
            int proveedorRepuestoId,
            int cantidad,
            int usuarioId,
            string? observaciones)
        {
            if (cantidad <= 0)
                return ServiceResult.Error("La cantidad debe ser mayor a cero.");

            var repuesto = await ObtenerRepuestoActivoAsync(repuestoId);

            if (repuesto == null)
                return ServiceResult.Error("Repuesto no encontrado.");

            var relacion = await ObtenerRelacionProveedorCompletaAsync(proveedorRepuestoId);

            if (relacion == null || relacion.RepuestoId != repuestoId)
                return ServiceResult.Error("El proveedor seleccionado no corresponde al repuesto.");

            if (!relacion.Proveedor.Activo)
                return ServiceResult.Error("El proveedor se encuentra inactivo.");

            repuesto.StockActual += cantidad;

            await RegistrarMovimientoAsync(
                repuestoId,
                cantidad,
                TipoMovimientoStock.IngresoCompra,
                usuarioId,
                observaciones,
                proveedorRepuestoId);

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Ingreso registrado correctamente.");
        }

        public async Task<ServiceResult> RegistrarSalidaAsync(
            int repuestoId,
            int cantidad,
            int usuarioId,
            string? observaciones,
            int? ordenTrabajoId = null)
        {
            if (cantidad <= 0)
                return ServiceResult.Error("La cantidad debe ser mayor a cero.");

            var repuesto = await ObtenerRepuestoActivoAsync(repuestoId);

            if (repuesto == null)
                return ServiceResult.Error("Repuesto no encontrado.");

            if (repuesto.StockActual < cantidad)
                return ServiceResult.Error("No hay stock suficiente.");

            repuesto.StockActual -= cantidad;

            await RegistrarMovimientoAsync(
                repuestoId,
                -cantidad,
                TipoMovimientoStock.EgresoOrdenTrabajo,
                usuarioId,
                observaciones,
                null,
                ordenTrabajoId);

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Salida registrada correctamente.");
        }

        public async Task<ServiceResult> RegistrarAjusteAsync(
            int repuestoId,
            int diferencia,
            int usuarioId,
            string observaciones)
        {
            if (diferencia == 0)
                return ServiceResult.Error("Debe indicar una diferencia distinta de cero.");

            if (string.IsNullOrWhiteSpace(observaciones))
                return ServiceResult.Error("Debe indicar el motivo del ajuste.");

            var repuesto = await ObtenerRepuestoActivoAsync(repuestoId);

            if (repuesto == null)
                return ServiceResult.Error("Repuesto no encontrado.");

            if (diferencia < 0 && repuesto.StockActual < Math.Abs(diferencia))
                return ServiceResult.Error("No hay stock suficiente para realizar el ajuste.");

            repuesto.StockActual += diferencia;

            await RegistrarMovimientoAsync(
                repuestoId,
                diferencia,
                TipoMovimientoStock.Ajuste,
                usuarioId,
                observaciones);

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Ajuste registrado correctamente.");
        }

        // =====================================
        // PROVEEDORES
        // =====================================

        public async Task<ServiceResult> AgregarProveedorARepuestoAsync(
            int repuestoId,
            int proveedorId,
            decimal precioCompra,
            string? codigoProveedor,
            bool principal)
        {
            if (precioCompra <= 0)
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
            decimal precio)
        {
            if (precio <= 0)
                return ServiceResult.Error("El precio debe ser mayor a cero.");

            var relacion = await ObtenerRelacionProveedorCompletaAsync(proveedorRepuestoId);

            if (relacion == null)
                return ServiceResult.Error("Relación no encontrada.");

            relacion.PrecioCompraActual = precio;

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Precio actualizado correctamente.");
        }

        public async Task<ServiceResult> CambiarProveedorPrincipalAsync(
            int proveedorRepuestoId)
        {
            var relacion = await ObtenerRelacionProveedorCompletaAsync(proveedorRepuestoId);

            if (relacion == null)
                return ServiceResult.Error("Relación no encontrada.");

            await QuitarProveedorPrincipalAsync(relacion.RepuestoId);

            relacion.Principal = true;

            await GuardarCambiosAsync();

            return ServiceResult.Ok("Proveedor principal actualizado.");
        }

        public async Task<ServiceResult> EliminarProveedorDelRepuestoAsync(
            int proveedorRepuestoId)
        {
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
        // SALIDA DE REPUESTO POR APROBACIÓN
        // =====================================

        public async Task<(
            bool Exitoso,
            int CantidadDescontada,
            int CantidadFaltante,
            bool AlertaStockMinimo,
            string Mensaje)>
            RegistrarSalidaParaOrdenAsync(
                int repuestoId,
                int cantidadNecesaria,
                int usuarioId,
                int? mecanicoId,
                int ordenTrabajoId)
        {
            if (cantidadNecesaria <= 0)
            {
                return (
                    false,
                    0,
                    0,
                    false,
                    "La cantidad del repuesto debe ser mayor a cero.");
            }

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
                    false,
                    "Repuesto no encontrado.");
            }


            // =====================================
            // CALCULAR STOCK DISPONIBLE
            // =====================================

            var stockAnterior =
                repuesto.StockActual;

            var cantidadDescontada =
                Math.Min(
                    stockAnterior,
                    cantidadNecesaria);

            var cantidadFaltante =
                cantidadNecesaria -
                cantidadDescontada;


            // =====================================
            // DESCONTAR SOLO LO DISPONIBLE
            // =====================================

            if (cantidadDescontada > 0)
            {
                repuesto.StockActual -=
                    cantidadDescontada;

                await RegistrarMovimientoAsync(
                    repuestoId,
                    -cantidadDescontada,
                    TipoMovimientoStock.EgresoOrdenTrabajo,
                    usuarioId,
                    cantidadFaltante > 0
                        ? $"Salida parcial. Faltan {cantidadFaltante} unidad(es) para completar la cantidad necesaria."
                        : "Repuesto descontado por aprobación de presupuesto.",
                    null,
                    ordenTrabajoId);
            }


            // =====================================
            // ALERTA DE STOCK MÍNIMO
            // =====================================

            var alertaStockMinimo =
                repuesto.StockActual <=
                repuesto.StockMinimo;

            if (alertaStockMinimo)
            {
                await CrearAlertaStockMinimoAsync(
                    repuesto,
                    ordenTrabajoId,
                    mecanicoId);
            }


            // =====================================
            // ALERTA DE FALTANTE
            // =====================================

            if (cantidadFaltante > 0)
            {
                await CrearAlertaFaltanteAsync(
                    repuesto,
                    cantidadFaltante,
                    ordenTrabajoId,
                    mecanicoId);
            }


            return (
                true,
                cantidadDescontada,
                cantidadFaltante,
                alertaStockMinimo,
                cantidadFaltante > 0
                    ? $"Se descontaron {cantidadDescontada} unidad(es). Faltan {cantidadFaltante} unidad(es)."
                    : "Stock descontado correctamente.");
        }


        // =====================================
        // ALERTA DE STOCK MÍNIMO
        // =====================================

        private async Task CrearAlertaStockMinimoAsync(
            Repuesto repuesto,
            int ordenTrabajoId,
            int? mecanicoId)
        {
            var mensaje =
                $"ALERTA DE STOCK: el repuesto " +
                $"'{repuesto.Nombre}' quedó en " +
                $"{repuesto.StockActual} unidad(es), " +
                $"por debajo o igual al mínimo de " +
                $"{repuesto.StockMinimo}. " +
                $"Orden #{ordenTrabajoId}.";


            var personas =
                await ObtenerPersonalInternoAsync(
                    mecanicoId);


            foreach (var personaId in personas)
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
        // ALERTA DE FALTANTE
        // =====================================

        private async Task CrearAlertaFaltanteAsync(
            Repuesto repuesto,
            int cantidadFaltante,
            int ordenTrabajoId,
            int? mecanicoId)
        {
            var mensaje =
                $"FALTANTE DE STOCK: el repuesto " +
                $"'{repuesto.Nombre}' " +
                $"no tiene cantidad suficiente " +
                $"para la orden #{ordenTrabajoId}. " +
                $"Faltan {cantidadFaltante} unidad(es). " +
                $"Se requiere reposición para continuar " +
                $"con la reparación.";


            var personas =
                await ObtenerPersonalInternoAsync(
                    mecanicoId);


            foreach (var personaId in personas)
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
        // PERSONAL INTERNO
        // =====================================

        private async Task<List<int>>
            ObtenerPersonalInternoAsync(
                int? mecanicoId)
        {
            var personas =
                await _context.PersonaRoles
                    .Include(pr => pr.Rol)
                    .Where(pr =>
                        pr.FechaBaja == null &&
                        pr.Rol.Activo &&
                        (
                            pr.Rol.Nombre == "ADMIN" ||

                            (
                                mecanicoId.HasValue &&
                                pr.PersonaId ==
                                    mecanicoId.Value &&
                                pr.Rol.Nombre ==
                                    "MECANICO"
                            )
                        ))
                    .Select(pr =>
                        pr.PersonaId)
                    .Distinct()
                    .ToListAsync();

            return personas;
        }


        // =====================================
        // CONSULTAR DISPONIBILIDAD
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
                int cantidadNecesaria)
        {
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
        // RESERVAR / DESCONTAR REPUESTO
        // AL APROBAR UNA ORDEN
        // =====================================

        public async Task<(
            bool Exitoso,
            int CantidadDescontada,
            int CantidadFaltante,
            bool AlertaStockMinimo,
            string Mensaje)>
            ReservarParaOrdenAsync(
                int repuestoId,
                int cantidadNecesaria,
                int usuarioId,
                int ordenTrabajoId)
        {
            if (cantidadNecesaria <= 0)
            {
                return (
                    false,
                    0,
                    0,
                    false,
                    "La cantidad del repuesto debe ser mayor a cero.");
            }

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
                    false,
                    "Repuesto no encontrado.");
            }


            // =====================================
            // CALCULAR CANTIDAD DISPONIBLE
            // =====================================

            var cantidadDescontada =
                Math.Min(
                    repuesto.StockActual,
                    cantidadNecesaria);

            var cantidadFaltante =
                cantidadNecesaria -
                cantidadDescontada;


            // =====================================
            // DESCONTAR SOLO LO DISPONIBLE
            // =====================================

            if (cantidadDescontada > 0)
            {
                repuesto.StockActual -=
                    cantidadDescontada;

                await RegistrarMovimientoAsync(
                    repuestoId,
                    -cantidadDescontada,
                    TipoMovimientoStock.EgresoOrdenTrabajo,
                    usuarioId,
                    cantidadFaltante > 0
                        ? $"Reserva parcial para la orden #{ordenTrabajoId}. Faltan {cantidadFaltante} unidad(es)."
                        : $"Repuesto reservado para la orden #{ordenTrabajoId}.",
                    null,
                    ordenTrabajoId);
            }


            // =====================================
            // ALERTA STOCK MÍNIMO
            // =====================================

            var alertaStockMinimo =
                repuesto.StockActual <=
                repuesto.StockMinimo;


            // =====================================
            // AVISAR A STOCK
            // =====================================

            if (alertaStockMinimo)
            {
                await NotificarResponsableStockAsync(
                    repuesto,
                    ordenTrabajoId,
                    cantidadFaltante);
            }
            else if (cantidadFaltante > 0)
            {
                await NotificarResponsableStockAsync(
                    repuesto,
                    ordenTrabajoId,
                    cantidadFaltante);
            }


            return (
                true,
                cantidadDescontada,
                cantidadFaltante,
                alertaStockMinimo,
                cantidadFaltante > 0
                    ? $"Faltan {cantidadFaltante} unidad(es)."
                    : "Repuesto reservado correctamente.");
        }


        // =====================================
        // NOTIFICAR RESPONSABLE DE STOCK
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

            if (repuesto.PrecioVenta <= 0)
                return ServiceResult.Error("El precio de venta debe ser mayor a cero.");

            if (repuesto.StockActual < 0)
                return ServiceResult.Error("El stock no puede ser negativo.");

            if (repuesto.StockMinimo < 0)
                return ServiceResult.Error("El stock mínimo no puede ser negativo.");

            if (await ExisteSkuAsync(repuesto.SKU, excluirId))
                return ServiceResult.Error("Ya existe un repuesto con ese SKU.");

            return ServiceResult.Ok();
        }

        


        // Participa en la transacción de aprobación; no confirma cambios por su cuenta.
        public async Task<ServiceResult> ProcesarAprobacionIncrementalAsync(
            int ordenTrabajoId, int usuarioId, IEnumerable<PresupuestoVersionItem> items)
        {
            if (_context.Database.CurrentTransaction == null)
                return ServiceResult.Error("El procesamiento incremental requiere una transacción.");
            var cantidades = items.Where(i => i.RepuestoId.HasValue)
                .GroupBy(i => i.RepuestoId!.Value)
                .Select(g => new { RepuestoId = g.Key, Cantidad = g.Sum(i => (long)i.Cantidad) })
                .OrderBy(g => g.RepuestoId).ToList();
            var faltantes = new List<string>();
            foreach (var solicitado in cantidades)
            {
                if (solicitado.Cantidad <= 0 || solicitado.Cantidad > int.MaxValue)
                    return ServiceResult.Error("La cantidad total de un repuesto no es válida.");
                var repuesto = await _context.Repuestos.FromSqlInterpolated(
                    $"SELECT * FROM [Repuestos] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {solicitado.RepuestoId}")
                    .FirstOrDefaultAsync();
                if (repuesto == null) return ServiceResult.Error("Repuesto no encontrado.");
                await _context.Entry(repuesto).ReloadAsync();
                if (repuesto.StockActual < 0)
                    return ServiceResult.Error("El repuesto tiene stock negativo; debe revisarlo el encargado de Stock.");
                var movimientos = _context.MovimientosStock.Where(m =>
                    m.OrdenTrabajoId == ordenTrabajoId && m.RepuestoId == solicitado.RepuestoId &&
                    m.Tipo == TipoMovimientoStock.EgresoOrdenTrabajo);
                if (await movimientos.AnyAsync(m => m.Cantidad >= 0))
                    return ServiceResult.Error("Existe un egreso con signo inválido; debe revisarlo el encargado de Stock.");
                var procesado = -(await movimientos.SumAsync(m => (long?)m.Cantidad) ?? 0);
                var adicional = Math.Max(0L, solicitado.Cantidad - procesado);
                if (adicional == 0) continue;
                var resultado = await ReservarParaOrdenAsync(solicitado.RepuestoId,
                    (int)adicional, usuarioId, ordenTrabajoId);
                if (!resultado.Exitoso) return ServiceResult.Error(resultado.Mensaje);
                if (resultado.CantidadFaltante > 0)
                    faltantes.Add($"Repuesto #{solicitado.RepuestoId}: faltan {resultado.CantidadFaltante} unidad(es)");
            }
            return ServiceResult.Ok(faltantes.Count == 0 ? "" :
                "Se registraron sólo las salidas disponibles. Pendiente de reposición: " + string.Join("; ", faltantes) + ".");
        }

        private async Task RegistrarMovimientoAsync(
            int repuestoId,
            int cantidad,
            TipoMovimientoStock tipo,
            int usuarioId,
            string? observaciones,
            int? proveedorRepuestoId = null,
            int? ordenTrabajoId = null)
        {
            _context.MovimientosStock.Add(new MovimientoStock
            {
                RepuestoId = repuestoId,
                Cantidad = cantidad,
                Tipo = tipo,
                Fecha = DateTime.UtcNow,
                RealizadoPorUsuarioId = usuarioId,
                Observaciones = observaciones,
                ProveedorRepuestoId = proveedorRepuestoId,
                OrdenTrabajoId = ordenTrabajoId
            });

            await Task.CompletedTask;
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
