using DiarsFinal.Controllers;
using DiarsFinal.Data;
using DiarsFinal.Datos;
using DiarsFinal.Models;
using DiarsFinal.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TestProject1.UnitTests.Data
{
    [TestFixture]
    public class UnitTestPedidoData
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private PedidoData _pedidoData = null!;

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

            var foreignKeys = await _context.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT "from" || ' -> ' || "table" || '.' || "to" AS "Value"
                    FROM pragma_foreign_key_list('Producto')
                    """)
                .ToListAsync();

            TestContext.WriteLine(
                string.Join(Environment.NewLine, foreignKeys));

            _pedidoData = new PedidoData(_context);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        // GENERAR CÓDIGO: devuelve un código de seis cifras
        [Test]
        public async Task GenerarCodigoPedido_SinPedidos_DevuelveCodigoValido()
        {
            var codigo = await _pedidoData.GenerarCodigoPedido();

            Assert.That(codigo, Is.InRange(100000, 999998));
        }

        // GENERAR CÓDIGO: no debe repetir un código existente
        [Test]
        public async Task GenerarCodigoPedido_ConCodigoExistente_NoLoRepite()
        {
            var pedidoExistente = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                FechaEntrega = new DateTime(2026, 10, 19),
                FechaCreacion = DateTime.Now,
                Precio = 50m,
                Estado = "1",
                Tipo = "0",
                Comentario = "",
                Codigo = 123456
            };

            _context.Pedidos.Add(pedidoExistente);
            await _context.SaveChangesAsync();

            var codigo = await _pedidoData.GenerarCodigoPedido();

            Assert.That(codigo, Is.InRange(100000, 999998));
            Assert.That(codigo, Is.Not.EqualTo(123456));
        }

        // GENERAR PEDIDO: sin especificación ni capacidad semanal
        [Test]
        public async Task GenerarPedido_SinCapacidadBase_DevuelveMenosUno()
        {
            var fecha = new DateTime(2026, 10, 19);

            var carrito = new System.Collections.Generic.List<CarritoItem>();

            var resultado = await _pedidoData.GenerarPedido(
                carrito,
                "Cliente de prueba",
                null,
                "12345678",
                "999999999",
                fecha);

            Assert.That(resultado, Is.EqualTo(-1));
            Assert.That(await _context.Pedidos.CountAsync(), Is.EqualTo(0));
        }

        // GENERAR PEDIDO: el consumo supera la capacidad disponible
        [Test]
        public async Task GenerarPedido_ConsumoExcedeCapacidad_DevuelveMenosUno()
        {
            var fecha = new DateTime(2026, 10, 19);

            _context.Especificaciones.Add(new Especificacione
            {
                DiasTrabajo = "2026-10-19",
                CapacidadTotal = 10,
                CapacidadActual = 2
            });

            await _context.SaveChangesAsync();

            var categoria = new Categorium
            {
                Nombre = "Categoría de prueba"
            };

            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();

            var producto = new Producto
            {
                Descripcion = "Producto de prueba",
                Tipo = "0",
                Activo = "1",
                Consumo = 3,
                Precio = 20m,
                IdCategoria = categoria.IdCategoria
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            var carrito = new System.Collections.Generic.List<CarritoItem>
                {
                    new CarritoItem
                    {
                        IdProducto = producto.IdProducto,
                        Cantidad = 1
                    }
                };

            var resultado = await _pedidoData.GenerarPedido(
                carrito,
                "Cliente de prueba",
                null,
                "12345678",
                "999999999",
                fecha);

            Assert.That(resultado, Is.EqualTo(-1));
            Assert.That(await _context.Pedidos.CountAsync(), Is.EqualTo(0));
        }

        // GENERAR PEDIDO: crea la especificación de la fecha
        // utilizando la capacidad total del día de la semana.
        [Test]
        public async Task GenerarPedido_FechaSinEspecificacion_CreaCapacidadDiaria()
        {
            var fecha = new DateTime(2026, 10, 19); // Lunes

            // Capacidad base del lunes
            _context.Especificaciones.Add(new Especificacione
            {
                DiasTrabajo = "Lunes",
                CapacidadTotal = 10,
                CapacidadActual = 10
            });

            await _context.SaveChangesAsync();

            // Crear primero la categoría requerida
            var categoria = new Categorium
            {
                Nombre = "Categoría de prueba"
            };

            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();

            // Crear un único producto válido
            var producto = new Producto
            {
                Descripcion = "Producto de prueba",
                Tipo = "0",
                Activo = "1",
                Consumo = 3,
                Precio = 20m,
                IdCategoria = categoria.IdCategoria
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            // Dos unidades: consumo total = 3 * 2 = 6
            var carrito = new System.Collections.Generic.List<CarritoItem>
    {
        new CarritoItem
        {
            IdProducto = producto.IdProducto,
            Cantidad = 2
        }
    };

            var resultado = await _pedidoData.GenerarPedido(
                carrito,
                "Cliente de prueba",
                "Pedido de prueba",
                "12345678",
                "999999999",
                fecha);

            Assert.That(resultado, Is.InRange(100000, 999998));

            // Verificar que se creó la especificación de la fecha
            var especificacionDiaria = await _context.Especificaciones
                .FirstOrDefaultAsync(e => e.DiasTrabajo == "2026-10-19");

            Assert.That(especificacionDiaria, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(especificacionDiaria!.CapacidadTotal, Is.EqualTo(10));
                Assert.That(especificacionDiaria.CapacidadActual, Is.EqualTo(4));
            });

            // Verificar el pedido generado
            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.Codigo == resultado);

            Assert.That(pedido, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(pedido!.Nombre, Is.EqualTo("Cliente de prueba"));
                Assert.That(pedido.Precio, Is.EqualTo(40m));
                Assert.That(pedido.Dni, Is.EqualTo("12345678"));
                Assert.That(pedido.Telefono, Is.EqualTo("999999999"));
            });

            // Verificar que se guardó el detalle del pedido
            Assert.That(
                await _context.DetallePedidos.CountAsync(
                    d => d.IdPedido == pedido!.IdPedido),
                Is.EqualTo(1));
        }

        [Test]
        public async Task ObtenerPedidosDashboard_FiltraYOrdenaPedidos()
        {
            // Arrange
            _context.Pedidos.AddRange(
                new Pedido
                {
                    Nombre = "Pedido posterior",
                    Estado = "1",
                    Tipo = "0",
                    Comentario = "",
                    Codigo = 100001,
                    FechaEntrega = new DateTime(2026, 10, 25),
                    FechaCreacion = new DateTime(2026, 10, 17),
                    Precio = 100m,
                    Dni = "12345678",
                    Telefono = "999999999"
                },
                new Pedido
                {
                    Nombre = "Pedido anterior",
                    Estado = "1",
                    Tipo = "0",
                    Comentario = "",
                    Codigo = 100002,
                    FechaEntrega = new DateTime(2026, 10, 20),
                    FechaCreacion = new DateTime(2026, 10, 17),
                    Precio = 50m,
                    Dni = "12345678",
                    Telefono = "999999999"
                },
                new Pedido
                {
                    Nombre = "Pedido cancelado",
                    Estado = "2",
                    Tipo = "0",
                    Comentario = "",
                    Codigo = 100003,
                    FechaEntrega = new DateTime(2026, 10, 18),
                    FechaCreacion = new DateTime(2026, 10, 17),
                    Precio = 30m,
                    Dni = "12345678",
                    Telefono = "999999999"
                },
                new Pedido
                {
                    Nombre = "Pedido estado cinco",
                    Estado = "5",
                    Tipo = "0",
                    Comentario = "",
                    Codigo = 100004,
                    FechaEntrega = new DateTime(2026, 10, 19),
                    FechaCreacion = new DateTime(2026, 10, 17),
                    Precio = 40m,
                    Dni = "12345678",
                    Telefono = "999999999"
                },
                new Pedido
                {
                    Nombre = "Pedido estado cero",
                    Estado = "0",
                    Tipo = "0",
                    Comentario = "",
                    Codigo = 100005,
                    FechaEntrega = new DateTime(2026, 10, 17),
                    FechaCreacion = new DateTime(2026, 10, 17),
                    Precio = 20m,
                    Dni = "12345678",
                    Telefono = "999999999"
                }
            );

            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ObtenerPedidosDashboard();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(
                    resultado[0].Pedido.Nombre,
                    Is.EqualTo("Pedido anterior"));

                Assert.That(
                    resultado[1].Pedido.Nombre,
                    Is.EqualTo("Pedido posterior"));

                Assert.That(
                    resultado.All(p =>
                        p.DetallesPedido != null &&
                        p.DetallesPersonalizados != null),
                    Is.True);
            });
        }

        [Test]
        public async Task ObtenerPedidosDashboard_SinPedidos_DevuelveListaVacia()
        {
            // Act
            var resultado = await _pedidoData.ObtenerPedidosDashboard();

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task ObtenerPedidosDashboard_IncluyeDetallesPedido()
        {
            // Arrange
            var pedido = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "1",
                Tipo = "0",
                Comentario = "",
                Codigo = 100001,
                FechaEntrega = new DateTime(2026, 10, 20),
                FechaCreacion = new DateTime(2026, 10, 17),
                Precio = 50m
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ObtenerPedidosDashboard();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(1));
            Assert.That(resultado[0].DetallesPedido, Is.Not.Null);
            Assert.That(resultado[0].DetallesPedido, Is.Empty);
        }

        [Test]
        public async Task ObtenerPedidosDashboard_IncluyeDetallesPersonalizados()
        {
            // Arrange
            var pedido = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "1",
                Tipo = "0",
                Comentario = "",
                Codigo = 100002,
                FechaEntrega = new DateTime(2026, 10, 20),
                FechaCreacion = new DateTime(2026, 10, 17),
                Precio = 50m
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ObtenerPedidosDashboard();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(1));
            Assert.That(resultado[0].DetallesPersonalizados, Is.Not.Null);
            Assert.That(resultado[0].DetallesPersonalizados, Is.Empty);
        }

        [Test]
        public async Task ObtenerPedidosDashboard_PedidosSinDetalles_DevuelveListasVacias()
        {
            // Arrange
            var pedido = new Pedido
            {
                Nombre = "Cliente sin detalles",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "1",
                Tipo = "0",
                Comentario = "",
                Codigo = 100003,
                FechaEntrega = new DateTime(2026, 10, 21),
                FechaCreacion = new DateTime(2026, 10, 17),
                Precio = 0m
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ObtenerPedidosDashboard();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(1));

            Assert.Multiple(() =>
            {
                Assert.That(resultado[0].Pedido.Nombre,
                    Is.EqualTo("Cliente sin detalles"));

                Assert.That(resultado[0].DetallesPedido, Is.Empty);

                Assert.That(resultado[0].DetallesPersonalizados, Is.Empty);
            });
        }

        [Test]
        public async Task CambiarEstado_PedidoExiste_ActualizaEstado()
        {
            // Arrange
            var pedido = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "0",
                Tipo = "0",
                Comentario = "",
                Codigo = 100001,
                FechaEntrega = new DateTime(2026, 10, 20),
                FechaCreacion = new DateTime(2026, 10, 17),
                Precio = 50m
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            await _pedidoData.CambiarEstado(pedido.IdPedido, "1");

            // Assert
            var pedidoActualizado = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPedido == pedido.IdPedido);

            Assert.That(pedidoActualizado, Is.Not.Null);
            Assert.That(pedidoActualizado!.Estado, Is.EqualTo("1"));
        }

        [Test]
        public async Task CambiarEstado_PedidoNoExiste_NoModificaPedidos()
        {
            // Arrange
            int idInexistente = -1;

            // Act
            Assert.DoesNotThrowAsync(async () =>
                await _pedidoData.CambiarEstado(idInexistente, "1"));

            // Assert
            Assert.That(
                await _context.Pedidos.CountAsync(),
                Is.EqualTo(0));
        }

        [Test]
        public async Task ObtenerPorCodigo_CodigoExiste_DevuelvePedido()
        {
            // Arrange
            var pedido = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "1",
                Tipo = "0",
                Comentario = "",
                Codigo = 100002,
                FechaEntrega = new DateTime(2026, 10, 20),
                FechaCreacion = new DateTime(2026, 10, 17),
                Precio = 80m
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ObtenerPorCodigo(100002);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(resultado!.Codigo, Is.EqualTo(100002));
                Assert.That(resultado.Nombre, Is.EqualTo("Cliente de prueba"));
                Assert.That(resultado.Precio, Is.EqualTo(80m));
            });
        }

        [Test]
        public async Task ObtenerPorCodigo_CodigoNoExiste_DevuelveNull()
        {
            // Arrange
            int codigoInexistente = -1;

            // Act
            var resultado =
                await _pedidoData.ObtenerPorCodigo(codigoInexistente);

            // Assert
            Assert.That(resultado, Is.Null);
        }

        [Test]
        public async Task ConfirmarPago_EstadoCero_CambiaEstadoAUno()
        {
            // Arrange
            var pedido = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "0",
                Tipo = "0",
                Comentario = "",
                Codigo = 100003,
                FechaEntrega = new DateTime(2026, 10, 20),
                FechaCreacion = new DateTime(2026, 10, 17),
                Precio = 60m
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            await _pedidoData.ConfirmarPago(pedido.IdPedido);

            // Assert
            var pedidoActualizado = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPedido == pedido.IdPedido);

            Assert.That(pedidoActualizado, Is.Not.Null);
            Assert.That(pedidoActualizado!.Estado, Is.EqualTo("1"));
        }

        [Test]
        public async Task ConfirmarPago_EstadoDistintoDeCero_NoModificaEstado()
        {
            // Arrange
            var pedido = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "2",
                Tipo = "0",
                Comentario = "",
                Codigo = 100004,
                FechaEntrega = new DateTime(2026, 10, 20),
                FechaCreacion = new DateTime(2026, 10, 17),
                Precio = 60m
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            await _pedidoData.ConfirmarPago(pedido.IdPedido);

            // Assert
            var pedidoActualizado = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPedido == pedido.IdPedido);

            Assert.That(pedidoActualizado, Is.Not.Null);
            Assert.That(pedidoActualizado!.Estado, Is.EqualTo("2"));
        }

        [Test]
        public async Task ObtenerProducto_IdExiste_DevuelveProducto()
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
                Descripcion = "Producto de prueba",
                Tipo = "0",
                Activo = "1",
                Consumo = 3,
                Precio = 20m,
                IdCategoria = categoria.IdCategoria
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ObtenerProducto(
                producto.IdProducto);

            // Assert
            Assert.That(resultado, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(resultado!.IdProducto,
                    Is.EqualTo(producto.IdProducto));

                Assert.That(resultado.Descripcion,
                    Is.EqualTo("Producto de prueba"));

                Assert.That(resultado.Precio, Is.EqualTo(20m));
            });
        }

        [Test]
        public async Task ObtenerProducto_IdNoExiste_DevuelveNull()
        {
            // Arrange
            int idInexistente = -1;

            // Act
            var resultado = await _pedidoData.ObtenerProducto(
                idInexistente);

            // Assert
            Assert.That(resultado, Is.Null);
        }

        [Test]
        public async Task ObtenerEspecificacion_FechaExiste_DevuelveEspecificacion()
        {
            // Arrange
            string fechaTexto = "2026-10-19";
            DateTime fecha = new DateTime(2026, 10, 19);

            var especificacion = new Especificacione
            {
                DiasTrabajo = fechaTexto,
                CapacidadTotal = 15,
                CapacidadActual = 8
            };

            _context.Especificaciones.Add(especificacion);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ObtenerEspecificacion(
                fechaTexto, fecha);

            // Assert
            Assert.That(resultado, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(resultado!.DiasTrabajo,
                    Is.EqualTo(fechaTexto));

                Assert.That(resultado.CapacidadTotal,
                    Is.EqualTo(15));

                Assert.That(resultado.CapacidadActual,
                    Is.EqualTo(8));
            });

            // No debe crear una especificación duplicada.
            Assert.That(
                await _context.Especificaciones.CountAsync(),
                Is.EqualTo(1));
        }

        [Test]
        public async Task ObtenerEspecificacion_FechaNueva_CreaDesdePlantilla()
        {
            // Arrange: 19 de octubre de 2026 es lunes.
            string fechaTexto = "2026-10-19";
            DateTime fecha = new DateTime(2026, 10, 19);

            _context.Especificaciones.Add(new Especificacione
            {
                DiasTrabajo = "Lunes",
                CapacidadTotal = 12,
                CapacidadActual = 5
            });

            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ObtenerEspecificacion(
                fechaTexto, fecha);

            // Assert
            Assert.That(resultado, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(resultado!.DiasTrabajo,
                    Is.EqualTo(fechaTexto));

                Assert.That(resultado.CapacidadTotal,
                    Is.EqualTo(12));

                // La capacidad nueva comienza con la capacidad total,
                // no con la capacidad restante de la plantilla.
                Assert.That(resultado.CapacidadActual,
                    Is.EqualTo(12));
            });

            Assert.That(
                await _context.Especificaciones.CountAsync(),
                Is.EqualTo(2));
        }

        [Test]
        public async Task ObtenerEspecificacion_SinFechaNiPlantilla_DevuelveNull()
        {
            // Arrange
            string fechaTexto = "2026-10-19";
            DateTime fecha = new DateTime(2026, 10, 19);

            // No agregamos ninguna especificación.

            // Act
            var resultado = await _pedidoData.ObtenerEspecificacion(
                fechaTexto, fecha);

            // Assert
            Assert.That(resultado, Is.Null);

            Assert.That(
                await _context.Especificaciones.CountAsync(),
                Is.EqualTo(0));
        }

        [Test]
        public async Task ObtenerEspecificacion_Miercoles_CreaDesdePlantillaCorrecta()
        {
            // Arrange: 21 de octubre de 2026 es miércoles.
            string fechaTexto = "2026-10-21";
            DateTime fecha = new DateTime(2026, 10, 21);

            _context.Especificaciones.AddRange(
                new Especificacione
                {
                    DiasTrabajo = "Lunes",
                    CapacidadTotal = 10,
                    CapacidadActual = 10
                },
                new Especificacione
                {
                    DiasTrabajo = "Miercoles",
                    CapacidadTotal = 20,
                    CapacidadActual = 7
                });

            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ObtenerEspecificacion(
                fechaTexto, fecha);

            // Assert
            Assert.That(resultado, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(resultado!.DiasTrabajo,
                    Is.EqualTo(fechaTexto));

                Assert.That(resultado.CapacidadTotal,
                    Is.EqualTo(20));

                Assert.That(resultado.CapacidadActual,
                    Is.EqualTo(20));
            });
        }

        [Test]
        public void AddPedido_PedidoValido_AgregaAlContexto()
        {
            // Arrange
            var pedido = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "1",
                Tipo = "0",
                Comentario = "",
                Codigo = 100101,
                FechaEntrega = new DateTime(2026, 10, 20),
                FechaCreacion = new DateTime(2026, 10, 17),
                Precio = 50m
            };

            // Act
            _pedidoData.AddPedido(pedido);

            // Assert
            Assert.That(
                _context.Entry(pedido).State,
                Is.EqualTo(EntityState.Added));
        }

        [Test]
        public void AddDetallePersonalizado_DetalleValido_AgregaAlContexto()
        {
            // Arrange
            var detalle = new DetallePersonalizado();

            // Act
            _pedidoData.AddDetallePersonalizado(detalle);

            // Assert
            Assert.That(
                _context.Entry(detalle).State,
                Is.EqualTo(EntityState.Added));
        }

        [Test]
        public async Task Save_CambiosPendientes_GuardaEnBaseDatos()
        {
            // Arrange
            var pedido = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "1",
                Tipo = "0",
                Comentario = "",
                Codigo = 100102,
                FechaEntrega = new DateTime(2026, 10, 20),
                FechaCreacion = new DateTime(2026, 10, 17),
                Precio = 75m
            };

            _pedidoData.AddPedido(pedido);

            // Act
            await _pedidoData.Save();

            // Assert
            var guardado = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Codigo == 100102);

            Assert.That(guardado, Is.Not.Null);
            Assert.That(guardado!.Nombre, Is.EqualTo("Cliente de prueba"));
        }

        [Test]
        public async Task ObtenerProductoPorId_IdExiste_DevuelveProducto()
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
                Descripcion = "Producto de prueba",
                Tipo = "0",
                Activo = "1",
                Consumo = 3,
                Precio = 20m,
                IdCategoria = categoria.IdCategoria
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ObtenerProductoPorId(
                producto.IdProducto);

            // Assert
            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado!.IdProducto, Is.EqualTo(producto.IdProducto));
            Assert.That(resultado.Descripcion, Is.EqualTo("Producto de prueba"));
        }

        [Test]
        public async Task ObtenerProductoPorId_IdNoExiste_DevuelveNull()
        {
            // Arrange
            int idInexistente = -1;

            // Act
            var resultado = await _pedidoData.ObtenerProductoPorId(
                idInexistente);

            // Assert
            Assert.That(resultado, Is.Null);
        }

        [Test]
        public async Task ValidarCapacidad_CapacidadSuficiente_DevuelveTrue()
        {
            // Arrange
            var fecha = new DateTime(2026, 10, 19);

            _context.Especificaciones.Add(new Especificacione
            {
                DiasTrabajo = "2026-10-19",
                CapacidadTotal = 20,
                CapacidadActual = 15
            });

            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ValidarCapacidad(fecha, 10);

            // Assert
            Assert.That(resultado, Is.True);
        }

        [Test]
        public async Task ValidarCapacidad_FechaSinEspecificacion_DevuelveFalse()
        {
            // Arrange
            var fecha = new DateTime(2026, 10, 19);

            // No agregamos ninguna especificación para esa fecha.

            // Act
            var resultado = await _pedidoData.ValidarCapacidad(fecha, 5);

            // Assert
            Assert.That(resultado, Is.False);
        }

        [Test]
        public async Task AddPedido_SinLlamarSave_NoPersisteEnBaseDatos()
        {
            // Arrange
            var pedido = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "1",
                Tipo = "0",
                Comentario = "",
                Codigo = 100201,
                FechaEntrega = new DateTime(2026, 10, 20),
                FechaCreacion = new DateTime(2026, 10, 17),
                Precio = 50m
            };

            // Act
            _pedidoData.AddPedido(pedido);

            // Assert
            Assert.That(
                _context.Entry(pedido).State,
                Is.EqualTo(EntityState.Added));

            Assert.That(
                await _context.Pedidos.AsNoTracking()
                    .AnyAsync(p => p.Codigo == 100201),
                Is.False);
        }

        [Test]
        public void AddDetallePersonalizado_SinGuardar_QuedaPendiente()
        {
            // Arrange
            var detalle = new DetallePersonalizado();

            // Act
            _pedidoData.AddDetallePersonalizado(detalle);

            // Assert
            Assert.That(
                _context.Entry(detalle).State,
                Is.EqualTo(EntityState.Added));
        }

        [Test]
        public async Task Save_DetallePersonalizado_PersisteLosCambios()
        {
            // Arrange: crear y guardar el producto
            var producto = await CrearProducto("Producto personalizado");

            // Crear y guardar el pedido antes del detalle
            var pedido = await CrearPedido(
                "1",
                new DateTime(2026, 10, 20),
                200060
            );

            var detalle = new DetallePersonalizado
            {
                IdPedido = pedido.IdPedido,
                IdProducto = producto.IdProducto,
                Posicion = 1
            };

            // Act
            _context.DetallePersonalizados.Add(detalle);
            await _context.SaveChangesAsync();

            // Assert: comprobar que se guardó en la base de datos
            var guardado = await _context.DetallePersonalizados
                .SingleOrDefaultAsync(x =>
                    x.IdPedido == pedido.IdPedido &&
                    x.IdProducto == producto.IdProducto);

            Assert.That(guardado, Is.Not.Null);
            Assert.That(guardado!.IdPedido, Is.EqualTo(pedido.IdPedido));
            Assert.That(guardado.IdProducto, Is.EqualTo(producto.IdProducto));
            Assert.That(guardado.Posicion, Is.EqualTo(1));
        }

        [Test]
        public async Task ValidarCapacidad_ConsumoExcedeCapacidad_DevuelveFalse()
        {
            // Arrange
            var fecha = new DateTime(2026, 10, 19);

            _context.Especificaciones.Add(new Especificacione
            {
                DiasTrabajo = "2026-10-19",
                CapacidadTotal = 20,
                CapacidadActual = 5
            });

            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ValidarCapacidad(fecha, 6);

            // Assert
            Assert.That(resultado, Is.False);
        }

        [Test]
        public async Task ValidarCapacidad_ConsumoIgualCapacidad_DevuelveTrue()
        {
            // Arrange
            var fecha = new DateTime(2026, 10, 19);

            _context.Especificaciones.Add(new Especificacione
            {
                DiasTrabajo = "2026-10-19",
                CapacidadTotal = 20,
                CapacidadActual = 5
            });

            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.ValidarCapacidad(fecha, 5);

            // Assert
            Assert.That(resultado, Is.True);
        }

        private async Task<Producto> CrearProducto(string nombre)
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
                Tipo = "0",
                Activo = "1",
                Consumo = 1,
                Precio = 20m,
                IdCategoria = categoria.IdCategoria
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return producto;
        }

        private async Task<Pedido> CrearPedido(
            string tipo,
            DateTime fechaCreacion,
            int codigo)
        {
            var pedido = new Pedido
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                Estado = "1",
                Tipo = tipo,
                Comentario = "",
                Codigo = codigo,
                FechaEntrega = fechaCreacion,
                FechaCreacion = fechaCreacion,
                Precio = 50m
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            return pedido;
        }

        [Test]
        public async Task TopProductosFinales_SinDetalles_DevuelveListaVacia()
        {
            var resultado = await _pedidoData.TopProductosFinales();

            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task TopProductosFinales_SoloPedidosFinales()
        {
            var productoFinal = await CrearProducto("Producto final");
            var productoPersonalizado = await CrearProducto("Producto personalizado");

            var pedidoFinal = await CrearPedido(
                "0", new DateTime(2026, 10, 20), 200001);

            var pedidoPersonalizado = await CrearPedido(
                "1", new DateTime(2026, 10, 21), 200002);

            _context.DetallePedidos.AddRange(
                new DetallePedido
                {
                    IdPedido = pedidoFinal.IdPedido,
                    IdProducto = productoFinal.IdProducto,
                    Cantidad = 3
                },
                new DetallePedido
                {
                    IdPedido = pedidoPersonalizado.IdPedido,
                    IdProducto = productoPersonalizado.IdProducto,
                    Cantidad = 10
                });

            await _context.SaveChangesAsync();

            var resultado = await _pedidoData.TopProductosFinales();

            Assert.That(resultado, Has.Count.EqualTo(1));
            Assert.That(resultado[0].Nombre, Is.EqualTo("Producto final"));
            Assert.That(resultado[0].Cantidad, Is.EqualTo(3));
        }

        [Test]
        public async Task TopProductosFinales_SumaCantidadesPorProducto()
        {
            var producto = await CrearProducto("Producto A");

            var pedido1 = await CrearPedido(
                "0", new DateTime(2026, 10, 20), 200003);

            var pedido2 = await CrearPedido(
                "0", new DateTime(2026, 10, 21), 200004);

            _context.DetallePedidos.AddRange(
                new DetallePedido
                {
                    IdPedido = pedido1.IdPedido,
                    IdProducto = producto.IdProducto,
                    Cantidad = 2
                },
                new DetallePedido
                {
                    IdPedido = pedido2.IdPedido,
                    IdProducto = producto.IdProducto,
                    Cantidad = 4
                });

            await _context.SaveChangesAsync();

            var resultado = await _pedidoData.TopProductosFinales();

            Assert.That(resultado, Has.Count.EqualTo(1));
            Assert.That(resultado[0].Nombre, Is.EqualTo("Producto A"));
            Assert.That(resultado[0].Cantidad, Is.EqualTo(6));
        }

        [Test]
        public async Task TopProductosFinales_MasDeCincoProductos_DevuelveCinco()
        {
            for (int i = 1; i <= 6; i++)
            {
                var producto = await CrearProducto($"Producto {i}");

                var pedido = await CrearPedido(
                    "0",
                    new DateTime(2026, 10, 20),
                    200010 + i);

                _context.DetallePedidos.Add(new DetallePedido
                {
                    IdPedido = pedido.IdPedido,
                    IdProducto = producto.IdProducto,
                    Cantidad = i
                });

                await _context.SaveChangesAsync();
            }

            var resultado = await _pedidoData.TopProductosFinales();

            Assert.That(resultado, Has.Count.EqualTo(5));
            Assert.That(resultado[0].Cantidad, Is.EqualTo(6));
            Assert.That(resultado[4].Cantidad, Is.EqualTo(2));
        }

        [Test]
        public async Task TopProductosPersonalizados_SinDetalles_DevuelveListaVacia()
        {
            var resultado =
                await _pedidoData.TopProductosPersonalizados();

            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task TopProductosPersonalizados_CuentaDetallesPorProducto()
        {
            // Arrange
            var productoA = await CrearProducto("Producto A");
            var productoB = await CrearProducto("Producto B");

            var pedido1 = await CrearPedido(
                "1", new DateTime(2026, 10, 20), 200020);

            var pedido2 = await CrearPedido(
                "1", new DateTime(2026, 10, 21), 200021);

            var pedido3 = await CrearPedido(
                "1", new DateTime(2026, 10, 22), 200022);

            _context.DetallePersonalizados.AddRange(
                new DetallePersonalizado
                {
                    IdPedido = pedido1.IdPedido,
                    IdProducto = productoA.IdProducto,
                    Posicion = 1
                },
                new DetallePersonalizado
                {
                    IdPedido = pedido2.IdPedido,
                    IdProducto = productoA.IdProducto,
                    Posicion = 1
                },
                new DetallePersonalizado
                {
                    IdPedido = pedido3.IdPedido,
                    IdProducto = productoB.IdProducto,
                    Posicion = 1
                });

            await _context.SaveChangesAsync();

            // Act
            var resultado =
                await _pedidoData.TopProductosPersonalizados();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(2));

            var resultadoA = resultado.Single(
                x => x.Nombre == "Producto A");

            var resultadoB = resultado.Single(
                x => x.Nombre == "Producto B");

            Assert.That(resultadoA.Cantidad, Is.EqualTo(2));
            Assert.That(resultadoB.Cantidad, Is.EqualTo(1));
        }

        [Test]
        public async Task TopProductosPersonalizados_OrdenaPorCantidadDescendente()
        {
            // Arrange
            var productoA = await CrearProducto("Producto A");
            var productoB = await CrearProducto("Producto B");
            var productoC = await CrearProducto("Producto C");

            var detalles = new List<DetallePersonalizado>();

            // Producto A: 3 pedidos diferentes
            for (int i = 0; i < 3; i++)
            {
                var pedido = await CrearPedido(
                    "1",
                    new DateTime(2026, 10, 20 + i),
                    200030 + i);

                detalles.Add(new DetallePersonalizado
                {
                    IdPedido = pedido.IdPedido,
                    IdProducto = productoA.IdProducto,
                    Posicion = 1
                });
            }

            // Producto B: 2 pedidos diferentes
            for (int i = 0; i < 2; i++)
            {
                var pedido = await CrearPedido(
                    "1",
                    new DateTime(2026, 10, 23 + i),
                    200040 + i);

                detalles.Add(new DetallePersonalizado
                {
                    IdPedido = pedido.IdPedido,
                    IdProducto = productoB.IdProducto,
                    Posicion = 1
                });
            }

            // Producto C: 1 pedido
            var pedidoC = await CrearPedido(
                "1",
                new DateTime(2026, 10, 25),
                200050);

            detalles.Add(new DetallePersonalizado
            {
                IdPedido = pedidoC.IdPedido,
                IdProducto = productoC.IdProducto,
                Posicion = 1
            });

            _context.DetallePersonalizados.AddRange(detalles);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.TopProductosPersonalizados();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(3));

            Assert.That(resultado[0].Nombre, Is.EqualTo("Producto A"));
            Assert.That(resultado[0].Cantidad, Is.EqualTo(3));

            Assert.That(resultado[1].Nombre, Is.EqualTo("Producto B"));
            Assert.That(resultado[1].Cantidad, Is.EqualTo(2));

            Assert.That(resultado[2].Nombre, Is.EqualTo("Producto C"));
            Assert.That(resultado[2].Cantidad, Is.EqualTo(1));
        }

        [Test]
        public async Task TopProductosPersonalizados_MasDeTresProductos_DevuelveTres()
        {
            // Arrange: crear cuatro productos diferentes
            var productoA = await CrearProducto("Producto A");
            var productoB = await CrearProducto("Producto B");
            var productoC = await CrearProducto("Producto C");
            var productoD = await CrearProducto("Producto D");

            // Crear un pedido diferente para cada producto
            var pedido1 = await CrearPedido(
                "1", new DateTime(2026, 10, 20), 200020);

            var pedido2 = await CrearPedido(
                "1", new DateTime(2026, 10, 21), 200021);

            var pedido3 = await CrearPedido(
                "1", new DateTime(2026, 10, 22), 200022);

            var pedido4 = await CrearPedido(
                "1", new DateTime(2026, 10, 23), 200023);

            // Agregar los detalles personalizados con claves únicas
            _context.DetallePersonalizados.AddRange(
                new DetallePersonalizado
                {
                    IdPedido = pedido1.IdPedido,
                    IdProducto = productoA.IdProducto,
                    Posicion = 1
                },
                new DetallePersonalizado
                {
                    IdPedido = pedido2.IdPedido,
                    IdProducto = productoB.IdProducto,
                    Posicion = 1
                },
                new DetallePersonalizado
                {
                    IdPedido = pedido3.IdPedido,
                    IdProducto = productoC.IdProducto,
                    Posicion = 1
                },
                new DetallePersonalizado
                {
                    IdPedido = pedido4.IdPedido,
                    IdProducto = productoD.IdProducto,
                    Posicion = 1
                }
            );

            await _context.SaveChangesAsync();

            // Act
            var resultado = await _pedidoData.TopProductosPersonalizados();

            // Assert
            Assert.That(resultado, Has.Count.EqualTo(3));

            Assert.That(
                resultado.Select(x => x.Nombre).Distinct().Count(),
                Is.EqualTo(3));
        }

        [Test]
        public async Task TopMesesVentas_SinPedidos_DevuelveListaVacia()
        {
            var resultado = await _pedidoData.TopMesesVentas();

            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);
        }

        [Test]
        public async Task TopMesesVentas_AgrupaPorAnioYMes()
        {
            await CrearPedido("0", new DateTime(2026, 10, 2), 200030);
            await CrearPedido("0", new DateTime(2026, 10, 5), 200031);
            await CrearPedido("1", new DateTime(2026, 11, 2), 200032);

            var resultado = await _pedidoData.TopMesesVentas();

            Assert.That(resultado, Has.Count.EqualTo(2));

            var octubre = resultado.Single(x => x.Mes == "2026-10");
            var noviembre = resultado.Single(x => x.Mes == "2026-11");

            Assert.That(octubre.Cantidad, Is.EqualTo(2));
            Assert.That(noviembre.Cantidad, Is.EqualTo(1));
        }

        [Test]
        public async Task TopMesesVentas_OrdenaPorCantidadDescendente()
        {
            await CrearPedido("0", new DateTime(2026, 10, 2), 200040);

            await CrearPedido("0", new DateTime(2026, 11, 2), 200041);
            await CrearPedido("0", new DateTime(2026, 11, 3), 200042);

            await CrearPedido("0", new DateTime(2026, 12, 2), 200043);
            await CrearPedido("0", new DateTime(2026, 12, 3), 200044);
            await CrearPedido("0", new DateTime(2026, 12, 4), 200045);

            var resultado = await _pedidoData.TopMesesVentas();

            Assert.That(resultado[0].Mes, Is.EqualTo("2026-12"));
            Assert.That(resultado[0].Cantidad, Is.EqualTo(3));

            Assert.That(resultado[1].Mes, Is.EqualTo("2026-11"));
            Assert.That(resultado[1].Cantidad, Is.EqualTo(2));

            Assert.That(resultado[2].Mes, Is.EqualTo("2026-10"));
            Assert.That(resultado[2].Cantidad, Is.EqualTo(1));
        }

        [Test]
        public async Task TopMesesVentas_MasDeTresMeses_DevuelveTres()
        {
            await CrearPedido("0", new DateTime(2026, 1, 10), 200050);

            await CrearPedido("0", new DateTime(2026, 2, 10), 200051);
            await CrearPedido("0", new DateTime(2026, 2, 11), 200052);

            await CrearPedido("0", new DateTime(2026, 3, 10), 200053);
            await CrearPedido("0", new DateTime(2026, 3, 11), 200054);
            await CrearPedido("0", new DateTime(2026, 3, 12), 200055);

            await CrearPedido("0", new DateTime(2026, 4, 10), 200056);
            await CrearPedido("0", new DateTime(2026, 4, 11), 200057);
            await CrearPedido("0", new DateTime(2026, 4, 12), 200058);
            await CrearPedido("0", new DateTime(2026, 4, 13), 200059);

            var resultado = await _pedidoData.TopMesesVentas();

            Assert.That(resultado, Has.Count.EqualTo(3));
            Assert.That(resultado[0].Mes, Is.EqualTo("2026-4"));
            Assert.That(resultado[0].Cantidad, Is.EqualTo(4));
        }

        [Test]
        public async Task GenerarPedidoPersonalizado_PedidoValido_CalculaPrecioTotal()
        {
            // Arrange
            var fechaEntrega = new DateTime(2026, 10, 20);

            var especificacion = new Especificacione
            {
                DiasTrabajo = fechaEntrega.ToString("yyyy-MM-dd"),
                CapacidadTotal = 100,
                CapacidadActual = 100
            };

            _context.Especificaciones.Add(especificacion);
            await _context.SaveChangesAsync();

            var productoA = await CrearProducto("Producto A");
            var productoB = await CrearProducto("Producto B");

            productoA.Precio = 20m;
            productoA.Consumo = 3;

            productoB.Precio = 35m;
            productoB.Consumo = 4;

            await _context.SaveChangesAsync();

            var vm = new PedidoPersonalizadoVM
            {
                Nombre = "Cliente de prueba",
                Dni = "12345678",
                Telefono = "999999999",
                FechaEntrega = fechaEntrega,
                Comentario = "Pedido de prueba",
                Selecciones = new()
        {
            new()
            {
                IdProducto = productoA.IdProducto,
                Posicion = 1
            },
            new()
            {
                IdProducto = productoB.IdProducto,
                Posicion = 2
            }
        }
            };

            // Act
            var resultado = await _pedidoData.GenerarPedidoPersonalizado(vm);

            // Assert
            Assert.That(resultado.Codigo, Is.GreaterThan(0));
            Assert.That(resultado.Total, Is.EqualTo(55m));

            var pedido = await _context.Pedidos
                .SingleOrDefaultAsync(p => p.Codigo == resultado.Codigo);

            Assert.That(pedido, Is.Not.Null);

            var detalles = await _context.DetallePersonalizados
                .Where(d => d.IdPedido == pedido!.IdPedido)
                .ToListAsync();

            Assert.That(detalles, Has.Count.EqualTo(2));

            var especificacionActualizada = await _context.Especificaciones
                .AsNoTracking()
                .SingleAsync(e =>
                    e.IdEspecificaciones == especificacion.IdEspecificaciones);

            Assert.That(
                especificacionActualizada.CapacidadActual,
                Is.EqualTo(93));
        }
    }
}