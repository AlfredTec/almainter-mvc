using System.ComponentModel.DataAnnotations;

namespace AlmaInter.Models.ViewModels
{
    public class ArticuloEditarViewModel
    {
        public Guid Id { get; set; }

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

        [Display(Name = "Artículo activo")]
        public bool Activo { get; set; }

        [Display(Name = "Foto")]
        public IFormFile? Foto { get; set; }

        [Display(Name = "Quitar la foto actual")]
        public bool QuitarFoto { get; set; }

        // Solo para mostrar; no se modifica desde aquí
        public string? ImagenUrl { get; set; }
        public int CantidadCasa { get; set; }
        public bool TieneMovimientos { get; set; }
    }
}
