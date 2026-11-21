using DiarsFinal.Models;
using DiarsFinal.ViewModels.DiarsFinal.ViewModels;

namespace DiarsFinal.ViewModels
{
    public class DashboardVM
    {
        public List<DashboardPedidoVM> Pedidos { get; set; }
            = new();

        public Pedido? PedidoSeguimiento { get; set; }

        public InformeVM Informe { get; set; }
    }
}
