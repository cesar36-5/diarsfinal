using System;
using System.Collections.Generic;

namespace DiarsFinal.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public int IdCategoria { get; set; }

    public decimal Precio { get; set; }

    public string Descripcion { get; set; } = null!;

    public string? Nombre { get; set; }

    public int Consumo { get; set; }

    public string Tipo { get; set; } = null!;

    public string? RutaImagen { get; set; } = null!;

    public string Activo { get; set; } = null!;

    public virtual ICollection<DetallePedido> DetallePedidos { get; set; } = new List<DetallePedido>();

    public virtual ICollection<DetallePersonalizado> DetallePersonalizados { get; set; } = new List<DetallePersonalizado>();

    public virtual Categorium IdCategoriaNavigation { get; set; } = null!;

    public virtual ICollection<Compatibilidad> Compatibilidades { get; set; }
    = new List<Compatibilidad>();
}
