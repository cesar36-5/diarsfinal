using DiarsFinal.Controllers;
using DiarsFinal.Models;
using Microsoft.EntityFrameworkCore;

namespace DiarsFinal.Data
{
    public class PersonalizadoData
    {
        private readonly DiarsContext _context;

        public PersonalizadoData(DiarsContext context)
        {
            _context = context;
        }

        public async Task<List<Producto>> ObtenerBases()
        {
            return await _context.Productos
                .Where(p =>
                    p.IdCategoria == 3 &&
                    p.Activo == "1")
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }

        public async Task<List<Producto>> ObtenerCompatibles(
            int idProducto)
        {
            return await _context.Compatibilidades
                .Where(c => c.IdProductoA == idProducto)
                .Select(c => c.ProductoB)
                .Where(p => p.Activo == "1")
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }
    }
}
