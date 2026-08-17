using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Especialidad
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; }

        [MaxLength(200)]
        public string Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; }

        public List<MecanicoEspecialidad> Mecanicos { get; set; } = new();
        public List<OrdenTrabajoEspecialidad> OrdenesTrabajo { get; set; } = new();
    }


    public class MecanicoEspecialidad
    {
        public int PersonaId { get; set; }
        public Persona Persona { get; set; }

        public int EspecialidadId { get; set; }
        public Especialidad Especialidad { get; set; }
    }


}
