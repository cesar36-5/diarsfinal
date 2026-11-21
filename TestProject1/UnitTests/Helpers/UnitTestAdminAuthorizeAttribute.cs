using DiarsFinal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NUnit.Framework;
using System.Collections.Generic;

namespace TestProject1.UnitTests.Helpers
{
    [TestFixture]
    public class UnitTestAdminAuthorizeAttribute
    {
        private DefaultHttpContext _httpContext = null!;
        private AuthorizationFilterContext _filterContext = null!;
        private AdminAuthorizeAttribute _attribute = null!;

        [SetUp]
        public void Setup()
        {
            _httpContext = new DefaultHttpContext();

            // Configurar la sesión simulada.
            _httpContext.Session = new TestSession();

            var actionContext = new ActionContext(
                _httpContext,
                new RouteData(),
                new ActionDescriptor());

            _filterContext = new AuthorizationFilterContext(
                actionContext,
                new List<IFilterMetadata>());

            _attribute = new AdminAuthorizeAttribute();
        }

        [Test]
        public void OnAuthorization_SinUsuario_RedireccionaAlLogin()
        {
            _attribute.OnAuthorization(_filterContext);

            Assert.That(
                _filterContext.Result,
                Is.InstanceOf<RedirectToActionResult>());

            var redirect = (RedirectToActionResult)_filterContext.Result!;

            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
            Assert.That(redirect.ControllerName, Is.EqualTo("Login"));
        }

        [Test]
        public void OnAuthorization_UsuarioNulo_RedireccionaAlLogin()
        {
            // La sesión está vacía: GetString("Usuario") devuelve null.
            _attribute.OnAuthorization(_filterContext);

            Assert.That(
                _filterContext.Result,
                Is.InstanceOf<RedirectToActionResult>());

            var redirect = (RedirectToActionResult)_filterContext.Result!;

            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
            Assert.That(redirect.ControllerName, Is.EqualTo("Login"));
        }

        [Test]
        public void OnAuthorization_UsuarioVacio_RedireccionaAlLogin()
        {
            _httpContext.Session.SetString("Usuario", "");

            _attribute.OnAuthorization(_filterContext);

            Assert.That(
                _filterContext.Result,
                Is.InstanceOf<RedirectToActionResult>());
        }

        [Test]
        public void OnAuthorization_UsuarioExistente_NoRedirecciona()
        {
            _httpContext.Session.SetString("Usuario", "admin");

            _attribute.OnAuthorization(_filterContext);

            Assert.That(_filterContext.Result, Is.Null);
        }

        // Implementación sencilla de ISession para las pruebas.
        private sealed class TestSession : ISession
        {
            private readonly Dictionary<string, byte[]> _data = new();

            public bool IsAvailable => true;

            public string Id => "test-session";

            public IEnumerable<string> Keys => _data.Keys;

            public void Clear() => _data.Clear();

            public Task CommitAsync(
                CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task LoadAsync(
                CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public void Remove(string key) => _data.Remove(key);

            public void Set(string key, byte[] value)
                => _data[key] = value;

            public bool TryGetValue(string key, out byte[]? value)
            {
                if (_data.TryGetValue(key, out var stored))
                {
                    value = stored;
                    return true;
                }

                value = null;
                return false;
            }
        }
    }
}