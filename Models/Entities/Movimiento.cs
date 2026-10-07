using AlmaInter.Models.Enums;

namespace AlmaInter.Models.Entities
{
    public class Movimiento
    {
        public Guid Id { get; set; }

        public Guid ArticuloId { get; set; }
        public Articulo Articulo { get; set; } = null!;

        public Guid UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public TipoMovimiento Tipo { get; set; }
        public int Cantidad { get; set; }
        public DateTimeOffset Fecha { get; set; }

    }
}
