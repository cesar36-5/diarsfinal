using DiarsFinal.Controllers;
using DiarsFinal.Data;
using DiarsFinal.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TestProject1.UnitTests.Data
{
    [TestFixture]
    public class UnitTestEspecificacionesData
    {
        private DiarsContext _context = null!;
        private EspecificacionesData _especificacionesData = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DiarsContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new DiarsContext(options);
            _especificacionesData = new EspecificacionesData(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // LISTAR: debe devolver únicamente los primeros 7 registros
        [Test]
        public async Task Listar_ConMasDeSieteRegistros_DevuelveSoloSiete()
        {
            string[] dias =
            {
                "Lunes", "Martes", "Miercoles", "Jueves",
                "Viernes", "Sabado", "Domingo"
            };

            for (int i = 1; i <= 10; i++)
            {
                _context.Especificaciones.Add(
                    new Especificacione
                    {
                        IdEspecificaciones = i,
                        DiasTrabajo = i <= 7
                            ? dias[i - 1]
                            : "26-10-17",
                        CapacidadTotal = 10,
                        CapacidadActual = i <= 7 ? 10 : 3
                    });
            }

            await _context.SaveChangesAsync();

            var resultado = await _especificacionesData.Listar();

            Assert.That(resultado, Has.Count.EqualTo(7));
        }

        // LISTAR: sin registros debe devolver una lista vacía
        [Test]
        public async Task Listar_SinRegistros_DevuelveListaVacia()
        {
            var resultado = await _especificacionesData.Listar();

            Assert.That(resultado, Is.Not.Null);
            Assert.That(resultado, Is.Empty);
        }

        // BUSCAR POR ID: registro existente
        [Test]
        public async Task BuscarPorId_IdExistente_DevuelveEspecificacion()
        {
            var especificacion = new Especificacione
            {
                IdEspecificaciones = 1,
                DiasTrabajo = "Lunes",
                CapacidadTotal = 10,
                CapacidadActual = 10
            };

            _context.Especificaciones.Add(especificacion);
            await _context.SaveChangesAsync();

            var resultado = await _especificacionesData.BuscarPorId(1);

            Assert.That(resultado, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(resultado!.IdEspecificaciones, Is.EqualTo(1));
                Assert.That(resultado.DiasTrabajo, Is.EqualTo("Lunes"));
                Assert.That(resultado.CapacidadTotal, Is.EqualTo(10));
                Assert.That(resultado.CapacidadActual, Is.EqualTo(10));
            });
        }

        // BUSCAR POR ID: registro inexistente
        [Test]
        public async Task BuscarPorId_IdInexistente_DevuelveNull()
        {
            var resultado = await _especificacionesData.BuscarPorId(999);

            Assert.That(resultado, Is.Null);
        }

        // EDITAR: debe actualizar únicamente las capacidades
        [Test]
        public async Task Editar_IdExistente_ActualizaCapacidades()
        {
            var especificacion = new Especificacione
            {
                IdEspecificaciones = 1,
                DiasTrabajo = "Lunes",
                CapacidadTotal = 100,
                CapacidadActual = 40
            };

            _context.Especificaciones.Add(especificacion);
            await _context.SaveChangesAsync();

            var modeloEditado = new Especificacione
            {
                IdEspecificaciones = 1,
                DiasTrabajo = "Martes",
                CapacidadTotal = 200,
                CapacidadActual = 80
            };

            await _especificacionesData.Editar(modeloEditado);

            var resultado = await _context.Especificaciones
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEspecificaciones == 1);

            Assert.That(resultado, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(resultado!.CapacidadTotal, Is.EqualTo(200));
                Assert.That(resultado.CapacidadActual, Is.EqualTo(80));

                // Editar solo modifica las capacidades.
                Assert.That(resultado.DiasTrabajo, Is.EqualTo("Lunes"));
            });
        }

        // EDITAR: un ID inexistente no debe modificar otros registros
        [Test]
        public async Task Editar_IdInexistente_NoModificaRegistros()
        {
            var especificacion = new Especificacione
            {
                IdEspecificaciones = 1,
                DiasTrabajo = "Lunes",
                CapacidadTotal = 100,
                CapacidadActual = 40
            };

            _context.Especificaciones.Add(especificacion);
            await _context.SaveChangesAsync();

            var modeloEditado = new Especificacione
            {
                IdEspecificaciones = 999,
                DiasTrabajo = "26-10-17",
                CapacidadTotal = 200,
                CapacidadActual = 80
            };

            await _especificacionesData.Editar(modeloEditado);

            var resultado = await _context.Especificaciones
                .AsNoTracking()
                .SingleAsync(e => e.IdEspecificaciones == 1);

            Assert.Multiple(() =>
            {
                Assert.That(resultado.CapacidadTotal, Is.EqualTo(100));
                Assert.That(resultado.CapacidadActual, Is.EqualTo(40));
                Assert.That(resultado.DiasTrabajo, Is.EqualTo("Lunes"));
            });

            Assert.That(
                await _context.Especificaciones.CountAsync(),
                Is.EqualTo(1));
        }
    }
}