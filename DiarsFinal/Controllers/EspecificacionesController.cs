using DiarsFinal.Data;
using DiarsFinal.Models;
using Microsoft.AspNetCore.Mvc;

namespace DiarsFinal.Controllers
{
    public class EspecificacionesController : Controller
    {
        private readonly EspecificacionesData _data;

        public EspecificacionesController(EspecificacionesData data)
        {
            _data = data;
        }

        // INDEX
        public async Task<IActionResult> Index()
        {
            var lista = await _data.Listar();

            return View(lista);
        }

        // GET EDITAR
        public async Task<IActionResult> Editar(int id)
        {
            var especificacion = await _data.BuscarPorId(id);

            if (especificacion == null)
            {
                return NotFound();
            }

            return View(especificacion);
        }

        // POST EDITAR
        [HttpPost]
        public async Task<IActionResult> Editar(Especificacione model)
        {
            await _data.Editar(model);

            return RedirectToAction("Index");
        }
    }
}