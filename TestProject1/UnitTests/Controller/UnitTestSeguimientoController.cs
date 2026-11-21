using DiarsFinal.Controllers;
using DiarsFinal.Datos;
using DiarsFinal.Data;
using DiarsFinal.Models;
using DiarsFinal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestSeguimientoController
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private PedidoData _pedidoData = null!;
        private SeguimientoController _controller = null!;

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
            _controller = new SeguimientoController(_pedidoData);
        }

        [TearDown]
        public async Task TearDown()
        {
            _controller?.Dispose();

            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        [Test]
        public void Index_RetornaVistaConModeloVacio()
        {
            var resultado = _controller.Index();

            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(vista.Model, Is.InstanceOf<SeguimientoVM>());

            var vm = (SeguimientoVM)vista.Model!;

            Assert.That(vm.Pedido, Is.Null);
            Assert.That(vm.Error, Is.Null);
        }

        [Test]
        public async Task Buscar_PedidoInexistente_MuestraMensajeError()
        {
            var resultado = await _controller.Buscar(999);

            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(vista.ViewName, Is.EqualTo("Index"));
            Assert.That(vista.Model, Is.InstanceOf<SeguimientoVM>());

            var vm = (SeguimientoVM)vista.Model!;

            Assert.That(vm.Pedido, Is.Null);
            Assert.That(vm.Error, Is.EqualTo("Pedido no encontrado"));
        }

        [Test]
        public async Task Buscar_PedidoExistente_MuestraPedidoSinError()
        {
            var pedido = CrearPedido(codigo: 12345);

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            var resultado = await _controller.Buscar(12345);

            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(vista.ViewName, Is.EqualTo("Index"));
            Assert.That(vista.Model, Is.InstanceOf<SeguimientoVM>());

            var vm = (SeguimientoVM)vista.Model!;

            Assert.That(vm.Pedido, Is.Not.Null);
            Assert.That(vm.Pedido!.Codigo, Is.EqualTo(12345));
            Assert.That(vm.Error, Is.Null);
        }

        private static Pedido CrearPedido(int codigo)
        {
            return new Pedido
            {
                Codigo = codigo,
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "987654321",
                FechaEntrega = new DateTime(2026, 11, 15),
                FechaCreacion = DateTime.Now,
                Precio = 150.00m,
                Estado = "1",
                Tipo = "0",
                Comentario = ""
            };
        }
    }
}