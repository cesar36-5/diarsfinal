namespace DiarsFinal.Controllers
{
    public class PagoYape : IPasarelaPago
    {
        public string Nombre => "Yape";
        public string Tipo => "YAPE";

        public object ObtenerDatosVista(decimal total)
        {
            return new
            {
                QrUrl = "/imagenes/yape-qr.png", // fijo
                Monto = total
            };
        }
    }
}
