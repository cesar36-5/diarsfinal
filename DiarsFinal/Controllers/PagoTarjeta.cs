namespace DiarsFinal.Controllers
{
    public class PagoTarjeta : IPasarelaPago
    {
        public string Nombre => "Tarjeta";
        public string Tipo => "TARJETA";

        public object ObtenerDatosVista(decimal total)
        {
            return new
            {
                Monto = total
            };
        }
    }
}
