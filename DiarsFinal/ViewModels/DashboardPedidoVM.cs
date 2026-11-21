using DiarsFinal.Models;

namespace DiarsFinal.ViewModels
{
    public class DashboardPedidoVM
    {
        public Pedido Pedido { get; set; } = null!;

        public List<DetallePedido> DetallesPedido { get; set; }
            = new();

        public List<DetallePersonalizado> DetallesPersonalizados { get; set; }
            = new();
    }
}