using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using DiarsFinal.Controllers;
using DiarsFinal.Data;
using DiarsFinal.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestEspecificacionesController
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private EspecificacionesData _data = null!;
        private EspecificacionesController _controller = null!;

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

            _data = new EspecificacionesData(_context);
            _controller = new EspecificacionesController(_data);
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
        public async Task Index_SinEspecificaciones_DevuelveVistaConListaVacia()
        {
            // Act
            var resultado = await _controller.Index();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());

            var vista = (ViewResult)resultado;
            var lista = vista.Model as IEnumerable<Especificacione>;

            Assert.That(lista, Is.Not.Null);
            Assert.That(lista, Is.Empty);
        }

        // =========================================
        // EDITAR - GET
        // =========================================

        [Test]
        public async Task EditarGet_IdInexistente_DevuelveNotFound()
        {
            // Arrange
            int id = -999;

            // Act
            var resultado = await _controller.Editar(id);

            // Assert
            Assert.That(resultado, Is.InstanceOf<NotFoundResult>());
        }

        // =========================================
        // EDITAR - POST
        // =========================================

        [Test]
        public async Task EditarPost_SeEjecuta_RedireccionaAIndex()
        {
            // Arrange
            var model = new Especificacione();

            // Act
            var resultado = await _controller.Editar(model);

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo("Index"));
        }
    }
}