namespace MecaniCar360.Models
{
    public class BoxTrabajo
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public bool TieneElevador { get; set; }

        public bool Activo { get; set; } = true;
    }
}