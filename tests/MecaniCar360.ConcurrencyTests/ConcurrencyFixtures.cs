using System.Security.Cryptography;
using System.Text;
using MecaniCar360.Models;
using MecaniCar360.Models.Enums;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MecaniCar360.Data;
using Xunit;

namespace MecaniCar360.Concurrency;

public sealed class ConcurrencyFixtures
{
    public string Label { get; } = "TEST_CONCURRENCY_" + Guid.NewGuid().ToString("N");
    public int Admin { get; private set; }
    public int Mechanic1 { get; private set; }
    public int Mechanic2 { get; private set; }
    public int MechanicPerson1 { get; private set; }
    public int MechanicPerson2 { get; private set; }
    public int Client { get; private set; }
    public int[] Vehicles { get; private set; } = [];
    public DateTime Slot { get; private set; }

    // Fixture arrangement only, on the guarded empty test database: EF SaveChanges
    // retains constraints and DVH/DVV. No existing application accounts are used.
    public static async Task<ConcurrencyFixtures> CreateAsync(ConcurrencyEnvironment env)
    {
        var f = new ConcurrencyFixtures();
        await using var scope = await env.OpenAsync();
        var db = scope.Db;
        var roles = await db.Roles.ToDictionaryAsync(r => r.Nombre);
        async Task<Usuario> User(string role, int n)
        {
            var email = f.Label + n + "@example.invalid";
            var person = new Persona { Nombre = "Fixture", Apellido = f.Label, Dni = "9" + RandomNumberGenerator.GetInt32(10000000,99999999),
                Telefono = "1123456789", Email = email, Activo = true };
            var user = new Usuario { Persona=person, Username="fixture"+Guid.NewGuid().ToString("N"), EmailLogin=email,
                PasswordHash=BCrypt.Net.BCrypt.HashPassword("Fixture!9a"+Guid.NewGuid().ToString("N")), PrimerLogin=false, Activo=true };
            person.Roles.Add(new PersonaRol { Persona=person, Rol=roles[role] });
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            return user;
        }
        var admin = await User("ADMIN", 0); f.Admin=admin.Id;
        var first = await User("MECANICO", 1); f.Mechanic1=first.Id; f.MechanicPerson1=first.PersonaId;
        var second = await User("MECANICO", 2); f.Mechanic2=second.Id; f.MechanicPerson2=second.PersonaId;
        var client = await User("CLIENTE", 3); f.Client=client.PersonaId;
        var ahora = DateTime.UtcNow;
        var creacionInvitacion = ahora.AddHours(-1);
        db.InvitacionesCliente.Add(new InvitacionCliente { PersonaId=f.Client, EmailDestino=client.EmailLogin,
            TokenHash=Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), FechaCreacion=creacionInvitacion,
            FechaExpiracion=creacionInvitacion.AddHours(48), FechaConsumida=ahora, EmitidaPorUsuarioId=f.Admin });
        var marca = new Marca { Nombre=f.Label };
        var modelo = new Modelo { Nombre=f.Label, Marca=marca };
        var vehicles = Enumerable.Range(0,3).Select(_ => new Vehiculo { Marca=marca, Modelo=modelo,
            Patente="ZZ"+RandomNumberGenerator.GetInt32(100,1000)+
                (char)('A'+RandomNumberGenerator.GetInt32(26))+(char)('A'+RandomNumberGenerator.GetInt32(26)),
            Anio=2024, Kilometraje=100 }).ToArray();
        foreach (var v in vehicles) db.DominiosVehiculares.Add(new DominioVehicular {
            PersonaId=f.Client, Vehiculo=v, FechaDesde=DateTime.Now.AddDays(-1) });
        await db.SaveChangesAsync();
        f.Vehicles=vehicles.Select(v=>v.Id).ToArray();
        f.Slot=DateTime.Today.AddDays(30).AddHours(10);
        while (await db.Turnos.AnyAsync(t=>t.FechaInicio.Date==f.Slot.Date)) f.Slot=f.Slot.AddDays(1);
        Assert.True(await new ClienteHabilitadoService(db).EstaHabilitadoAsync(f.Client));
        f.Record(env,"fixture",new { f.Admin,f.Mechanic1,f.Mechanic2,f.MechanicPerson1,f.MechanicPerson2,f.Client,f.Vehicles });
        await env.VerifyAsync();
        return f;
    }

    public async Task<int> TurnoAsync(ConcurrencyEnvironment env, EstadoTurno estado, bool past = false)
    {
        await using var scope = await env.OpenAsync(Admin);
        // Past dates cannot be created by the scheduling API: arrange the starting
        // state with EF, never alter it manually once an operation is under test.
        var turno=new Turno { ClienteId=Client, VehiculoId=Vehicles[0], Tipo=TipoTurno.Diagnostico,
            FechaInicio=past ? DateTime.Today.AddDays(-1).AddHours(10) : Slot,
            Motivo=Label, Estado=estado, CreadoPorUsuarioId=Admin };
        turno.HistorialEstados.Add(new TurnoEstadoHistorial { Estado=estado, UsuarioId=Admin,
            FechaCambio=DateTime.Now, Observaciones=Label+" arrangement" });
        scope.Db.Turnos.Add(turno);
        await scope.Db.SaveChangesAsync();
        Record(env,"turno",new { turno.Id });
        return turno.Id;
    }

    public RegistrarIngresoViewModel Reception(int turno) => new() {
        TurnoId=turno, VehiculoId=Vehicles[0], Kilometraje=100, NivelCombustible=NivelCombustible.Medio,
        EstadoExterior=EstadoExteriorRecepcion.SinDanosVisiblesDeclarados, VerificadoConCliente=true
    };

    public async Task<int> OrdenAsync(ConcurrencyEnvironment env)
    {
        var turno=await TurnoAsync(env,EstadoTurno.Confirmado);
        await using var scope=await env.OpenAsync(Admin);
        var result=await scope.Recepcion.RegistrarIngresoYCrearOrdenAsync(Reception(turno),Admin);
        Assert.True(result.Exitoso,result.Mensaje);
        Record(env,"orden",new { result.Data!.Id, TurnoId=turno });
        return result.Data!.Id;
    }

    private void Record(ConcurrencyEnvironment env,string kind,object ids)
    {
        var directory=Path.Combine(Path.GetTempPath(),env.Guard.Database,"manifests");
        Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,Label+"_"+kind+"_"+Guid.NewGuid().ToString("N")+".json");
        using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        System.Text.Json.JsonSerializer.Serialize(file,new { Fixture=Label,Kind=kind,Ids=ids });
    }

    public static async Task<byte[]> SnapshotAsync(ConcurrencyEnvironment env)
    {
        await using var scope=await env.OpenAsync();
        return await SnapshotAsync(scope.Db);
    }

    // Read through the operation's connection/transaction, including uncommitted
    // rows. Digests are evidence only and are never written to logs/manifests.
    public static async Task<byte[]> SnapshotAsync(MecaniCarContext db, bool verticalOnly = false,
        CancellationToken cancellationToken = default)
    {
        var text=new StringBuilder();
        // Canonical ordered table snapshots include DVH/DVV, notifications and all
        // business/audit rows. Values remain in memory and never enter test output.
        using var metadata=db.Database.GetDbConnection().CreateCommand();
        metadata.Transaction=db.Database.CurrentTransaction?.GetDbTransaction();
        metadata.CommandText="SELECT QUOTENAME(SCHEMA_NAME(schema_id))+'.'+QUOTENAME(name) FROM sys.tables " +
            (verticalOnly ? "WHERE name=N'DigitosVerificadoresVerticales' " : "") + "ORDER BY schema_id,name";
        var tables=new List<string>();
        using(var reader=await metadata.ExecuteReaderAsync(cancellationToken))
            while(await reader.ReadAsync(cancellationToken)) tables.Add(reader.GetString(0));
        if(tables.Count==0) throw new InvalidOperationException("Persistence evidence requires visible tables.");
        foreach(var table in tables)
        {
            using var cmd=db.Database.GetDbConnection().CreateCommand();
            cmd.Transaction=db.Database.CurrentTransaction?.GetDbTransaction();
            cmd.CommandText=SnapshotJson.Query(table);
            SnapshotJson.Append(text, table, await cmd.ExecuteScalarAsync(cancellationToken));
        }
        return SnapshotJson.Hash(text);
    }
}
