using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using DiarsFinal.Controllers;
using DiarsFinal.Datos;
using DiarsFinal.ViewModels;
using DiarsFinal.ViewModels.DiarsFinal.ViewModels;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestDashboardController
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private PedidoData _pedidoData = null!;
        private DashboardController _controller = null!;

        [SetUp]
        public async Task Setup()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();

            var options = new DbContextOptionsBuilder<DiarsContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new DiarsContext(options);

            await _context.Database.EnsureCreatedAsync();

            _pedidoData = new PedidoData(_context);
            _controller = new DashboardController(_pedidoData);
        }

        [TearDown]
        public async Task TearDown()
        {
            _controller?.Dispose();

            if (_context != null)
                await _context.DisposeAsync();

            if (_connection != null)
                await _connection.DisposeAsync();
        }

        [Test]
        public async Task Index_SeccionPedidos_DevuelveVistaConModelo()
        {
            // Act
            var resultado = await _controller.Index();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(vista.Model, Is.InstanceOf<DashboardVM>());
            Assert.That(vista.ViewName, Is.Null);
            Assert.That(_controller.ViewBag.Seccion, Is.EqualTo("Pedidos"));
        }

        [Test]
        public async Task Index_SeccionInforme_DevuelveInformeEnModelo()
        {
            // Act
            var resultado = await _controller.Index("Informe");

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            var modelo = vista.Model as DashboardVM;

            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo!.Informe, Is.Not.Null);
            Assert.That(_controller.ViewBag.Seccion, Is.EqualTo("Informe"));
        }

        [TestCase("Seguimiento")]
        [TestCase("Otro")]
        public async Task Index_OtraSeccion_ConservaLaSeccionSolicitada(
            string seccion)
        {
            // Act
            var resultado = await _controller.Index(seccion);

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());
            Assert.That(_controller.ViewBag.Seccion, Is.EqualTo(seccion));
        }

        [Test]
        public async Task CambiarEstado_SeEjecuta_RedireccionaAIndex()
        {
            // Arrange
            int idPedido = 1;
            string estado = "En preparación";

            // Act
            var resultado = await _controller.CambiarEstado(
                idPedido,
                estado);

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo(nameof(DashboardController.Index)));
        }

        [Test]
        public async Task BuscarSeguimiento_PedidoInexistente_DevuelveVistaConError()
        {
            // Arrange
            int codigo = -999;

            // Act
            var resultado = await _controller.BuscarSeguimiento(codigo);

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            var modelo = vista.Model as DashboardVM;

            Assert.That(vista.ViewName, Is.EqualTo("Index"));
            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo!.PedidoSeguimiento, Is.Null);
            Assert.That(
                _controller.ViewBag.Seccion,
                Is.EqualTo("Seguimiento"));

            Assert.That(
                _controller.ViewBag.Error,
                Is.EqualTo("Pedido no encontrado"));
        }

        [Test]
        public async Task ConfirmarPago_SeEjecuta_RedireccionaASeccionSeguimiento()
        {
            // Arrange
            int idPedido = 1;

            // Act
            var resultado = await _controller.ConfirmarPago(idPedido);

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo(nameof(DashboardController.Index)));

            Assert.That(
                redireccion.RouteValues!["seccion"],
                Is.EqualTo("Seguimiento"));
        }
    }
}