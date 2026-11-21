using DiarsFinal.Datos;
using DiarsFinal.Models;
using DiarsFinal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using DiarsFinal.Helpers;

namespace DiarsFinal.Controllers
{
    public class PedidoController : Controller
    {
        private readonly ProductoData _productoData;
        private readonly PedidoData _pedidoData;
        private readonly IEnumerable<IPasarelaPago> _pasarelas;

        public PedidoController(
            ProductoData productoData,
            PedidoData pedidoData,
            IEnumerable<IPasarelaPago> pasarelas)
        {
            _productoData = productoData;
            _pedidoData = pedidoData;
            _pasarelas = pasarelas;
        }

        public async Task<IActionResult> Index()
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
                try
                {
                    carrito =
                        JsonSerializer.Deserialize<List<CarritoItem>>
                        (carritoSession)!;
                }
                catch
                {
                    carrito = new List<CarritoItem>();
                }
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

        [HttpPost]
        public async Task<IActionResult> GenerarPedido(
    string nombre,
    string dni,
    string? comentario,
    string telefono,
    DateTime fechaEntrega,
    string metodoPago)
        {
            var pasarela = _pasarelas.FirstOrDefault(p => p.Tipo == metodoPago);

            if (pasarela == null)
            {
                TempData["Error"] = "Método de pago inválido";
                return RedirectToAction(nameof(Index));
            }

            string? carritoSession = HttpContext.Session.GetString("Carrito");

            if (carritoSession == null)
            {
                TempData["Error"] = "Carrito vacío";
                return RedirectToAction(nameof(Index));
            }

            List<CarritoItem> carrito =
                JsonSerializer.Deserialize<List<CarritoItem>>(carritoSession)
                ?? new List<CarritoItem>();

            decimal total = 0;

            foreach (var item in carrito)
            {
                var producto = await _productoData.ObtenerProducto(item.IdProducto);

                if (producto != null)
                    total += producto.Precio * item.Cantidad;
            }

            var datosPago = pasarela.ObtenerDatosVista(total);

            int? codigo = await _pedidoData.GenerarPedido(
                carrito,
                nombre,
                comentario,
                dni,
                telefono,
                fechaEntrega);

            if (codigo == -1 || codigo == null)
            {
                TempData["Error"] = "No hay capacidad suficiente para esa fecha.";
                return RedirectToAction(nameof(Index));
            }

            return View("Pago", new PagoViewModel
            {
                Metodo = metodoPago,
                Total = total,
                CodigoPedido = codigo.Value,
                Datos = datosPago
            });
        }

        [HttpPost]
        public IActionResult ConfirmarPagoTarjeta(int codigoPedido)
        {
            TempData["Correcto"] = $"Pago con tarjeta realizado. Pedido #{codigoPedido}";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ModificarCantidad(
    int idProducto,
    int cantidad)
        {
            string? carritoSession =
                HttpContext.Session.GetString("Carrito");

            List<CarritoItem> carrito;

            if (carritoSession == null)
            {
                carrito = new List<CarritoItem>();
            }
            else
            {
                carrito =
                    JsonSerializer.Deserialize<List<CarritoItem>>
                    (carritoSession)!;
            }

            var item =
                carrito.FirstOrDefault(x =>
                    x.IdProducto == idProducto);

            if (item != null)
            {
                item.Cantidad += cantidad;

                if (item.Cantidad <= 0)
                {
                    carrito.Remove(item);
                }
            }

            HttpContext.Session.SetString(
                "Carrito",
                JsonSerializer.Serialize(carrito));

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult EliminarProducto(int idProducto)
        {
            string? carritoSession =
                HttpContext.Session.GetString("Carrito");

            List<CarritoItem> carrito;

            if (carritoSession == null)
            {
                carrito = new List<CarritoItem>();
            }
            else
            {
                carrito =
                    JsonSerializer.Deserialize<List<CarritoItem>>
                    (carritoSession)!;
            }

            var item =
                carrito.FirstOrDefault(x =>
                    x.IdProducto == idProducto);

            if (item != null)
            {
                carrito.Remove(item);
            }

            HttpContext.Session.SetString(
                "Carrito",
                JsonSerializer.Serialize(carrito));

            return RedirectToAction(nameof(Index));
        }
    }
}