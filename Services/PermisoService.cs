using MecaniCar360.Data;
using MecaniCar360.Models;
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

        public async Task<bool> EsAdministradorAsync(int usuarioId)
        {
            var usuario =
                await ObtenerUsuarioConPermisosAsync(
                    usuarioId);

            return usuario != null && EsAdministrador(usuario);
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

            if (usuario == null)
                return false;


            // =====================================
            // ADMINISTRADOR
            // =====================================

            if (EsAdministrador(usuario))
                return true;


            // =====================================
            // RECORRER ROLES
            // =====================================

            foreach (var personaRol
                in usuario.Persona.Roles)
            {
                if (personaRol.FechaBaja != null)
                    continue;

                if (!personaRol.Rol.Activo)
                    continue;


                foreach (var rolFamilia
                    in personaRol.Rol.Familias)
                {
                    var tienePermiso =
                        await FamiliaTienePermisoAsync(
                            rolFamilia.FamiliaId,
                            patente);

                    if (tienePermiso)
                        return true;
                }
            }

            return false;
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

            if (usuario == null)
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


            // =====================================
            // RESTO DE USUARIOS
            // =====================================

            var patentes =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);


            foreach (var personaRol
                in usuario.Persona.Roles)
            {
                if (personaRol.FechaBaja != null)
                    continue;

                if (!personaRol.Rol.Activo)
                    continue;


                foreach (var rolFamilia
                    in personaRol.Rol.Familias)
                {
                    await ObtenerPatentesFamiliaAsync(
                        rolFamilia.FamiliaId,
                        patentes);
                }
            }


            return patentes.ToList();
        }


        // =====================================
        // RECORRER FAMILIA
        // =====================================

        private async Task<bool>
            FamiliaTienePermisoAsync(
                int familiaId,
                string patenteBuscada)
        {
            var familia =
                await _context.Familias
                    .Include(f => f.Patentes)
                        .ThenInclude(fp => fp.Patente)
                    .Include(f => f.FamiliasHijas)
                    .FirstOrDefaultAsync(
                        f => f.Id == familiaId &&
                             f.Activo);

            if (familia == null)
                return false;


            // =====================================
            // PATENTES DE ESTA FAMILIA
            // =====================================

            foreach (var familiaPatente
                in familia.Patentes)
            {
                if (!familiaPatente.Patente.Activo)
                    continue;

                if (familiaPatente.Patente.Nombre
                    .Equals(
                        patenteBuscada,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }


            // =====================================
            // FAMILIAS HIJAS
            // =====================================

            foreach (var hija
                in familia.FamiliasHijas)
            {
                if (!hija.Activo)
                    continue;


                if (await FamiliaTienePermisoAsync(
                    hija.Id,
                    patenteBuscada))
                {
                    return true;
                }
            }


            return false;
        }


        // =====================================
        // OBTENER PATENTES RECURSIVAMENTE
        // =====================================

        private async Task ObtenerPatentesFamiliaAsync(
            int familiaId,
            HashSet<string> patentes)
        {
            var familia =
                await _context.Familias
                    .Include(f => f.Patentes)
                        .ThenInclude(fp => fp.Patente)
                    .Include(f => f.FamiliasHijas)
                    .FirstOrDefaultAsync(
                        f => f.Id == familiaId &&
                             f.Activo);

            if (familia == null)
                return;


            // =====================================
            // PATENTES DE ESTA FAMILIA
            // =====================================

            foreach (var familiaPatente
                in familia.Patentes)
            {
                if (familiaPatente.Patente.Activo)
                {
                    patentes.Add(
                        familiaPatente.Patente.Nombre);
                }
            }


            // =====================================
            // FAMILIAS HIJAS
            // =====================================

            foreach (var hija
                in familia.FamiliasHijas)
            {
                if (!hija.Activo)
                    continue;


                await ObtenerPatentesFamiliaAsync(
                    hija.Id,
                    patentes);
            }
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