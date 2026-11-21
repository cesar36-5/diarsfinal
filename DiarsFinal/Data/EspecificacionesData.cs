using DiarsFinal.Models;
using DiarsFinal.Controllers;
using Microsoft.EntityFrameworkCore;
using System;

namespace DiarsFinal.Data
{
    public class EspecificacionesData
    {
        private readonly DiarsContext _context;

        public EspecificacionesData(DiarsContext context)
        {
            _context = context;
        }

        // LISTAR 7 PRIMERAS
        public async Task<List<Especificacione>> Listar()
        {
            return await _context.Especificaciones
                .Take(7)
                .ToListAsync();
        }

        // BUSCAR POR ID
        public async Task<Especificacione?> BuscarPorId(int id)
        {
            return await _context.Especificaciones
                .FirstOrDefaultAsync(e => e.IdEspecificaciones == id);
        }

        // EDITAR SOLO CAPACIDADES
        public async Task Editar(Especificacione model)
        {
            var especificacion = await _context.Especificaciones
                .FirstOrDefaultAsync(e => e.IdEspecificaciones == model.IdEspecificaciones);

            if (especificacion == null)
            {
                return;
            }

            especificacion.CapacidadTotal = model.CapacidadTotal;
            especificacion.CapacidadActual = model.CapacidadActual;

            await _context.SaveChangesAsync();
        }
    }
}