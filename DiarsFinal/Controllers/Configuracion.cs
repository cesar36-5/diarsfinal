namespace DiarsFinal.Controllers
{
    public class Configuracion
    {
        private static Configuracion? _instancia;

        private static bool _instanciado = false;

        private Configuracion()
        {
        }

        public static Configuracion? ObtenerInstancia()
        {
            if (_instancia == null)
            {
                _instancia = new Configuracion();
                _instanciado = true;
            }

            return _instancia;
        }

        public string ObtenerEstado(int estado)
        {
            switch (estado)
            {
                case 0:
                    return "En proceso";

                case 1:
                    return "En espera";

                case 2:
                    return "Cancelado";

                case 3:
                    return "En preparación";

                case 4:
                    return "Listo";

                case 5:
                    return "Recogido";

                default:
                    return "Estado no hallado";
            }
        }
    }
}