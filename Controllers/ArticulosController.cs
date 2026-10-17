using AlmaInter.Data;
using AlmaInter.Models.Entities;
using AlmaInter.Models.Enums;
using AlmaInter.Models.ViewModels;
using AlmaInter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace AlmaInter.Controllers
{
    [Authorize]
    public class ArticulosController(AppDbContext db, IStorageService storage) : Controller
    {
        private const long TamanoMaximoFoto = 2 * 1024 * 1024; // 2 MB
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
            ValidarTamanoFoto(model.Foto);

            string? urlImagen = null;
            if (ModelState.IsValid && model.Foto is { Length: > 0 })
            {
                try
                {
                    urlImagen = await storage.SubirImagenAsync(model.Foto);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(model.Foto), ex.Message);
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            var articulo = new Articulo
            {
                Nombre = model.Nombre.Trim(),
                UbicacionCasa = model.UbicacionCasa.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim(),
                CantidadCasa = model.CantidadInicial,
                ImagenUrl = urlImagen,
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

            try
            {
                await db.SaveChangesAsync();
            }
            catch
            {
                await storage.EliminarAsync(urlImagen); // no dejar una foto huérfana
                throw;
            }

            TempData["Mensaje"] = $"Artículo «{articulo.Nombre}» creado correctamente";
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
                ImagenUrl = articulo.ImagenUrl,
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

            ValidarTamanoFoto(model.Foto);

            string? urlNueva = null;
            if (ModelState.IsValid && model.Foto is { Length: > 0 })
            {
                try
                {
                    urlNueva = await storage.SubirImagenAsync(model.Foto);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(model.Foto), ex.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                model.ImagenUrl = articulo.ImagenUrl;
                model.CantidadCasa = articulo.CantidadCasa;
                model.TieneMovimientos = await db.Movimientos.AnyAsync(m => m.ArticuloId == model.Id);
                return View(model);
            }

            var urlAnterior = articulo.ImagenUrl;

            articulo.Nombre = model.Nombre.Trim();
            articulo.UbicacionCasa = model.UbicacionCasa.Trim();
            articulo.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim();
            articulo.Activo = model.Activo;

            if (urlNueva is not null)
                articulo.ImagenUrl = urlNueva;
            else if (model.QuitarFoto)
                articulo.ImagenUrl = null;

            try
            {
                await db.SaveChangesAsync();
            }
            catch
            {
                await storage.EliminarAsync(urlNueva);
                throw;
            }

            // Ya guardado: se borra la foto anterior si cambió
            if (urlAnterior != articulo.ImagenUrl)
                await storage.EliminarAsync(urlAnterior);

            TempData["Mensaje"] = $"Artículo «{articulo.Nombre}» actualizado correctamente";
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

            var urlImagen = articulo.ImagenUrl;

            db.Articulos.Remove(articulo);
            await db.SaveChangesAsync();
            await storage.EliminarAsync(urlImagen);

            TempData["Mensaje"] = $"Artículo «{articulo.Nombre}» eliminado";
            return RedirectToAction(nameof(Index));
        }

        private void ValidarTamanoFoto(IFormFile? foto)
        {
            if (foto is { Length: > TamanoMaximoFoto })
                ModelState.AddModelError("Foto", "La foto no puede pesar más de 2 MB");
        }

        private Guid ObtenerUsuarioId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    }
}
