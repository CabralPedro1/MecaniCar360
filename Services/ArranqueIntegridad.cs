using MecaniCar360.Models.DTOs;

namespace MecaniCar360.Services;

public static class ArranqueIntegridad
{
    public static Task EjecutarAsync(IntegridadService verificador, EstadoIntegridad estado, Action inicializar,
        bool omitirInicializacion = false) =>
        EjecutarAsync(() => verificador.VerificarAsync(), estado, inicializar, omitirInicializacion);

    // Permite probar la secuencia de arranque sin abrir una conexión SQL.
    public static async Task EjecutarAsync(Func<Task<ResultadoIntegridad>> verificar, EstadoIntegridad estado,
        Action inicializar, bool omitirInicializacion = false)
    {
        var previo = await verificar();
        if (!previo.EsValida) { estado.Comprometer(previo.Errores); return; }
        try
        {
            // Sólo omite migraciones/seed/bootstrap; nunca la verificación de integridad.
            if (!omitirInicializacion) inicializar();
            var posterior = await verificar();
            if (posterior.EsValida) estado.ConfirmarValida(); else estado.Comprometer(posterior.Errores);
        }
        catch
        {
            estado.Comprometer(new[] { new ErrorIntegridad("Inicializacion", null, "VERIFICACION_NO_DISPONIBLE") });
        }
    }
}
