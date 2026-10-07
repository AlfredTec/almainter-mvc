namespace AlmaInter.Models.Entities
{
    public class Articulo
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string UbicacionCasa { get; set; } = null!;
        public int CantidadCasa { get; set; }
        public string? ImagenUrl { get; set; }
        public bool Activo { get; set; } = true;
        public DateTimeOffset FechaIngreso { get; set; }

        public ICollection<Movimiento> Movimientos { get; set; } = [];
    }
}
