using DiarsFinal.Datos;
using DiarsFinal.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DiarsFinal.Controllers
{
    public class SeguimientoController : Controller
    {
        private readonly PedidoData _pedidoData;

        public SeguimientoController(PedidoData pedidoData)
        {
            _pedidoData = pedidoData;
        }

        public IActionResult Index()
        {
            return View(new SeguimientoVM());
        }

        [HttpPost]
        public async Task<IActionResult> Buscar(int codigo)
        {
            SeguimientoVM vm = new SeguimientoVM();

            vm.Pedido =
                await _pedidoData.ObtenerPorCodigo(codigo);

            if (vm.Pedido == null)
            {
                vm.Error = "Pedido no encontrado";
            }

            return View("Index", vm);
        }
    }
}