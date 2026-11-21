using System;
using System.Collections.Generic;

namespace DiarsFinal.Models;

public partial class DetallePedido
{
    public int IdProducto { get; set; }

    public int IdPedido { get; set; }

    public int Cantidad { get; set; }

    public virtual Pedido IdPedidoNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
