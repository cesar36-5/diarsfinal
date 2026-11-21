using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DiarsFinal.Controllers;
using DiarsFinal.Datos;
using DiarsFinal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestCatalogoController
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private ProductoData _productoData = null!;
        private CatalogoController _controller = null!;

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

            _sessionData = new Dictionary<string, byte[]>();
            _sessionMock = new Mock<ISession>();

            _sessionMock
                .Setup(s => s.TryGetValue(
                    It.IsAny<string>(),
                    out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) =>
                {
                    if (_sessionData.TryGetValue(key, out var stored))
                    {
                        value = stored;
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

            var httpContext = new DefaultHttpContext();
            httpContext.Session = _sessionMock.Object;

            _controller = new CatalogoController(_productoData)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };
        }

        [TearDown]
        public async Task TearDown()
        {
            _controller?.Dispose();

            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        // MÉTODO AUXILIAR: CREAR PRODUCTO
        private async Task<Producto> CrearProducto(
            string nombre,
            string tipo = "0",
            string activo = "1")
        {
            var categoria = new Categorium
            {
                Nombre = "Categoría de prueba"
            };

            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();

            var producto = new Producto
            {
                Nombre = nombre,
                Descripcion = "Descripción de " + nombre,
                Tipo = tipo,
                Activo = activo,
                Consumo = 1,
                Precio = 20m,
                IdCategoria = categoria.IdCategoria
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return producto;
        }

        // MÉTODO AUXILIAR: GUARDAR CARRITO EN SESIÓN
        private void GuardarCarrito(List<CarritoItem> carrito)
        {
            var json = JsonSerializer.Serialize(carrito);
            _sessionData["Carrito"] = Encoding.UTF8.GetBytes(json);
        }

        // MÉTODO AUXILIAR: LEER CARRITO DE SESIÓN
        private List<CarritoItem> LeerCarrito()
        {
            if (!_sessionData.TryGetValue("Carrito", out var bytes))
            {
                return new List<CarritoItem>();
            }

            var json = Encoding.UTF8.GetString(bytes);

            return JsonSerializer.Deserialize<List<CarritoItem>>(json)
                   ?? new List<CarritoItem>();
        }

        // =====================================================
        // PRUEBAS DE INDEX()
        // =====================================================

        [Test]
        public async Task Index_ConProductos_DevuelveVistaConProductos()
        {
            // Arrange
            var producto = await CrearProducto("Producto catálogo");

            // Act
            var resultado = await _controller.Index();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            var modelo = vista.Model as List<Producto>;

            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo, Has.Count.EqualTo(1));
            Assert.That(modelo![0].IdProducto,
                Is.EqualTo(producto.IdProducto));
        }

        [Test]
        public async Task Index_SinProductos_DevuelveVistaConListaVacia()
        {
            // Act
            var resultado = await _controller.Index();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            var modelo = vista.Model as List<Producto>;

            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo, Is.Empty);
        }

        [Test]
        public async Task Index_ConProductosDeDistintosTipos_SoloMuestraCatalogoActivo()
        {
            // Arrange
            await CrearProducto("Producto catálogo", "0", "1");
            await CrearProducto("Producto inactivo", "0", "0");
            await CrearProducto("Producto final", "1", "1");

            // Act
            var resultado = await _controller.Index();

            // Assert
            var vista = resultado as ViewResult;
            var modelo = vista?.Model as List<Producto>;

            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo, Has.Count.EqualTo(1));
            Assert.That(modelo![0].Nombre, Is.EqualTo("Producto catálogo"));
        }

        // =====================================================
        // PRUEBAS DE AGREGAR CARRITO()
        // =====================================================

        [Test]
        public void AgregarCarrito_SesionSinCarrito_CreaCarritoNuevo()
        {
            // Act
            var resultado = _controller.AgregarCarrito(10, 2);

            // Assert
            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var carrito = LeerCarrito();

            Assert.That(carrito, Has.Count.EqualTo(1));
            Assert.That(carrito[0].IdProducto, Is.EqualTo(10));
            Assert.That(carrito[0].Cantidad, Is.EqualTo(2));
        }

        [Test]
        public void AgregarCarrito_ProductoExistente_SumaCantidad()
        {
            // Arrange
            GuardarCarrito(new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = 10,
                    Cantidad = 2
                }
            });

            // Act
            var resultado = _controller.AgregarCarrito(10, 3);

            // Assert
            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var carrito = LeerCarrito();

            Assert.That(carrito, Has.Count.EqualTo(1));
            Assert.That(carrito[0].Cantidad, Is.EqualTo(5));
        }

        [Test]
        public void AgregarCarrito_ProductoDiferente_AgregaNuevoElemento()
        {
            // Arrange
            GuardarCarrito(new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = 10,
                    Cantidad = 2
                }
            });

            // Act
            _controller.AgregarCarrito(20, 1);

            // Assert
            var carrito = LeerCarrito();

            Assert.That(carrito, Has.Count.EqualTo(2));
            Assert.That(
                carrito.Any(x =>
                    x.IdProducto == 10 && x.Cantidad == 2),
                Is.True);
            Assert.That(
                carrito.Any(x =>
                    x.IdProducto == 20 && x.Cantidad == 1),
                Is.True);
        }

        [Test]
        public void AgregarCarrito_ActualizaSesion_GuardaCarritoComoJson()
        {
            // Act
            _controller.AgregarCarrito(15, 4);

            // Assert
            Assert.That(_sessionData.ContainsKey("Carrito"), Is.True);

            var json = Encoding.UTF8.GetString(
                _sessionData["Carrito"]);

            Assert.DoesNotThrow(() =>
                JsonSerializer.Deserialize<List<CarritoItem>>(json));

            var carrito =
                JsonSerializer.Deserialize<List<CarritoItem>>(json);

            Assert.That(carrito, Is.Not.Null);
            Assert.That(carrito![0].IdProducto, Is.EqualTo(15));
            Assert.That(carrito[0].Cantidad, Is.EqualTo(4));
        }

        [Test]
        public void AgregarCarrito_AgregaProducto_RedireccionaAIndex()
        {
            // Act
            var resultado = _controller.AgregarCarrito(10, 1);

            // Assert
            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo(nameof(CatalogoController.Index)));
        }

        // =====================================================
        // PRUEBAS DE PEDIDO()
        // =====================================================

        [Test]
        public async Task Pedido_SesionSinCarrito_DevuelveListaVacia()
        {
            // Act
            var resultado = await _controller.Pedido();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            var modelo =
                vista.Model as List<(Producto producto, int cantidad)>;

            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo, Is.Empty);
        }

        [Test]
        public async Task Pedido_ConUnProducto_DevuelveProductoYCantidad()
        {
            // Arrange
            var producto = await CrearProducto("Producto pedido");

            GuardarCarrito(new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = producto.IdProducto,
                    Cantidad = 3
                }
            });

            // Act
            var resultado = await _controller.Pedido();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            var modelo =
                vista.Model as List<(Producto producto, int cantidad)>;

            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo, Has.Count.EqualTo(1));
            Assert.That(
                modelo![0].producto.IdProducto,
                Is.EqualTo(producto.IdProducto));
            Assert.That(modelo[0].cantidad, Is.EqualTo(3));
        }

        [Test]
        public async Task Pedido_ConVariosProductos_DevuelveTodos()
        {
            // Arrange
            var productoA = await CrearProducto("Producto A");
            var productoB = await CrearProducto("Producto B");

            GuardarCarrito(new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = productoA.IdProducto,
                    Cantidad = 2
                },
                new CarritoItem
                {
                    IdProducto = productoB.IdProducto,
                    Cantidad = 4
                }
            });

            // Act
            var resultado = await _controller.Pedido();

            // Assert
            var vista = resultado as ViewResult;
            var modelo =
                vista?.Model as List<(Producto producto, int cantidad)>;

            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo, Has.Count.EqualTo(2));

            Assert.That(
                modelo!.Any(x =>
                    x.producto.IdProducto == productoA.IdProducto &&
                    x.cantidad == 2),
                Is.True);

            Assert.That(
                modelo.Any(x =>
                    x.producto.IdProducto == productoB.IdProducto &&
                    x.cantidad == 4),
                Is.True);
        }

        [Test]
        public async Task Pedido_ProductoNoEncontrado_OmiteProductoInexistente()
        {
            // Arrange
            var producto = await CrearProducto("Producto válido");

            GuardarCarrito(new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = producto.IdProducto,
                    Cantidad = 2
                },
                new CarritoItem
                {
                    IdProducto = 999999,
                    Cantidad = 5
                }
            });

            // Act
            var resultado = await _controller.Pedido();

            // Assert
            var vista = resultado as ViewResult;
            var modelo =
                vista?.Model as List<(Producto producto, int cantidad)>;

            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo, Has.Count.EqualTo(1));
            Assert.That(
                modelo![0].producto.IdProducto,
                Is.EqualTo(producto.IdProducto));
        }

        [Test]
        public async Task Pedido_ConVariosElementos_ConservaCantidadDeCadaProducto()
        {
            // Arrange
            var productoA = await CrearProducto("Producto A");
            var productoB = await CrearProducto("Producto B");
            var productoC = await CrearProducto("Producto C");

            GuardarCarrito(new List<CarritoItem>
            {
                new CarritoItem
                {
                    IdProducto = productoA.IdProducto,
                    Cantidad = 1
                },
                new CarritoItem
                {
                    IdProducto = productoB.IdProducto,
                    Cantidad = 3
                },
                new CarritoItem
                {
                    IdProducto = productoC.IdProducto,
                    Cantidad = 6
                }
            });

            // Act
            var resultado = await _controller.Pedido();

            // Assert
            var vista = resultado as ViewResult;
            var modelo =
                vista?.Model as List<(Producto producto, int cantidad)>;

            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo, Has.Count.EqualTo(3));

            Assert.That(
                modelo!.Single(x =>
                    x.producto.IdProducto == productoA.IdProducto).cantidad,
                Is.EqualTo(1));

            Assert.That(
                modelo.Single(x =>
                    x.producto.IdProducto == productoB.IdProducto).cantidad,
                Is.EqualTo(3));

            Assert.That(
                modelo.Single(x =>
                    x.producto.IdProducto == productoC.IdProducto).cantidad,
                Is.EqualTo(6));
        }
    }
}