using System.ComponentModel.DataAnnotations;
using MecaniCar360.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MecaniCar360.Models.ViewModels;

public sealed class RegistrarIngresoViewModel : IValidatableObject
{
    [Range(1, int.MaxValue)] public int TurnoId { get; set; }
    [Required, Range(1, int.MaxValue)] public int? VehiculoId { get; set; }
    [Required, Range(0, int.MaxValue)] public int? Kilometraje { get; set; }
    [Required, EnumDataType(typeof(NivelCombustible))] public NivelCombustible? NivelCombustible { get; set; }
    [Required, EnumDataType(typeof(EstadoExteriorRecepcion))] public EstadoExteriorRecepcion? EstadoExterior { get; set; }
    [StringLength(2000)] public string? DescripcionEstadoExterior { get; set; }
    public List<AccesoriosRecepcion> AccesoriosSeleccionados { get; set; } = new();
    [StringLength(1000)] public string? OtrosAccesorios { get; set; }
    [StringLength(4000)] public string? ObservacionesRecepcion { get; set; }
    public bool ClienteEspera { get; set; }
    public bool VerificadoConCliente { get; set; }
    public bool ConfirmarKilometrajeMenor { get; set; }

    [BindNever, ValidateNever] public Turno? Turno { get; set; }
    [BindNever, ValidateNever] public List<Vehiculo> Vehiculos { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!VerificadoConCliente)
            yield return new("Debe verificar los datos junto con el cliente.", new[] { nameof(VerificadoConCliente) });
        if (EstadoExterior == EstadoExteriorRecepcion.ConObservaciones && string.IsNullOrWhiteSpace(DescripcionEstadoExterior))
            yield return new("Describa el estado exterior.", new[] { nameof(DescripcionEstadoExterior) });
        if (EstadoExterior == EstadoExteriorRecepcion.SinDanosVisiblesDeclarados && !string.IsNullOrWhiteSpace(DescripcionEstadoExterior))
            yield return new("Seleccione Con observaciones o quite la descripcion exterior.", new[] { nameof(DescripcionEstadoExterior) });
        if (AccesoriosSeleccionados == null || AccesoriosSeleccionados.Any(a => !Enum.IsDefined(a) || a == AccesoriosRecepcion.Ninguno))
            yield return new("Seleccion de accesorios invalida.", new[] { nameof(AccesoriosSeleccionados) });
        bool otros = AccesoriosSeleccionados?.Contains(AccesoriosRecepcion.Otros) == true;
        if (otros == string.IsNullOrWhiteSpace(OtrosAccesorios))
            yield return new("La descripcion de Otros debe corresponder a su seleccion.", new[] { nameof(OtrosAccesorios) });
    }
}
