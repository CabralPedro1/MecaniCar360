namespace MecaniCar360.Models.Enums
{
    [Flags]
    public enum AccesoriosRecepcion
    {
        Ninguno = 0,
        RuedaDeAuxilio = 1,
        Crique = 2,
        LlaveDeRueda = 4,
        Matafuego = 8,
        Balizas = 16,
        Otros = 32
    }
}
