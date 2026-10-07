using AlmaInter.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmaInter.Data.Configurations
{
    public class MovimientoConfiguration : IEntityTypeConfiguration<Movimiento>
    {

        public void Configure(EntityTypeBuilder<Movimiento> builder)
        {
            builder.ToTable("movimientos", t =>
            {
                t.HasCheckConstraint("ck_movimientos_tipo",
                    "tipo_movimiento IN ('Entrada','Salida','Consumo')");
                t.HasCheckConstraint("ck_movimientos_cantidad", "cantidad > 0");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.Tipo)
                .HasColumnName("tipo_movimiento")
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.Fecha).HasDefaultValueSql("now()");

            builder.HasOne(x => x.Articulo)
                .WithMany(a => a.Movimientos)
                .HasForeignKey(x => x.ArticuloId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Usuario)
                .WithMany(u => u.Movimientos)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.UsuarioId, x.ArticuloId });
        }

    }
}
