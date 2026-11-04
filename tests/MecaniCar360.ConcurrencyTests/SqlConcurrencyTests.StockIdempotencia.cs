using MecaniCar360.Models;
using MecaniCar360.Models.DTOs;
using MecaniCar360.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MecaniCar360.Concurrency;

public sealed partial class SqlConcurrencyTests
{
    private sealed record StockFixture(int Actor, int Persona, int Repuesto, int Oferta,
        int Orden, int Primero, int Segundo, string Label)
    {
        public Task<ServiceResult> Execute(ActorContext s, string tipo, Guid clave, bool distinto = false) => tipo switch
        {
            "Ingreso" => s.Stock.RegistrarIngresoAsync(Repuesto, Oferta, distinto ? 8 : 7, 12.50m, Actor, Label, clave),
            "Salida" => s.Stock.RegistrarSalidaAsync(Repuesto, distinto ? 8 : 7, Actor, Label, Orden, clave),
            "Positivo" => s.Stock.RegistrarAjusteAsync(Repuesto, distinto ? 4 : 3, Actor, Label, clave, Primero),
            "Negativo" => s.Stock.RegistrarAjusteAsync(Repuesto, distinto ? -8 : -7, Actor, Label, clave),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo))
        };
    }

    private static async Task<StockFixture> PrepareStock(ConcurrencyEnvironment env)
    {
        var f = await ConcurrencyFixtures.CreateAsync(env);
        var orden = await f.OrdenAsync(env);
        await using var s = await env.OpenAsync();
        var role = await s.Db.Roles.SingleAsync(r => r.Nombre == "STOCK" && r.Activo);
        var user = new Usuario {
            Username = "stock" + Guid.NewGuid().ToString("N"), EmailLogin = f.Label + "stock@example.invalid",
            Activo = true, PrimerLogin = false,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Fixture!9a" + Guid.NewGuid().ToString("N")),
            Persona = new Persona { Nombre = "Stock", Apellido = f.Label, Activo = true,
                Email = f.Label + "stock@example.invalid", Telefono = "1123456789" }
        };
        user.Persona.Roles.Add(new PersonaRol { Rol = role });
        s.Db.Usuarios.Add(user);
        var repuesto = new Repuesto { SKU = Guid.NewGuid().ToString("N")[..30],
            Nombre = f.Label, PrecioVenta = 20, StockActual = 15, StockMinimo = 10 };
        var oferta = new ProveedorRepuesto { Repuesto = repuesto, PrecioCompraActual = 12.50m,
            Proveedor = new Proveedor { Nombre = f.Label, Apellido = "Fixture", Telefono = "1123456789",
                Email = f.Label + "proveedor@example.invalid", FechaCreacion = DateTime.UtcNow } };
        // Arrangement only: a partly consumed lot permits a positive restitution.
        var primero = new LoteRepuesto { Repuesto = repuesto, ProveedorRepuesto = oferta,
            CodigoLote = Guid.NewGuid().ToString("N"), CantidadIngresada = 10, CantidadDisponible = 5,
            PrecioCompra = 12.50m, FechaIngreso = DateTime.UtcNow.AddDays(-2) };
        var segundo = new LoteRepuesto { Repuesto = repuesto, ProveedorRepuesto = oferta,
            CodigoLote = Guid.NewGuid().ToString("N"), CantidadIngresada = 10, CantidadDisponible = 10,
            PrecioCompra = 12.50m, FechaIngreso = DateTime.UtcNow.AddDays(-1) };
        s.Db.LotesRepuesto.AddRange(primero, segundo);
        (await s.Db.OrdenesTrabajo.SingleAsync(o => o.Id == orden)).EstadoActual = EstadoOrden.Aprobado;
        await s.Db.SaveChangesAsync();
        await env.VerifyAsync();
        return new(user.Id, user.PersonaId, repuesto.Id, oferta.Id, orden, primero.Id, segundo.Id, f.Label);
    }

    private static async Task VerifyStock(ConcurrencyEnvironment env, StockFixture f, string tipo, Guid clave)
    {
        // Independent context/connection; never inspect the operation's tracked entities.
        await using var read = await env.OpenAsync();
        var db = read.Db;
        var delta = tipo == "Ingreso" ? 7 : tipo == "Positivo" ? 3 : -7;
        var stock = await db.Repuestos.SingleAsync(r => r.Id == f.Repuesto);
        Assert.Equal(15 + delta, stock.StockActual);
        var lotes = await db.LotesRepuesto.Where(l => l.RepuestoId == f.Repuesto).ToListAsync();
        Assert.Equal(stock.StockActual, lotes.Sum(l => l.CantidadDisponible));
        Assert.All(lotes, l => Assert.InRange(l.CantidadDisponible, 0, l.CantidadIngresada));
        Assert.Equal(tipo == "Ingreso" ? 3 : 2, lotes.Count);
        if (tipo == "Ingreso")
        {
            var nuevo = lotes.Single(l => l.Id != f.Primero && l.Id != f.Segundo);
            Assert.Equal(7, nuevo.CantidadIngresada);
            Assert.Equal(7, nuevo.CantidadDisponible);
            Assert.Equal(12.50m, nuevo.PrecioCompra);
            Assert.Equal(f.Oferta, nuevo.ProveedorRepuestoId);
        }
        Assert.Equal(delta < 0 ? 0 : tipo == "Positivo" ? 8 : 5, lotes.Single(l => l.Id == f.Primero).CantidadDisponible);
        Assert.Equal(delta < 0 ? 8 : 10, lotes.Single(l => l.Id == f.Segundo).CantidadDisponible);
        var movimiento = Assert.Single(await db.MovimientosStock.Include(m => m.Lotes)
            .Where(m => m.RepuestoId == f.Repuesto).ToListAsync());
        Assert.Equal(delta, movimiento.Cantidad);
        Assert.Equal(f.Actor, movimiento.RealizadoPorUsuarioId);
        Assert.Equal(f.Label, movimiento.Observaciones);
        Assert.Equal(tipo == "Ingreso" ? TipoMovimientoStock.IngresoCompra :
            tipo == "Salida" ? TipoMovimientoStock.EgresoOrdenTrabajo : TipoMovimientoStock.Ajuste, movimiento.Tipo);
        Assert.Equal(tipo == "Salida" ? (int?)f.Orden : null, movimiento.OrdenTrabajoId);
        Assert.Equal(tipo == "Ingreso" ? (int?)f.Oferta : null, movimiento.ProveedorRepuestoId);
        Assert.Equal(Math.Abs(delta), movimiento.Lotes.Sum(l => l.Cantidad));
        if (delta < 0)
        {
            Assert.Equal(2, movimiento.Lotes.Count);
            Assert.Equal(5, movimiento.Lotes.Single(l => l.LoteRepuestoId == f.Primero).Cantidad);
            Assert.Equal(2, movimiento.Lotes.Single(l => l.LoteRepuestoId == f.Segundo).Cantidad);
        }
        else
        {
            var detalle = Assert.Single(movimiento.Lotes);
            Assert.Equal(tipo == "Positivo" ? f.Primero :
                lotes.Single(l => l.Id != f.Primero && l.Id != f.Segundo).Id, detalle.LoteRepuestoId);
        }
        var registro = Assert.Single(await db.OperacionesStockIdempotentes
            .Where(o => o.UsuarioId == f.Actor).ToListAsync());
        Assert.Equal(clave, registro.Clave);
        Assert.Equal(movimiento.Id, registro.MovimientoStockId);
        Assert.Equal(tipo is "Positivo" or "Negativo" ? "Ajuste" : tipo, registro.TipoOperacion);
        Assert.Equal(32, registro.HuellaSolicitud.Length);
        Assert.False(string.IsNullOrWhiteSpace(registro.MensajeResultado));
        Assert.Equal(1, await db.Auditorias.CountAsync(a => a.Accion == "MOVIMIENTO_STOCK" &&
            a.Entidad == "MovimientoStock" && a.EntidadId == movimiento.Id && a.UsuarioId == f.Actor));
        var avisos = await db.Notificaciones.Where(n => n.TipoRecurso == TipoRecursoNotificacion.Repuesto &&
            n.RecursoId == f.Repuesto).ToListAsync();
        if (delta < 0)
        {
            Assert.Contains(avisos, n => n.PersonaId == f.Persona && n.Titulo == "Stock bajo");
            Assert.Equal(avisos.Count, avisos.Select(n => n.PersonaId).Distinct().Count());
            var destinatarios = await db.PersonaRoles.Where(pr => pr.FechaBaja == null && pr.Rol.Activo &&
                pr.Rol.Nombre == "STOCK" && pr.Persona.Activo && pr.Persona.Usuario != null && pr.Persona.Usuario.Activo)
                .Select(pr => pr.PersonaId).Distinct().OrderBy(id => id).ToArrayAsync();
            Assert.Equal(destinatarios, avisos.Select(n => n.PersonaId).OrderBy(id => id).ToArray());
            Assert.All(avisos, n => Assert.Equal("Stock bajo", n.Titulo));
        }
        else Assert.Empty(avisos);
        await env.VerifyAsync();
    }

    [SqlConcurrencyTheory, Trait("Category", "SqlConcurrency"), Trait("Security", "SEC05")]
    [InlineData("Ingreso")] [InlineData("Salida")] [InlineData("Positivo")] [InlineData("Negativo")]
    public async Task StockIdempotencia_PrimeraReintentoYConflicto(string tipo)
    {
        var env = new ConcurrencyEnvironment();
        try
        {
            var f = await PrepareStock(env);
            var clave = Guid.NewGuid();
            string mensaje;
            await using (var actor = await env.OpenAsync(f.Actor))
            {
                var r = await f.Execute(actor, tipo, clave);
                Assert.True(r.Exitoso, r.Mensaje); mensaje = r.Mensaje;
            }
            await VerifyStock(env, f, tipo, clave);
            var before = await ConcurrencyFixtures.SnapshotAsync(env);
            await using (var actor = await env.OpenAsync(f.Actor))
            {
                var r = await f.Execute(actor, tipo, clave);
                Assert.True(r.Exitoso, r.Mensaje); Assert.Equal(mensaje, r.Mensaje);
            }
            Assert.Equal(before, await ConcurrencyFixtures.SnapshotAsync(env));
            await using (var actor = await env.OpenAsync(f.Actor))
            {
                var r = await f.Execute(actor, tipo, clave, distinto: true);
                Assert.False(r.Exitoso);
                Assert.Equal("La clave de operación ya fue utilizada con otra solicitud. Inicie una operación nueva.", r.Mensaje);
            }
            Assert.Equal(before, await ConcurrencyFixtures.SnapshotAsync(env));
        }
        finally { await env.VerifyAsync(); }
    }

    [SqlConcurrencyTheory, Trait("Category", "SqlConcurrency"), Trait("Security", "SEC05")]
    [InlineData("Ingreso")] [InlineData("Salida")] [InlineData("Positivo")] [InlineData("Negativo")]
    public async Task StockIdempotencia_Concurrente(string tipo)
    {
        var env = new ConcurrencyEnvironment();
        try
        {
            var f = await PrepareStock(env);
            var clave = Guid.NewGuid();
            var (a,b) = await Race(env, f.Actor, f.Actor, "OperacionesStockIdempotentes",
                async s => Outcome.From(await f.Execute(s, tipo, clave)),
                async s => Outcome.From(await f.Execute(s, tipo, clave)));
            Assert.True(a.Success, a.Message); Assert.True(b.Success, b.Message);
            Assert.Equal(a.Message, b.Message);
            await VerifyStock(env, f, tipo, clave);
        }
        finally { await env.VerifyAsync(); }
    }

    [SqlConcurrencyTheory, Trait("Category", "SqlConcurrency"), Trait("Security", "SEC05")]
    [InlineData("Ingreso")] [InlineData("Salida")] [InlineData("Positivo")] [InlineData("Negativo")]
    public async Task StockIdempotencia_RollbackAntesCommit(string tipo)
    {
        var env = new ConcurrencyEnvironment();
        try
        {
            var f = await PrepareStock(env);
            var clave = Guid.NewGuid();
            var before = await ConcurrencyFixtures.SnapshotAsync(env);
            var evidence = new WriteEvidence();
            var failure = new FailBeforeCommit(evidence);
            await using (var actor = await env.OpenAsync(f.Actor, evidence, failure))
                await Assert.ThrowsAsync<InjectedPersistenceFailure>(() => f.Execute(actor, tipo, clave));
            Assert.True(failure.Fired && evidence.Written && evidence.IntegrityMaintained);
            Assert.Equal(before, await ConcurrencyFixtures.SnapshotAsync(env));
            await env.VerifyAsync();
            await using (var retry = await env.OpenAsync(f.Actor))
            {
                var r = await f.Execute(retry, tipo, clave);
                Assert.True(r.Exitoso, r.Mensaje);
            }
            await VerifyStock(env, f, tipo, clave);
        }
        finally { await env.VerifyAsync(); }
    }
}
