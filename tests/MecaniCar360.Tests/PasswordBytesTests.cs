using System.Data.Common;
using System.Reflection;
using System.Text;
using MecaniCar360.Data;
using MecaniCar360.Helpers;
using MecaniCar360.Models.ViewModels;
using MecaniCar360.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace MecaniCar360.Tests;

public class PasswordBytesTests
{
    public static IEnumerable<object[]> Limites()
    {
        yield return new object[] { "Aa1!" + new string('x', 68), 72, true };
        yield return new object[] { "Aa1!" + new string('x', 69), 73, false };
        yield return new object[] { "Aa1!" + new string('é', 34), 72, true };
        yield return new object[] { "Aa1!" + new string('é', 34) + "x", 73, false };
        yield return new object[] { "Aa1!" + string.Concat(Enumerable.Repeat("😀", 17)), 72, true };
        yield return new object[] { "Aa1!" + string.Concat(Enumerable.Repeat("😀", 17)) + "x", 73, false };
    }

    [Theory]
    [MemberData(nameof(Limites))]
    public void LimiteEsEnBytesSinTransformar(string password, int bytes, bool valid)
    {
        Assert.Equal(bytes, Encoding.UTF8.GetByteCount(password));
        Assert.Equal(valid, PasswordValidator.EsValida(password, out var error));
        if (!valid) Assert.Contains("72 bytes UTF-8", error);
    }

    private sealed class NoConnection : DbConnectionInterceptor
    {
        public int Attempts;
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        { Attempts++; throw new InvalidOperationException("Database access prohibited in this test."); }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        { Attempts++; throw new InvalidOperationException("Database access prohibited in this test."); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PasswordExcesivo_NoAccedeABaseNiConsumeToken(bool activacion)
    {
        var guard = new NoConnection();
        using var db = new MecaniCarContext(new DbContextOptionsBuilder<MecaniCarContext>()
            .UseSqlServer("Server=not-used.invalid;Database=NeverOpened;Integrated Security=True")
            .AddInterceptors(guard).Options);
        var password = "Aa1!" + new string('x', 69);
        var token = TokenInvitacion.Generar();
        var result = activacion
            ? await new InvitacionClienteService(db, null!, null!, null!, null!, null!, null!, null!)
                .ActivarAsync(new ActivarClienteViewModel { Token = token, Username = "prueba",
                    Nombre = "Prueba", Apellido = "Ficticia", Dni = "30123456", Telefono = "1123456789",
                    Password = password, ConfirmarPassword = password })
            : await new SeguridadClienteService(db, null!, null!, null!, null!, null!, null!, null!, null!)
                .RestablecerAsync(new RestablecerPasswordClienteViewModel { Token = token,
                    Password = password, ConfirmarPassword = password });
        Assert.False(result.Exitoso);
        Assert.Contains("72 bytes UTF-8", result.Mensaje);
        Assert.Equal(0, guard.Attempts);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(12)]
    [InlineData(72)]
    [InlineData(90)]
    public void VerificacionAnteriorSigueAceptandoCredencialesLegadas(int bytes)
    {
        var password = "Aa1!" + new string('x', bytes - 4);
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var verify = typeof(AccountService).GetMethod("VerificarPassword", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.True((bool)verify.Invoke(null, new object[] { password, hash })!);
        Assert.False((bool)verify.Invoke(null, new object[] { "Bb2!" + password, hash })!);
        if (bytes > 72)
        {
            Assert.False(PasswordValidator.EsValida(password, out _));
            // Legacy truncation is intentionally preserved for verification only.
            Assert.True((bool)verify.Invoke(null, new object[] { password[..72] + "different", hash })!);
        }
    }
}
