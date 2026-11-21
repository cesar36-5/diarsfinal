using System;
using System.Collections.Generic;

namespace DiarsFinal.Models;

public partial class DetallePersonalizado
{
    public int IdPedido { get; set; }

    public int IdProducto { get; set; }

    public string? Comentario { get; set; }

    public int Posicion { get; set; }

    public virtual Pedido IdPedidoNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
