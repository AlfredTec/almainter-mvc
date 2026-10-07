using AlmaInter.Models.Entities;
using AlmaInter.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlmaInter.Data
{
    public static class DbSeeder
    {
        public static async Task SembrarAdminAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            if (await db.Usuarios.AnyAsync())
                return;

            var username = config["Seed:AdminUsername"] ?? "admin";
            var password = config["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("No se creó el administrador inicial: falta la configuración Seed:AdminPassword.");
                return;
            }

            var admin = new Usuario
            {
                Username = username,
                NombreCompleto = "Administrador",
                Rol = RolUsuario.Admin,
                Activo = true
            };
            admin.PasswordHash = hasher.HashPassword(admin, password);

            db.Usuarios.Add(admin);
            await db.SaveChangesAsync();
        }

    }
}
