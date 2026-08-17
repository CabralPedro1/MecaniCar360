using MecaniCar360.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class OrdenTrabajo
    {
        public int Id { get; set; }

        // =====================================
        // TURNO
        // =====================================

        public int TurnoId { get; set; }
        public Turno Turno { get; set; } = null!;

        // =====================================
        // MECÁNICO
        // =====================================

        public int? MecanicoId { get; set; }
        public Persona? Mecanico { get; set; }

        // =====================================
        // ESTADO
        // =====================================

        public EstadoOrden EstadoActual { get; set; } = EstadoOrden.Pendiente;

        // =====================================
        // TIEMPOS
        // =====================================

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        public int? HorasEstimadas { get; set; }

        [Range(0, 1000)]
        public int? HorasReales { get; set; }

        // =====================================
        // USUARIO
        // =====================================

        public int CreadaPorUsuarioId { get; set; }
        public Usuario CreadaPorUsuario { get; set; } = null!;

        // =====================================
        // DIAGNÓSTICOS
        // =====================================

        public List<Diagnostico> Diagnosticos { get; set; } = new();

        public decimal? CostoDiagnostico { get; set; }

        // =====================================
        // PRESUPUESTO
        // =====================================

        public Presupuesto? Presupuesto { get; set; }

        // =====================================
        // HISTORIAL
        // =====================================

        public List<OrdenTrabajoEstadoHistorial> HistorialEstados { get; set; } = new();

        // =====================================
        // INFORMACIÓN ADICIONAL
        // =====================================

        public string? Observaciones { get; set; }

        public NivelUrgencia Urgencia { get; set; } = NivelUrgencia.Media;

        // =====================================
        // RESULTADOS
        // =====================================

        public CalificacionTrabajo? Calificacion { get; set; }

        public Factura? Factura { get; set; }

        public List<OrdenTrabajoEspecialidad> Especialidades { get; set; } = new();

        public List<EvidenciaTrabajo> Evidencias { get; set; } = new();
    }


    public class OrdenTrabajoEspecialidad
    {
        public int OrdenTrabajoId { get; set; }
        public OrdenTrabajo OrdenTrabajo { get; set; } = null!;

        public int EspecialidadId { get; set; }
        public Especialidad Especialidad { get; set; } = null!;

        public int? MecanicoResponsableId { get; set; }
        public Persona? MecanicoResponsable { get; set; }
    }


    public class OrdenTrabajoEstadoHistorial
    {
        public int Id { get; set; }

        public int OrdenTrabajoId { get; set; }
        public OrdenTrabajo OrdenTrabajo { get; set; } = null!;

        public EstadoOrden Estado { get; set; }

        public DateTime Fecha { get; set; }

        public int? MecanicoId { get; set; }
        public Persona? Mecanico { get; set; }
    }
}