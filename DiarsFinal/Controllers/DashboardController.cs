using DiarsFinal.Datos;
using DiarsFinal.ViewModels;
using DiarsFinal.ViewModels.DiarsFinal.ViewModels;
using Microsoft.AspNetCore.Identity;
using DiarsFinal.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DiarsFinal.Controllers
{
    [AdminAuthorize]
    public class DashboardController : Controller
    {
        private readonly PedidoData _pedidoData;

        public DashboardController(
            PedidoData pedidoData)
        {
            _pedidoData = pedidoData;
        }

        public async Task<IActionResult> Index(string seccion = "Pedidos")
        {
            DashboardVM vm = new DashboardVM();

            vm.Pedidos = await _pedidoData.ObtenerPedidosDashboard();

            if (seccion == "Informe")
            {
                vm.Informe = new InformeVM
                {
                    TopFinales = await _pedidoData.TopProductosFinales(),
                    TopPersonalizados = await _pedidoData.TopProductosPersonalizados(),
                    TopMeses = await _pedidoData.TopMesesVentas()
                };
            }

            ViewBag.Seccion = seccion;

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult>
            CambiarEstado(
            int idPedido,
            string estado)
        {
            await _pedidoData
                .CambiarEstado(
                    idPedido,
                    estado);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult>
        BuscarSeguimiento(int codigo)
        {
            DashboardVM vm =
                new DashboardVM();

            vm.Pedidos =
                await _pedidoData
                .ObtenerPedidosDashboard();

            vm.PedidoSeguimiento =
                await _pedidoData
                .ObtenerPorCodigo(codigo);

            ViewBag.Seccion = "Seguimiento";

            if (vm.PedidoSeguimiento == null)
            {
                ViewBag.Error =
                    "Pedido no encontrado";
            }

            return View(
                "Index",
                vm);
        }

        [HttpPost]
        public async Task<IActionResult>
        ConfirmarPago(int idPedido)
        {
            await _pedidoData
                .ConfirmarPago(idPedido);

            return RedirectToAction(
                nameof(Index),
                new { seccion = "Seguimiento" });
        }
    }
}
