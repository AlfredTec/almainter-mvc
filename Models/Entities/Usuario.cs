using AlmaInter.Models.Enums;

namespace AlmaInter.Models.Entities
{
    public class Usuario
    {

        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string NombreCompleto { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public RolUsuario Rol { get; set; }
        public bool Activo { get; set; } = true;
        public DateTimeOffset FechaCreacion { get; set; }

        public ICollection<Movimiento> Movimientos { get; set; } = [];
    }
}
