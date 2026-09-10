using MecaniCar360.Models.Enums;

namespace MecaniCar360.Models
{
    public class Turno
    {
        public int Id { get; set; }

        // =====================================
        // VEHÍCULO Y CLIENTE
        // =====================================

        public int VehiculoId { get; set; }
        public Vehiculo Vehiculo { get; set; } = null!;

        public int ClienteId { get; set; }
        public Persona Cliente { get; set; } = null!;

        // =====================================
        // TIPO DE TURNO
        // =====================================

        public TipoTurno Tipo { get; set; }

        public string Motivo { get; set; } = string.Empty;

        // =====================================
        // AGENDA
        // =====================================

        public DateTime FechaInicio { get; set; }

        // =====================================
        // ESTADO
        // =====================================

        public EstadoTurno Estado { get; set; } =
            EstadoTurno.Pendiente;

        // =====================================
        // INFORMACIÓN ADICIONAL
        // =====================================

        public string? Observaciones { get; set; }

        public DateTime FechaCreacion { get; set; } =
            DateTime.Now;

        // =====================================
        // USUARIO QUE CREÓ EL TURNO
        // =====================================

        public int CreadoPorUsuarioId { get; set; }

        public Usuario CreadoPorUsuario { get; set; } =
            null!;

        // =====================================
        // HISTORIAL
        // =====================================

        public List<TurnoEstadoHistorial> HistorialEstados
        {
            get;
            set;
        } = new();

        // =====================================
        // INGRESO Y ORDEN DE TRABAJO
        // =====================================

        public IngresoVehiculo? IngresoVehiculo { get; set; }
    }


    public class TurnoEstadoHistorial
    {
        public int Id { get; set; }

        public int TurnoId { get; set; }

        public Turno Turno { get; set; } = null!;

        public EstadoTurno Estado { get; set; }

        public DateTime FechaCambio { get; set; } =
            DateTime.Now;

        public int? UsuarioId { get; set; }

        public Usuario? Usuario { get; set; }

        public string? Observaciones { get; set; }
    }
}