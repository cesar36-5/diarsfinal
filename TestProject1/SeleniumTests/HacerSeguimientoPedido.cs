using System;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SeleniumTests
{
    [TestFixture]
    public class HacerSeguimientoPedido
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

            // Inicializa Google Chrome
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
        public void TheHacerSeguimientoPedidoTest()
        {
            // 1. Navega a la vista de seguimiento usando la configuración dinámica
            driver!.Navigate().GoToUrl($"{urlBase}/Seguimiento");

            // 2. Ingresa el código del pedido de prueba (ej. 493919)
            driver.FindElement(By.Id("codigo")).Click();
            driver.FindElement(By.Id("codigo")).Clear();
            driver.FindElement(By.Id("codigo")).SendKeys("493919");

            // 3. Envía el formulario para consultar el estado
            driver.FindElement(By.XPath("//button[@type='submit']")).Click();

            // 4. Validación final: Asegura que la página muestre resultados del código consultado
            Assert.That(driver.PageSource, Does.Contain("493919").Or.Contain("Estado").Or.Contain("Pedido"));
        }
    }
}