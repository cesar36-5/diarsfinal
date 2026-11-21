namespace DiarsFinal.Models
{
    public partial class Compatibilidad
    {
        public int IdProductoA { get; set; }

        public int IdProductoB { get; set; }

        public virtual Producto ProductoA { get; set; } = null!;

        public virtual Producto ProductoB { get; set; } = null!;
    }
}
