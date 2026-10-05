using MecaniCar360.Models.DTOs;

namespace MecaniCar360.Services;

public static class ArranqueIntegridad
{
    public static async Task EjecutarAsync(IntegridadService verificador, EstadoIntegridad estado, Action inicializar)
    {
        var previo = await verificador.VerificarAsync();
        if (!previo.EsValida) { estado.Comprometer(previo.Errores); return; }
        try
        {
            inicializar();
            var posterior = await verificador.VerificarAsync();
            if (posterior.EsValida) estado.ConfirmarValida(); else estado.Comprometer(posterior.Errores);
        }
        catch
        {
            estado.Comprometer(new[] { new ErrorIntegridad("Inicializacion", null, "VERIFICACION_NO_DISPONIBLE") });
        }
    }
}
