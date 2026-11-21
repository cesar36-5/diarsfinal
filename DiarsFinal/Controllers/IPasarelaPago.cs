namespace DiarsFinal.Controllers
{
    public interface IPasarelaPago
    {
        string Nombre { get; }

        string Tipo { get; } // "YAPE", "TARJETA", "EFECTIVO"

        object ObtenerDatosVista(decimal total);
    }
}
