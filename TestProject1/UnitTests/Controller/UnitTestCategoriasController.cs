using DiarsFinal.Controllers;
using DiarsFinal.Datos;
using DiarsFinal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestCategoriasController
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private CategoriaData _categoriaData = null!;
        private CategoriasController _controller = null!;

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

            _categoriaData = new CategoriaData(_context);
            _controller = new CategoriasController(_categoriaData);
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
        public async Task Index_ConCategorias_DevuelveVistaConCategorias()
        {
            // Arrange
            _context.Categoria.AddRange(
                new Categorium { Nombre = "Bebidas" },
                new Categorium { Nombre = "Postres" }
            );

            await _context.SaveChangesAsync();

            // Act
            var resultado = await _controller.Index();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            var categorias = vista.Model as IEnumerable<Categorium>;

            Assert.That(categorias, Is.Not.Null);
            Assert.That(categorias!.Count(), Is.EqualTo(2));
            Assert.That(categorias.Select(c => c.Nombre),
                Does.Contain("Bebidas"));
            Assert.That(categorias.Select(c => c.Nombre),
                Does.Contain("Postres"));
        }

        [Test]
        public async Task Index_SinCategorias_DevuelveVistaConListaVacia()
        {
            // Act
            var resultado = await _controller.Index();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            var categorias = vista.Model as IEnumerable<Categorium>;

            Assert.That(categorias, Is.Not.Null);
            Assert.That(categorias, Is.Empty);
        }

        // =========================================
        // CREAR
        // =========================================

        [Test]
        public async Task Crear_ModeloValido_GuardaCategoriaYRedirige()
        {
            // Arrange
            var categoria = new Categorium
            {
                Nombre = "Bebidas"
            };

            // Act
            var resultado = await _controller.Crear(categoria);

            // Assert
            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo(nameof(CategoriasController.Index)));

            var guardada = await _context.Categoria
                .FirstOrDefaultAsync(c => c.Nombre == "Bebidas");

            Assert.That(guardada, Is.Not.Null);
        }

        [Test]
        public async Task Crear_ModeloInvalido_NoGuardaCategoriaYRedirige()
        {
            // Arrange
            var categoria = new Categorium
            {
                Nombre = ""
            };

            _controller.ModelState.AddModelError(
                nameof(Categorium.Nombre),
                "El nombre es obligatorio.");

            // Act
            var resultado = await _controller.Crear(categoria);

            // Assert
            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo(nameof(CategoriasController.Index)));

            var categorias = await _context.Categoria.ToListAsync();

            Assert.That(categorias, Is.Empty);
        }

        // =========================================
        // EDITAR - GET
        // =========================================

        [Test]
        public async Task EditarGet_CategoriaExistente_DevuelveVistaConCategoria()
        {
            // Arrange
            var categoria = new Categorium
            {
                Nombre = "Bebidas"
            };

            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();

            int id = categoria.IdCategoria;

            // Act
            var resultado = await _controller.Editar(id);

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            var modelo = vista.Model as Categorium;

            Assert.That(modelo, Is.Not.Null);
            Assert.That(modelo!.IdCategoria, Is.EqualTo(id));
            Assert.That(modelo.Nombre, Is.EqualTo("Bebidas"));
        }

        [Test]
        public async Task EditarGet_CategoriaInexistente_DevuelveNotFound()
        {
            // Arrange
            int id = 999;

            // Act
            var resultado = await _controller.Editar(id);

            // Assert
            Assert.That(resultado, Is.InstanceOf<NotFoundResult>());
        }

        // =========================================
        // EDITAR - POST
        // =========================================

        [Test]
        public async Task EditarPost_ModeloValido_EditaCategoriaYRedirige()
        {
            // Arrange: insertar la categoría inicial
            var existente = new Categorium
            {
                Nombre = "Bebidas"
            };

            _context.Categoria.Add(existente);
            await _context.SaveChangesAsync();

            int id = existente.IdCategoria;

            // Dejar de rastrear la instancia original
            _context.Entry(existente).State = EntityState.Detached;

            var categoria = new Categorium
            {
                IdCategoria = id,
                Nombre = "Bebidas actualizadas"
            };

            // Act
            var resultado = await _controller.Editar(categoria);

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo(nameof(CategoriasController.Index)));

            // Consultar la categoría actualizada sin reutilizar una entidad rastreada
            var actualizada = await _context.Categoria
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCategoria == id);

            Assert.That(actualizada, Is.Not.Null);
            Assert.That(
                actualizada!.Nombre,
                Is.EqualTo("Bebidas actualizadas"));
        }

        [Test]
        public async Task EditarPost_ModeloInvalido_DevuelveVistaSinEditar()
        {
            // Arrange
            var existente = new Categorium
            {
                Nombre = "Bebidas"
            };

            _context.Categoria.Add(existente);
            await _context.SaveChangesAsync();

            var categoria = new Categorium
            {
                IdCategoria = existente.IdCategoria,
                Nombre = ""
            };

            _controller.ModelState.AddModelError(
                nameof(Categorium.Nombre),
                "El nombre es obligatorio.");

            // Act
            var resultado = await _controller.Editar(categoria);

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;

            Assert.That(vista.Model, Is.SameAs(categoria));

            var sinCambios = await _context.Categoria
                .FindAsync(existente.IdCategoria);

            Assert.That(sinCambios, Is.Not.Null);
            Assert.That(sinCambios!.Nombre, Is.EqualTo("Bebidas"));
        }

        // =========================================
        // ELIMINAR
        // =========================================

        [Test]
        public async Task Eliminar_InvocaEliminacionYRedirigeAIndex()
        {
            // Arrange
            var categoria = new Categorium
            {
                Nombre = "Bebidas"
            };

            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();

            int id = categoria.IdCategoria;

            // Act
            var resultado = await _controller.Eliminar(id);

            // Assert
            Assert.That(resultado, Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo(nameof(CategoriasController.Index)));

            var eliminada = await _context.Categoria.FindAsync(id);

            Assert.That(eliminada, Is.Null);
        }
    }
}