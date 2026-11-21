namespace DiarsFinal.ViewModels
{
    public class PagoViewModel
    {
        public string Metodo { get; set; }
        public decimal Total { get; set; }
        public int CodigoPedido { get; set; }
        public dynamic Datos { get; set; }
    }
}
