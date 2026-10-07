using AlmaInter.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlmaInter.Data.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("usuarios", t =>
                t.HasCheckConstraint("ck_usuarios_rol", "rol IN ('Admin','Tecnico')"));

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.Username).IsRequired().HasMaxLength(50);
            builder.HasIndex(x => x.Username).IsUnique();

            builder.Property(x => x.NombreCompleto).IsRequired().HasMaxLength(100);
            builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(255);

            builder.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.FechaCreacion).HasDefaultValueSql("now()");
        }

    }
}
