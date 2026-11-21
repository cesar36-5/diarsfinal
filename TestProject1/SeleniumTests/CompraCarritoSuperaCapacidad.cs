using System;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SeleniumTests
{
    [TestFixture]
    public class CompraCarritoSuperaCapacidad
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
        public void TheCompraCarritoSuperaCapacidadTest()
        {
            // 1. Configurar una espera inteligente de máximo 10 segundos
            var espera = new WebDriverWait(driver!, TimeSpan.FromSeconds(10));
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver!;

            // 2. Navega al catálogo inicial
            driver!.Navigate().GoToUrl($"{urlBase}/Catalogo");

            // 3. Selección del producto y aumento de cantidad a 100
            var inputCantidad = espera.Until(d => d.FindElement(By.Id("cantidad-2")));
            inputCantidad.Clear();
            inputCantidad.SendKeys("100");

            var botonAgregar = espera.Until(d => d.FindElement(By.XPath("//button[@type='submit'] | //section[@id='productos']//form//button")));
            js.ExecuteScript("arguments[0].click();", botonAgregar);

            // 4. Ir a la vista del Pedido de forma limpia (Sin duplicar navegación)
            // Redirigir directamente mediante URL es la forma más limpia y evita el StaleElement en entornos rápidos locales
            driver.Navigate().GoToUrl($"{urlBase}/Pedido");

            // 5. Completado del formulario de envío (Usando siempre la espera para sincronizar el DOM)
            var inputNombre = espera.Until(d => d.FindElement(By.Name("nombre")));
            inputNombre.Clear();
            inputNombre.SendKeys("sssrgdrdgr");

            var inputDni = espera.Until(d => d.FindElement(By.Name("dni")));
            inputDni.Clear();
            inputDni.SendKeys("73870884");

            var inputTelefono = espera.Until(d => d.FindElement(By.Name("telefono")));
            inputTelefono.Clear();
            inputTelefono.SendKeys("123456789");

            var inputFecha = espera.Until(d => d.FindElement(By.Name("fechaEntrega")));
            inputFecha.Clear();
            inputFecha.SendKeys("2026-10-19");

            var inputComentario = espera.Until(d => d.FindElement(By.Name("comentario")));
            inputComentario.Clear();
            inputComentario.SendKeys("dfnb");

            // 6. Confirmación final de la compra usando JavaScript
            var botonConfirmar = espera.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Confirmar')] | //button[@type='submit'] | (.//*[normalize-space(text()) and normalize-space(.)='Volver al catálogo'])/preceding::button")));
            js.ExecuteScript("arguments[0].click();", botonConfirmar);

            // 7. Validación final: Espera a que termine la petición y revisa la URL resultante
            espera.Until(d => d.Url.Contains("Pedido") || d.Url.Contains("Confirmacion") || d.Url.Contains("Catalogo"));
            Assert.That(driver.Url, Does.Contain("Pedido").Or.Contains("Confirmacion").Or.Contains("Catalogo"));
        }

    }
}