using DiarsFinal.Models;

namespace DiarsFinal.ViewModels
{
    public class PedidoPersonalizadoVM
    {
        public List<Producto> Bases { get; set; }
            = new();

        public List<SeleccionPersonalizadaVM> Selecciones { get; set; }
            = new();

        public string Nombre { get; set; } = "";

        public string Dni { get; set; } = "";

        public string Telefono { get; set; } = "";

        public DateTime FechaEntrega { get; set; }
        public string? Comentario { get; set; }
    }
}
