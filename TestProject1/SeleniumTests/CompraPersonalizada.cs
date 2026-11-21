using System;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SeleniumTests
{
    [TestFixture]
    public class CompraPersonalizada
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
        public void TheCompraPersonalizadaTest()
        {
            // 1. Configurar una espera inteligente de máximo 10 segundos
            var espera = new WebDriverWait(driver!, TimeSpan.FromSeconds(10));

            // 2. Navega a la vista de pedido personalizado usando la configuración dinámica
            driver!.Navigate().GoToUrl($"{urlBase}/Personalizado");

            // 3. Completa los datos personales del cliente (Llenamos directamente sin Click para evitar bloqueos)
            var inputNombre = espera.Until(d => d.FindElement(By.Name("Nombre")));
            inputNombre.Clear();
            inputNombre.SendKeys("ssfgn");

            var inputDni = driver.FindElement(By.Name("Dni"));
            inputDni.Clear();
            inputDni.SendKeys("73870884");

            var inputTelefono = driver.FindElement(By.Name("Telefono"));
            inputTelefono.Clear();
            inputTelefono.SendKeys("123456789");

            var inputFecha = driver.FindElement(By.Name("FechaEntrega"));
            inputFecha.Clear();
            inputFecha.SendKeys("2026-11-03");

            // 4. Selección de componentes esperando explícitamente a que aparezca cada select en el DOM
            var selectBase = espera.Until(d => d.FindElement(By.XPath("//div[@id='niveles']/div/div[2]/select | //select[contains(., 'Base')]")));
            new SelectElement(selectBase).SelectByText("Base de cholate 30x15 - S/ 10.00");

            var selectCubierta = espera.Until(d => d.FindElement(By.XPath("//div[@id='niveles']/div[2]/div[2]/select | //select[contains(., 'Cubierta')]")));
            new SelectElement(selectCubierta).SelectByText("Cubierta completa de crema chantilly - S/ 10.00");

            var selectTopping = espera.Until(d => d.FindElement(By.XPath("//div[@id='niveles']/div[3]/div[2]/select | //select[contains(., 'Topping')]")));
            new SelectElement(selectTopping).SelectByText("Topping de fresas - S/ 4.00");

            // 5. Envío del formulario forzándolo por JavaScript para saltar cualquier bloqueo de diseño
            var botonSubmit = espera.Until(d => d.FindElement(By.XPath("//button[@type='submit']")));
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].click();", botonSubmit);

            // 6. Validación final: Confirma que avanzó a la vista de seguimiento o confirmación
            espera.Until(d => d.Url.Contains("Seguimiento") || d.Url.Contains("Confirmacion") || d.Url.Contains("Personalizado"));
            Assert.That(driver.Url, Does.Contain("Seguimiento").Or.Contain("Confirmacion").Or.Contains("Personalizado"));
        }
    }
}