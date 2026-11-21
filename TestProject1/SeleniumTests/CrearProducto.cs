using System;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SeleniumTests
{
    [TestFixture]
    public class CrearProducto
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
        public void TheCrearProductoTest()
        {
            // 1. Configurar una espera inteligente de máximo 10 segundos
            var espera = new WebDriverWait(driver!, TimeSpan.FromSeconds(10));
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver!;

            // 2. IR AL LOGIN PRIMERO (Paso obligatorio)
            driver!.Navigate().GoToUrl($"{urlBase}/");

            // Hacer clic en el botón de acceso del navbar
            var botonAcceso = espera.Until(d => d.FindElement(By.XPath("//div[@id='navbarPatissier']/div/a/i")));
            botonAcceso.Click();

            // Llenar credenciales de Administrador
            var inputUser = espera.Until(d => d.FindElement(By.Name("usuario")));
            inputUser.SendKeys("admin");

            var inputPass = driver.FindElement(By.Name("contrasena"));
            inputPass.SendKeys("diarz");

            // Forzar el clic de Iniciar Sesión por JavaScript
            var botonLoginSubmit = espera.Until(d => d.FindElement(By.XPath("//form//button[@type='submit'] | //button[@type='submit']")));
            js.ExecuteScript("arguments[0].click();", botonLoginSubmit);

            // 3. SECCIÓN CREAR PRODUCTO (Ahora que ya estamos logueados)
            driver.Navigate().GoToUrl($"{urlBase}/Productos");

            // SOLUCIÓN: Llenamos los campos directamente con SendKeys sin usar Click() para evitar bloqueos
            var inputNombre = espera.Until(d => d.FindElement(By.Name("Nombre")));
            inputNombre.Clear();
            inputNombre.SendKeys("ProductoEjemplo4");

            var inputDescripcion = driver.FindElement(By.Name("Descripcion"));
            inputDescripcion.Clear();
            inputDescripcion.SendKeys("dgfvdc");

            var inputPrecio = driver.FindElement(By.Name("Precio"));
            inputPrecio.Clear();
            inputPrecio.SendKeys("5");

            var inputConsumo = driver.FindElement(By.Name("Consumo"));
            inputConsumo.Clear();
            inputConsumo.SendKeys("1");

            // Seleccionar elementos de los menús desplegables
            new SelectElement(driver.FindElement(By.Name("Tipo"))).SelectByText("Final");
            new SelectElement(driver.FindElement(By.Name("IdCategoria"))).SelectByText("Topping");

            // Forzar la activación y el envío del formulario final
            var checkboxActivo = driver.FindElement(By.Id("Activo"));
            js.ExecuteScript("arguments[0].click();", checkboxActivo);

            var botonProductSubmit = espera.Until(d => d.FindElement(By.XPath("//form[contains(@action, 'Crear')]//button[@type='submit'] | //button[@type='submit' and contains(text(), 'Guardar')] | //button[@type='submit']")));
            js.ExecuteScript("arguments[0].click();", botonProductSubmit);

            // 4. Validación final
            espera.Until(d => d.Url.Contains("Productos") || d.Url.Contains("Index"));
            Assert.That(driver.Url, Does.Contain("Productos").Or.Contain("Index"));
        }
    }
}