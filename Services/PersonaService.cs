using MecaniCar360.Data;
using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Services
{
    public class PersonaService
    {
        private readonly MecaniCarContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly PermisoService _permisoService;

        public PersonaService(
            MecaniCarContext context,
            PermisoService permisoService, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
            _permisoService = permisoService;
        }


        // =============================
        // CONSULTAS
        // =============================

        private IQueryable<ClienteOperativoViewModel> ClientesOperativos()
        {
            return _context.Personas.AsNoTracking()
                .Where(p => p.Roles.Any(pr => pr.FechaBaja == null &&
                    pr.Rol.Activo && pr.Rol.Nombre == RolesSistema.CLIENTE))
                .Select(p => new ClienteOperativoViewModel
                {
                    Id = p.Id, Nombre = p.Nombre, Apellido = p.Apellido,
                    Dni = p.Dni, Telefono = p.Telefono, Email = p.Email, Activo = p.Activo
                });
        }

        public async Task<ServiceResult<ClientesOperativosViewModel>> ObtenerClientesOperativosAsync(
            int usuarioSolicitanteId, string? busqueda)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "PERSONA_VER"))
                return ServiceResult<ClientesOperativosViewModel>.Error("Acceso denegado.");

            var consulta = ClientesOperativos();
            var texto = busqueda?.Trim();
            if (!string.IsNullOrEmpty(texto))
                consulta = consulta.Where(p =>
                    ((p.Nombre ?? "") + " " + (p.Apellido ?? "")).Contains(texto) ||
                    ((p.Apellido ?? "") + " " + (p.Nombre ?? "")).Contains(texto) ||
                    (p.Dni ?? "").Contains(texto) || (p.Email ?? "").Contains(texto) ||
                    (p.Telefono ?? "").Contains(texto));

            return ServiceResult<ClientesOperativosViewModel>.Ok(new ClientesOperativosViewModel
            {
                Busqueda = texto,
                Clientes = await consulta.OrderBy(p => p.Apellido).ThenBy(p => p.Nombre).ThenBy(p => p.Id).ToListAsync()
            });
        }

        public async Task<ServiceResult<ClienteDetalleOperativoViewModel>> ObtenerClienteOperativoAsync(
            int id, int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "PERSONA_VER"))
                return ServiceResult<ClienteDetalleOperativoViewModel>.Error("Acceso denegado.");

            var cliente = await ClientesOperativos().SingleOrDefaultAsync(p => p.Id == id);
            if (cliente == null)
                return ServiceResult<ClienteDetalleOperativoViewModel>.Error("Cliente no encontrado.");

            var vm = new ClienteDetalleOperativoViewModel
            {
                Cliente = cliente,
                PuedeVerVehiculos = await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "VEHICULO_VER")
            };
            if (vm.PuedeVerVehiculos)
                vm.Vehiculos = await _context.Vehiculos.AsNoTracking()
                    .Where(v => v.DominiosVehiculares.Any(d => d.PersonaId == id && d.FechaHasta == null))
                    .OrderBy(v => v.Patente)
                    .Select(v => new VehiculoClienteOperativoViewModel
                    {
                        Patente = v.Patente, Marca = v.Marca.Nombre, Modelo = v.Modelo.Nombre,
                        Anio = v.Anio, Color = v.Color, Activo = v.Activo
                    }).ToListAsync();

            return ServiceResult<ClienteDetalleOperativoViewModel>.Ok(vm);
        }

        public async Task<ServiceResult<List<Persona>>> ObtenerTodasAsync(
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_VER"))
            {
                return ServiceResult<List<Persona>>.Error(
                    "No posee permisos para consultar personas.");
            }

            var personas = await _context.Personas
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return ServiceResult<List<Persona>>.Ok(personas);
        }


        public async Task<ServiceResult<List<Persona>>> ObtenerActivasAsync(int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "PERSONA_VER")) return ServiceResult<List<Persona>>.Error("Acceso denegado.");

            var personas = await _context.Personas
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .Where(p => p.Activo)
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return ServiceResult<List<Persona>>.Ok(personas);
        }


        public async Task<ServiceResult<Persona>> ObtenerPorIdAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_VER"))
            {
                return ServiceResult<Persona>.Error(
                    "No posee permisos para consultar personas.");
            }

            var persona = await ObtenerPersonaCompletaAsync(id);

            if (persona == null)
                return ServiceResult<Persona>.Error(
                    "Persona no encontrada.");

            return ServiceResult<Persona>.Ok(persona);
        }

        public async Task<ServiceResult<Persona>> ObtenerPorIdParaEditarAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_MODIFICAR"))
            {
                return ServiceResult<Persona>.Error(
                    "No posee permisos para modificar personas.");
            }

            var persona = await ObtenerPersonaCompletaAsync(id);

            if (persona == null)
                return ServiceResult<Persona>.Error(
                    "Persona no encontrada.");

            return ServiceResult<Persona>.Ok(persona);
        }


        public async Task<ServiceResult<Persona>> ObtenerPorDniAsync(
            string dni, int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "PERSONA_VER")) return ServiceResult<Persona>.Error("Acceso denegado.");

            var persona = await ObtenerPersonaPorDniAsync(dni);

            if (persona == null)
                return ServiceResult<Persona>.Error(
                    "Persona no encontrada.");

            return ServiceResult<Persona>.Ok(persona);
        }


        public async Task<ServiceResult<List<Persona>>> ObtenerPorRolAsync(
            string nombreRol, int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "PERSONA_VER")) return ServiceResult<List<Persona>>.Error("Acceso denegado.");

            nombreRol = nombreRol.Trim().ToUpper();

            var personas = await _context.Personas
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .Where(p =>
                    p.Activo &&
                    p.Roles.Any(r =>
                        r.FechaBaja == null &&
                        r.Rol.Activo &&
                        r.Rol.Nombre == nombreRol))
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return ServiceResult<List<Persona>>.Ok(personas);
        }


        public Task<ServiceResult<List<Persona>>> ObtenerMecanicosAsync(int usuarioSolicitanteId)
            => ObtenerPorRolAsync(RolesSistema.MECANICO, usuarioSolicitanteId);


        public Task<ServiceResult<List<Persona>>> ObtenerClientesAsync(int usuarioSolicitanteId)
            => ObtenerPorRolAsync(RolesSistema.CLIENTE, usuarioSolicitanteId);


        public Task<ServiceResult<List<Persona>>> ObtenerAdministrativosAsync(int usuarioSolicitanteId)
            => ObtenerPorRolAsync(RolesSistema.ADMIN, usuarioSolicitanteId);


        // =============================
        // ABM
        // =============================

        public async Task<ServiceResult> CrearAsync(
            Persona persona,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_CREAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para crear personas.");
            }

            if (string.IsNullOrWhiteSpace(persona.Nombre) || string.IsNullOrWhiteSpace(persona.Apellido) ||
                string.IsNullOrWhiteSpace(persona.Dni))
                return ServiceResult.Error("Nombre, apellido y DNI son obligatorios para el alta administrativa.");

            if (await ExisteDniAsync(persona.Dni))
                return ServiceResult.Error(
                    "Ya existe una persona con ese DNI.");

            persona.Dni = persona.Dni.Trim();

            persona.Id = 0;
            persona.Usuario = null;
            persona.Roles = new();
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.Personas.Add(persona);

            await _context.SaveChangesAsync();
            _auditoria.RegistrarOperacion("PERSONA_CREADA", "Persona", persona.Id, usuarioSolicitanteId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult.Ok(
                "Persona creada correctamente.");
        }


        public async Task<ServiceResult> EditarAsync(
            Persona persona,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.EsAdministradorAsync(usuarioSolicitanteId) &&
                await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == persona.Id && pr.FechaBaja == null && pr.Rol.Nombre == RolesSistema.ADMIN))
                return ServiceResult.Error("Sólo ADMIN puede administrar esta persona.");

            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_MODIFICAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar personas.");
            }

            if (_context.Database.CurrentTransaction != null || _context.ChangeTracker.HasChanges())
                return ServiceResult.Error("Hay otra operacion pendiente.");
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            // Mismo orden que invitaciones: Persona -> Usuario/roles -> invitaciones.
            var existente = await _context.Personas.FromSqlInterpolated(
                $"SELECT * FROM [Personas] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {persona.Id}")
                .SingleOrDefaultAsync();
            if (existente != null) await _context.Entry(existente).ReloadAsync();

            if (existente == null)
                return ServiceResult.Error(
                    "Persona no encontrada.");

            if (string.IsNullOrWhiteSpace(persona.Nombre) || string.IsNullOrWhiteSpace(persona.Apellido) ||
                string.IsNullOrWhiteSpace(persona.Dni))
                return ServiceResult.Error("Nombre, apellido y DNI son obligatorios para la edición administrativa.");

            if (await ExisteDniAsync(
                persona.Dni,
                persona.Id))
            {
                return ServiceResult.Error(
                    "Ya existe una persona con ese DNI.");
            }

            var emailNuevo = MecaniCar360.Helpers.IdentificadorCuenta.Normalizar(persona.Email);
            var cambioEmail = !string.Equals(MecaniCar360.Helpers.IdentificadorCuenta.Normalizar(existente.Email),
                emailNuevo, StringComparison.OrdinalIgnoreCase);
            if (cambioEmail && !await _context.Usuarios.AnyAsync(u => u.PersonaId == existente.Id))
                await _context.InvitacionesCliente.Where(i => i.PersonaId == existente.Id &&
                    i.FechaConsumida == null && i.FechaInvalidacion == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.FechaInvalidacion, DateTime.UtcNow));

            var valoresPrevios = _context.Entry(existente).CurrentValues.Clone();
            var auditoriasPrevias = _context.ChangeTracker.Entries<MecaniCar360.Models.Auditoria>().Select(e => e.Entity).ToHashSet();
            var confirmado = false;
            try
            {
                existente.Nombre = persona.Nombre;
                existente.Apellido = persona.Apellido;
                existente.Dni = persona.Dni.Trim();
                existente.Telefono = persona.Telefono;
                existente.Email = string.IsNullOrEmpty(emailNuevo) ? null : emailNuevo;

                // El estado se administra mediante
                // ActivarAsync / DesactivarAsync.
                // No lo modificamos desde la edición general.

                _auditoria.RegistrarOperacion("PERSONA_MODIFICADA", "Persona", existente.Id, usuarioSolicitanteId,
                    "Persona modificada.");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                confirmado = true;

                return ServiceResult.Ok(
                    "Persona actualizada correctamente.");
            }
            finally
            {
                if (!confirmado)
                {
                    await transaction.RollbackAsync();
                    _context.Entry(existente).CurrentValues.SetValues(valoresPrevios);
                    _context.Entry(existente).OriginalValues.SetValues(valoresPrevios);
                    _context.Entry(existente).State = EntityState.Unchanged;
                    foreach (var e in _context.ChangeTracker.Entries<MecaniCar360.Models.Auditoria>().Where(e => !auditoriasPrevias.Contains(e.Entity)).ToList())
                        e.State = EntityState.Detached;
                }
            }
        }


        public async Task<ServiceResult> ActivarAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.EsAdministradorAsync(usuarioSolicitanteId) &&
                await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == id && pr.FechaBaja == null && pr.Rol.Nombre == RolesSistema.ADMIN))
                return ServiceResult.Error("Sólo ADMIN puede administrar esta persona.");

            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_DESACTIVAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar el estado de una persona.");
            }

            var persona = await ObtenerPersonaAsync(id);

            if (persona == null)
                return ServiceResult.Error(
                    "Persona no encontrada.");

            if (persona.Activo)
                return ServiceResult.Error(
                    "La persona ya se encuentra activa.");

            persona.Activo = true;

            _auditoria.RegistrarOperacion("PERSONA_ACTIVADA", "Persona", persona.Id, usuarioSolicitanteId);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                "Persona activada correctamente.");
        }


        public async Task<ServiceResult> DesactivarAsync(
            int id,
            int usuarioSolicitanteId)
        {
            if (!await _permisoService.EsAdministradorAsync(usuarioSolicitanteId) &&
                await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == id && pr.FechaBaja == null && pr.Rol.Nombre == RolesSistema.ADMIN))
                return ServiceResult.Error("Sólo ADMIN puede administrar esta persona.");

            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_DESACTIVAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar el estado de una persona.");
            }

            await using var transaction = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await _permisoService.BloquearAdministradoresAsync();

            // Revalidar la autoridad con el estado actualizado bajo el mutex.
            if (!await _permisoService.EsAdministradorAsync(usuarioSolicitanteId) &&
                await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == id && pr.FechaBaja == null && pr.Rol.Nombre == RolesSistema.ADMIN))
                return ServiceResult.Error("Sólo ADMIN puede administrar esta persona.");

            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "PERSONA_DESACTIVAR"))
            {
                return ServiceResult.Error(
                    "No posee permisos para modificar el estado de una persona.");
            }


            var persona = await ObtenerPersonaAsync(id);

            if (persona == null)
                return ServiceResult.Error(
                    "Persona no encontrada.");

            if (!persona.Activo)
                return ServiceResult.Error(
                    "La persona ya se encuentra desactivada.");

            var esAdministrador = await _permisoService
                .EsAdministradorEfectivoPersonaAsync(id);

            if (esAdministrador)
            {
                if (!await _permisoService.EsAdministradorAsync(
                    usuarioSolicitanteId))
                {
                    return ServiceResult.Error(
                        "Sólo un administrador puede desactivar a otro administrador.");
                }

                var esSolicitante = await _context.Usuarios
                    .AnyAsync(u =>
                        u.Id == usuarioSolicitanteId &&
                        u.PersonaId == id);

                if (esSolicitante)
                {
                    return ServiceResult.Error(
                        "No puede desactivarse a sí mismo como administrador.");
                }

                if (await _permisoService
                    .ContarAdministradoresEfectivosAsync() <= 1)
                {
                    return ServiceResult.Error(
                        "No se puede desactivar al último administrador efectivo.");
                }
            }

            persona.Activo = false;

            _auditoria.RegistrarOperacion("PERSONA_DESACTIVADA", "Persona", persona.Id, usuarioSolicitanteId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult.Ok(
                "Persona desactivada correctamente.");
        }


        // =============================
        // ROLES
        // =============================

        public async Task<bool> TieneRolAsync(
            int personaId,
            string nombreRol, int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(usuarioSolicitanteId, "ROL_VER")) return false;

            nombreRol = nombreRol.Trim().ToUpper();

            return await _context.PersonaRoles.AnyAsync(pr =>
                pr.PersonaId == personaId &&
                pr.FechaBaja == null &&
                pr.Rol.Activo &&
                pr.Rol.Nombre == nombreRol);
        }


        // Historical internal assignments preserve eligibility after removing the last role
        // from a person without an active account. Pure clients have no such history.
        private IQueryable<Persona> PersonalAdministrable() => _context.Personas.Where(p =>
            p.Roles.Any(pr => RolesSistema.Internos.Contains(pr.Rol.Nombre)));

        public async Task<bool> PuedeConsultarRolesInternosAsync(int personaId, int actorId) =>
            await _permisoService.TienePermisoAsync(actorId, "PERSONA_VER") &&
            await _permisoService.TienePermisoAsync(actorId, "ROL_VER") &&
            await PersonalAdministrable().AsNoTracking().AnyAsync(p => p.Id == personaId);

        public async Task<ServiceResult> AsignarRolAsync(int personaId, int rolId, int usuarioOtorgaId)
            => await CambiarAsignacionInternaAsync(personaId, rolId, usuarioOtorgaId, quitar: false);

        public async Task<ServiceResult> QuitarRolAsync(int personaId, int rolId, int usuarioSolicitanteId)
            => await CambiarAsignacionInternaAsync(personaId, rolId, usuarioSolicitanteId, quitar: true);

        private async Task<ServiceResult> CambiarAsignacionInternaAsync(int personaId, int rolId, int actorId, bool quitar)
        {
            if (!await _permisoService.TienePermisoAsync(actorId, "ROL_MODIFICAR"))
                return ServiceResult.Error("No posee permisos para modificar roles.");
            if (_context.Database.CurrentTransaction != null || _context.ChangeTracker.HasChanges())
                return ServiceResult.Error("No se puede modificar roles con otra operación pendiente.");

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            // M1 first, then the target Persona, then Usuario/PersonaRol. Never acquire M1
            // after locking a target. Both assignment and removal follow this order.
            await _permisoService.BloquearAdministradoresAsync();
            var previas = _context.ChangeTracker.Entries().ToDictionary(e => e.Entity,
                e => (Actuales: e.CurrentValues.Clone(), Originales: e.OriginalValues.Clone()),
                ReferenceEqualityComparer.Instance);
            var confirmado = false;
            try
            {
                if (!await _permisoService.TienePermisoAsync(actorId, "ROL_MODIFICAR"))
                    return ServiceResult.Error("No posee permisos para modificar roles.");
                var persona = await _context.Personas.FromSqlInterpolated(
                    $"SELECT * FROM [Personas] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {personaId}")
                    .AsNoTracking().SingleOrDefaultAsync();
                if (persona == null || !await PersonalAdministrable().AsNoTracking().AnyAsync(p => p.Id == personaId))
                    return ServiceResult.Error("La persona no es elegible para administrar roles internos.");

                var rol = await _context.Roles.AsNoTracking().SingleOrDefaultAsync(r => r.Id == rolId);
                if (rol == null || !RolesSistema.Internos.Contains(rol.Nombre))
                    return ServiceResult.Error("Sólo se pueden administrar roles internos. CLIENTE no puede asignarse ni quitarse aquí.");
                if (!quitar && !rol.Activo)
                    return ServiceResult.Error("El rol está inactivo.");
                var relacion = await _context.PersonaRoles.SingleOrDefaultAsync(pr => pr.PersonaId == personaId && pr.RolId == rolId);
                if (quitar && (relacion == null || relacion.FechaBaja != null))
                    return ServiceResult.Error("La persona no posee ese rol vigente.");
                if (!quitar && relacion != null && relacion.FechaBaja == null)
                    return ServiceResult.Error("La persona ya posee ese rol.");

                if (rol.Nombre == RolesSistema.ADMIN)
                {
                    if (!await _permisoService.EsAdministradorAsync(actorId))
                        return ServiceResult.Error("Sólo un administrador puede modificar ADMIN.");
                    if (quitar)
                    {
                        if (await _context.Usuarios.AnyAsync(u => u.Id == actorId && u.PersonaId == personaId))
                            return ServiceResult.Error("No puede quitarse su propia asignación ADMIN.");
                        if (await _permisoService.EsAdministradorEfectivoPersonaAsync(personaId) &&
                            await _permisoService.ContarAdministradoresEfectivosAsync() <= 1)
                            return ServiceResult.Error("No se puede quitar la última asignación ADMIN activa.");
                    }
                }
                if (quitar)
                {
                    if (await _context.Usuarios.AnyAsync(u => u.PersonaId == personaId && u.Activo) &&
                        !await _context.PersonaRoles.AnyAsync(pr => pr.PersonaId == personaId && pr.RolId != rolId &&
                            pr.FechaBaja == null && pr.Rol.Activo && RolesSistema.Internos.Contains(pr.Rol.Nombre)))
                        return ServiceResult.Error("Una cuenta interna activa debe conservar al menos un rol interno activo y vigente.");
                    relacion!.FechaBaja = DateTime.Now;
                }
                else
                {
                    if (relacion == null)
                    {
                        relacion = new PersonaRol { PersonaId = personaId, RolId = rolId };
                        _context.PersonaRoles.Add(relacion);
                    }
                    relacion.FechaBaja = null;
                    relacion.FechaAlta = DateTime.Now;
                    relacion.OtorgadoPorUsuarioId = actorId;
                }
                _auditoria.RegistrarOperacion(quitar ? "ROL_QUITADO" : "ROL_ASIGNADO", "Persona", personaId, actorId, $"Rol #{rolId}.");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                confirmado = true;
                return ServiceResult.Ok(quitar ? "Rol removido correctamente." : "Rol asignado correctamente.");
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Error("No se pudo guardar el cambio de rol. Intente nuevamente.");
            }
            finally
            {
                if (!confirmado)
                {
                    try { await transaction.RollbackAsync(); }
                    finally
                    {
                        foreach (var entry in _context.ChangeTracker.Entries().ToList())
                        {
                            if (!previas.TryGetValue(entry.Entity, out var valores)) entry.State = EntityState.Detached;
                            else
                            {
                                entry.CurrentValues.SetValues(valores.Actuales);
                                entry.OriginalValues.SetValues(valores.Originales);
                                entry.State = EntityState.Unchanged;
                            }
                        }
                    }
                }
            }
        }

        public async Task<ServiceResult<MecaniCar360.ViewModels.AdministrarRolesViewModel>> ObtenerAdministracionRolesAsync(int personaId, int actorId)
        {
            if (!await PuedeConsultarRolesInternosAsync(personaId, actorId))
                return ServiceResult<MecaniCar360.ViewModels.AdministrarRolesViewModel>.Error("Acceso denegado.");
            var vm = await PersonalAdministrable().AsNoTracking().Where(p => p.Id == personaId)
                .Select(p => new MecaniCar360.ViewModels.AdministrarRolesViewModel
                {
                    PersonaId = p.Id, Nombre = p.Nombre, Apellido = p.Apellido, Dni = p.Dni,
                    TieneCuenta = p.Usuario != null,
                    RolesActuales = p.Roles.Where(pr => pr.FechaBaja == null && RolesSistema.Internos.Contains(pr.Rol.Nombre))
                        .Select(pr => new MecaniCar360.ViewModels.RolInternoViewModel
                        { Id = pr.RolId, Nombre = pr.Rol.Nombre, Activo = pr.Rol.Activo }).ToList()
                }).SingleOrDefaultAsync();
            if (vm == null) return ServiceResult<MecaniCar360.ViewModels.AdministrarRolesViewModel>.Error("Persona no disponible.");
            var modificar = await _permisoService.TienePermisoAsync(actorId, "ROL_MODIFICAR");
            var admin = await _permisoService.EsAdministradorAsync(actorId);
            var cuentaActiva = await _context.Usuarios.AnyAsync(u => u.PersonaId == personaId && u.Activo);
            var propia = await _context.Usuarios.AnyAsync(u => u.Id == actorId && u.PersonaId == personaId);
            var ultimoAdmin = await _permisoService.EsAdministradorEfectivoPersonaAsync(personaId) &&
                await _permisoService.ContarAdministradoresEfectivosAsync() <= 1;
            foreach (var rol in vm.RolesActuales)
            {
                rol.Restriccion = !modificar ? "Sólo consulta." :
                    rol.Nombre == RolesSistema.ADMIN && (!admin || propia || ultimoAdmin) ? "Asignaci?n ADMIN protegida." :
                    cuentaActiva && !vm.RolesActuales.Any(r => r.Id != rol.Id && r.Activo) ? "La cuenta debe conservar un rol interno activo." : null;
                rol.PuedeQuitar = rol.Restriccion == null;
            }
            if (modificar)
            {
                var disponibles = await ObtenerRolesDisponiblesAsync(personaId, actorId);
                if (!disponibles.Exitoso) return ServiceResult<MecaniCar360.ViewModels.AdministrarRolesViewModel>.Error(disponibles.Mensaje);
                vm.RolesDisponibles.AddRange(disponibles.Data!.Select(r => new MecaniCar360.ViewModels.RolInternoViewModel
                    { Id = r.Id, Nombre = r.Nombre, Activo = r.Activo }));
            }
            vm.PuedeAsignar = modificar && vm.RolesDisponibles.Count > 0;
            return ServiceResult<MecaniCar360.ViewModels.AdministrarRolesViewModel>.Ok(vm);
        }

        public async Task<ServiceResult<List<Rol>>>
            ObtenerRolesDisponiblesAsync(
                int personaId,
                int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_VER"))
            {
                return ServiceResult<List<Rol>>.Error(
                    "No posee permisos para consultar roles.");
            }

            if (!await PersonalAdministrable().AsNoTracking().AnyAsync(p => p.Id == personaId))
                return ServiceResult<List<Rol>>.Error("La persona no es elegible para administrar roles internos.");
            var admin = await _permisoService.EsAdministradorAsync(usuarioSolicitanteId);

            var asignados = await _context.PersonaRoles
                .Where(pr =>
                    pr.PersonaId == personaId &&
                    pr.FechaBaja == null)
                .Select(pr => pr.RolId)
                .ToListAsync();

            var roles = await _context.Roles
                .Where(r =>
                    r.Activo &&
                    RolesSistema.Internos.Contains(r.Nombre) &&
                    (admin || r.Nombre != RolesSistema.ADMIN) &&
                    !asignados.Contains(r.Id))
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return ServiceResult<List<Rol>>.Ok(roles);
        }


        public async Task<ServiceResult<List<Rol>>>
            ObtenerRolesPersonaAsync(
                int personaId,
                int usuarioSolicitanteId)
        {
            if (!await _permisoService.TienePermisoAsync(
                usuarioSolicitanteId,
                "ROL_VER"))
            {
                return ServiceResult<List<Rol>>.Error(
                    "No posee permisos para consultar roles.");
            }

            var roles = await _context.PersonaRoles
                .Where(pr =>
                    pr.PersonaId == personaId &&
                    pr.FechaBaja == null)
                .Select(pr => pr.Rol)
                .OrderBy(r => r.Nombre)
                .ToListAsync();

            return ServiceResult<List<Rol>>.Ok(roles);
        }


        // =============================
        // MÉTODOS PRIVADOS
        // =============================

        private async Task<Persona?> ObtenerPersonaAsync(
            int id)
        {
            return await _context.Personas
                .FirstOrDefaultAsync(p =>
                    p.Id == id);
        }


        private async Task<Persona?> ObtenerPersonaCompletaAsync(
            int id)
        {
            return await _context.Personas
                .Include(p => p.Usuario)
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .FirstOrDefaultAsync(p =>
                    p.Id == id);
        }


        private async Task<Persona?> ObtenerPersonaPorDniAsync(
            string dni)
        {
            dni = dni.Trim();

            return await _context.Personas
                .Include(p => p.Usuario)
                .Include(p => p.Roles)
                    .ThenInclude(pr => pr.Rol)
                .FirstOrDefaultAsync(p =>
                    p.Dni == dni);
        }


        internal async Task<bool> ExisteDniAsync(
            string dni,
            int? excluirId = null)
        {
            dni = dni.Trim();

            return await _context.Personas.AnyAsync(p =>
                p.Dni == dni &&
                (!excluirId.HasValue ||
                 p.Id != excluirId.Value));
        }
    }
}
