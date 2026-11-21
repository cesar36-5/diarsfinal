using DiarsFinal.Datos;
using DiarsFinal.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DiarsFinal.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly ProductoData _productoData;

        public CatalogoController(ProductoData productoData)
        {
            _productoData = productoData;
        }

        // MOSTRAR CATÁLOGO
        public async Task<IActionResult> Index()
        {
            //HttpContext.Session.Clear();
            var productos =
                await _productoData.ListarProductosCatalogo();

            return View(productos);
        }

        // AGREGAR AL CARRITO
        [HttpPost]
        public IActionResult AgregarCarrito(
            int idProducto,
            int cantidad)
        {
            List<CarritoItem> carrito;

            string? carritoSession =
                HttpContext.Session.GetString("Carrito");

            if (carritoSession == null)
            {
                carrito = new List<CarritoItem>();
            }
            else
            {
                carrito = JsonSerializer.Deserialize<List<CarritoItem>>
                    (carritoSession)!;
            }

            var itemExistente =
                carrito.FirstOrDefault(x =>
                    x.IdProducto == idProducto);

            if (itemExistente != null)
            {
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                carrito.Add(new CarritoItem
                {
                    IdProducto = idProducto,
                    Cantidad = cantidad
                });
            }

            HttpContext.Session.SetString(
                "Carrito",
                JsonSerializer.Serialize(carrito));

            return RedirectToAction(nameof(Index));
        }

        // VER PEDIDO
        public async Task<IActionResult> Pedido()
        {
            List<CarritoItem> carrito;

            string? carritoSession =
                HttpContext.Session.GetString("Carrito");

            if (carritoSession == null)
            {
                carrito = new List<CarritoItem>();
            }
            else
            {
                carrito = JsonSerializer.Deserialize<List<CarritoItem>>
                    (carritoSession)!;
            }

            List<(Producto producto, int cantidad)> pedido =
                new List<(Producto, int)>();

            foreach (var item in carrito)
            {
                var producto =
                    await _productoData.ObtenerProducto(
                        item.IdProducto);

                if (producto != null)
                {
                    pedido.Add((producto, item.Cantidad));
                }
            }

            return View(pedido);
        }
    }
}