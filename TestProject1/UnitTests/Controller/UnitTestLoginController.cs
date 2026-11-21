using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using DiarsFinal.Controllers;
using DiarsFinal.Datos;
using DiarsFinal.Data;
using System.Threading.Tasks;

namespace TestProject1.UnitTests.Controller
{
    [TestFixture]
    public class UnitTestLoginController
    {
        private SqliteConnection _connection = null!;
        private DiarsContext _context = null!;
        private LoginData _loginData = null!;
        private LoginController _controller = null!;
        private DefaultHttpContext _httpContext = null!;

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

            _loginData = new LoginData(_context);
            _controller = new LoginController(_loginData);

            _httpContext = new DefaultHttpContext();
            _httpContext.Session = new TestSession();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
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
        // INDEX GET
        // =========================================

        [Test]
        public void IndexGet_DevuelveVista()
        {
            // Act
            var resultado = _controller.Index();

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());
        }

        // =========================================
        // INDEX POST - LOGIN INCORRECTO
        // =========================================

        [Test]
        public async Task IndexPost_CredencialesIncorrectas_DevuelveVistaConError()
        {
            // Arrange
            string usuario = "usuario_inexistente";
            string contrasena = "incorrecta";

            // Act
            var resultado = await _controller.Index(usuario, contrasena);

            // Assert
            Assert.That(resultado, Is.InstanceOf<ViewResult>());
            Assert.That(
                _controller.ViewBag.Error,
                Is.EqualTo("Usuario o contraseña incorrectos"));

            Assert.That(
                _httpContext.Session.GetString("Usuario"),
                Is.Null);
        }

        // =========================================
        // LOGOUT
        // =========================================

        [Test]
        public void Logout_LimpiaSesionYRedirigeAlLogin()
        {
            // Arrange
            _httpContext.Session.SetString("Usuario", "admin");

            // Act
            var resultado = _controller.Logout();

            // Assert
            Assert.That(
                resultado,
                Is.InstanceOf<RedirectToActionResult>());

            var redireccion = (RedirectToActionResult)resultado;

            Assert.That(
                redireccion.ActionName,
                Is.EqualTo(nameof(LoginController.Index)));

            Assert.That(
                _httpContext.Session.GetString("Usuario"),
                Is.Null);
        }
    }

    // Sesión de prueba en el mismo archivo
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _datos = new();

        public IEnumerable<string> Keys => _datos.Keys;

        public string Id => "test-session";

        public bool IsAvailable => true;

        public void Clear() => _datos.Clear();

        public Task CommitAsync(
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task LoadAsync(
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Remove(string key) => _datos.Remove(key);

        public void Set(string key, byte[] value)
            => _datos[key] = value;

        public bool TryGetValue(string key, out byte[] value)
            => _datos.TryGetValue(key, out value!);
    }
}