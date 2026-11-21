namespace DiarsFinal.ViewModels
{
    namespace DiarsFinal.ViewModels
    {
        public class InformeVM
        {
            public List<ProductoTopVM> TopFinales { get; set; }
            public List<ProductoTopVM> TopPersonalizados { get; set; }
            public List<VentasMesVM> TopMeses { get; set; }
        }

        public class ProductoTopVM
        {
            public string Nombre { get; set; }
            public int Cantidad { get; set; }
        }

        public class VentasMesVM
        {
            public string Mes { get; set; }
            public int Cantidad { get; set; }
        }
    }
}
