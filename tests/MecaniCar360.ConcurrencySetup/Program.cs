using MecaniCar360.Concurrency;
using MecaniCar360.Data;
using MecaniCar360.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// No web host, configuration files, user secrets or DefaultConnection.
if (!args.SequenceEqual(new[] { "prepare", "--confirm-create-isolated" }))
{
    Console.Error.WriteLine("Required: prepare --confirm-create-isolated. This command CREATES an isolated database.");
    return 2;
}
try
{
    var guard = IsolatedDatabaseGuard.FromEnvironment();
    await using (var master = guard.NewMasterConnection())
    {
        await master.OpenAsync();
        await guard.CheckEffectiveAsync(master, master: true);
        using var exists = master.CreateCommand();
        exists.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name=@name";
        exists.Parameters.AddWithValue("@name", guard.Database);
        if (Convert.ToInt32(await exists.ExecuteScalarAsync()) != 0)
            throw new InvalidOperationException("Database already exists; reuse is forbidden.");
        using var create = master.CreateCommand();
        // Identifier consists solely of fixed ASCII prefix and validated GUID.
        create.CommandText = $"CREATE DATABASE [{guard.Database}]";
        await create.ExecuteNonQueryAsync();
    }
    var options = new DbContextOptionsBuilder<MecaniCarContext>().UseSqlServer(guard.ConnectionString).Options;
    await using (var db = new MecaniCarContext(options))
    {
        await db.Database.OpenConnectionAsync();
        await guard.CheckEffectiveAsync((SqlConnection)db.Database.GetDbConnection());
        await db.Database.MigrateAsync();
    }
    string backup;
    await using (var sql = new SqlConnection(guard.ConnectionString))
    {
        await sql.OpenAsync();
        await guard.CheckEffectiveAsync(sql);
        using var path = sql.CreateCommand();
        path.CommandText = "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))";
        var directory = Convert.ToString(await path.ExecuteScalarAsync());
        if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("SQL backup directory unavailable.");
        backup = Path.Combine(directory, guard.Database + "_" + Guid.NewGuid().ToString("N") + ".bak");
        using var command = sql.CreateCommand();
        command.CommandTimeout = 1800;
        command.CommandText = $"BACKUP DATABASE [{guard.Database}] TO DISK=@path WITH COPY_ONLY,CHECKSUM,STOP_ON_ERROR; RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM,STOP_ON_ERROR;";
        command.Parameters.AddWithValue("@path", backup);
        await command.ExecuteNonQueryAsync();
    }
    await using (var db = new MecaniCarContext(options))
        await new IntegridadService(db).InicializarAsync(backup, true, true);
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
        ["BootstrapAdmin:Email"] = "bootstrap-" + guard.RunId + "@example.invalid"
    }).Build();
    await using (var db = new MecaniCarContext(options))
    {
        var state = new EstadoIntegridad(Path.Combine(Path.GetTempPath(), guard.Database, "integrity"),
            Path.Combine(Path.GetTempPath(), guard.Database, "unused-webroot"));
        await ArranqueIntegridad.EjecutarAsync(new IntegridadService(db), state, () => InicializadorBD.Inicializar(db, config));
        if (!state.Resultado.EsValida || !(await new IntegridadService(db).VerificarAsync()).EsValida ||
            !await db.Usuarios.AnyAsync() || (await db.Database.GetPendingMigrationsAsync()).Any() || db.Database.HasPendingModelChanges())
            throw new InvalidOperationException("Seed/integrity/migrations verification failed.");
    }
    // Independent persisted read: never emit mail or activate the seeded account.
    await using (var verify = new MecaniCarContext(options))
    {
        await verify.Database.OpenConnectionAsync();
        await guard.CheckEffectiveAsync((SqlConnection)verify.Database.GetDbConnection());
        var admin = await verify.Usuarios.AsNoTracking().Include(u => u.Persona)
            .ThenInclude(p => p.Roles).ThenInclude(r => r.Rol).SingleAsync();
        var link = await verify.EnlacesPasswordCliente.AsNoTracking().SingleAsync();
        if (admin.Activo || admin.PasswordHash != null || !admin.PrimerLogin || !admin.Persona.Activo ||
            admin.EmailLogin != config["BootstrapAdmin:Email"] || link.UsuarioId != admin.Id ||
            link.FechaInvalidacion == null || link.FechaConsumida != null || link.TeniaPassword ||
            link.Finalidad != MecaniCar360.Models.FinalidadPasswordCliente.Agregar ||
            link.FechaExpiracion != link.FechaCreacion.AddMinutes(10) ||
            admin.Persona.Roles.Count(r => r.FechaBaja == null && r.Rol.Activo && r.Rol.Nombre == "ADMIN") != 1 ||
            await verify.Auditorias.CountAsync(a => a.Accion == "ADMIN_ACTIVACION_PENDIENTE" && a.EntidadId == admin.Id) != 1 ||
            !(await new IntegridadService(verify).VerificarAsync()).EsValida)
            throw new InvalidOperationException("Pending bootstrap persistence verification failed.");
    }
    await using (var sql = new SqlConnection(guard.ConnectionString))
    {
        await sql.OpenAsync();
        await guard.CheckEffectiveAsync(sql);
        using var cmd = sql.CreateCommand();
        cmd.CommandText = "CREATE TABLE dbo.MC360ConcurrencyExecution (RunId varchar(32) NOT NULL PRIMARY KEY, StartedUtc datetime2 NOT NULL); EXEC sys.sp_addextendedproperty @name=N'MC360_Concurrency_Run',@value=@run; DECLARE @actor nvarchar(128)=ORIGINAL_LOGIN(); EXEC sys.sp_addextendedproperty @name=N'MC360_Concurrency_PreparedBy',@value=@actor;";
        cmd.Parameters.AddWithValue("@run", guard.RunId);
        await cmd.ExecuteNonQueryAsync();
    }
    Console.WriteLine($"PREPARED {guard.Database}. Backup: {backup}. Runner permissions must be provisioned separately.");
    return 0;
}
catch (Exception ex)
{
    // No automatic repair/drop or raw exception output (connection/credentials).
    Console.Error.WriteLine($"Preparation failed ({ex.GetType().Name}). Stop and inspect the isolated database. No cleanup performed.");
    return 1;
}
