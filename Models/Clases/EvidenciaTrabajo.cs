namespace MecaniCar360.Models
{
    public class EvidenciaTrabajo
    {
        public int Id { get; set; }

        public int OrdenTrabajoId { get; set; }
        public OrdenTrabajo OrdenTrabajo { get; set; }

        public string Descripcion { get; set; }
        public string RutaArchivo { get; set; }

        public DateTime Fecha { get; set; }  

        public int SubidaPorUsuarioId { get; set; }
        public Usuario SubidaPorUsuario { get; set; }
    }


}
