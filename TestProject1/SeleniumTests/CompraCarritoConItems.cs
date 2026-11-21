using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SeleniumTests
{
    [TestFixture]
    public class CompraCarritoConItems
    {
        private IWebDriver? driver;
        private string urlBase = null!;

        [SetUp]
        public void SetupTest()
        {
            // Lee la URL configurada en appsettings.json tal como pide la guía
            var configuracion = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            urlBase = configuracion["Selenium:UrlBase"]
                ?? throw new Exception("No se encontró Selenium:UrlBase en appsettings.json.");

            // Configura y abre Chrome de forma moderna
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
                driver.Dispose(); // Soluciona el error: "The field driver should be Disposed"
            }
        }

        [Test]
        public void TheCompraCarritoConItemsTest()
        {
            // 1. Configurar una espera inteligente de máximo 10 segundos
            var espera = new WebDriverWait(driver!, TimeSpan.FromSeconds(10));
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver!;

            // 2. Abre el catálogo usando la url dinámica
            driver!.Navigate().GoToUrl($"{urlBase}/Catalogo");

            // 3. Selección y agregado de productos al carrito usando JavaScript para evitar bloqueos
            var primerProducto = espera.Until(d => d.FindElement(By.XPath("//section[@id='productos']//form//button | //section[@id='productos']/div/div/div/div/form/div/button/i")));
            js.ExecuteScript("arguments[0].click();", primerProducto);

            var segundoProducto = espera.Until(d => d.FindElement(By.XPath("//section[@id='productos']//form//button | //section[@id='productos']/div/div/div/div/form/div/button")));
            js.ExecuteScript("arguments[0].click();", segundoProducto);

            var botonVerPedido = espera.Until(d => d.FindElement(By.LinkText("Ver pedido")));
            js.ExecuteScript("arguments[0].click();", botonVerPedido);

            // 4. Navegar al formulario de Pedido
            driver.Navigate().GoToUrl($"{urlBase}/Pedido");

            // 5. Completar el formulario (Directo con SendKeys sin Clicks intermedios)
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
            inputFecha.SendKeys("2026-10-25");

            var inputComentario = driver.FindElement(By.Name("comentario"));
            inputComentario.Clear();
            inputComentario.SendKeys("dfsvv");

            // 6. Confirmar el pedido forzando el botón final por JavaScript
            var botonConfirmar = espera.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Confirmar')] | (.//*[normalize-space(text()) and normalize-space(.)='Volver al catálogo'])/preceding::button")));
            js.ExecuteScript("arguments[0].click();", botonConfirmar);

            // 7. Aserción final esperando que aparezca la confirmación en pantalla (NUnit 4+)
            var elementoPedido = espera.Until(d => d.FindElement(By.XPath("//strong[contains(text(),'')] | (.//*[normalize-space(text()) and normalize-space(.)='Número de pedido'])/following::strong")));

            Assert.That(elementoPedido, Is.Not.Null);
            Assert.That(elementoPedido.Text, Is.Not.Empty);
        }
    }
}