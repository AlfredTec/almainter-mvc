using System.ComponentModel.DataAnnotations;

namespace AlmaInter.Models.ViewModels
{
    public class ArticuloCrearViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "La ubicación es obligatoria")]
        [StringLength(100)]
        [Display(Name = "Ubicación")]
        public string UbicacionCasa { get; set; } = null!;

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Range(0, 100000, ErrorMessage = "La cantidad debe estar entre 0 y 100000")]
        [Display(Name = "Stock inicial")]
        public int CantidadInicial { get; set; }
    }
}
