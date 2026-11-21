using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using DiarsFinal.Controllers;
using DiarsFinal.Controllers;
using DiarsFinal.Data;
using DiarsFinal.Datos;
using DiarsFinal.Models;
using DiarsFinal.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestPedidoController
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private ProductoData _productoData = null!;
        private PedidoData _pedidoData = null!;
        private PedidoController _controller = null!;

        private Mock<ISession> _sessionMock = null!;
        private Dictionary<string, byte[]> _sessionData = null!;

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

            _productoData = new ProductoData(_context);
            _pedidoData = new PedidoData(_context);

            _sessionData = new Dictionary<string, byte[]>();

            _sessionMock = new Mock<ISession>();

            _sessionMock
                .Setup(s => s.TryGetValue(
                    It.IsAny<string>(),
                    out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) =>
                {
                    if (_sessionData.TryGetValue(key, out var guardado))
                    {
                        value = guardado;
                        return true;
                    }

                    value = null!;
                    return false;
                });

            _sessionMock
                .Setup(s => s.Set(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>()))
                .Callback<string, byte[]>((key, value) =>
                {
                    _sessionData[key] = value;
                });

            _sessionMock
                .Setup(s => s.Remove(It.IsAny<string>()))
                .Callback<string>(key => _sessionData.Remove(key));

            _sessionMock
                .Setup(s => s.Clear())
                .Callback(() => _sessionData.Clear());

            var pasarelas = new IPasarelaPago[]
            {
                new PagoYape(),
                new PagoTarjeta(),
                new PagoEfectivo()
            };

            _controller = new PedidoController(
                _productoData,
                _pedidoData,
                pasarelas);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = _sessionMock.Object;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _controller.TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>()
            );
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

        // =========================================
        // INDEX
        // =========================================

        [Test]
        public async Task Index_SinCarrito_DevuelveVistaConListaVacia()
        {
            // Act
            var resultado = await _controller.Index();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(vista.Model, Is.Not.Null);

            var pedido =
                vista.Model as List<(Producto producto, int cantidad)>;

            Assert.That(pedido, Is.Not.Null);
            Assert.That(pedido, Is.Empty);
        }

        [Test]
        public async Task Index_ConJsonInvalido_DevuelveVistaConListaVacia()
        {
            // Arrange
            _sessionMock.Object.SetString("Carrito", "{json incorrecto");

            // Act
            var resultado = await _controller.Index();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            var pedido =
                vista.Model as List<(Producto producto, int cantidad)>;

            Assert.That(pedido, Is.Not.Null);
            Assert.That(pedido, Is.Empty);
        }

        [Test]
        public async Task Index_ConCarritoVacio_DevuelveListaVacia()
        {
            // Arrange
            _sessionMock.Object.SetString("Carrito", "[]");

            // Act
            var resultado = await _controller.Index();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            var pedido =
                vista.Model as List<(Producto producto, int cantidad)>;

            Assert.That(pedido, Is.Not.Null);
            Assert.That(pedido, Is.Empty);
        }

        // =========================================
        // GENERAR PEDIDO
        // =========================================

        [Test]
        public async Task GenerarPedido_MetodoPagoInvalido_RedireccionaAlIndex()
        {
            // Arrange
            _sessionMock.Object.SetString("Carrito", "[]");

            // Act
            var resultado = await _controller.GenerarPedido(
                "Cliente Prueba",
                "12345678",
                null,
                "987654321",
                DateTime.Today.AddDays(3),
                "INVALIDO");

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo(nameof(PedidoController.Index)));

            Assert.That(
                _controller.TempData["Error"],
                Is.EqualTo("Método de pago inválido"));
        }

        [Test]
        public async Task GenerarPedido_SinCarrito_RedireccionaAlIndex()
        {
            // Arrange: no guardar la clave Carrito en la sesión.

            // Act
            var resultado = await _controller.GenerarPedido(
                "Cliente Prueba",
                "12345678",
                null,
                "987654321",
                DateTime.Today.AddDays(3),
                "YAPE");

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo(nameof(PedidoController.Index)));

            Assert.That(
                _controller.TempData["Error"],
                Is.EqualTo("Carrito vacío"));
        }

        // =========================================
        // CONFIRMAR PAGO CON TARJETA
        // =========================================

        [Test]
        public void ConfirmarPagoTarjeta_CodigoValido_MuestraConfirmacion()
        {
            int codigoPedido = 123;

            var resultado = _controller.ConfirmarPagoTarjeta(codigoPedido);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());
            Assert.That(
                _controller.TempData["Correcto"],
                Is.EqualTo($"Pago con tarjeta realizado. Pedido #{codigoPedido}")
            );

            var redireccion = resultado as RedirectToActionResult;
            Assert.That(redireccion!.ActionName, Is.EqualTo("Index"));
        }

        // =========================================
        // MODIFICAR CANTIDAD
        // =========================================

        [Test]
        public void ModificarCantidad_ProductoExistente_AumentaCantidad()
        {
            // Arrange
            var carrito = new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = 1,
                    Cantidad = 2
                }
            };

            _sessionMock.Object.SetString(
                "Carrito",
                JsonSerializer.Serialize(carrito));

            // Act
            var resultado =
                _controller.ModificarCantidad(1, 1);

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var carritoActualizado =
                JsonSerializer.Deserialize<List<CarritoItem>>(
                    _sessionMock.Object.GetString("Carrito")!);

            Assert.That(carritoActualizado, Has.Count.EqualTo(1));
            Assert.That(carritoActualizado![0].Cantidad, Is.EqualTo(3));
        }

        [Test]
        public void ModificarCantidad_ProductoExistente_ReduceCantidad()
        {
            // Arrange
            var carrito = new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = 1,
                    Cantidad = 3
                }
            };

            _sessionMock.Object.SetString(
                "Carrito",
                JsonSerializer.Serialize(carrito));

            // Act
            _controller.ModificarCantidad(1, -1);

            // Assert
            var carritoActualizado =
                JsonSerializer.Deserialize<List<CarritoItem>>(
                    _sessionMock.Object.GetString("Carrito")!);

            Assert.That(carritoActualizado, Has.Count.EqualTo(1));
            Assert.That(carritoActualizado![0].Cantidad, Is.EqualTo(2));
        }

        [Test]
        public void ModificarCantidad_CantidadLlegaACero_EliminaProducto()
        {
            // Arrange
            var carrito = new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = 1,
                    Cantidad = 1
                }
            };

            _sessionMock.Object.SetString(
                "Carrito",
                JsonSerializer.Serialize(carrito));

            // Act
            _controller.ModificarCantidad(1, -1);

            // Assert
            var carritoActualizado =
                JsonSerializer.Deserialize<List<CarritoItem>>(
                    _sessionMock.Object.GetString("Carrito")!);

            Assert.That(carritoActualizado, Is.Empty);
        }

        [Test]
        public void ModificarCantidad_CarritoInexistente_CreaCarritoVacio()
        {
            // Act
            var resultado = _controller.ModificarCantidad(99, 1);

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var carrito =
                JsonSerializer.Deserialize<List<CarritoItem>>(
                    _sessionMock.Object.GetString("Carrito")!);

            Assert.That(carrito, Is.Empty);
        }

        // =========================================
        // ELIMINAR PRODUCTO
        // =========================================

        [Test]
        public void EliminarProducto_ProductoExistente_LoEliminaDelCarrito()
        {
            // Arrange
            var carrito = new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = 1,
                    Cantidad = 2
                },
                new CarritoItem
                {
                    IdProducto = 2,
                    Cantidad = 1
                }
            };

            _sessionMock.Object.SetString(
                "Carrito",
                JsonSerializer.Serialize(carrito));

            // Act
            var resultado = _controller.EliminarProducto(1);

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var carritoActualizado =
                JsonSerializer.Deserialize<List<CarritoItem>>(
                    _sessionMock.Object.GetString("Carrito")!);

            Assert.That(carritoActualizado, Has.Count.EqualTo(1));
            Assert.That(
                carritoActualizado![0].IdProducto,
                Is.EqualTo(2));
        }

        [Test]
        public void EliminarProducto_IdInexistente_ConservaCarrito()
        {
            // Arrange
            var carrito = new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = 1,
                    Cantidad = 2
                }
            };

            _sessionMock.Object.SetString(
                "Carrito",
                JsonSerializer.Serialize(carrito));

            // Act
            _controller.EliminarProducto(999);

            // Assert
            var carritoActualizado =
                JsonSerializer.Deserialize<List<CarritoItem>>(
                    _sessionMock.Object.GetString("Carrito")!);

            Assert.That(carritoActualizado, Has.Count.EqualTo(1));
            Assert.That(
                carritoActualizado![0].IdProducto,
                Is.EqualTo(1));
        }

        [Test]
        public void EliminarProducto_SinCarrito_GuardaListaVacia()
        {
            // Act
            var resultado = _controller.EliminarProducto(1);

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var carrito =
                JsonSerializer.Deserialize<List<CarritoItem>>(
                    _sessionMock.Object.GetString("Carrito")!);

            Assert.That(carrito, Is.Empty);
        }
    }
}