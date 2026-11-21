using DiarsFinal.Controllers;
using DiarsFinal.Data;
using DiarsFinal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace TestProject1.UnitTests.Data
{
    [TestFixture]
    public class UnitTestLoginData
    {
        private DiarsContext _context = null!;
        private LoginData _loginData = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DiarsContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new DiarsContext(options);
            _loginData = new LoginData(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // LOGIN: usuario y contraseña correctos
        [Test]
        public async Task ValidarLogin_CredencialesCorrectas_DevuelveUsuario()
        {
            string contrasena = "Admin123*";

            var hasher = new PasswordHasher<object>();
            string hash = hasher.HashPassword(null, contrasena);

            var usuario = new Usuario
            {
                Usuario1 = "admin",
                Contrasena = hash
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await _loginData.ValidarLogin(
                "admin",
                contrasena);

            Assert.That(resultado, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(resultado!.Usuario1, Is.EqualTo("admin"));
                Assert.That(resultado.Contrasena, Is.EqualTo(hash));
            });
        }

        // LOGIN: usuario inexistente
        [Test]
        public async Task ValidarLogin_UsuarioInexistente_DevuelveNull()
        {
            var resultado = await _loginData.ValidarLogin(
                "inexistente",
                "Admin123*");

            Assert.That(resultado, Is.Null);
        }

        // LOGIN: contraseña incorrecta
        [Test]
        public async Task ValidarLogin_ContrasenaIncorrecta_DevuelveNull()
        {
            string contrasenaCorrecta = "Admin123*";

            var hasher = new PasswordHasher<object>();
            string hash = hasher.HashPassword(
                null,
                contrasenaCorrecta);

            var usuario = new Usuario
            {
                Usuario1 = "admin",
                Contrasena = hash
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await _loginData.ValidarLogin(
                "admin",
                "ContrasenaIncorrecta");

            Assert.That(resultado, Is.Null);
        }

        // LOGIN: contraseña vacía
        [Test]
        public async Task ValidarLogin_ContrasenaVacia_DevuelveNull()
        {
            string contrasenaCorrecta = "Admin123*";

            var hasher = new PasswordHasher<object>();
            string hash = hasher.HashPassword(
                null,
                contrasenaCorrecta);

            var usuario = new Usuario
            {
                Usuario1 = "admin",
                Contrasena = hash
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await _loginData.ValidarLogin(
                "admin",
                "");

            Assert.That(resultado, Is.Null);
        }
    }
}