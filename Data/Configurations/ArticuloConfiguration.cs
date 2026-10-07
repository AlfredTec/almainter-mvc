using AlmaInter.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmaInter.Data.Configurations
{
    public class ArticuloConfiguration : IEntityTypeConfiguration<Articulo>
    {

        public void Configure(EntityTypeBuilder<Articulo> builder)
        {
            builder.ToTable("articulos", t =>
                t.HasCheckConstraint("ck_articulos_cantidad_casa", "cantidad_casa >= 0"));

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(x => x.UbicacionCasa).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Descripcion).HasMaxLength(500);
            builder.Property(x => x.ImagenUrl).HasMaxLength(512);

            builder.Property(x => x.FechaIngreso).HasDefaultValueSql("now()");
        }
    }
}
