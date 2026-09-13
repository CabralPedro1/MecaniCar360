namespace MecaniCar360.Models.DTOs
{
    public class GarantiaItemDto
    {
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
        public int FacturaItemId { get; set; }

        [System.ComponentModel.DataAnnotations.Range(1, 120)]
        public int MesesGarantia { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(1000)]
        public string? Observaciones { get; set; }
    }
}
