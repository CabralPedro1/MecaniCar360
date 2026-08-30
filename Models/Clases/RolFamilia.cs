namespace MecaniCar360.Models
{
    public class RolFamilia
    {
        public int RolId { get; set; }

        public Rol Rol { get; set; }
            = null!;


        public int FamiliaId { get; set; }

        public Familia Familia { get; set; }
            = null!;
    }
}