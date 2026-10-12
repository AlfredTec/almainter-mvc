
using AlmaInter.Models.Enums;

namespace AlmaInter.Models.ViewModels
{
    public class UsuarioListItemViewModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string NombreCompleto { get; set; } = null!;
        public RolUsuario Rol { get; set; }
        public bool Activo { get; set; }
    }
}
