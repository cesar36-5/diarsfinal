using DiarsFinal.Data;
using DiarsFinal.Datos;
using Microsoft.AspNetCore.Mvc;

namespace DiarsFinal.Controllers
{
    public class LoginController : Controller
    {
        private readonly LoginData _loginData;

        public LoginController(LoginData loginData)
        {
            _loginData = loginData;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(
            string usuario,
            string contrasena)
        {
            var admin = await _loginData.ValidarLogin(
                usuario,
                contrasena);

            if (admin == null)
            {
                ViewBag.Error =
                    "Usuario o contraseña incorrectos";

                return View();
            }

            HttpContext.Session.SetString(
                "Usuario",
                admin.Usuario1);

            return RedirectToAction(
                "Index",
                "Dashboard");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Index));
        }
    }
}