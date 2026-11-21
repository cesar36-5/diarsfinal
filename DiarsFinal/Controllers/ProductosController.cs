using DiarsFinal.Datos;
using DiarsFinal.Helpers;
using DiarsFinal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using static System.Net.Mime.MediaTypeNames;

namespace DiarsFinal.Controllers
{
    [AdminAuthorize]
    public class ProductosController : Controller
    {
        private readonly ProductoData _productoData;
        private readonly CategoriaData _categoriaData;

        public ProductosController(
        ProductoData p,
        CategoriaData c)
        {
            _productoData = p;
            _categoriaData = c;
        }

        // LISTAR
        public async Task<IActionResult> Index()
        {
            var productos = await _productoData.ListarProductos();

            ViewBag.Categorias =
                await _categoriaData.ListarCategorias();

            return View(productos);
        }


        // POST CREAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Producto producto, IFormFile? imagen)
        {
            ModelState.Remove("IdCategoriaNavigation");

            producto.Activo = producto.Activo == "1"
                ? "1"
                : "0";

            Console.WriteLine(ModelState.IsValid);

            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine(error.ErrorMessage);
            }

            if (ModelState.IsValid)
            {
                var ruta = await GuardarImagen(imagen);

                if (ruta != null)
                {
                    producto.RutaImagen = ruta;
                }
                else
                {
                    producto.RutaImagen = "no-image.png";
                }

                await _productoData.AgregarProducto(producto);

                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Error en los datos del producto";
            return RedirectToAction(nameof(Index));
        }

        // GET EDITAR
        public async Task<IActionResult> Editar(int id)
        {

            var producto = await _productoData.ObtenerProducto(id);

            if (producto == null)
                return NotFound();

            ViewBag.Categorias = await _categoriaData.ListarCategorias();

            return View(producto);
        }

        // POST EDITAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Producto producto, IFormFile imagen)
        {
            Console.WriteLine("FILE NAME: " + imagen?.FileName);
            Console.WriteLine("FILE SIZE: " + imagen?.Length);

            Console.WriteLine("Request.Form files: " + Request.Form.Files.Count);

            var productoDB = await _productoData.ObtenerProducto(producto.IdProducto);

            if (productoDB == null)
                return NotFound();

            productoDB.Nombre = producto.Nombre;
            productoDB.Descripcion = producto.Descripcion;
            productoDB.Precio = producto.Precio;
            productoDB.Consumo = producto.Consumo;
            productoDB.Tipo = producto.Tipo;
            productoDB.IdCategoria = producto.IdCategoria;
            productoDB.Activo = producto.Activo;

            if (imagen != null && imagen.Length > 0)
            {
                var ruta = await GuardarImagen(imagen);

                Console.WriteLine("RUTA GENERADA: " + ruta);

                productoDB.RutaImagen = ruta;
            }

            await _productoData.EditarProducto(productoDB);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditarC(int id)
        {
            var producto = await _productoData.ObtenerProducto(id);

            if (producto == null)
            {
                return NotFound();
            }

            ViewBag.Categorias =
                await _categoriaData.ListarCategorias();

            ViewBag.ProductosCompatibles =
                await _productoData.ListarProductosFinales();

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarC(Producto producto, IFormFile imagen)
        {
            if (string.IsNullOrEmpty(producto.Descripcion))
            {
                producto.Descripcion = "";

                ModelState.Remove("Descripcion");
            }

            ModelState.Remove("IdCategoriaNavigation");
            ModelState.Remove("RutaImagen");

            producto.Activo = producto.Activo == "1"
                ? "1"
                : "0";

            if (ModelState.IsValid)
            {

                if (imagen != null && imagen.Length > 0)
                {
                    var ruta = await GuardarImagen(imagen);

                    if (ruta != null)
                    {
                        producto.RutaImagen = ruta;
                    }
                }
                await _productoData.EditarProducto(producto);

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categorias =
                await _categoriaData.ListarCategorias();

            ViewBag.ProductosCompatibles =
                await _productoData.ListarProductosFinales();

            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarCompatibilidad(
        int idProductoA,
        int idProductoB)
            {
                await _productoData.AgregarCompatibilidad(
                    idProductoA,
                    idProductoB);

                return RedirectToAction(
                    nameof(EditarC),
                    new { id = idProductoA });
            }

        [HttpPost]
        public async Task<IActionResult> EliminarCompatibilidad(
            int idProductoA,
            int idProductoB)
        {
            await _productoData.EliminarCompatibilidad(
                idProductoA,
                idProductoB);

            return RedirectToAction(
                nameof(EditarC),
                new { id = idProductoA });
        }

        // ELIMINAR
        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                bool resultado = await _productoData.EliminarProducto(id);

                if (resultado)
                {
                    var productosActualizados = await _productoData.ListarProductos();
                    return PartialView("_ProductosTable", productosActualizados);
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Producto no encontrado o no se pudo eliminar."
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            await _productoData.CambiarEstado(id);

            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> GuardarImagen(IFormFile imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return null;

            Console.WriteLine($"Guardando: {imagen.FileName}");

            var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes", "productos");

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            var nombreArchivo = Guid.NewGuid() + Path.GetExtension(imagen.FileName);

            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            Console.WriteLine("Ruta física: " + rutaFisica);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await imagen.CopyToAsync(stream);
            }

            return "/imagenes/productos/" + nombreArchivo;
        }
    }
}