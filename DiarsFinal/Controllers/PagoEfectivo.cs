namespace DiarsFinal.Controllers
{
    public class PagoEfectivo : IPasarelaPago
    {
        public string Nombre => "PagoEfectivo";
        public string Tipo => "EFECTIVO";

        public object ObtenerDatosVista(decimal total)
        {
            return new
            {
                CodigoPago = "EF-2026-999999",
                Monto = total
            };
        }
    }
}
