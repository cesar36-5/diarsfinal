using NUnit.Framework;
using DiarsFinal.Controllers;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestConfiguracion
    {
        // =========================================
        // OBTENER INSTANCIA
        // =========================================

        [Test]
        public void ObtenerInstancia_PrimeraLlamada_DevuelveInstancia()
        {
            // Arrange
            // La prueba presupone que todavía no se ha solicitado
            // una instancia de Configuracion en este proceso.

            // Act
            var resultado = Configuracion.ObtenerInstancia();

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.InstanceOf<Configuracion>());
        }

        [Test]
        public void ObtenerInstancia_LlamadaPosterior_DevuelveMismaInstancia()
        {
            // Act
            var primera = Configuracion.ObtenerInstancia();
            var segunda = Configuracion.ObtenerInstancia();

            // Assert
            Assert.That(primera, Is.Not.Null);
            Assert.That(segunda, Is.SameAs(primera));
        }

        // =========================================
        // OBTENER ESTADO
        // =========================================

        [TestCase(0, "En proceso")]
        [TestCase(1, "En espera")]
        [TestCase(2, "Cancelado")]
        [TestCase(3, "En preparación")]
        [TestCase(4, "Listo")]
        [TestCase(5, "Recogido")]
        public void ObtenerEstado_EstadoValido_DevuelveDescripcion(
            int estado,
            string descripcionEsperada)
        {
            // Arrange
            var configuracion = ObtenerConfiguracionParaPrueba();

            // Act
            string resultado = configuracion.ObtenerEstado(estado);

            // Assert
            Assert.That(resultado, Is.EqualTo(descripcionEsperada));
        }

        [TestCase(-1)]
        [TestCase(6)]
        [TestCase(99)]
        public void ObtenerEstado_EstadoInvalido_DevuelveEstadoNoHallado(
            int estado)
        {
            // Arrange
            var configuracion = ObtenerConfiguracionParaPrueba();

            // Act
            string resultado = configuracion.ObtenerEstado(estado);

            // Assert
            Assert.That(resultado, Is.EqualTo("Estado no hallado"));
        }

        // =========================================
        // MÉTODO AUXILIAR
        // =========================================

        private static Configuracion ObtenerConfiguracionParaPrueba()
        {
            var configuracion = Configuracion.ObtenerInstancia();

            if (configuracion != null)
                return configuracion;

            throw new InvalidOperationException(
                "No se puede recuperar la instancia existente de Configuracion.");
        }
    }
}