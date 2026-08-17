using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class CalificacionTrabajo
    {
        public int Id { get; set; }

        public int OrdenTrabajoId { get; set; }
        public OrdenTrabajo OrdenTrabajo { get; set; }

        public int ClienteId { get; set; }
        public Persona Cliente { get; set; }
        [Range(1, 5)]
        public int Puntuacion { get; set; }
        public string? Comentario { get; set; }

        public DateTime Fecha { get; set; }  
    }


}
