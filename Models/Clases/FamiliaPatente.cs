namespace MecaniCar360.Models
{
    public class FamiliaPatente
    {
        public int FamiliaId { get; set; }

        public Familia Familia { get; set; }
            = null!;


        public int PatenteId { get; set; }

        public Patente Patente { get; set; }
            = null!;
    }
}