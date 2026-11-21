using DiarsFinal.Controllers;
using DiarsFinal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DiarsFinal.Data {
    public class LoginData
    {
        private readonly DiarsContext _context;

        public LoginData(DiarsContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ValidarLogin(
            string usuario,
            string contrasena)
        {
            var admin = await _context.Usuarios
                .FirstOrDefaultAsync(x => x.Usuario1 == usuario);

            if (admin == null)
                return null;

            var hasher = new PasswordHasher<object>();

            var resultado = hasher.VerifyHashedPassword(
                null,
                admin.Contrasena,
                contrasena);

            if (resultado == PasswordVerificationResult.Success)
            {
                return admin;
            }

            return null;
        }
    }
}