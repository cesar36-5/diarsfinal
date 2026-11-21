using DiarsFinal.Controllers;
using DiarsFinal.Data;
using DiarsFinal.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace TestProject1.UnitTests.Data
{
    public class UnitTestPersonalizadoData
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private PersonalizadoData _personalizadoData = null!;

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

            _personalizadoData = new PersonalizadoData(_context);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private async Task CrearTresCategorias()
        {
            _context.Categoria.AddRange(
                new Categorium { Nombre = "Categoría 1" },
                new Categorium { Nombre = "Categoría 2" },
                new Categorium { Nombre = "Categoría 3" }
            );

            await _context.SaveChangesAsync();
        }

        private async Task<Producto> CrearProductoParaCategoria(
            string nombre,
            int idCategoria,
            string activo = "1")
        {
            var producto = new Producto
            {
                Nombre = nombre,
                Descripcion = nombre,
                Tipo = "0",
                Activo = activo,
                Consumo = 1,
                Precio = 20m,
                IdCategoria = idCategoria
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return producto;
        }

        [Test]
        public async Task ObtenerBases_FiltraActivosYCategoria_OrdenaPorNombre()
        {
            // Arrange
            await CrearTresCategorias();

            await CrearProductoParaCategoria("Zeta", 3, "1");
            await CrearProductoParaCategoria("Alfa", 3, "1");
            await CrearProductoParaCategoria("Beta", 3, "0");
            await CrearProductoParaCategoria("Otro producto", 2, "1");

            // Act
            var resultado = await _personalizadoData.ObtenerBases();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(2));

            Assert.That(
                resultado.Select(p => p.Nombre),
                Is.EqualTo(new[] { "Alfa", "Zeta" }));

            Assert.That(resultado.All(p => p.Activo == "1"), Is.True);
            Assert.That(resultado.All(p => p.IdCategoria == 3), Is.True);
        }

        [Test]
        public async Task ObtenerBases_SinProductosValidos_DevuelveListaVacia()
        {
            // Arrange
            await CrearTresCategorias();

            await CrearProductoParaCategoria("Producto inactivo", 3, "0");
            await CrearProductoParaCategoria("Producto de otra categoría", 2, "1");

            // Act
            var resultado = await _personalizadoData.ObtenerBases();

            // Assert
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task ObtenerBases_VariosProductosValidos_DevuelveTodos()
        {
            // Arrange
            await CrearTresCategorias();

            await CrearProductoParaCategoria("Base C", 3);
            await CrearProductoParaCategoria("Base A", 3);
            await CrearProductoParaCategoria("Base B", 3);

            // Act
            var resultado = await _personalizadoData.ObtenerBases();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(3));

            Assert.That(
                resultado.Select(p => p.Nombre),
                Is.EqualTo(new[] { "Base A", "Base B", "Base C" }));
        }

        [Test]
        public async Task ObtenerCompatibles_ProductosValidos_DevuelveOrdenados()
        {
            // Arrange
            await CrearTresCategorias();

            var baseProducto = await CrearProductoParaCategoria("Base", 3);
            var productoZ = await CrearProductoParaCategoria("Zeta", 3);
            var productoA = await CrearProductoParaCategoria("Alfa", 3);
            var productoInactivo = await CrearProductoParaCategoria(
                "Beta", 3, "0");

            _context.Compatibilidades.AddRange(
                new Compatibilidad
                {
                    IdProductoA = baseProducto.IdProducto,
                    IdProductoB = productoZ.IdProducto
                },
                new Compatibilidad
                {
                    IdProductoA = baseProducto.IdProducto,
                    IdProductoB = productoA.IdProducto
                },
                new Compatibilidad
                {
                    IdProductoA = baseProducto.IdProducto,
                    IdProductoB = productoInactivo.IdProducto
                }
            );

            await _context.SaveChangesAsync();

            // Act
            var resultado = await _personalizadoData.ObtenerCompatibles(
                baseProducto.IdProducto);

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(2));

            Assert.That(
                resultado.Select(p => p.Nombre),
                Is.EqualTo(new[] { "Alfa", "Zeta" }));

            Assert.That(resultado.All(p => p.Activo == "1"), Is.True);
        }

        [Test]
        public async Task ObtenerCompatibles_SoloIncluyeRelacionesDeLaBaseSolicitada()
        {
            // Arrange
            await CrearTresCategorias();

            var baseA = await CrearProductoParaCategoria("Base A", 3);
            var baseB = await CrearProductoParaCategoria("Base B", 3);

            var compatibleA = await CrearProductoParaCategoria("Compatible A", 3);
            var compatibleB = await CrearProductoParaCategoria("Compatible B", 3);

            _context.Compatibilidades.AddRange(
                new Compatibilidad
                {
                    IdProductoA = baseA.IdProducto,
                    IdProductoB = compatibleA.IdProducto
                },
                new Compatibilidad
                {
                    IdProductoA = baseB.IdProducto,
                    IdProductoB = compatibleB.IdProducto
                }
            );

            await _context.SaveChangesAsync();

            // Act
            var resultado = await _personalizadoData.ObtenerCompatibles(
                baseA.IdProducto);

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(1));
            Assert.That(resultado[0].Nombre, Is.EqualTo("Compatible A"));
        }

        [Test]
        public async Task ObtenerCompatibles_SinCompatibilidades_DevuelveListaVacia()
        {
            // Arrange
            await CrearTresCategorias();

            var baseProducto = await CrearProductoParaCategoria("Base", 3);

            // Act
            var resultado = await _personalizadoData.ObtenerCompatibles(
                baseProducto.IdProducto);

            // Assert
            Assert.That(resultado, Is.Empty);
        }


    }
}