using DiarsFinal.Controllers;
using DiarsFinal.Datos;
using DiarsFinal.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace TestProject1.UnitTests.Data
{
    public class UnitTestProductoData
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private ProductoData _productoData = null!;

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
        }

        [TearDown]
        public async Task TearDown()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private async Task<Producto> CrearProducto(
            string nombre,
            string tipo,
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
                Descripcion = nombre,
                Tipo = tipo,
                Activo = activo,
                Consumo = 1,
                Precio = 20m,
                IdCategoria = categoria.IdCategoria.Value
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return producto;
        }

        [Test]
        public async Task ListarProductosFinales_SoloDevuelveTipoUno()
        {
            // Arrange
            await CrearProducto("Producto final A", "1");
            await CrearProducto("Producto final B", "1");
            await CrearProducto("Producto personalizado", "0");

            // Act
            var resultado = await _productoData.ListarProductosFinales();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(2));
            Assert.That(resultado.All(p => p.Tipo == "1"), Is.True);
            Assert.That(
                resultado.Select(p => p.Nombre),
                Does.Contain("Producto final A"));
            Assert.That(
                resultado.Select(p => p.Nombre),
                Does.Contain("Producto final B"));
        }

        [Test]
        public async Task ListarProductosFinales_SinProductosFinales_DevuelveListaVacia()
        {
            // Arrange
            await CrearProducto("Producto personalizado", "0");

            // Act
            var resultado = await _productoData.ListarProductosFinales();

            // Assert
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task AgregarProducto_ProductoValido_PersisteLosCambios()
        {
            // Arrange
            var categoria = new Categorium
            {
                Nombre = "Categoría de prueba"
            };

            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();

            var producto = new Producto
            {
                Nombre = "Producto nuevo",
                Descripcion = "Producto para prueba",
                Tipo = "1",
                Activo = "1",
                Consumo = 2,
                Precio = 35m,
                IdCategoria = categoria.IdCategoria.Value
            };

            // Act
            await _productoData.AgregarProducto(producto);

            // Assert
            var guardado = await _context.Productos
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.IdProducto == producto.IdProducto);

            Assert.That(guardado, Is.Not.Null);
            Assert.That(guardado!.Nombre, Is.EqualTo("Producto nuevo"));
            Assert.That(guardado.Precio, Is.EqualTo(35m));
            Assert.That(guardado.Tipo, Is.EqualTo("1"));
            Assert.That(guardado.Activo, Is.EqualTo("1"));
        }

        [Test]
        public async Task CambiarEstado_ProductoActivo_CambiaAInactivo()
        {
            // Arrange
            var producto = await CrearProducto(
                "Producto de prueba",
                "1",
                "1");

            // Act
            await _productoData.CambiarEstado(producto.IdProducto);

            // Assert
            var actualizado = await _context.Productos
                .AsNoTracking()
                .SingleAsync(p => p.IdProducto == producto.IdProducto);

            Assert.That(actualizado.Activo, Is.EqualTo("0"));
        }

        [Test]
        public async Task CambiarEstado_ProductoInactivo_CambiaAActivo()
        {
            // Arrange
            var producto = await CrearProducto(
                "Producto de prueba",
                "1",
                "0");

            // Act
            await _productoData.CambiarEstado(producto.IdProducto);

            // Assert
            var actualizado = await _context.Productos
                .AsNoTracking()
                .SingleAsync(p => p.IdProducto == producto.IdProducto);

            Assert.That(actualizado.Activo, Is.EqualTo("1"));
        }

        [Test]
        public async Task CambiarEstado_IdInexistente_NoGeneraExcepcion()
        {
            // Arrange
            const int idInexistente = 999999;

            // Act y Assert
            Assert.DoesNotThrowAsync(async () =>
                await _productoData.CambiarEstado(idInexistente));
        }

        [Test]
        public async Task ListarProductos_ExistenProductos_DevuelveTodosConCategoria()
        {
            // Arrange
            var categoria1 = new Categorium
            {
                Nombre = "Bebidas"
            };

            var categoria2 = new Categorium
            {
                Nombre = "Postres"
            };

            _context.Categoria.AddRange(categoria1, categoria2);
            await _context.SaveChangesAsync();

            var producto1 = new Producto
            {
                Nombre = "Producto A",
                Descripcion = "Descripción A",
                Tipo = "1",
                Activo = "1",
                Consumo = 1,
                Precio = 20m,
                IdCategoria = categoria1.IdCategoria.Value
            };

            var producto2 = new Producto
            {
                Nombre = "Producto B",
                Descripcion = "Descripción B",
                Tipo = "0",
                Activo = "1",
                Consumo = 2,
                Precio = 30m,
                IdCategoria = categoria2.IdCategoria.Value
            };

            _context.Productos.AddRange(producto1, producto2);
            await _context.SaveChangesAsync();

            var productoData = new ProductoData(_context);

            // Act
            var resultado = await productoData.ListarProductos();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(2));

            Assert.That(
                resultado.Any(p =>
                    p.Nombre == "Producto A" &&
                    p.IdCategoriaNavigation != null &&
                    p.IdCategoriaNavigation.Nombre == "Bebidas"),
                Is.True);

            Assert.That(
                resultado.Any(p =>
                    p.Nombre == "Producto B" &&
                    p.IdCategoriaNavigation != null &&
                    p.IdCategoriaNavigation.Nombre == "Postres"),
                Is.True);
        }

        [Test]
        public async Task ListarProductos_SinProductos_DevuelveListaVacia()
        {
            // Arrange
            var productoData = new ProductoData(_context);

            // Act
            var resultado = await productoData.ListarProductos();

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task AgregarCompatibilidad_CompatibilidadNueva_GuardaRelacion()
        {
            // Arrange
            var productoA = await CrearProducto("Producto A", "1");
            var productoB = await CrearProducto("Producto B", "1");

            var productoData = new ProductoData(_context);

            // Act
            await productoData.AgregarCompatibilidad(
                productoA.IdProducto,
                productoB.IdProducto);

            // Assert
            var compatibilidad = await _context.Compatibilidades
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.IdProductoA == productoA.IdProducto &&
                    c.IdProductoB == productoB.IdProducto);

            Assert.That(compatibilidad, Is.Not.Null);
            Assert.That(compatibilidad!.IdProductoA, Is.EqualTo(productoA.IdProducto));
            Assert.That(compatibilidad.IdProductoB, Is.EqualTo(productoB.IdProducto));
        }

        [Test]
        public async Task AgregarCompatibilidad_RelacionYaExiste_NoDuplicaRegistro()
        {
            // Arrange
            var productoA = await CrearProducto("Producto A", "1");
            var productoB = await CrearProducto("Producto B", "1");

            _context.Compatibilidades.Add(new Compatibilidad
            {
                IdProductoA = productoA.IdProducto,
                IdProductoB = productoB.IdProducto
            });

            await _context.SaveChangesAsync();

            var productoData = new ProductoData(_context);

            // Act
            await productoData.AgregarCompatibilidad(
                productoA.IdProducto,
                productoB.IdProducto);

            // Assert
            var cantidad = await _context.Compatibilidades
                .CountAsync(c =>
                    c.IdProductoA == productoA.IdProducto &&
                    c.IdProductoB == productoB.IdProducto);

            Assert.That(cantidad, Is.EqualTo(1));
        }

        [Test]
        public async Task AgregarCompatibilidad_SeInvocaDosVeces_SoloGuardaUnaRelacion()
        {
            // Arrange
            var productoA = await CrearProducto("Producto A", "1");
            var productoB = await CrearProducto("Producto B", "1");

            var productoData = new ProductoData(_context);

            // Act
            await productoData.AgregarCompatibilidad(
                productoA.IdProducto,
                productoB.IdProducto);

            await productoData.AgregarCompatibilidad(
                productoA.IdProducto,
                productoB.IdProducto);

            // Assert
            var cantidad = await _context.Compatibilidades
                .CountAsync(c =>
                    c.IdProductoA == productoA.IdProducto &&
                    c.IdProductoB == productoB.IdProducto);

            Assert.That(cantidad, Is.EqualTo(1));
        }

        [Test]
        public async Task ObtenerProducto_IdExistente_DevuelveProductoConCategoria()
        {
            // Arrange
            var producto = await CrearProducto("Producto A", "1");

            var productoData = new ProductoData(_context);

            // Act
            var resultado = await productoData.ObtenerProducto(
                producto.IdProducto);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado!.IdProducto, Is.EqualTo(producto.IdProducto));
            Assert.That(resultado.Nombre, Is.EqualTo("Producto A"));
            Assert.That(resultado.IdCategoriaNavigation, Is.Not.Null);
        }

        [Test]
        public async Task ObtenerProducto_IdInexistente_DevuelveNull()
        {
            // Arrange
            var productoData = new ProductoData(_context);

            // Act
            var resultado = await productoData.ObtenerProducto(999999);

            // Assert
            Assert.That(resultado, Is.Null);
        }

        [Test]
        public async Task ObtenerProducto_ConCompatibilidades_CargaProductosCompatibles()
        {
            // Arrange
            var productoA = await CrearProducto("Producto A", "1");
            var productoB = await CrearProducto("Producto B", "1");

            _context.Compatibilidades.Add(new Compatibilidad
            {
                IdProductoA = productoA.IdProducto,
                IdProductoB = productoB.IdProducto
            });

            await _context.SaveChangesAsync();

            var productoData = new ProductoData(_context);

            // Act
            var resultado = await productoData.ObtenerProducto(
                productoA.IdProducto);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado!.Compatibilidades, Has.Count.EqualTo(1));

            var compatibilidad = resultado.Compatibilidades.First();

            Assert.That(compatibilidad.ProductoB, Is.Not.Null);
            Assert.That(
                compatibilidad.ProductoB.Nombre,
                Is.EqualTo("Producto B"));
        }

        [Test]
        public async Task ListarProductosCatalogo_SoloDevuelveProductosActivosDeTipoCero()
        {
            // Arrange
            var productoCatalogo = await CrearProducto(
                "Producto catálogo", "0", "1");

            await CrearProducto(
                "Producto inactivo", "0", "0");

            await CrearProducto(
                "Producto final", "1", "1");

            var productoData = new ProductoData(_context);

            // Act
            var resultado = await productoData.ListarProductosCatalogo();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(1));
            Assert.That(
                resultado[0].IdProducto,
                Is.EqualTo(productoCatalogo.IdProducto));

            Assert.That(resultado[0].Tipo, Is.EqualTo("0"));
            Assert.That(resultado[0].Activo, Is.EqualTo("1"));
            Assert.That(resultado[0].IdCategoriaNavigation, Is.Not.Null);
        }

        [Test]
        public async Task ListarProductosCatalogo_SinProductosValidos_DevuelveListaVacia()
        {
            // Arrange
            await CrearProducto("Producto inactivo", "0", "0");
            await CrearProducto("Producto final", "1", "1");

            var productoData = new ProductoData(_context);

            // Act
            var resultado = await productoData.ListarProductosCatalogo();

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task ListarProductosCatalogo_TodosSonValidos_DevuelveTodos()
        {
            // Arrange
            await CrearProducto("Producto A", "0", "1");
            await CrearProducto("Producto B", "0", "1");
            await CrearProducto("Producto C", "0", "1");

            var productoData = new ProductoData(_context);

            // Act
            var resultado = await productoData.ListarProductosCatalogo();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(3));
            Assert.That(
                resultado.All(p => p.Tipo == "0" && p.Activo == "1"),
                Is.True);
            Assert.That(
                resultado.All(p => p.IdCategoriaNavigation != null),
                Is.True);
        }

        [Test]
        public async Task EliminarCompatibilidad_RelacionExistente_EliminaRegistro()
        {
            // Arrange
            var productoA = await CrearProducto("Producto A", "1");
            var productoB = await CrearProducto("Producto B", "1");

            _context.Compatibilidades.Add(new Compatibilidad
            {
                IdProductoA = productoA.IdProducto,
                IdProductoB = productoB.IdProducto
            });

            await _context.SaveChangesAsync();

            var productoData = new ProductoData(_context);

            // Act
            await productoData.EliminarCompatibilidad(
                productoA.IdProducto,
                productoB.IdProducto);

            // Assert
            var existe = await _context.Compatibilidades
                .AnyAsync(c =>
                    c.IdProductoA == productoA.IdProducto &&
                    c.IdProductoB == productoB.IdProducto);

            Assert.That(existe, Is.False);
        }

        [Test]
        public async Task EliminarCompatibilidad_RelacionInexistente_NoGeneraExcepcion()
        {
            // Arrange
            var productoData = new ProductoData(_context);

            // Act y Assert
            Assert.DoesNotThrowAsync(async () =>
                await productoData.EliminarCompatibilidad(999, 888));
        }

        [Test]
        public async Task EliminarCompatibilidad_EliminaSoloLaRelacionIndicada()
        {
            // Arrange
            var productoA = await CrearProducto("Producto A", "1");
            var productoB = await CrearProducto("Producto B", "1");
            var productoC = await CrearProducto("Producto C", "1");

            _context.Compatibilidades.AddRange(
                new Compatibilidad
                {
                    IdProductoA = productoA.IdProducto,
                    IdProductoB = productoB.IdProducto
                },
                new Compatibilidad
                {
                    IdProductoA = productoA.IdProducto,
                    IdProductoB = productoC.IdProducto
                });

            await _context.SaveChangesAsync();

            var productoData = new ProductoData(_context);

            // Act
            await productoData.EliminarCompatibilidad(
                productoA.IdProducto,
                productoB.IdProducto);

            // Assert
            var relacionesRestantes = await _context.Compatibilidades
                .AsNoTracking()
                .ToListAsync();

            Assert.That(relacionesRestantes, Has.Count.EqualTo(1));
            Assert.That(
                relacionesRestantes[0].IdProductoA,
                Is.EqualTo(productoA.IdProducto));
            Assert.That(
                relacionesRestantes[0].IdProductoB,
                Is.EqualTo(productoC.IdProducto));
        }

        [Test]
        public async Task EditarProducto_ProductoExistente_ActualizaDatos()
        {
            // Arrange
            var producto = await CrearProducto("Producto original", "1");

            var productoData = new ProductoData(_context);

            // Modificar los datos
            producto.Nombre = "Producto actualizado";
            producto.Descripcion = "Nueva descripción";
            producto.Precio = 45m;
            producto.Consumo = 3;

            // Act
            await productoData.EditarProducto(producto);

            // Assert
            var resultado = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProducto == producto.IdProducto);

            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado!.Nombre, Is.EqualTo("Producto actualizado"));
            Assert.That(resultado.Descripcion, Is.EqualTo("Nueva descripción"));
            Assert.That(resultado.Precio, Is.EqualTo(45m));
            Assert.That(resultado.Consumo, Is.EqualTo(3));
        }

        [Test]
        public async Task EditarProducto_CambiaEstado_ConservaLosCambios()
        {
            // Arrange
            var producto = await CrearProducto("Producto A", "1", "1");

            var productoData = new ProductoData(_context);

            producto.Activo = "0";

            // Act
            await productoData.EditarProducto(producto);

            // Assert
            var resultado = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProducto == producto.IdProducto);

            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado!.Activo, Is.EqualTo("0"));
        }

        [Test]
        public async Task EliminarProducto_IdExistente_EliminaYDevuelveTrue()
        {
            // Arrange
            var producto = await CrearProducto("Producto a eliminar", "1");

            var productoData = new ProductoData(_context);

            // Act
            var resultado = await productoData.EliminarProducto(
                producto.IdProducto);

            // Assert
            Assert.That(resultado, Is.True);

            var existe = await _context.Productos
                .AsNoTracking()
                .AnyAsync(p => p.IdProducto == producto.IdProducto);

            Assert.That(existe, Is.False);
        }

        [Test]
        public async Task EliminarProducto_IdInexistente_DevuelveFalse()
        {
            // Arrange
            var productoData = new ProductoData(_context);

            // Act
            var resultado = await productoData.EliminarProducto(999999);

            // Assert
            Assert.That(resultado, Is.False);
        }


    }
}