using AlmaInter.Data;
using AlmaInter.Models.Entities;
using AlmaInter.Models.Enums;
using AlmaInter.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AlmaInter.Controllers
{

    [Authorize(Policy = "SoloAdmin")]
    public class UsuariosController(AppDbContext db, IPasswordHasher<Usuario> hasher) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var usuarios = await db.Usuarios
                .AsNoTracking()
                .OrderByDescending(u => u.Activo)
                .ThenBy(u => u.NombreCompleto)
                .Select(u => new UsuarioListItemViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    NombreCompleto = u.NombreCompleto,
                    Rol = u.Rol,
                    Activo = u.Activo
                })
                .ToListAsync();

            return View(usuarios);
        }

        [HttpGet]
        public IActionResult Crear() => View(new UsuarioCrearViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(UsuarioCrearViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var username = model.Username.Trim();
            var usernameLower = username.ToLowerInvariant();

            var existe = await db.Usuarios.AnyAsync(u => u.Username.ToLower() == usernameLower);
            if (existe)
            {
                ModelState.AddModelError(nameof(model.Username), "Ese nombre de usuario ya existe");
                return View(model);
            }

            var usuario = new Usuario
            {
                Username = username,
                NombreCompleto = model.NombreCompleto.Trim(),
                Rol = model.Rol,
                Activo = true
            };
            usuario.PasswordHash = hasher.HashPassword(usuario, model.Password);

            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            TempData["Mensaje"] = $"Usuario «{usuario.Username}» creado correctamente";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(Guid id)
        {
            var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (usuario is null)
                return NotFound();

            return View(new UsuarioEditarViewModel
            {
                Id = usuario.Id,
                Username = usuario.Username,
                NombreCompleto = usuario.NombreCompleto,
                Rol = usuario.Rol,
                Activo = usuario.Activo
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(UsuarioEditarViewModel model)
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == model.Id);
            if (usuario is null)
                return NotFound();

            var miId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (usuario.Id == miId && (!model.Activo || model.Rol != RolUsuario.Admin))
            {
                ModelState.AddModelError(string.Empty,
                    "No puedes desactivarte ni quitarte el rol de Administrador a ti mismo");
            }

            if (!ModelState.IsValid)
            {
                model.Username = usuario.Username;
                return View(model);
            }

            usuario.NombreCompleto = model.NombreCompleto.Trim();
            usuario.Rol = model.Rol;
            usuario.Activo = model.Activo;

            if (!string.IsNullOrEmpty(model.NuevaPassword))
                usuario.PasswordHash = hasher.HashPassword(usuario, model.NuevaPassword);

            await db.SaveChangesAsync();

            TempData["Mensaje"] = $"Usuario «{usuario.Username}» actualizado correctamente";
            return RedirectToAction(nameof(Index));
        }
    }
}
