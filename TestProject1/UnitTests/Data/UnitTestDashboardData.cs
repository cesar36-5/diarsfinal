using DiarsFinal.Data;
using DiarsFinal.Datos;
using Moq;
using NUnit.Framework;
using DiarsFinal.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace TestProject1.UnitTests.Data
{
    [TestFixture]
    public class UnitTestDashboardData
    {
        private Mock<ISqlQueryExecutor> _mockExecutor = null!;
        private DashboardData _dashboardData = null!;

        [SetUp]
        public void Setup()
        {
            _mockExecutor = new Mock<ISqlQueryExecutor>();

            _dashboardData = new DashboardData(
                _mockExecutor.Object);
        }

        [Test]
        public async Task TopProductosFinales_ConResultados_DevuelveProductos()
        {
            // Arrange
            var tabla = CrearTablaProductos();
            tabla.Rows.Add("Laptop", 15);
            tabla.Rows.Add("Mouse", 10);
            tabla.Rows.Add("Teclado", 5);

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            var resultado =
                await _dashboardData.TopProductosFinales(3);

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(3));

            Assert.Multiple(() =>
            {
                Assert.That(resultado[0].Nombre, Is.EqualTo("Laptop"));
                Assert.That(resultado[0].Cantidad, Is.EqualTo(15));

                Assert.That(resultado[1].Nombre, Is.EqualTo("Mouse"));
                Assert.That(resultado[1].Cantidad, Is.EqualTo(10));

                Assert.That(resultado[2].Nombre, Is.EqualTo("Teclado"));
                Assert.That(resultado[2].Cantidad, Is.EqualTo(5));
            });
        }

        [Test]
        public async Task TopProductosFinales_SinResultados_DevuelveListaVacia()
        {
            // Arrange
            var tabla = CrearTablaProductos();

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            var resultado =
                await _dashboardData.TopProductosFinales(5);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task TopProductosFinales_EnvíaParametroTopCorrecto()
        {
            // Arrange
            var tabla = CrearTablaProductos();

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            await _dashboardData.TopProductosFinales(7);

            // Assert
            _mockExecutor.Verify(
                x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, object>>(
                        p => p.ContainsKey("@top")
                            && Convert.ToInt32(p["@top"]) == 7)),
                Times.Once);
        }

        [Test]
        public async Task TopProductosFinales_EjecutaConsultaUnaVez()
        {
            // Arrange
            var tabla = CrearTablaProductos();

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            await _dashboardData.TopProductosFinales(3);

            // Assert
            _mockExecutor.Verify(
                x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }

        [Test]
        public void TopProductosFinales_CuandoEjecutorFalla_LanzaExcepcion()
        {
            // Arrange
            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ThrowsAsync(new InvalidOperationException(
                    "Error simulado al ejecutar la consulta."));

            // Act y Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await _dashboardData.TopProductosFinales(3));
        }

        private static DataTable CrearTablaProductos()
        {
            var tabla = new DataTable();

            tabla.Columns.Add("Nombre", typeof(string));
            tabla.Columns.Add("Cantidad", typeof(int));

            return tabla;
        }

        [Test]
        public async Task TopProductosPersonalizados_ConResultados_DevuelveProductos()
        {
            // Arrange
            var tabla = CrearTablaProductos();
            tabla.Rows.Add("Taza personalizada", 20);
            tabla.Rows.Add("Polo personalizado", 12);
            tabla.Rows.Add("Mug personalizado", 7);

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            var resultado =
                await _dashboardData.TopProductosPersonalizados(3);

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(3));

            Assert.Multiple(() =>
            {
                Assert.That(resultado[0].Nombre,
                    Is.EqualTo("Taza personalizada"));
                Assert.That(resultado[0].Cantidad, Is.EqualTo(20));

                Assert.That(resultado[1].Nombre,
                    Is.EqualTo("Polo personalizado"));
                Assert.That(resultado[1].Cantidad, Is.EqualTo(12));

                Assert.That(resultado[2].Nombre,
                    Is.EqualTo("Mug personalizado"));
                Assert.That(resultado[2].Cantidad, Is.EqualTo(7));
            });
        }

        [Test]
        public async Task TopProductosPersonalizados_SinResultados_DevuelveListaVacia()
        {
            // Arrange
            var tabla = CrearTablaProductos();

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            var resultado =
                await _dashboardData.TopProductosPersonalizados(5);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task TopProductosPersonalizados_EnvíaParametroTopCorrecto()
        {
            // Arrange
            var tabla = CrearTablaProductos();

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            await _dashboardData.TopProductosPersonalizados(7);

            // Assert
            _mockExecutor.Verify(
                x => x.EjecutarConsultaAsync(
                    It.Is<string>(q =>
                        q.Contains("DetallePersonalizado")
                        && q.Contains("pe.Tipo = '1'")),
                    It.Is<Dictionary<string, object>>(p =>
                        p.ContainsKey("@top")
                        && Convert.ToInt32(p["@top"]) == 7)),
                Times.Once);
        }

        [Test]
        public async Task TopProductosPersonalizados_EjecutaConsultaUnaVez()
        {
            // Arrange
            var tabla = CrearTablaProductos();

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            await _dashboardData.TopProductosPersonalizados(3);

            // Assert
            _mockExecutor.Verify(
                x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }

        [Test]
        public void TopProductosPersonalizados_CuandoEjecutorFalla_LanzaExcepcion()
        {
            // Arrange
            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ThrowsAsync(new InvalidOperationException(
                    "Error simulado al ejecutar la consulta."));

            // Act y Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await _dashboardData.TopProductosPersonalizados(3));
        }

        [Test]
        public async Task TopMesesVentas_ConResultados_DevuelveMeses()
        {
            // Arrange
            var tabla = CrearTablaMeses();
            tabla.Rows.Add("2026-01", 25);
            tabla.Rows.Add("2026-03", 18);
            tabla.Rows.Add("2026-02", 12);

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            var resultado = await _dashboardData.TopMesesVentas(3);

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(3));

            Assert.Multiple(() =>
            {
                Assert.That(resultado[0].Mes, Is.EqualTo("2026-01"));
                Assert.That(resultado[0].Cantidad, Is.EqualTo(25));

                Assert.That(resultado[1].Mes, Is.EqualTo("2026-03"));
                Assert.That(resultado[1].Cantidad, Is.EqualTo(18));

                Assert.That(resultado[2].Mes, Is.EqualTo("2026-02"));
                Assert.That(resultado[2].Cantidad, Is.EqualTo(12));
            });
        }

        [Test]
        public async Task TopMesesVentas_SinResultados_DevuelveListaVacia()
        {
            // Arrange
            var tabla = CrearTablaMeses();

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            var resultado = await _dashboardData.TopMesesVentas(5);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task TopMesesVentas_EnvíaParametroTopCorrecto()
        {
            // Arrange
            var tabla = CrearTablaMeses();

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            await _dashboardData.TopMesesVentas(4);

            // Assert
            _mockExecutor.Verify(
                x => x.EjecutarConsultaAsync(
                    It.Is<string>(q =>
                        q.Contains("FROM Pedido")
                        && q.Contains("FORMAT(FechaCreacion")),
                    It.Is<Dictionary<string, object>>(p =>
                        p.ContainsKey("@top")
                        && Convert.ToInt32(p["@top"]) == 4)),
                Times.Once);
        }

        [Test]
        public async Task TopMesesVentas_EjecutaConsultaUnaVez()
        {
            // Arrange
            var tabla = CrearTablaMeses();

            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(tabla);

            // Act
            await _dashboardData.TopMesesVentas(3);

            // Assert
            _mockExecutor.Verify(
                x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }

        [Test]
        public void TopMesesVentas_CuandoEjecutorFalla_LanzaExcepcion()
        {
            // Arrange
            _mockExecutor
                .Setup(x => x.EjecutarConsultaAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>()))
                .ThrowsAsync(new InvalidOperationException(
                    "Error simulado al consultar las ventas."));

            // Act y Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _dashboardData.TopMesesVentas(3));
        }

        private static DataTable CrearTablaMeses()
        {
            var tabla = new DataTable();

            tabla.Columns.Add("Mes", typeof(string));
            tabla.Columns.Add("Cantidad", typeof(int));

            return tabla;
        }
    }
}