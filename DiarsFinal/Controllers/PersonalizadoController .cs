using DiarsFinal.Data;
using DiarsFinal.Datos;
using DiarsFinal.Models;
using DiarsFinal.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DiarsFinal.Controllers
{
    public class PersonalizadoController : Controller
    {
        private readonly PersonalizadoData _personalizadoData;
        private readonly PedidoData _pedidoData;

        private readonly IEnumerable<IPasarelaPago> _pasarelas;

        public PersonalizadoController(
            PersonalizadoData personalizadoData,
            PedidoData pedidodata,
            IEnumerable<IPasarelaPago> pasarelas)
        {
            _personalizadoData = personalizadoData;
            _pedidoData = pedidodata;
            _pasarelas = pasarelas;
        }

        public async Task<IActionResult> Index()
        {
            PedidoPersonalizadoVM vm =
                new PedidoPersonalizadoVM();

            vm.Bases =
                await _personalizadoData.ObtenerBases();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerCompatibles(int idProducto)
        {
            var productos =
                await _personalizadoData
                .ObtenerCompatibles(idProducto);

            Console.WriteLine(
                $"Compatibles para {idProducto}: {productos.Count}");

            return Json(
                productos.Select(p => new
                {
                    idProducto = p.IdProducto,
                    nombre = p.Nombre,
                    precio = p.Precio
                }));
        }

        [HttpPost]
        public async Task<IActionResult> GenerarPedido(
    PedidoPersonalizadoVM vm,
    string metodoPago)
        {
            if (vm.Selecciones == null ||
                vm.Selecciones.Count == 0)
            {
                TempData["Error"] =
                    "No hay productos seleccionados";

                return RedirectToAction(nameof(Index));
            }

            var resultado =
                await _pedidoData
                    .GenerarPedidoPersonalizado(vm);

            if (resultado.Codigo == -1)
            {
                TempData["Error"] =
                    "No hay capacidad suficiente para esa fecha.";

                return RedirectToAction(nameof(Index));
            }

            var pasarela =
                _pasarelas.FirstOrDefault(
                    p => p.Tipo == metodoPago);

            if (pasarela == null)
            {
                TempData["Error"] =
                    "Método de pago inválido";

                return RedirectToAction(nameof(Index));
            }

            var datosPago =
                pasarela.ObtenerDatosVista(
                    resultado.Total);

            TempData["Correcto"] =
                $"Pedido generado correctamente. Código: {resultado.Codigo}";

            return View("Pago", new PagoViewModel
            {
                Metodo = metodoPago,
                Total = resultado.Total,
                CodigoPedido = resultado.Codigo,
                Datos = datosPago
            });
        }

        [HttpPost]
        public IActionResult ConfirmarPagoTarjeta(int codigoPedido)
        {

            TempData["Correcto"] =
                $"Pago con tarjeta confirmado. Pedido #{codigoPedido}";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmarPagoYape(int codigoPedido)
        {
            TempData["Correcto"] =
                $"Pago con Yape confirmado. Pedido #{codigoPedido}";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmarPagoEfectivo(int codigoPedido)
        {
            TempData["Correcto"] =
                $"Pedido #{codigoPedido} confirmado para pago en efectivo.";

            return RedirectToAction(nameof(Index));
        }
    }
}
