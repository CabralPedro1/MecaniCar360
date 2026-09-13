using System.ComponentModel.DataAnnotations;
using MecaniCar360.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MecaniCar360.Models.ViewModels
{
    public class RegistrarPagoViewModel
    {
        [Range(1, int.MaxValue)]
        public int FacturaId { get; set; }

        [Display(Name = "Monto")]
        [Range(typeof(decimal), "0.01", "9999999999999999.99",
            ParseLimitsInInvariantCulture = true, ErrorMessage = "Ingrese un monto positivo.")]
        public decimal Monto { get; set; }

        [Display(Name = "Método de pago")]
        [EnumDataType(typeof(MetodoPago), ErrorMessage = "Seleccione un método de pago válido.")]
        public MetodoPago MetodoPago { get; set; }

        public decimal SaldoEsperado { get; set; }

        [BindNever, ValidateNever]
        public Factura? Factura { get; set; }
    }
}
