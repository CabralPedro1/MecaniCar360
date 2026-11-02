namespace MecaniCar360.Services;

// La instancia operativa no expone pantallas técnicas ni redefine la seguridad.
// No depende de patentes, ADMIN, configuración ni de un mecanismo de desbloqueo web.
public static class PoliticaSeguridadEstructural
{
    public static bool PermiteConsultarPantallasTecnicas => false;
    public static bool PermiteModificar => false;
    public const string Mensaje = "La configuración estructural de seguridad es de solo lectura en la aplicación operativa.";
}
