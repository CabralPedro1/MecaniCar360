using MecaniCar360.Models.Enums;

namespace MecaniCar360.Models
{
    public class Pago
    {
        public int Id { get; set; }

        public int FacturaId { get; set; }
        public Factura Factura { get; set; }

        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }  

        public MetodoPago MetodoPago { get; set; }
        public EstadoPago Estado { get; set; } = EstadoPago.Pendiente;

        public int RegistradoPorUsuarioId { get; set; }
        public Usuario RegistradoPorUsuario { get; set; }
    }


}
