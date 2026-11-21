using System;
using System.Collections.Generic;

namespace DiarsFinal.Models;

public partial class Pedido
{
    public int IdPedido { get; set; }

    public int Codigo { get; set; }

    public string Dni { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaEntrega { get; set; }

    public string Tipo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public decimal Precio { get; set; }

    public string? Comentario { get; set; }

    public string Telefono { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public virtual ICollection<DetallePedido> DetallePedidos { get; set; } = new List<DetallePedido>();

    public virtual ICollection<DetallePersonalizado> DetallePersonalizados { get; set; } = new List<DetallePersonalizado>();
}
