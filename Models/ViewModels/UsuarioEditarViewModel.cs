using AlmaInter.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AlmaInter.Models.ViewModels
{
    public class UsuarioEditarViewModel
    {
        public Guid Id { get; set; }

        // Solo se muestra; no se puede cambiar
        public string? Username { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = null!;

        [Display(Name = "Rol")]
        public RolUsuario Rol { get; set; }

        [Display(Name = "Usuario activo")]
        public bool Activo { get; set; }

        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña (opcional)")]
        public string? NuevaPassword { get; set; }

        [Compare(nameof(NuevaPassword), ErrorMessage = "Las contraseñas no coinciden")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        public string? ConfirmarNuevaPassword { get; set; }
    }
}
