using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Patterns.Composite;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class PermisoService
    {
        private readonly MecaniCarContext _context;

        public PermisoService(
            MecaniCarContext context)
        {
            _context = context;
        }

        // =====================================
        // VERIFICAR PERMISO
        // =====================================

        public async Task<bool> TieneAlgunoAsync(int usuarioId, params string[] patentes)
        {
            foreach (var patente in patentes)
                if (await TienePermisoAsync(usuarioId, patente)) return true;
            return false;
        }

        public Task<int?> ObtenerPersonaActivaIdAsync(int usuarioId) =>
            _context.Usuarios.AsNoTracking()
                .Where(u => u.Id == usuarioId && u.Activo && u.Persona.Activo)
                .Select(u => (int?)u.PersonaId).FirstOrDefaultAsync();

        public async Task<bool> EsAdministradorAsync(int usuarioId)
        {
            var usuario =
                await ObtenerUsuarioConPermisosAsync(
                    usuarioId);

            return usuario != null && EsAdministrador(usuario);
        }

        internal async Task BloquearAdministradoresAsync()
        {
            if (_context.Database.CurrentTransaction == null)
                throw new InvalidOperationException("La proteccion ADMIN requiere una transaccion activa.");

            var rol = await _context.Roles.FromSqlInterpolated(
                $"SELECT * FROM [Roles] WITH (UPDLOCK, HOLDLOCK) WHERE [Nombre] = {RolesSistema.ADMIN}")
                .AsNoTracking().SingleOrDefaultAsync();
            if (rol == null)
                throw new InvalidOperationException("No existe el rol ADMIN requerido para proteger administradores.");

            // Refrescar el grafo de seguridad consultado antes de esperar el mutex.
            // No descartar escrituras pendientes del caller ni limpiar el ChangeTracker.
            var entradas = _context.ChangeTracker.Entries().Where(e =>
                e.Entity is Usuario or Persona or PersonaRol or Rol or
                    Familia or RolFamilia or FamiliaPatente or Patente).ToList();
            if (entradas.Any(e => e.State != EntityState.Unchanged))
                throw new InvalidOperationException("La proteccion ADMIN requiere entidades de seguridad sin cambios pendientes.");
            foreach (var entrada in entradas)
                await entrada.ReloadAsync();
        }

        public async Task<bool> EsAdministradorEfectivoPersonaAsync(
            int personaId)
        {
            return await _context.PersonaRoles
                .AnyAsync(pr =>
                    pr.PersonaId == personaId &&
                    pr.FechaBaja == null &&
                    pr.Rol.Activo &&
                    pr.Rol.Nombre == RolesSistema.ADMIN &&
                    pr.Persona.Activo &&
                    pr.Persona.Usuario != null &&
                    pr.Persona.Usuario.Activo);
        }

        public Task<int> ContarAdministradoresEfectivosAsync()
        {
            return _context.PersonaRoles
                .CountAsync(pr =>
                    pr.FechaBaja == null &&
                    pr.Rol.Activo &&
                    pr.Rol.Nombre == RolesSistema.ADMIN &&
                    pr.Persona.Activo &&
                    pr.Persona.Usuario != null &&
                    pr.Persona.Usuario.Activo);
        }

        public async Task<bool>
            TienePermisoAsync(
                int usuarioId,
                string patente)
        {
            var usuario =
                await ObtenerUsuarioConPermisosAsync(
                    usuarioId);

            if (usuario == null || await ClientePendienteAsync(usuario))
                return false;


            // =====================================
            // ADMINISTRADOR
            // =====================================

            if (EsAdministrador(usuario))
                return true;


            var componentes = await ConstruirComponentesAsync(usuario);
            return RecorridoPermisos.ObtenerPatentes(componentes)
                .Contains(patente, StringComparer.OrdinalIgnoreCase);
        }


        // =====================================
        // OBTENER PERMISOS
        // =====================================

        public async Task<List<string>>
            ObtenerPatentesAsync(
                int usuarioId)
        {
            var usuario =
                await ObtenerUsuarioConPermisosAsync(
                    usuarioId);

            if (usuario == null || await ClientePendienteAsync(usuario))
                return new List<string>();


            // =====================================
            // ADMINISTRADOR
            // =====================================

            if (EsAdministrador(usuario))
            {
                return await _context.Patentes
                    .Where(p => p.Activo)
                    .Select(p => p.Nombre)
                    .ToListAsync();
            }


            var componentes = await ConstruirComponentesAsync(usuario);
            return RecorridoPermisos.ObtenerPatentes(componentes).ToList();
        }

        private async Task<bool> ClientePendienteAsync(Usuario usuario) =>
            usuario.Persona.Roles.Any(r => r.FechaBaja == null && r.Rol.Nombre == RolesSistema.CLIENTE)
            && !usuario.Persona.Roles.Any(r => r.FechaBaja == null && r.Rol.Nombre != RolesSistema.CLIENTE)
            && !await new ClienteHabilitadoService(_context).EstaHabilitadoAsync(usuario.PersonaId);

        public async Task<PermisosVisuales> ResolverParaVistaAsync(int usuarioId)
        {
            var usuario = await ObtenerUsuarioConPermisosAsync(usuarioId);
            if (usuario == null || await ClientePendienteAsync(usuario))
                return new(false, Array.Empty<string>());
            if (EsAdministrador(usuario)) return new(true, Array.Empty<string>());
            return new(false, RecorridoPermisos.ObtenerPatentes(
                await ConstruirComponentesAsync(usuario)).ToArray());
        }

        private async Task<IReadOnlyList<IComponentePermiso>> ConstruirComponentesAsync(Usuario usuario)
        {
            var raices = usuario.Persona.Roles
                .Where(pr => pr.FechaBaja == null && pr.Rol.Activo)
                .SelectMany(pr => pr.Rol.Familias)
                .Select(rf => rf.FamiliaId).Distinct().ToList();
            var pendientes = new Queue<int>(raices);
            var cargadas = new HashSet<int>();
            var familias = new Dictionary<int, FamiliaPermiso>();
            var patentes = new Dictionary<int, PatentePermiso>();
            var enlaces = new List<(int Padre, int Hija)>();

            while (pendientes.TryDequeue(out var id))
            {
                if (!cargadas.Add(id)) continue;

                // Mantener la consulta/tracking existente; no consultar padres ni
                // expandir ramas inactivas. No se modifica ninguna entidad EF.
                var familia = await _context.Familias
                    .Include(f => f.Patentes).ThenInclude(fp => fp.Patente)
                    .Include(f => f.FamiliasHijas)
                    .FirstOrDefaultAsync(f => f.Id == id && f.Activo);
                if (familia == null) continue;

                var componente = new FamiliaPermiso(familia);
                familias.Add(id, componente);
                foreach (var relacion in familia.Patentes)
                {
                    if (!patentes.TryGetValue(relacion.PatenteId, out var hoja))
                    {
                        hoja = new PatentePermiso(relacion.Patente);
                        patentes.Add(relacion.PatenteId, hoja);
                    }
                    componente.Agregar(hoja);
                }

                foreach (var hija in familia.FamiliasHijas)
                {
                    if (!hija.Activo) continue;
                    enlaces.Add((id, hija.Id));
                    pendientes.Enqueue(hija.Id);
                }
            }

            // Conectar los nodos ya cargados permite reutilizarlos incluso en ciclos.
            // El recorrido Composite corta revisitas sin descartar otras ramas.
            foreach (var (padre, hija) in enlaces)
                if (familias.TryGetValue(hija, out var componente))
                    familias[padre].Agregar(componente);

            return raices.Where(familias.ContainsKey)
                .Select(id => (IComponentePermiso)familias[id]).ToList();
        }


        // =====================================
        // USUARIO + ROLES + FAMILIAS
        // =====================================

        private async Task<Usuario?>
            ObtenerUsuarioConPermisosAsync(
                int usuarioId)
        {
            return await _context.Usuarios
                .Include(u => u.Persona)
                    .ThenInclude(p => p.Roles)
                        .ThenInclude(pr => pr.Rol)
                            .ThenInclude(r => r.Familias)
                                .ThenInclude(rf => rf.Familia)
                .FirstOrDefaultAsync(
                    u => u.Id == usuarioId &&
                         u.Activo &&
                         u.Persona.Activo);
        }

        private static bool EsAdministrador(Usuario usuario)
        {
            return usuario.Persona.Roles.Any(pr =>
                pr.FechaBaja == null &&
                pr.Rol.Activo &&
                pr.Rol.Nombre.Equals(
                    RolesSistema.ADMIN,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
