using DiarsFinal.Controllers;
using DiarsFinal.Data;
using DiarsFinal.Datos;
using DiarsFinal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestProductosController
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private ProductosController _controller = null!;

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

            var productoData = new ProductoData(_context);
            var categoriaData = new CategoriaData(_context);

            _controller = new ProductosController(
                productoData,
                categoriaData);

            var httpContext = new DefaultHttpContext();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _controller.TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>());
        }

        [TearDown]
        public async Task TearDown()
        {
            _controller?.Dispose();
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private async Task CrearCategoria(int id = 1)
        {
            if (!await _context.Categoria.AnyAsync(
                c => c.IdCategoria == id))
            {
                _context.Categoria.Add(new Categorium
                {
                    IdCategoria = id,
                    Nombre = $"Categoría {id}"
                });

                await _context.SaveChangesAsync();
            }
        }

        private async Task<Producto> CrearProducto(
            int id = 1,
            int idCategoria = 1,
            string nombre = "Producto de prueba",
            decimal precio = 25m,
            string tipo = "1",
            string activo = "1")
        {
            await CrearCategoria(idCategoria);

            var producto = new Producto
            {
                IdProducto = id,
                IdCategoria = idCategoria,
                Nombre = nombre,
                Descripcion = "Descripción de prueba",
                Precio = precio,
                Consumo = 1,
                Tipo = tipo,
                Activo = activo,
                RutaImagen = null
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return producto;
        }

        // 1. INDEX

        [Test]
        public async Task Index_ConProductos_RetornaVistaConProductosYCategorias()
        {
            await CrearProducto();
            await CrearCategoria(2);

            var resultado = await _controller.Index();

            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(vista.Model, Is.InstanceOf<IEnumerable<Producto>>());
            Assert.That(((IEnumerable<Producto>)vista.Model!).Count(),
                Is.EqualTo(1));

            Assert.That(_controller.ViewBag.Categorias, Is.Not.Null);
            Assert.That(
                ((IEnumerable<Categorium>)_controller.ViewBag.Categorias).Count(),
                Is.EqualTo(2));
        }

        [Test]
        public async Task Index_SinProductos_RetornaListaVacia()
        {
            var resultado = await _controller.Index();

            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(
                ((IEnumerable<Producto>)vista.Model!).Count(),
                Is.EqualTo(0));
        }

        // 2. CREAR

        [Test]
        public async Task Crear_ProductoValido_GuardaProductoYRedirecciona()
        {
            await CrearCategoria();

            var producto = new Producto
            {
                IdProducto = 10,
                IdCategoria = 1,
                Nombre = "Producto nuevo",
                Descripcion = "Descripción nueva",
                Precio = 50m,
                Consumo = 2,
                Tipo = "1",
                Activo = "1"
            };

            var resultado = await _controller.Crear(producto, null!);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var guardado = await _context.Productos
                .AsNoTracking()
                .SingleAsync(p => p.IdProducto == 10);

            Assert.That(guardado.Nombre, Is.EqualTo("Producto nuevo"));
            Assert.That(guardado.Precio, Is.EqualTo(50m));
            Assert.That(guardado.Activo, Is.EqualTo("1"));
        }

        [Test]
        public async Task Crear_ProductoInactivoMantieneEstadoCero()
        {
            await CrearCategoria();

            var producto = new Producto
            {
                IdProducto = 11,
                IdCategoria = 1,
                Nombre = "Producto inactivo",
                Descripcion = "Prueba",
                Precio = 20m,
                Consumo = 1,
                Tipo = "1",
                Activo = "0"
            };

            var resultado = await _controller.Crear(producto, null!);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var guardado = await _context.Productos
                .AsNoTracking()
                .SingleAsync(p => p.IdProducto == 11);

            Assert.That(guardado.Activo, Is.EqualTo("0"));
        }

        [Test]
        public async Task Crear_ModelStateInvalido_NoGuardaYRedirecciona()
        {
            var producto = new Producto
            {
                IdProducto = 12,
                Nombre = "Producto inválido",
                Activo = "1"
            };

            _controller.ModelState.AddModelError(
                "Nombre",
                "Error de validación de prueba");

            var resultado = await _controller.Crear(producto, null!);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            Assert.That(
                _controller.TempData["Error"],
                Is.EqualTo("Error en los datos del producto"));

            Assert.That(
                await _context.Productos.AnyAsync(p => p.IdProducto == 12),
                Is.False);
        }

        // 3. EDITAR GET

        [Test]
        public async Task Editar_IdExistente_RetornaVistaConProducto()
        {
            await CrearProducto();

            var resultado = await _controller.Editar(1);

            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(vista.Model, Is.InstanceOf<Producto>());

            var producto = (Producto)vista.Model!;

            Assert.That(producto.IdProducto, Is.EqualTo(1));
            Assert.That(producto.Nombre, Is.EqualTo("Producto de prueba"));
            Assert.That(_controller.ViewBag.Categorias, Is.Not.Null);
        }

        private void ConfigurarPeticionFormulario()
        {
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Method = "POST";
            httpContext.Request.ContentType =
                "application/x-www-form-urlencoded";

            httpContext.Request.Form = new FormCollection(
                new Dictionary<string,
                    Microsoft.Extensions.Primitives.StringValues>());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _controller.TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>());
        }

        [Test]
        public async Task Editar_ProductoExistente_ActualizaDatos()
        {
            ConfigurarPeticionFormulario();
            await CrearProducto();

            var cambios = new Producto
            {
                IdProducto = 1,
                IdCategoria = 1,
                Nombre = "Producto actualizado",
                Descripcion = "Nueva descripción",
                Precio = 75m,
                Consumo = 3,
                Tipo = "0",
                Activo = "0"
            };

            var resultado = await _controller.Editar(cambios, null!);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var actualizado = await _context.Productos
                .AsNoTracking()
                .SingleAsync(p => p.IdProducto == 1);

            Assert.That(actualizado.Nombre, Is.EqualTo("Producto actualizado"));
            Assert.That(actualizado.Descripcion, Is.EqualTo("Nueva descripción"));
            Assert.That(actualizado.Precio, Is.EqualTo(75m));
            Assert.That(actualizado.Consumo, Is.EqualTo(3));
            Assert.That(actualizado.Tipo, Is.EqualTo("0"));
            Assert.That(actualizado.Activo, Is.EqualTo("0"));
        }

        [Test]
        public async Task Editar_IdInexistente_RetornaNotFound()
        {
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Method = "POST";
            httpContext.Request.ContentType =
                "application/x-www-form-urlencoded";

            httpContext.Request.Form =
                new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var producto = new Producto
            {
                IdProducto = 999,
                Nombre = "No existe"
            };

            var resultado = await _controller.Editar(producto, null!);

            Assert.That(resultado, Is.InstanceOf<NotFoundResult>());
        }

        // 5. EDITAR CATEGORÍA / PRODUCTO COMPATIBLE

        [Test]
        public async Task EditarC_IdExistente_CargaListasYProducto()
        {
            await CrearProducto(
                id: 1,
                nombre: "Producto base",
                tipo: "1");

            await CrearProducto(
                id: 2,
                nombre: "Producto final",
                tipo: "1");

            var resultado = await _controller.EditarC(1);

            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(vista.Model, Is.InstanceOf<Producto>());
            Assert.That(_controller.ViewBag.Categorias, Is.Not.Null);
            Assert.That(_controller.ViewBag.ProductosCompatibles, Is.Not.Null);
        }

        [Test]
        public async Task EditarC_IdInexistente_RetornaNotFound()
        {
            var resultado = await _controller.EditarC(999);

            Assert.That(resultado, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task EditarC_Post_ActualizaProductoYRedirecciona()
        {
            await CrearProducto();

            // Desvincular las entidades que quedaron rastreadas
            // después de preparar los datos de prueba.
            _context.ChangeTracker.Clear();

            var cambios = new Producto
            {
                IdProducto = 1,
                IdCategoria = 1,
                Nombre = "Producto final actualizado",
                Descripcion = "",
                Precio = 60m,
                Consumo = 2,
                Tipo = "1",
                Activo = "0"
            };

            var resultado = await _controller.EditarC(cambios, null!);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var actualizado = await _context.Productos
                .AsNoTracking()
                .SingleAsync(p => p.IdProducto == 1);

            Assert.That(actualizado.Nombre, Is.EqualTo("Producto final actualizado"));
            Assert.That(actualizado.Descripcion, Is.EqualTo(""));
            Assert.That(actualizado.Activo, Is.EqualTo("0"));
        }

        // 6. COMPATIBILIDADES

        [Test]
        public async Task AgregarCompatibilidad_ProductosExistentes_CreaRelacion()
        {
            await CrearProducto(id: 1, nombre: "Producto A");
            await CrearProducto(id: 2, nombre: "Producto B");

            var resultado = await _controller.AgregarCompatibilidad(1, 2);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            Assert.That(
                await _context.Compatibilidades.AnyAsync(
                    c => c.IdProductoA == 1 && c.IdProductoB == 2),
                Is.True);
        }

        [Test]
        public async Task AgregarCompatibilidad_RelacionExistente_NoDuplica()
        {
            var productoA = await CrearProducto(id: 1, nombre: "Producto A");
            var productoB = await CrearProducto(id: 2, nombre: "Producto B");

            _context.Compatibilidades.Add(new Compatibilidad
            {
                IdProductoA = productoA.IdProducto,
                IdProductoB = productoB.IdProducto,
                ProductoA = productoA,
                ProductoB = productoB
            });

            await _context.SaveChangesAsync();

            var resultado = await _controller.AgregarCompatibilidad(1, 2);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            Assert.That(
                await _context.Compatibilidades.CountAsync(
                    c => c.IdProductoA == 1 && c.IdProductoB == 2),
                Is.EqualTo(1));
        }

        [Test]
        public async Task EliminarCompatibilidad_RelacionExistente_EliminaRelacion()
        {
            var productoA = await CrearProducto(id: 1, nombre: "Producto A");
            var productoB = await CrearProducto(id: 2, nombre: "Producto B");

            _context.Compatibilidades.Add(new Compatibilidad
            {
                IdProductoA = productoA.IdProducto,
                IdProductoB = productoB.IdProducto,
                ProductoA = productoA,
                ProductoB = productoB
            });

            await _context.SaveChangesAsync();

            var resultado = await _controller.EliminarCompatibilidad(1, 2);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            Assert.That(
                await _context.Compatibilidades.AnyAsync(
                    c => c.IdProductoA == 1 && c.IdProductoB == 2),
                Is.False);
        }

        [Test]
        public async Task EliminarCompatibilidad_RelacionInexistente_NoFalla()
        {
            var resultado = await _controller.EliminarCompatibilidad(1, 2);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());
        }

        // 7. ELIMINAR PRODUCTO

        [Test]
        public async Task Eliminar_IdExistente_RetornaTablaActualizada()
        {
            await CrearProducto();

            var resultado = await _controller.Eliminar(1);

            Assert.That(resultado, Is.InstanceOf<PartialViewResult>());

            var vista = (PartialViewResult)resultado;

            Assert.That(vista.ViewName, Is.EqualTo("_ProductosTable"));
            Assert.That(
                await _context.Productos.AnyAsync(p => p.IdProducto == 1),
                Is.False);
        }

        [Test]
        public async Task Eliminar_IdInexistente_RetornaJsonDeError()
        {
            var resultado = await _controller.Eliminar(999);

            Assert.That(resultado, Is.InstanceOf<JsonResult>());

            var json = (JsonResult)resultado;

            Assert.That(json.Value, Is.Not.Null);

            var tipo = json.Value!.GetType();

            Assert.That(
                (bool)tipo.GetProperty("success")!.GetValue(json.Value)!,
                Is.False);
        }

        // 8. CAMBIAR ESTADO

        [Test]
        public async Task CambiarEstado_ProductoActivo_CambiaAInactivo()
        {
            await CrearProducto(activo: "1");

            var resultado = await _controller.CambiarEstado(1);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var producto = await _context.Productos
                .AsNoTracking()
                .SingleAsync(p => p.IdProducto == 1);

            Assert.That(producto.Activo, Is.EqualTo("0"));
        }

        [Test]
        public async Task CambiarEstado_ProductoInactivo_CambiaAActivo()
        {
            await CrearProducto(activo: "0");

            var resultado = await _controller.CambiarEstado(1);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var producto = await _context.Productos
                .AsNoTracking()
                .SingleAsync(p => p.IdProducto == 1);

            Assert.That(producto.Activo, Is.EqualTo("1"));
        }

        [Test]
        public async Task CambiarEstado_IdInexistente_RedireccionaSinFallar()
        {
            var resultado = await _controller.CambiarEstado(999);

            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            Assert.That(
                await _context.Productos.AnyAsync(p => p.IdProducto == 999),
                Is.False);
        }
    }
}