using AlmaInter.Data;
using AlmaInter.Models.Entities;
using AlmaInter.Models.Enums;
using AlmaInter.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace AlmaInter.Controllers
{
    [Authorize]
    public class ArticulosController(AppDbContext db) : Controller
    {
        public async Task<IActionResult> Index(string? q)
        {
            var query = db.Articulos.AsNoTracking().AsQueryable();

            // El técnico solo ve artículos activos
            if (!User.IsInRole("Admin"))
                query = query.Where(a => a.Activo);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var patron = $"%{q.Trim()}%";
                query = query.Where(a =>
                    EF.Functions.ILike(a.Nombre, patron) ||
                    EF.Functions.ILike(a.UbicacionCasa, patron) ||
                    (a.Descripcion != null && EF.Functions.ILike(a.Descripcion, patron)));
            }

            var articulos = await query
                .OrderBy(a => a.Nombre)
                .Select(a => new ArticuloListItemViewModel
                {
                    Id = a.Id,
                    Nombre = a.Nombre,
                    UbicacionCasa = a.UbicacionCasa,
                    Descripcion = a.Descripcion,
                    CantidadCasa = a.CantidadCasa,
                    ImagenUrl = a.ImagenUrl,
                    Activo = a.Activo
                })
                .ToListAsync();

            ViewData["q"] = q;
            return View(articulos);
        }

        [Authorize(Policy = "SoloAdmin")]
        [HttpGet]
        public IActionResult Crear() => View(new ArticuloCrearViewModel());

        [Authorize(Policy = "SoloAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ArticuloCrearViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var articulo = new Articulo
            {
                Nombre = model.Nombre.Trim(),
                UbicacionCasa = model.UbicacionCasa.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim(),
                CantidadCasa = model.CantidadInicial,
                Activo = true
            };
            db.Articulos.Add(articulo);

            // El stock inicial queda en el historial como una Entrada (misma transacción)
            if (model.CantidadInicial > 0)
            {
                db.Movimientos.Add(new Movimiento
                {
                    Articulo = articulo,
                    UsuarioId = ObtenerUsuarioId(),
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = model.CantidadInicial
                });
            }

            await db.SaveChangesAsync();

            TempData["Mensaje"] = $"Artículo {articulo.Nombre} creado correctamente";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "SoloAdmin")]
        [HttpGet]
        public async Task<IActionResult> Editar(Guid id)
        {
            var articulo = await db.Articulos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (articulo is null)
                return NotFound();

            return View(new ArticuloEditarViewModel
            {
                Id = articulo.Id,
                Nombre = articulo.Nombre,
                UbicacionCasa = articulo.UbicacionCasa,
                Descripcion = articulo.Descripcion,
                Activo = articulo.Activo,
                CantidadCasa = articulo.CantidadCasa,
                TieneMovimientos = await db.Movimientos.AnyAsync(m => m.ArticuloId == id)
            });
        }

        [Authorize(Policy = "SoloAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(ArticuloEditarViewModel model)
        {
            var articulo = await db.Articulos.FirstOrDefaultAsync(a => a.Id == model.Id);
            if (articulo is null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                model.CantidadCasa = articulo.CantidadCasa;
                model.TieneMovimientos = await db.Movimientos.AnyAsync(m => m.ArticuloId == model.Id);
                return View(model);
            }

            articulo.Nombre = model.Nombre.Trim();
            articulo.UbicacionCasa = model.UbicacionCasa.Trim();
            articulo.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim();
            articulo.Activo = model.Activo;

            await db.SaveChangesAsync();

            TempData["Mensaje"] = $"Artículo {articulo.Nombre} actualizado correctamente";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "SoloAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            var articulo = await db.Articulos.FirstOrDefaultAsync(a => a.Id == id);
            if (articulo is null)
                return NotFound();

            if (await db.Movimientos.AnyAsync(m => m.ArticuloId == id))
            {
                TempData["Error"] = "No se puede eliminar un artículo con movimientos. Desactívalo en su lugar.";
                return RedirectToAction(nameof(Editar), new { id });
            }

            db.Articulos.Remove(articulo);
            await db.SaveChangesAsync();

            TempData["Mensaje"] = $"Artículo {articulo.Nombre} eliminado";
            return RedirectToAction(nameof(Index));
        }

        private Guid ObtenerUsuarioId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
