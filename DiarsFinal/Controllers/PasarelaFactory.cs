namespace DiarsFinal.Controllers
{
    public class PasarelaFactory
    {
        public static IPasarelaPago Crear(string metodo)
        {
            return metodo switch
            {
                "yape" => new PagoYape(),
                "tarjeta" => new PagoTarjeta(),
                "efectivo" => new PagoEfectivo(),
                _ => throw new Exception("Método no válido")
            };
        }
    }
}
