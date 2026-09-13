using System.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MecaniCar360.Services
{
    public partial class StockService
    {
        private sealed record Inventario(Repuesto Repuesto, List<LoteRepuesto> Lotes);
        private sealed record Consumo(int Cantidad, int Faltante, List<MovimientoStockLote> Detalles);

        private async Task<bool> UsuarioAutorizadoAsync(int usuarioId, string patente) => usuarioId > 0 &&
            await _permisos.TienePermisoAsync(usuarioId, patente) &&
            await _context.Usuarios.AsNoTracking().AnyAsync(u => u.Id == usuarioId && u.Activo && u.Persona.Activo);

        private static bool PrecioValido(decimal precio) => precio > 0 && precio <= 9999999999999999.99m &&
            decimal.Round(precio, 2) == precio;

        // Operaciones autónomas: este helper debe ser dueño de la transacción y del rollback.
        // Presupuesto usa ProcesarAprobacionIncrementalAsync y administra su propio contexto.
        private async Task<ServiceResult> EjecutarStockAsync(Func<Task<ServiceResult>> operacion)
        {
            if (_context.Database.CurrentTransaction != null)
                return ServiceResult.Error("Esta operación de Stock requiere una transacción propia.");
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var resultado = await operacion();
                if (!resultado.Exitoso)
                {
                    await tx.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    return resultado;
                }
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return resultado;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                if (ConflictoStock(ex))
                    return ServiceResult.Error("No se pudo registrar el cambio por un conflicto de datos o concurrencia. Actualice y revise antes de reintentar.");
                throw;
            }
        }

        private static bool ConflictoStock(Exception ex) =>
            ex is SqlException sql && sql.Number is 1205 or 1222 or 2601 or 2627 or 547 or 8115 or 8152 or 2628 ||
            ex.InnerException != null && ConflictoStock(ex.InnerException);

        private async Task<OrdenTrabajo?> BloquearOrdenAsync(int id)
        {
            var orden = await _context.OrdenesTrabajo.FromSqlInterpolated(
                $"SELECT * FROM [OrdenesTrabajo] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").FirstOrDefaultAsync();
            if (orden != null) await _context.Entry(orden).ReloadAsync();
            return orden;
        }

        private async Task<ServiceResult<Inventario>> BloquearInventarioAsync(int id, bool exigirActivo = true)
        {
            if (id <= 0) return ServiceResult<Inventario>.Error("Identificador de repuesto inválido.");
            var repuesto = await _context.Repuestos.FromSqlInterpolated(
                $"SELECT * FROM [Repuestos] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").FirstOrDefaultAsync();
            if (repuesto == null) return ServiceResult<Inventario>.Error("Repuesto no encontrado.");
            await _context.Entry(repuesto).ReloadAsync();
            if (exigirActivo && !repuesto.Activo) return ServiceResult<Inventario>.Error("El repuesto está inactivo.");
            var lotes = await _context.LotesRepuesto.FromSqlInterpolated(
                $"SELECT * FROM [LotesRepuesto] WITH (UPDLOCK, HOLDLOCK) WHERE [RepuestoId] = {id}")
                .OrderBy(l => l.FechaIngreso).ThenBy(l => l.Id).ToListAsync();
            foreach (var lote in lotes) await _context.Entry(lote).ReloadAsync();
            var inventario = new Inventario(repuesto, lotes);
            if (!Coherente(inventario))
                return ServiceResult<Inventario>.Error("StockActual no coincide con las existencias válidas de los lotes. Se requiere conciliación; no se modificó el saldo.");
            return ServiceResult<Inventario>.Ok(inventario);
        }

        private static bool Coherente(Inventario inventario) => inventario.Repuesto.StockActual >= 0 &&
            inventario.Lotes.All(l => l.RepuestoId == inventario.Repuesto.Id && l.CantidadIngresada > 0 &&
                l.CantidadDisponible >= 0 && l.CantidadDisponible <= l.CantidadIngresada) &&
            inventario.Lotes.Sum(l => (long)l.CantidadDisponible) == inventario.Repuesto.StockActual;

        // Único núcleo de consumo: sólo altera el inventario bloqueado y devuelve el desglose.
        // El caller es responsable de persistencia, movimiento, auditoría y transacción.
        private static ServiceResult<Consumo> ConsumirFifo(Inventario inventario, int cantidad, bool permitirParcial)
        {
            if (cantidad <= 0) return ServiceResult<Consumo>.Error("La cantidad debe ser positiva.");
            if (!Coherente(inventario)) return ServiceResult<Consumo>.Error("El saldo y los lotes requieren conciliación.");
            if (!permitirParcial && inventario.Repuesto.StockActual < cantidad)
                return ServiceResult<Consumo>.Error("No hay stock suficiente; no se realizó ningún consumo.");
            var consumir = Math.Min(cantidad, inventario.Repuesto.StockActual);
            var pendiente = consumir;
            var detalles = new List<MovimientoStockLote>();
            foreach (var lote in inventario.Lotes.Where(l => l.CantidadDisponible > 0)
                .OrderBy(l => l.FechaIngreso).ThenBy(l => l.Id))
            {
                if (pendiente == 0) break;
                var unidades = Math.Min(pendiente, lote.CantidadDisponible);
                lote.CantidadDisponible -= unidades;
                pendiente -= unidades;
                detalles.Add(new MovimientoStockLote { LoteRepuestoId = lote.Id, Cantidad = unidades });
            }
            inventario.Repuesto.StockActual -= consumir;
            return ServiceResult<Consumo>.Ok(new Consumo(consumir, cantidad - consumir, detalles));
        }

        private async Task GuardarMovimientoAsync(int repuestoId, int cantidad, TipoMovimientoStock tipo,
            int usuarioId, string? observaciones, List<MovimientoStockLote> detalles,
            int? proveedorRepuestoId = null, int? ordenTrabajoId = null)
        {
            if (_context.Database.CurrentTransaction == null || cantidad == 0 ||
                detalles.Any(d => d.Cantidad <= 0) || detalles.Sum(d => (long)d.Cantidad) != Math.Abs((long)cantidad))
                throw new InvalidOperationException("Movimiento sin transacción o desglose consistente.");
            var movimiento = new MovimientoStock
            {
                RepuestoId = repuestoId, Cantidad = cantidad, Tipo = tipo, Fecha = DateTime.UtcNow,
                RealizadoPorUsuarioId = usuarioId, Observaciones = observaciones?.Trim(),
                ProveedorRepuestoId = proveedorRepuestoId, OrdenTrabajoId = ordenTrabajoId, Lotes = detalles
            };
            _context.MovimientosStock.Add(movimiento);
            await _context.SaveChangesAsync();
            _auditoria.RegistrarOperacion("MOVIMIENTO_STOCK", "MovimientoStock", movimiento.Id, usuarioId,
                $"Tipo: {tipo}; repuesto: {repuestoId}; cantidad: {cantidad}; OT: {ordenTrabajoId}; lotes: {detalles.Count}.");
            await _context.SaveChangesAsync();
        }

        public async Task<ServiceResult> CrearRepuestoAsync(Repuesto repuesto, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_CREAR")) return ServiceResult.Error("Acceso denegado.");
            if (repuesto == null || repuesto.StockActual != 0) return ServiceResult.Error("El repuesto debe comenzar con stock cero. Registre un ingreso para incorporar existencias.");
            return await EjecutarStockAsync(async () =>
            {
                var validacion = await ValidarRepuestoAsync(repuesto);
                if (!validacion.Exitoso) return validacion;
                var nuevo = new Repuesto
                {
                    SKU = repuesto.SKU.Trim().ToUpperInvariant(), Nombre = repuesto.Nombre.Trim(), Marca = repuesto.Marca,
                    Modelo = repuesto.Modelo, Compatibilidad = repuesto.Compatibilidad, PrecioVenta = repuesto.PrecioVenta,
                    StockMinimo = repuesto.StockMinimo, StockActual = 0, Activo = true, FechaCreacion = DateTime.UtcNow
                };
                _context.Repuestos.Add(nuevo);
                await _context.SaveChangesAsync();
                _auditoria.RegistrarOperacion("REPUESTO_CREADO", "Repuesto", nuevo.Id, usuarioSolicitanteId);
                return ServiceResult.Ok("Repuesto creado correctamente.");
            });
        }

        public async Task<ServiceResult> EditarRepuestoAsync(Repuesto repuesto, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");
            if (repuesto == null) return ServiceResult.Error("Datos de repuesto inválidos.");
            return await EjecutarStockAsync(async () =>
            {
                var estado = await BloquearInventarioAsync(repuesto.Id);
                if (!estado.Exitoso) return ServiceResult.Error(estado.Mensaje);
                var validacion = await ValidarRepuestoAsync(repuesto, repuesto.Id);
                if (!validacion.Exitoso) return validacion;
                var existente = estado.Data!.Repuesto;
                existente.SKU = repuesto.SKU.Trim().ToUpperInvariant(); existente.Nombre = repuesto.Nombre.Trim();
                existente.Marca = repuesto.Marca; existente.Modelo = repuesto.Modelo;
                existente.Compatibilidad = repuesto.Compatibilidad; existente.PrecioVenta = repuesto.PrecioVenta;
                existente.StockMinimo = repuesto.StockMinimo;
                _auditoria.RegistrarOperacion("REPUESTO_MODIFICADO", "Repuesto", existente.Id, usuarioSolicitanteId);
                return ServiceResult.Ok("Repuesto actualizado correctamente.");
            });
        }

        public async Task<ServiceResult> CambiarEstadoAsync(int id, int usuarioSolicitanteId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioSolicitanteId, "STOCK_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");
            return await EjecutarStockAsync(async () =>
            {
                var estado = await BloquearInventarioAsync(id, false);
                if (!estado.Exitoso) return ServiceResult.Error(estado.Mensaje);
                var repuesto = estado.Data!.Repuesto;
                repuesto.Activo = !repuesto.Activo;
                _auditoria.RegistrarOperacion(repuesto.Activo ? "REPUESTO_ACTIVADO" : "REPUESTO_DESACTIVADO", "Repuesto", id, usuarioSolicitanteId);
                return ServiceResult.Ok(repuesto.Activo ? "Repuesto activado." : "Repuesto desactivado.");
            });
        }

        public async Task<ServiceResult> RegistrarIngresoAsync(int repuestoId, int proveedorRepuestoId,
            int cantidad, decimal precioCompra, int usuarioId, string? observaciones)
        {
            if (!await UsuarioAutorizadoAsync(usuarioId, "STOCK_MOVIMIENTO")) return ServiceResult.Error("Acceso denegado.");
            if (repuestoId <= 0 || proveedorRepuestoId <= 0 || cantidad <= 0 || !PrecioValido(precioCompra) || observaciones?.Length > 500)
                return ServiceResult.Error("Ingrese IDs positivos, cantidad positiva, precio válido con hasta dos decimales y observaciones de hasta 500 caracteres.");
            return await EjecutarStockAsync(async () =>
            {
                var estado = await BloquearInventarioAsync(repuestoId);
                if (!estado.Exitoso) return ServiceResult.Error(estado.Mensaje);
                var inventario = estado.Data!;
                if (!await _context.ProveedorRepuestos.AsNoTracking().AnyAsync(pr => pr.Id == proveedorRepuestoId &&
                    pr.RepuestoId == repuestoId && pr.Proveedor.Activo))
                    return ServiceResult.Error("La asociación no corresponde al repuesto o el proveedor está inactivo.");
                if ((long)inventario.Repuesto.StockActual + cantidad > int.MaxValue)
                    return ServiceResult.Error("El ingreso excede el saldo máximo permitido.");
                var ahora = DateTime.UtcNow;
                var lote = new LoteRepuesto
                {
                    CodigoLote = $"LOT-{ahora:yyyyMMdd}-{Guid.NewGuid():N}", RepuestoId = repuestoId,
                    ProveedorRepuestoId = proveedorRepuestoId, CantidadIngresada = cantidad,
                    CantidadDisponible = cantidad, PrecioCompra = precioCompra, FechaIngreso = ahora
                };
                _context.LotesRepuesto.Add(lote);
                inventario.Repuesto.StockActual += cantidad;
                await GuardarMovimientoAsync(repuestoId, cantidad, TipoMovimientoStock.IngresoCompra, usuarioId,
                    observaciones, new() { new() { LoteRepuesto = lote, Cantidad = cantidad } }, proveedorRepuestoId);
                return ServiceResult.Ok($"Ingreso registrado. Lote {lote.CodigoLote}.");
            });
        }

        public async Task<ServiceResult> RegistrarSalidaAsync(int repuestoId, int cantidad,
            int usuarioId, string? observaciones, int ordenTrabajoId)
        {
            if (!await UsuarioAutorizadoAsync(usuarioId, "STOCK_MOVIMIENTO")) return ServiceResult.Error("Acceso denegado.");
            if (repuestoId <= 0 || ordenTrabajoId <= 0 || cantidad <= 0 || observaciones?.Length > 500)
                return ServiceResult.Error("La salida requiere OT, repuesto y cantidad positivos y observaciones de hasta 500 caracteres.");
            return await EjecutarStockAsync(async () =>
            {
                var orden = await BloquearOrdenAsync(ordenTrabajoId);
                if (orden == null || orden.EstadoActual is not (EstadoOrden.Aprobado or EstadoOrden.EnReparacion) ||
                    orden.FechaFin.HasValue || await _context.Facturas.AnyAsync(f => f.OrdenTrabajoId == ordenTrabajoId))
                    return ServiceResult.Error("Sólo se admiten salidas para una OT aprobada o en reparación, sin cierre ni factura.");
                var estado = await BloquearInventarioAsync(repuestoId);
                if (!estado.Exitoso) return ServiceResult.Error(estado.Mensaje);
                var consumo = ConsumirFifo(estado.Data!, cantidad, false);
                if (!consumo.Exitoso) return ServiceResult.Error(consumo.Mensaje);
                await GuardarMovimientoAsync(repuestoId, -consumo.Data!.Cantidad, TipoMovimientoStock.EgresoOrdenTrabajo,
                    usuarioId, observaciones, consumo.Data.Detalles, ordenTrabajoId: ordenTrabajoId);
                if (estado.Data!.Repuesto.StockActual <= estado.Data.Repuesto.StockMinimo)
                    await NotificarResponsableStockAsync(estado.Data.Repuesto, ordenTrabajoId, 0);
                return ServiceResult.Ok("Salida registrada mediante FIFO.");
            });
        }

        public async Task<ServiceResult> RegistrarAjusteAsync(int repuestoId, int diferencia,
            int usuarioId, string observaciones, int? loteRepuestoId = null)
        {
            if (!await UsuarioAutorizadoAsync(usuarioId, "STOCK_MOVIMIENTO")) return ServiceResult.Error("Acceso denegado.");
            if (repuestoId <= 0 || diferencia == 0 || diferencia == int.MinValue ||
                string.IsNullOrWhiteSpace(observaciones) || observaciones.Length > 500)
                return ServiceResult.Error("Ingrese una diferencia válida y un motivo de hasta 500 caracteres.");
            if (diferencia > 0 && (!loteRepuestoId.HasValue || loteRepuestoId <= 0))
                return ServiceResult.Error("La restitución requiere identificar el lote físico de origen.");
            return await EjecutarStockAsync(async () =>
            {
                var estado = await BloquearInventarioAsync(repuestoId);
                if (!estado.Exitoso) return ServiceResult.Error(estado.Mensaje);
                var inventario = estado.Data!;
                List<MovimientoStockLote> detalles;
                if (diferencia < 0)
                {
                    var consumo = ConsumirFifo(inventario, -diferencia, false);
                    if (!consumo.Exitoso) return ServiceResult.Error(consumo.Mensaje);
                    detalles = consumo.Data!.Detalles;
                }
                else
                {
                    // El operador autorizado debe identificar físicamente el lote restituido.
                    // No se exige proveedor activo: no se trata de una compra nueva.
                    var lote = inventario.Lotes.SingleOrDefault(l => l.Id == loteRepuestoId);
                    if (lote == null || (long)lote.CantidadDisponible + diferencia > lote.CantidadIngresada ||
                        (long)inventario.Repuesto.StockActual + diferencia > int.MaxValue)
                        return ServiceResult.Error("El lote no pertenece al repuesto o la restitución excede sus límites.");
                    lote.CantidadDisponible += diferencia;
                    inventario.Repuesto.StockActual += diferencia;
                    detalles = new() { new() { LoteRepuestoId = lote.Id, Cantidad = diferencia } };
                }
                await GuardarMovimientoAsync(repuestoId, diferencia, TipoMovimientoStock.Ajuste, usuarioId, observaciones, detalles);
                return ServiceResult.Ok("Ajuste registrado con trazabilidad por lotes.");
            });
        }

        // Sólo participa en la transacción Serializable de PresupuestoService; nunca hace commit.
        // Ante error o excepción, el caller debe abortar toda la operación, hacer rollback completo
        // y descartar el estado tracked. Stock no limpia ni desengancha entidades del caller.
        internal async Task<ServiceResult> ProcesarAprobacionIncrementalAsync(int ordenTrabajoId, int usuarioId, int presupuestoVersionId)
        {
            if (_context.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                return ServiceResult.Error("El procesamiento incremental requiere una transacción Serializable.");
            if (!await UsuarioAutorizadoAsync(usuarioId, "CLIENTE_PRESUPUESTO_APROBAR")) return ServiceResult.Error("Acceso denegado.");
            var orden = await BloquearOrdenAsync(ordenTrabajoId);
            if (orden == null) return ServiceResult.Error("Orden no encontrada.");
            var personaId = await _permisos.ObtenerPersonaActivaIdAsync(usuarioId);
            var version = await _context.PresupuestoVersiones.AsNoTracking().Include(v => v.Items)
                .FirstOrDefaultAsync(v => v.Id == presupuestoVersionId && v.Presupuesto.OrdenTrabajoId == ordenTrabajoId &&
                    v.Decision == EstadoPresupuestoVersion.Pendiente && v.Presupuesto.Estado == EstadoPresupuesto.Pendiente &&
                    v.Presupuesto.OrdenTrabajo.EstadoActual == EstadoOrden.EsperandoAprobacion &&
                    !v.Presupuesto.OrdenTrabajo.FechaFin.HasValue && v.Presupuesto.OrdenTrabajo.Factura == null);
            if (version == null || !personaId.HasValue ||
                (!await _permisos.EsAdministradorAsync(usuarioId) && !await _context.OrdenesTrabajo.AnyAsync(o =>
                    o.Id == ordenTrabajoId && o.IngresoVehiculo.Turno.ClienteId == personaId.Value)) ||
                await _context.PresupuestoVersiones.AnyAsync(v => v.PresupuestoId == version.PresupuestoId && v.NumeroVersion > version.NumeroVersion))
                return ServiceResult.Error("No tiene acceso a esta versión vigente.");
            var cantidades = version.Items.Where(i => i.RepuestoId.HasValue).GroupBy(i => i.RepuestoId!.Value)
                .Select(g => new { RepuestoId = g.Key, Cantidad = g.Sum(i => (long)i.Cantidad) }).OrderBy(g => g.RepuestoId).ToList();
            var faltantes = new List<string>();
            foreach (var solicitado in cantidades)
            {
                if (solicitado.Cantidad <= 0 || solicitado.Cantidad > int.MaxValue)
                    return ServiceResult.Error("La cantidad total de un repuesto no es válida.");
                // Comprobar activo y coherencia incluso cuando no haya cantidad adicional.
                var estado = await BloquearInventarioAsync(solicitado.RepuestoId);
                if (!estado.Exitoso) return ServiceResult.Error(estado.Mensaje);
                var movimientos = _context.MovimientosStock.Where(m => m.OrdenTrabajoId == ordenTrabajoId &&
                    m.RepuestoId == solicitado.RepuestoId && m.Tipo == TipoMovimientoStock.EgresoOrdenTrabajo);
                if (await movimientos.AnyAsync(m => m.Cantidad >= 0)) return ServiceResult.Error("Existe un egreso con signo inválido.");
                var procesado = -(await movimientos.SumAsync(m => (long?)m.Cantidad) ?? 0);
                var adicional = Math.Max(0L, solicitado.Cantidad - procesado);
                if (adicional == 0) continue;
                var consumo = ConsumirFifo(estado.Data!, (int)adicional, true);
                if (!consumo.Exitoso) return ServiceResult.Error(consumo.Mensaje);
                var resultado = consumo.Data!;
                if (resultado.Cantidad > 0)
                    await GuardarMovimientoAsync(solicitado.RepuestoId, -resultado.Cantidad, TipoMovimientoStock.EgresoOrdenTrabajo,
                        usuarioId, $"Aprobación de versión #{presupuestoVersionId}. Faltan {resultado.Faltante} unidad(es).",
                        resultado.Detalles, ordenTrabajoId: ordenTrabajoId);
                if (resultado.Faltante > 0) faltantes.Add($"Repuesto #{solicitado.RepuestoId}: faltan {resultado.Faltante} unidad(es)");
                if (resultado.Faltante > 0 || estado.Data!.Repuesto.StockActual <= estado.Data.Repuesto.StockMinimo)
                    await NotificarResponsableStockAsync(estado.Data!.Repuesto, ordenTrabajoId, resultado.Faltante);
            }
            return ServiceResult.Ok(faltantes.Count == 0 ? "" : "Se registraron sólo las salidas disponibles. Pendiente de reposición: " + string.Join("; ", faltantes) + ".");
        }
    }
}
