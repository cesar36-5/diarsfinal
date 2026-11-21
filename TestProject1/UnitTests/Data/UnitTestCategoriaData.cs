using DiarsFinal.Controllers;
using DiarsFinal.Data;
using DiarsFinal.Datos;
using DiarsFinal.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TestProject1.UnitTests.Data
{
    [TestFixture]
    public class UnitTestCategoriaData
    {
        private DiarsContext _context = null!;
        private CategoriaData _categoriaData = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DiarsContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new DiarsContext(options);
            _categoriaData = new CategoriaData(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // LISTAR

        [Test]
        public async Task ListarCategorias_ConDatos_DevuelveCategorias()
        {
            // Arrange: crear categorías con nombres válidos
            var categorias = new List<Categorium>
                {
                    new Categorium { Nombre = "Accesorios" },
                    new Categorium { Nombre = "Ropa" },
                    new Categorium { Nombre = "Calzado" }
                };

            _context.Categoria.AddRange(categorias);
            await _context.SaveChangesAsync();

            // Act: obtener todas las categorías
            var resultado = await _categoriaData.ListarCategorias();

            // Assert: verificar la cantidad y los datos
            Assert.That(resultado, Has.Count.EqualTo(3));
            Assert.That(resultado.Select(c => c.Nombre),
                Is.EquivalentTo(new[] { "Accesorios", "Ropa", "Calzado" }));
        }

        [Test]
        public async Task ListarCategorias_SinDatos_DevuelveListaVacia()
        {
            var resultado = await _categoriaData.ListarCategorias();

            Assert.That(resultado, Is.Empty);
        }

        // OBTENER

        [Test]
        public async Task ObtenerCategoria_IdInexistente_DevuelveNull()
        {
            var resultado =
                await _categoriaData.ObtenerCategoria(999999);

            Assert.That(resultado, Is.Null);
        }

        // AGREGAR

        [Test]
        public async Task AgregarCategoria_CategoriaValida_GuardaRegistro()
        {
            var categoria = new Categorium
            {
                Nombre = "Accesorios"
            };

            await _categoriaData.AgregarCategoria(categoria);

            var resultado = await _context.Categoria.ToListAsync();

            Assert.That(resultado, Has.Count.EqualTo(1));
            Assert.That(resultado[0].Nombre, Is.EqualTo("Accesorios"));
        }

        [Test]
        public void AgregarCategoria_CategoriaNula_LanzaExcepcion()
        {
            Assert.ThrowsAsync<ArgumentNullException>(
                async () =>
                    await _categoriaData.AgregarCategoria(null!)
            );
        }

        // EDITAR

        [Test]
        public async Task EditarCategoria_CategoriaExistente_ActualizaRegistro()
        {
            // Arrange: crear y guardar la categoría original.
            var categoria = new Categorium
            {
                Nombre = "Accesorios"
            };

            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();

            var id = categoria.IdCategoria;

            // Simular una entidad que llega para ser editada.
            _context.Entry(categoria).State = EntityState.Detached;

            var categoriaEditada = new Categorium
            {
                IdCategoria = id,
                Nombre = "Repuestos"
            };

            // Act: ejecutar la edición.
            await _categoriaData.EditarCategoria(categoriaEditada);

            // Assert: comprobar que el nombre se actualizó.
            var resultado = await _context.Categoria
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.IdCategoria == id);

            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado!.Nombre, Is.EqualTo("Repuestos"));
        }

        [Test]
        public void EditarCategoria_CategoriaNula_LanzaExcepcion()
        {
            Assert.ThrowsAsync<ArgumentNullException>(
                async () =>
                    await _categoriaData.EditarCategoria(null!)
            );
        }

        // ELIMINAR

        [Test]
        public async Task EliminarCategoria_IdInexistente_DevuelveFalse()
        {
            var resultado =
                await _categoriaData.EliminarCategoria(999999);

            Assert.That(resultado, Is.False);
        }

        [Test]
        public async Task EliminarCategoria_IdExistente_DevuelveTrue()
        {
            // Arrange: crear y guardar una categoría válida
            var categoria = new Categorium
            {
                Nombre = "Accesorios"
            };

            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();

            var id = categoria.IdCategoria;

            // Act: eliminar la categoría
            var resultado = await _categoriaData.EliminarCategoria(id);

            // Assert: comprobar el resultado y que ya no exista
            Assert.That(resultado, Is.True);

            var categoriaEliminada = await _context.Categoria.FindAsync(id);

            Assert.That(categoriaEliminada, Is.Null);
        }
    }
}