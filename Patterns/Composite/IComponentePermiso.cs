namespace MecaniCar360.Patterns.Composite
{
    public interface IComponentePermiso
    {
        int Id { get; }

        string Nombre { get; }

        bool TienePermiso(string patente);
    }
}