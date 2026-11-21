using System;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SeleniumTests
{
    [TestFixture]
    public class InicioSesionExitoso
    {
        private IWebDriver? driver;
        private string urlBase = null!;

        [SetUp]
        public void SetupTest()
        {
            // Carga la URL dinámica desde el appsettings.json
            var configuracion = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            urlBase = configuracion["Selenium:UrlBase"]
                ?? throw new Exception("No se encontró Selenium:UrlBase en appsettings.json.");

            // Inicializa Google Chrome de manera correcta
            var opciones = new ChromeOptions();
            opciones.AcceptInsecureCertificates = true;
            driver = new ChromeDriver(opciones);
        }

        [TearDown]
        public void TeardownTest()
        {
            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }
        }

        [Test]
        public void TheInicioSesionExitosoTest()
        {
            // 1. Configurar una espera inteligente de máximo 10 segundos
            var espera = new WebDriverWait(driver!, TimeSpan.FromSeconds(10));

            // 2. Navega a la raíz del sitio
            driver!.Navigate().GoToUrl($"{urlBase}/");

            // 3. Primer flujo de Login (Esperando que el botón del Navbar sea cliqueable)
            var botonLogin = espera.Until(d => d.FindElement(By.XPath("//div[@id='navbarPatissier']/div/a/i")));
            botonLogin.Click();

            var inputUsuario = espera.Until(d => d.FindElement(By.Name("usuario")));
            inputUsuario.Click();
            inputUsuario.Clear();
            inputUsuario.SendKeys("admin");

            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Usuario'])/following::div")).Click();

            var inputContrasena = driver.FindElement(By.Name("contrasena"));
            inputContrasena.Click();
            inputContrasena.Clear();
            inputContrasena.SendKeys("diarz");

            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Iniciar sesión'])/following::div")).Click();

            var botonEnviar = espera.Until(d => d.FindElement(By.XPath("//button[@type='submit']")));
            botonEnviar.Click();

            // 4. Validación final: Asegura que entramos al Dashboard o la sección administrativa
            espera.Until(d => d.Url.Contains("Dashboard") || d.Url.Contains("Productos") || !d.Url.Contains("Login"));

            Assert.That(driver.Url, Does.Contain("Dashboard").Or.Contain("Productos").Or.Not.Contain("Login"));
        }
    }
}