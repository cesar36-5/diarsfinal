using DiarsFinal.Controllers;
using DiarsFinal.Models;
using Microsoft.EntityFrameworkCore;

namespace DiarsFinal.Datos
{
    public class ProductoData
    {
        private readonly DiarsContext _context;

        public ProductoData(DiarsContext context)
        {
            _context = context;
        }

        public async Task<List<Producto>> ListarProductosFinales()
        {
            return await _context.Productos
                .Where(p => p.Tipo == "1")
                .ToListAsync();
        }

        // CREAR
        public async Task AgregarProducto(Producto producto)
        {
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
        }

        public async Task CambiarEstado(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto != null)
            {
                producto.Activo = producto.Activo == "1"
                    ? "0"
                    : "1";

                await _context.SaveChangesAsync();
            }
        }

        // LISTAR
        public async Task<List<Producto>> ListarProductos()
        {
            return await _context.Productos
                .Include(p => p.IdCategoriaNavigation)
                .ToListAsync();
        }

        public async Task AgregarCompatibilidad(
            int idProductoA,
            int idProductoB)
        {
            bool existe = await _context.Compatibilidades
                .AnyAsync(c =>
                    c.IdProductoA == idProductoA &&
                    c.IdProductoB == idProductoB);

            if (!existe)
            {
                Compatibilidad compatibilidad =
                    new Compatibilidad
                    {
                        IdProductoA = idProductoA,
                        IdProductoB = idProductoB
                    };

                _context.Compatibilidades.Add(compatibilidad);

                await _context.SaveChangesAsync();
            }
        }

        // OBTENER POR ID
        public async Task<Producto?> ObtenerProducto(int id)
        {
            try
            {
                var producto = await _context.Productos
                    .Include(p => p.IdCategoriaNavigation)
                    .Include(p => p.Compatibilidades)
                        .ThenInclude(c => c.ProductoB)
                    .FirstOrDefaultAsync(p => p.IdProducto == id);

                return producto;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener producto: {ex.Message}");
                return null;
            }
        }

        // PRODUCTOS PARA CATÁLOGO
        public async Task<List<Producto>> ListarProductosCatalogo()
        {
            return await _context.Productos
                .Include(p => p.IdCategoriaNavigation)
                .Where(p =>
                    p.Tipo == "0" &&
                    p.Activo == "1")
                .ToListAsync();
        }

        public async Task EliminarCompatibilidad(
            int idProductoA,
            int idProductoB)
        {
            var compatibilidad =
                await _context.Compatibilidades
                .FirstOrDefaultAsync(c =>
                    c.IdProductoA == idProductoA &&
                    c.IdProductoB == idProductoB);

            if (compatibilidad != null)
            {
                _context.Compatibilidades.Remove(compatibilidad);

                await _context.SaveChangesAsync();
            }
        }

        // EDITAR
        public async Task EditarProducto(Producto producto)
        {
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();
        }

        // ELIMINAR
        public async Task<bool> EliminarProducto(int id)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(id);

                if (producto != null)
                {
                    _context.Productos.Remove(producto);
                    await _context.SaveChangesAsync();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar producto: {ex.Message}");
                return false;
            }
        }
    }
}