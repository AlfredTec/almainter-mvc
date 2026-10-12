using AlmaInter.Data;
using AlmaInter.Models.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace AlmaInter.Services
{

    public record UsuarioEstado(bool Activo, RolUsuario Rol);
    public static class SesionValidator
    {
        public static string Clave(Guid usuarioId) => $"usuario-estado:{usuarioId}";

        public static async Task ValidarAsync(CookieValidatePrincipalContext context)
        {
            var idTexto = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(idTexto, out var id))
            {
                await RechazarAsync(context);
                return;
            }

            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

            if (!cache.TryGetValue<UsuarioEstado>(Clave(id), out var estado))
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                estado = await db.Usuarios
                    .AsNoTracking()
                    .Where(u => u.Id == id)
                    .Select(u => new UsuarioEstado(u.Activo, u.Rol))
                    .FirstOrDefaultAsync();

                if (estado is not null)
                    cache.Set(Clave(id), estado, TimeSpan.FromMinutes(1));
            }

            var rolEnCookie = context.Principal?.FindFirstValue(ClaimTypes.Role);

            if (estado is null || !estado.Activo || rolEnCookie != estado.Rol.ToString())
                await RechazarAsync(context);
        }

        private static async Task RechazarAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
