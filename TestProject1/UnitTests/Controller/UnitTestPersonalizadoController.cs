using DiarsFinal.Controllers;
using DiarsFinal.Data;
using DiarsFinal.Datos;
using DiarsFinal.Models;
using DiarsFinal.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestPersonalizadoController
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private PersonalizadoController _controller = null!;

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

            var personalizadoData = new PersonalizadoData(_context);
            var pedidoData = new PedidoData(_context);

            IPasarelaPago[] pasarelas =
            {
                new PagoYape(),
                new PagoTarjeta(),
                new PagoEfectivo()
            };

            _controller = new PersonalizadoController(
                personalizadoData,
                pedidoData,
                pasarelas
            );

            var httpContext = new DefaultHttpContext();

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

            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private async Task<Producto> CrearProducto(
            int id,
            int categoria,
            string nombre,
            decimal precio,
            int consumo)
        {
            await CrearCategoria(categoria);

            var producto = new Producto
            {
                IdProducto = id,
                IdCategoria = categoria,
                Nombre = nombre,
                Precio = precio,
                Consumo = consumo,
                Descripcion = "Producto de prueba",
                Tipo = "1",
                Activo = "1"
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return producto;
        }

        private async Task CrearEspecificacion(
            string dia,
            int capacidadTotal,
            int capacidadActual)
        {
            _context.Especificaciones.Add(new Especificacione
            {
                DiasTrabajo = dia,
                CapacidadTotal = capacidadTotal,
                CapacidadActual = capacidadActual
            });

            await _context.SaveChangesAsync();
        }

        private static PedidoPersonalizadoVM CrearVM(
            int idProducto,
            DateTime fecha,
            string metodoNombre = "YAPE")
        {
            return new PedidoPersonalizadoVM
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                FechaEntrega = fecha,
                Comentario = "Prueba unitaria",
                Selecciones = new List<SeleccionPersonalizadaVM>
                {
                    new SeleccionPersonalizadaVM
                    {
                        IdProducto = idProducto,
                        Comentario = "Personalizado",
                        Posicion = 1
                    }
                }
            };
        }

        [Test]
        public async Task Index_CargaBases_RetornaVistaConViewModel()
        {
            await CrearProducto(1, 3, "Base de prueba", 20m, 2);

            var resultado = await _controller.Index();

            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            Assert.That(vista.Model, Is.InstanceOf<PedidoPersonalizadoVM>());

            var vm = (PedidoPersonalizadoVM)vista.Model!;
            Assert.That(vm.Bases.Count, Is.EqualTo(1));
            Assert.That(vm.Bases[0].Nombre, Is.EqualTo("Base de prueba"));
        }

        [Test]
        public async Task ObtenerCompatibles_IdValido_RetornaJson()
        {
            var productoA = await CrearProducto(
                1, 3, "Base", 20m, 1);

            var productoB = await CrearProducto(
                2, 4, "Compatible", 10m, 1);

            _context.Compatibilidades.Add(new Compatibilidad
            {
                IdProductoA = productoA.IdProducto,
                IdProductoB = productoB.IdProducto
            });

            await _context.SaveChangesAsync();

            var resultado = await _controller.ObtenerCompatibles(1);

            Assert.That(resultado, Is.InstanceOf<JsonResult>());

            var json = (JsonResult)resultado;
            Assert.That(json.Value, Is.Not.Null);

            var elementos = ((IEnumerable<object>)json.Value!).ToList();
            Assert.That(elementos, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GenerarPedido_SinSelecciones_RedireccionaAIndex()
        {
            var vm = new PedidoPersonalizadoVM();

            var resultado =
                await _controller.GenerarPedido(vm, "YAPE");

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var redirect = (RedirectToActionResult)resultado;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));

            Assert.That(
                _controller.TempData["Error"],
                Is.EqualTo("No hay productos seleccionados"));
        }

        [Test]
        public async Task GenerarPedido_SinCapacidad_RedireccionaAIndex()
        {
            var fecha = new DateTime(2030, 1, 7);

            await CrearProducto(1, 3, "Producto costoso en capacidad", 30m, 5);
            await CrearEspecificacion("2030-01-07", 2, 2);

            var vm = CrearVM(1, fecha);

            var resultado =
                await _controller.GenerarPedido(vm, "YAPE");

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var redirect = (RedirectToActionResult)resultado;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));

            Assert.That(
                _controller.TempData["Error"],
                Is.EqualTo("No hay capacidad suficiente para esa fecha."));
        }

        private async Task CrearCategoria(int idCategoria)
        {
            if (!await _context.Categoria.AnyAsync(c => c.IdCategoria == idCategoria))
            {
                _context.Categoria.Add(new Categorium
                {
                    IdCategoria = idCategoria,
                    Nombre = $"Categoría {idCategoria}"
                });

                await _context.SaveChangesAsync();
            }
        }

        [Test]
        public async Task GenerarPedido_MetodoPagoInvalido_RedireccionaAIndex()
        {
            var fecha = new DateTime(2030, 1, 7);

            await CrearProducto(1, 3, "Producto de prueba", 30m, 1);
            await CrearEspecificacion("2030-01-07", 10, 10);

            var vm = CrearVM(1, fecha);

            var resultado =
                await _controller.GenerarPedido(vm, "METODO_INEXISTENTE");

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var redirect = (RedirectToActionResult)resultado;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));

            Assert.That(
                _controller.TempData["Error"],
                Is.EqualTo("Método de pago inválido"));

            // El código genera el pedido antes de validar la pasarela.
            Assert.That(await _context.Pedidos.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task GenerarPedido_Valido_RetornaVistaPago()
        {
            var fecha = new DateTime(2030, 1, 7);

            await CrearProducto(1, 3, "Producto de prueba", 30m, 1);
            await CrearEspecificacion("2030-01-07", 10, 10);

            var vm = CrearVM(1, fecha);

            var productoAntes = await _context.Productos
                .SingleAsync(p => p.IdProducto == 1);

            var especificacionAntes = await _context.Especificaciones
                .SingleAsync(e => e.DiasTrabajo == "2030-01-07");

            Assert.That(productoAntes.Consumo, Is.EqualTo(1));
            Assert.That(especificacionAntes.CapacidadActual, Is.EqualTo(10));

            var resultado = await _controller.GenerarPedido(vm, "YAPE");

            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(vista.ViewName, Is.EqualTo("Pago"));
            Assert.That(vista.Model, Is.InstanceOf<PagoViewModel>());

            var pago = (PagoViewModel)vista.Model!;

            TestContext.WriteLine($"Total del pago: {pago.Total}");
            TestContext.WriteLine($"Código del pedido: {pago.CodigoPedido}");

            var pedidos = await _context.Pedidos.CountAsync();

            var especificacionDespues = await _context.Especificaciones
                .AsNoTracking()
                .SingleAsync(e => e.DiasTrabajo == "2030-01-07");

            Assert.That(pago.Metodo, Is.EqualTo("YAPE"));
            Assert.That(pago.Total, Is.EqualTo(30m));
            Assert.That(pago.CodigoPedido, Is.GreaterThan(0));
            Assert.That(pedidos, Is.EqualTo(1));
            Assert.That(especificacionDespues.CapacidadActual, Is.EqualTo(9));
        }

        [Test]
        public void ConfirmarPagoTarjeta_CodigoValido_MuestraConfirmacion()
        {
            int codigoPedido = 123;

            var resultado =
                _controller.ConfirmarPagoTarjeta(codigoPedido);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var redirect = (RedirectToActionResult)resultado;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));

            Assert.That(
                _controller.TempData["Correcto"],
                Is.EqualTo(
                    $"Pago con tarjeta confirmado. Pedido #{codigoPedido}"));
        }

        [Test]
        public void ConfirmarPagoTarjeta_CodigoCero_RedireccionaAIndex()
        {
            var resultado = _controller.ConfirmarPagoTarjeta(0);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var redirect = (RedirectToActionResult)resultado;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));

            Assert.That(
                _controller.TempData["Correcto"],
                Is.EqualTo("Pago con tarjeta confirmado. Pedido #0"));
        }
    }
}