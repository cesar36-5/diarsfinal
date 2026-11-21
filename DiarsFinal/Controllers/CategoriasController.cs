using DiarsFinal.Datos;
using DiarsFinal.Helpers;
using DiarsFinal.Models;
using Microsoft.AspNetCore.Mvc;

namespace DiarsFinal.Controllers
{
    [AdminAuthorize]
    public class CategoriasController : Controller
    {
        private readonly CategoriaData _categoriaData;

        public CategoriasController(CategoriaData categoriaData)
        {
            _categoriaData = categoriaData;
        }

        // INDEX
        public async Task<IActionResult> Index()
        {
            var categorias = await _categoriaData.ListarCategorias();

            return View(categorias);
        }

        // CREAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Categorium categoria)
        {
            if (ModelState.IsValid)
            {
                await _categoriaData.AgregarCategoria(categoria);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET EDITAR
        public async Task<IActionResult> Editar(int id)
        {
            var categoria = await _categoriaData.ObtenerCategoria(id);

            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // POST EDITAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Categorium categoria)
        {
            if (ModelState.IsValid)
            {
                await _categoriaData.EditarCategoria(categoria);

                return RedirectToAction(nameof(Index));
            }

            return View(categoria);
        }

        // ELIMINAR
        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _categoriaData.EliminarCategoria(id);

            return RedirectToAction(nameof(Index));
        }
    }
}