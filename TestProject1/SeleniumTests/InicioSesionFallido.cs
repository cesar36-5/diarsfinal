using System;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SeleniumTests
{
    [TestFixture]
    public class InicioSesionFallido
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
        public void TheInicioSesionFallidoTest()
        {
            // 1. Navega a la raíz del sitio usando la urlBase dinámica
            driver!.Navigate().GoToUrl($"{urlBase}/");

            // 2. Ejecuta el flujo de login incorrecto grabado
            driver.FindElement(By.XPath("//div[@id='navbarPatissier']/div[3]/a")).Click();
            driver.FindElement(By.Name("usuario")).Click();
            driver.FindElement(By.Name("usuario")).Clear();
            driver.FindElement(By.Name("usuario")).SendKeys("admin");

            driver.FindElement(By.Name("contrasena")).Clear();
            driver.FindElement(By.Name("contrasena")).SendKeys("1234");

            driver.FindElement(By.XPath("//button[@type='submit']")).Click();

            // 3. Validación final: Asegura que no ingresó al Dashboard y permaneció fuera
            Assert.That(driver.Url, Does.Not.Contain("Dashboard").And.Not.Contain("Admin"));
        }
    }
}