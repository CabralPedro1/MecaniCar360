using System.ComponentModel.DataAnnotations;
using MecaniCar360.Models.Enums;

namespace MecaniCar360.Models
{
    public class IngresoVehiculo
    {
        public int Id { get; set; }

        public int TurnoId { get; set; }
        public Turno Turno { get; set; } = null!;

        public int VehiculoId { get; set; }
        public Vehiculo Vehiculo { get; set; } = null!;

        public int RegistradoPorUsuarioId { get; set; }
        public Usuario RegistradoPorUsuario { get; set; } = null!;

        [Required, MaxLength(201)]
        public string ClienteNombreSnapshot { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        public string ClienteDniSnapshot { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string VehiculoPatenteSnapshot { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string VehiculoDescripcionSnapshot { get; set; } = string.Empty;

        public int Kilometraje { get; set; }

        public NivelCombustible NivelCombustible { get; set; }

        public EstadoExteriorRecepcion EstadoExterior { get; set; }

        [MaxLength(2000)]
        public string? ObservacionesEstadoExterior { get; set; }

        public AccesoriosRecepcion Accesorios { get; set; }

        [MaxLength(1000)]
        public string? OtrosAccesorios { get; set; }

        public bool DatosVerificadosConCliente { get; set; }

        public OrdenTrabajo? OrdenTrabajo { get; set; }

        public DateTime FechaIngreso { get; set; }

        public DateTime? FechaEgreso { get; set; }

        public bool ClienteEspera { get; set; }

        public string? ObservacionesRecepcion { get; set; }
    }
}