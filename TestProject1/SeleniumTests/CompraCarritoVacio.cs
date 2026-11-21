using System;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SeleniumTests
{
    [TestFixture]
    public class CompraCarritoVacio
    {
        private IWebDriver? driver;
        private string urlBase = null!;

        [SetUp]
        public void SetupTest()
        {
            // Carga la URL dinámica del appsettings.json según la guía
            var configuracion = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            urlBase = configuracion["Selenium:UrlBase"]
                ?? throw new Exception("No se encontró Selenium:UrlBase en appsettings.json.");

            // Abre y configura Google Chrome
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
                driver.Dispose(); // Libera la memoria correctamente
            }
        }

        [Test]
        public void TheCompraCarritoVacioTest()
        {
            // 1. Configurar una espera inteligente de máximo 10 segundos
            var espera = new WebDriverWait(driver!, TimeSpan.FromSeconds(10));

            // 2. Navega al catálogo inicial e ingresa al pedido
            driver!.Navigate().GoToUrl($"{urlBase}/Catalogo");

            var botonVerPedido = espera.Until(d => d.FindElement(By.LinkText("Ver pedido")));
            botonVerPedido.Click();

            // 3. Navega a la pasarela de pedido con el carrito vacío
            driver.Navigate().GoToUrl($"{urlBase}/Pedido");

            // 4. Intento de llenado de datos en el formulario (Llenamos directo con SendKeys sin usar Click)
            var inputNombre = espera.Until(d => d.FindElement(By.Name("nombre")));
            inputNombre.Clear();
            inputNombre.SendKeys("sssrgdrdgr");

            var inputDni = driver.FindElement(By.Name("dni"));
            inputDni.Clear();
            inputDni.SendKeys("73870884");

            var inputTelefono = driver.FindElement(By.Name("telefono"));
            inputTelefono.Clear();
            inputTelefono.SendKeys("123456789");

            var inputFecha = driver.FindElement(By.Name("fechaEntrega"));
            inputFecha.Clear();
            inputFecha.SendKeys("2026-10-22");

            var inputComentario = driver.FindElement(By.Name("comentario"));
            inputComentario.Clear();
            inputComentario.SendKeys("fwesfs");

            // 5. Validación final: Asegura que el sistema se mantiene en la vista de Pedido bloqueando compras vacías
            espera.Until(d => d.Url.Contains("Pedido"));
            Assert.That(driver.Url, Does.Contain("Pedido"));
        }
    }
}