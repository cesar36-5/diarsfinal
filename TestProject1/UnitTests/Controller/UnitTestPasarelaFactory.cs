using NUnit.Framework;
using DiarsFinal.Controllers;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestPasarelaFactory
    {
        // =========================================
        // FACTORY: CREAR
        // =========================================

        [Test]
        public void Crear_MetodoYape_DevuelvePagoYape()
        {
            // Act
            var resultado = PasarelaFactory.Crear("yape");

            // Assert
            Assert.That(resultado, Is.InstanceOf<PagoYape>());
            Assert.That(resultado.Nombre, Is.EqualTo("Yape"));
            Assert.That(resultado.Tipo, Is.EqualTo("YAPE"));
        }

        [Test]
        public void Crear_MetodoTarjeta_DevuelvePagoTarjeta()
        {
            // Act
            var resultado = PasarelaFactory.Crear("tarjeta");

            // Assert
            Assert.That(resultado, Is.InstanceOf<PagoTarjeta>());
            Assert.That(resultado.Nombre, Is.EqualTo("Tarjeta"));
            Assert.That(resultado.Tipo, Is.EqualTo("TARJETA"));
        }

        [Test]
        public void Crear_MetodoEfectivo_DevuelvePagoEfectivo()
        {
            // Act
            var resultado = PasarelaFactory.Crear("efectivo");

            // Assert
            Assert.That(resultado, Is.InstanceOf<PagoEfectivo>());
            Assert.That(resultado.Nombre, Is.EqualTo("PagoEfectivo"));
            Assert.That(resultado.Tipo, Is.EqualTo("EFECTIVO"));
        }

        [TestCase("bitcoin")]
        [TestCase("transferencia")]
        [TestCase("")]
        [TestCase("YAPE")]
        public void Crear_MetodoNoValido_LanzaExcepcion(string metodo)
        {
            // Act & Assert
            var excepcion = Assert.Throws<System.Exception>(
                () => PasarelaFactory.Crear(metodo));

            Assert.That(
                excepcion!.Message,
                Is.EqualTo("Método no válido"));
        }

        // =========================================
        // PAGO YAPE
        // =========================================

        [Test]
        public void PagoYape_ObtenerDatosVista_DevuelveQrYMonto()
        {
            // Arrange
            var pago = new PagoYape();
            decimal total = 150.50m;

            // Act
            var resultado = pago.ObtenerDatosVista(total);

            // Assert
            Assert.That(resultado, Is.Not.Null);

            var tipo = resultado.GetType();

            var qrUrl = tipo.GetProperty("QrUrl")!
                .GetValue(resultado);

            var monto = tipo.GetProperty("Monto")!
                .GetValue(resultado);

            Assert.That(qrUrl, Is.EqualTo("/imagenes/yape-qr.png"));
            Assert.That(monto, Is.EqualTo(total));
        }

        // =========================================
        // PAGO TARJETA
        // =========================================

        [Test]
        public void PagoTarjeta_ObtenerDatosVista_DevuelveMonto()
        {
            // Arrange
            var pago = new PagoTarjeta();
            decimal total = 250.75m;

            // Act
            var resultado = pago.ObtenerDatosVista(total);

            // Assert
            Assert.That(resultado, Is.Not.Null);

            var monto = resultado.GetType()
                .GetProperty("Monto")!
                .GetValue(resultado);

            Assert.That(monto, Is.EqualTo(total));
        }

        // =========================================
        // PAGO EFECTIVO
        // =========================================

        [Test]
        public void PagoEfectivo_ObtenerDatosVista_DevuelveCodigoYMonto()
        {
            // Arrange
            var pago = new PagoEfectivo();
            decimal total = 80.00m;

            // Act
            var resultado = pago.ObtenerDatosVista(total);

            // Assert
            Assert.That(resultado, Is.Not.Null);

            var tipo = resultado.GetType();

            var codigoPago = tipo.GetProperty("CodigoPago")!
                .GetValue(resultado);

            var monto = tipo.GetProperty("Monto")!
                .GetValue(resultado);

            Assert.That(
                codigoPago,
                Is.EqualTo("EF-2026-999999"));

            Assert.That(monto, Is.EqualTo(total));
        }

        // =========================================
        // PRUEBAS DE VALORES LÍMITE
        // =========================================

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(9999)]
        public void ObtenerDatosVista_MontoPositivoOZero_ConservaElMonto(
            decimal total)
        {
            // Arrange
            IPasarelaPago[] pasarelas =
            {
                new PagoYape(),
                new PagoTarjeta(),
                new PagoEfectivo()
            };

            // Act & Assert
            foreach (var pasarela in pasarelas)
            {
                var resultado = pasarela.ObtenerDatosVista(total);

                var monto = resultado.GetType()
                    .GetProperty("Monto")!
                    .GetValue(resultado);

                Assert.That(
                    monto,
                    Is.EqualTo(total),
                    $"Falló la pasarela {pasarela.Nombre}");
            }
        }
    }
}