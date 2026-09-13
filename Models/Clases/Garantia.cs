using System.ComponentModel.DataAnnotations;

namespace MecaniCar360.Models
{
    public class Garantia
    {
        public int Id { get; set; }


        // =====================================
        // ORDEN DE TRABAJO
        // =====================================

        public int OrdenTrabajoId { get; set; }

        public OrdenTrabajo OrdenTrabajo { get; set; }
            = null!;


        // =====================================
        // VIGENCIA
        // =====================================

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }

        public bool Activa { get; set; } = true;


        // =====================================
        // AUDITORÍA
        // =====================================

        public int CreadaPorUsuarioId { get; set; }

        public Usuario CreadaPorUsuario { get; set; }
            = null!;


        // =====================================
        // ÍTEMS CUBIERTOS
        // =====================================

        public List<GarantiaItem> Items { get; set; }
            = new();
    }


    // =====================================================
    // ÍTEM DE GARANTÍA
    // =====================================================

    public class GarantiaItem
    {
        public int Id { get; set; }


        // =====================================
        // GARANTÍA
        // =====================================

        public int GarantiaId { get; set; }

        public Garantia Garantia { get; set; }
            = null!;


        // =====================================
        // ÍTEM HISTÓRICO FACTURADO
        // =====================================

        public int FacturaItemId { get; set; }

        public FacturaItem FacturaItem { get; set; }
            = null!;


        // =====================================
        // COBERTURA
        // =====================================

        [Range(1, 120)]
        public int MesesGarantia { get; set; }


        [MaxLength(1000)]
        public string? Observaciones { get; set; }
    }
}
