using DiarsFinal.Controllers;
using DiarsFinal.Models;
using Microsoft.EntityFrameworkCore;

namespace DiarsFinal.Datos
{
    public class CategoriaData
    {
        private readonly DiarsContext _context;

        public CategoriaData(DiarsContext context)
        {
            _context = context;
        }

        // LISTAR
        public async Task<List<Categorium>> ListarCategorias()
        {
            return await _context.Categoria.ToListAsync();
        }

        // OBTENER
        public async Task<Categorium?> ObtenerCategoria(int id)
        {
            return await _context.Categoria.FindAsync(id);
        }

        // AGREGAR
        public async Task AgregarCategoria(Categorium categoria)
        {
            ArgumentNullException.ThrowIfNull(categoria);
            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();
        }

        // EDITAR
        public async Task EditarCategoria(Categorium categoria)
        {
            ArgumentNullException.ThrowIfNull(categoria);
            _context.Categoria.Update(categoria);
            await _context.SaveChangesAsync();
        }

        // ELIMINAR
        public async Task<bool> EliminarCategoria(int id)
        {
            var categoria = await _context.Categoria.FindAsync(id);

            if (categoria == null)
            {
                return false;
            }

            _context.Categoria.Remove(categoria);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}