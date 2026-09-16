using System.Data;
using Microsoft.Data.SqlClient;
using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class ProveedorService
    {
        private readonly MecaniCarContext _context;
        private readonly PermisoService _permisos;
        private readonly AuditoriaService _auditoria;

        public ProveedorService(MecaniCarContext context, PermisoService permisos, AuditoriaService auditoria)
        {
            _context = context;
            _permisos = permisos;
            _auditoria = auditoria;
        }

        //====================================
        // CONSULTAS
        //====================================

        public async Task<ServiceResult<List<Proveedor>>> ObtenerTodosAsync(int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "PROVEEDOR_VER")) return ServiceResult<List<Proveedor>>.Error("Acceso denegado.");

            var proveedores = await _context.Proveedores
                .Include(p => p.Repuestos)
                    .ThenInclude(pr => pr.Repuesto)
                .OrderBy(p => p.Nombre)
                .ThenBy(p => p.Apellido)
                .ToListAsync();

            return ServiceResult<List<Proveedor>>.Ok(proveedores);
        }

        public async Task<ServiceResult<Proveedor>> ObtenerPorIdAsync(int id, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "PROVEEDOR_VER")) return ServiceResult<Proveedor>.Error("Acceso denegado.");

            var proveedor = await ObtenerProveedorCompletoAsync(id);

            if (proveedor == null)
                return ServiceResult<Proveedor>.Error("Proveedor no encontrado.");

            return ServiceResult<Proveedor>.Ok(proveedor);
        }

        //====================================
        // MÉTODOS PRIVADOS
        //====================================

        private async Task<bool> ExisteProveedorAsync(string nombre, int? excluirId = null)
        {
            nombre = nombre.Trim().ToUpper();

            return await _context.Proveedores.AnyAsync(p =>
                p.Nombre.ToUpper() == nombre &&
                (!excluirId.HasValue || p.Id != excluirId.Value));
        }

        private async Task<Proveedor?> ObtenerProveedorCompletoAsync(int id)
        {
            return await _context.Proveedores
                .Include(p => p.Repuestos)
                    .ThenInclude(pr => pr.Repuesto)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        //====================================
        // ABM
        //====================================

        private async Task<ServiceResult> EjecutarAsync(Func<Task<ServiceResult>> accion)
        {
            if (_context.Database.CurrentTransaction != null)
                return ServiceResult.Error("La operación de proveedor requiere transacción propia.");
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var result = await accion();
                if (!result.Exitoso)
                {
                    await tx.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    return result;
                }
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                if (Conflicto(ex)) return ServiceResult.Error("Conflicto de datos o concurrencia. Actualice antes de reintentar.");
                throw;
            }
        }

        private static bool Conflicto(Exception ex) =>
            ex is SqlException sql && sql.Number is 1205 or 1222 or 2601 or 2627 or 547 or 8152 or 2628 ||
            ex.InnerException != null && Conflicto(ex.InnerException);

        private async Task<Proveedor?> BloquearAsync(int id)
        {
            var proveedor = await _context.Proveedores.FromSqlInterpolated(
                $"SELECT * FROM [Proveedores] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").FirstOrDefaultAsync();
            if (proveedor != null) await _context.Entry(proveedor).ReloadAsync();
            return proveedor;
        }

        private static bool DatosValidos(Proveedor? proveedor) => proveedor != null && !string.IsNullOrWhiteSpace(proveedor.Nombre);

        private static void CopiarDatos(Proveedor origen, Proveedor destino)
        {
            destino.Nombre = origen.Nombre.Trim();
            destino.Apellido = origen.Apellido?.Trim() ?? "";
            destino.Telefono = origen.Telefono?.Trim() ?? "";
            destino.Email = origen.Email?.Trim() ?? "";
        }

        public async Task<ServiceResult> CrearAsync(Proveedor proveedor, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "PROVEEDOR_CREAR")) return ServiceResult.Error("Acceso denegado.");
            if (!DatosValidos(proveedor)) return ServiceResult.Error("Debe ingresar el nombre.");
            return await EjecutarAsync(async () =>
            {
                if (await ExisteProveedorAsync(proveedor.Nombre)) return ServiceResult.Error("Ya existe un proveedor con ese nombre.");
                var nuevo = new Proveedor { Activo = true, FechaCreacion = DateTime.UtcNow };
                CopiarDatos(proveedor, nuevo);
                _context.Proveedores.Add(nuevo);
                await _context.SaveChangesAsync();
                _auditoria.RegistrarOperacion("PROVEEDOR_CREADO", "Proveedor", nuevo.Id, usuarioSolicitanteId);
                return ServiceResult.Ok("Proveedor creado correctamente.");
            });
        }

        public async Task<ServiceResult> EditarAsync(Proveedor proveedor, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "PROVEEDOR_MODIFICAR")) return ServiceResult.Error("Acceso denegado.");
            if (!DatosValidos(proveedor) || proveedor.Id <= 0) return ServiceResult.Error("Ingrese un proveedor válido y su nombre.");
            return await EjecutarAsync(async () =>
            {
                var existente = await BloquearAsync(proveedor.Id);
                if (existente == null) return ServiceResult.Error("Proveedor no encontrado.");
                if (await ExisteProveedorAsync(proveedor.Nombre, proveedor.Id)) return ServiceResult.Error("Ya existe un proveedor con ese nombre.");
                CopiarDatos(proveedor, existente);
                _auditoria.RegistrarOperacion("PROVEEDOR_MODIFICADO", "Proveedor", existente.Id, usuarioSolicitanteId);
                return ServiceResult.Ok("Proveedor actualizado correctamente.");
            });
        }

        public async Task<ServiceResult> CambiarEstadoAsync(int id, int usuarioSolicitanteId)
        {
            if (!await _permisos.TienePermisoAsync(usuarioSolicitanteId, "PROVEEDOR_DESACTIVAR")) return ServiceResult.Error("Acceso denegado.");
            return await EjecutarAsync(async () =>
            {
                // No toma locks de Repuesto después de Proveedor: evita invertir el orden de Stock.
                var proveedor = await BloquearAsync(id);
                if (proveedor == null) return ServiceResult.Error("Proveedor no encontrado.");
                proveedor.Activo = !proveedor.Activo;
                _auditoria.RegistrarOperacion(proveedor.Activo ? "PROVEEDOR_ACTIVADO" : "PROVEEDOR_DESACTIVADO",
                    "Proveedor", proveedor.Id, usuarioSolicitanteId);
                return ServiceResult.Ok(proveedor.Activo ? "Proveedor activado correctamente." : "Proveedor desactivado correctamente.");
            });
        }
    }
}
