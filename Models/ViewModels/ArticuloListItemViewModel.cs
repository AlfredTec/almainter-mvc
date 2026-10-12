namespace AlmaInter.Models.ViewModels
{
    public class ArticuloListItemViewModel
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string UbicacionCasa { get; set; } = null!;
        public string? Descripcion { get; set; }
        public int CantidadCasa { get; set; }
        public string? ImagenUrl { get; set; }
        public bool Activo { get; set; }
    }
}
