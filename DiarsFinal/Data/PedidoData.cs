using DiarsFinal.Controllers;
using DiarsFinal.Models;
using DiarsFinal.ViewModels;
using DiarsFinal.ViewModels.DiarsFinal.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DiarsFinal.Datos
{
    public class PedidoData
    {
        private readonly DiarsContext _context;

        public PedidoData(DiarsContext context)
        {
            _context = context;
        }

        public async Task<int> GenerarPedido(
            List<CarritoItem> carrito,
            string nombre,
            string? comentario,
            string dni,
            string telefono,
            DateTime fechaEntrega)
        {
            string fechaTexto =
                fechaEntrega.ToString("yyyy-MM-dd");

            var especificacion =
                await _context.Especificaciones
                .FirstOrDefaultAsync(e =>
                    e.DiasTrabajo == fechaTexto);

            int codigo = await GenerarCodigoPedido();

            if (especificacion == null)
            {
                string diaSemana =
                    fechaEntrega.DayOfWeek switch
                    {
                        DayOfWeek.Monday => "Lunes",
                        DayOfWeek.Tuesday => "Martes",
                        DayOfWeek.Wednesday => "Miercoles",
                        DayOfWeek.Thursday => "Jueves",
                        DayOfWeek.Friday => "Viernes",
                        DayOfWeek.Saturday => "Sabado",
                        _ => "Domingo"
                    };

                var baseDia =
                    await _context.Especificaciones
                    .FirstOrDefaultAsync(e =>
                        e.DiasTrabajo == diaSemana);

                if (baseDia == null)
                {
                    return -1;
                }

                especificacion =
                    new Especificacione
                    {
                        DiasTrabajo = fechaTexto,
                        CapacidadTotal =
                            baseDia.CapacidadTotal,
                        CapacidadActual =
                            baseDia.CapacidadTotal
                    };

                _context.Especificaciones
                    .Add(especificacion);

                await _context.SaveChangesAsync();
            }

            int consumoTotal = 0;
            decimal precioTotal = 0;

            foreach (var item in carrito)
            {
                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.IdProducto == item.IdProducto);

                if (producto == null)
                    continue;

                consumoTotal += producto.Consumo * item.Cantidad;
                precioTotal += producto.Precio * item.Cantidad;
            }

            if (consumoTotal > especificacion.CapacidadActual)
            {
                return -1;
            }

            Pedido pedido =
                new Pedido
                {
                    Nombre = nombre,
                    Dni = dni,
                    Telefono = telefono,
                    FechaEntrega = fechaEntrega,
                    FechaCreacion = DateTime.Now,
                    Precio = precioTotal,
                    Estado = "1",
                    Tipo = "0",
                    Comentario = "",
                    Codigo = codigo
                };

            _context.Pedidos.Add(pedido);

            await _context.SaveChangesAsync();

            foreach (var item in carrito)
            {
                DetallePedido detalle =
                    new DetallePedido
                    {
                        IdPedido = pedido.IdPedido,
                        IdProducto = item.IdProducto,
                        Cantidad = item.Cantidad
                    };

                _context.DetallePedidos.Add(detalle);
            }

            especificacion.CapacidadActual -= consumoTotal;

            await _context.SaveChangesAsync();

            return codigo;
        }

        public async Task<int> GenerarCodigoPedido()
        {
            var candidatos = Enumerable.Range(0, 5)
                .Select(_ => Random.Shared.Next(100000, 1000000))
                .Distinct()
                .ToList();

            var existentes = await _context.Pedidos
                .AsNoTracking()
                .Where(p => candidatos.Contains(p.Codigo))
                .Select(p => p.Codigo)
                .ToListAsync();

            var codigoDisponible = candidatos.Except(existentes).FirstOrDefault();

            if (codigoDisponible != 0)
            {
                return codigoDisponible;
            }

            return await GenerarCodigoPedido();
        }
        public async Task<List<DashboardPedidoVM>>
    ObtenerPedidosDashboard()
        {
            var pedidos = await _context.Pedidos
                .Where(p =>
                    p.Estado != "2" &&
                    p.Estado != "5" &&
                    p.Estado != "0")
                .OrderBy(p => p.FechaEntrega)
                .ToListAsync();

            List<DashboardPedidoVM> resultado =
                new List<DashboardPedidoVM>();

            foreach (var pedido in pedidos)
            {
                resultado.Add(
                    new DashboardPedidoVM
                    {
                        Pedido = pedido,

                        DetallesPedido =
                            await _context.DetallePedidos
                            .Include(d => d.IdProductoNavigation)
                            .Where(d =>
                                d.IdPedido ==
                                pedido.IdPedido)
                            .ToListAsync(),

                        DetallesPersonalizados =
                            await _context.DetallePersonalizados
                            .Include(d => d.IdProductoNavigation)
                            .Where(d => d.IdPedido == pedido.IdPedido)
                            .OrderBy(d => d.Posicion)
                            .ToListAsync()
                    });
            }

            return resultado;
        }

        public async Task CambiarEstado(
            int idPedido,
            string estadoNuevo)
        {
            var pedido =
                await _context.Pedidos
                .FirstOrDefaultAsync(p =>
                    p.IdPedido == idPedido);

            if (pedido == null)
                return;

            pedido.Estado = estadoNuevo;

            await _context.SaveChangesAsync();
        }

        public async Task<Pedido?> ObtenerPorCodigo(
            int codigo)
        {
            return await _context.Pedidos
                .FirstOrDefaultAsync(p =>
                    p.Codigo == codigo);
        }

        public async Task ConfirmarPago(
            int idPedido)
        {
            var pedido =
                await _context.Pedidos
                .FirstOrDefaultAsync(p =>
                    p.IdPedido == idPedido);

            if (pedido == null)
                return;

            if (pedido.Estado == "0")
            {
                pedido.Estado = "1";

                await _context.SaveChangesAsync();
            }
        }

        public async Task<Producto?> ObtenerProducto(int id)
        {
            return await _context.Productos
                .FirstOrDefaultAsync(p => p.IdProducto == id);
        }

        public async Task<Especificacione?> ObtenerEspecificacion(string fechaTexto, DateTime fecha)
        {
            // 1. Buscar por fecha exacta
            var esp = await _context.Especificaciones
                .FirstOrDefaultAsync(e => e.DiasTrabajo == fechaTexto);

            if (esp != null)
                return esp;

            // 2. Buscar plantilla por día de semana
            string diaSemana =
                fecha.DayOfWeek switch
                {
                    DayOfWeek.Monday => "Lunes",
                    DayOfWeek.Tuesday => "Martes",
                    DayOfWeek.Wednesday => "Miercoles",
                    DayOfWeek.Thursday => "Jueves",
                    DayOfWeek.Friday => "Viernes",
                    DayOfWeek.Saturday => "Sabado",
                    _ => "Domingo"
                };

            var baseDia = await _context.Especificaciones
                .FirstOrDefaultAsync(e => e.DiasTrabajo == diaSemana);

            if (baseDia == null)
                return null; // sin configuración base

            // 3. Crear nueva instancia para la fecha real
            var nueva = new Especificacione
            {
                DiasTrabajo = fechaTexto,
                CapacidadTotal = baseDia.CapacidadTotal,
                CapacidadActual = baseDia.CapacidadTotal
            };

            _context.Especificaciones.Add(nueva);
            await _context.SaveChangesAsync();

            return nueva;
        }



        public void AddPedido(Pedido pedido)
        {
            _context.Pedidos.Add(pedido);
        }

        public void AddDetallePersonalizado(DetallePersonalizado detalle)
        {
            _context.DetallePersonalizados.Add(detalle);
        }

        public Task Save()
        {
            return _context.SaveChangesAsync();
        }

        public async Task<Producto?> ObtenerProductoPorId(int id)
        {
            return await _context.Productos.FirstOrDefaultAsync(p => p.IdProducto == id);
        }

        public async Task<bool> ValidarCapacidad(DateTime fecha, int consumo)
        {
            string fechaTexto = fecha.ToString("yyyy-MM-dd");

            var esp = await _context.Especificaciones
                .FirstOrDefaultAsync(e => e.DiasTrabajo == fechaTexto);

            if (esp == null) return false;

            return esp.CapacidadActual >= consumo;
        }

        public async Task<List<ProductoTopVM>> TopProductosFinales()
        {
            return await _context.DetallePedidos
                .Include(x => x.IdProductoNavigation)
                .Where(x => x.IdPedidoNavigation.Tipo == "0") // FINAL
                .GroupBy(x => x.IdProductoNavigation.Nombre)
                .Select(g => new ProductoTopVM
                {
                    Nombre = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToListAsync();
        }

        public async Task<List<ProductoTopVM>> TopProductosPersonalizados()
        {
            return await _context.DetallePersonalizados
                .Include(x => x.IdProductoNavigation)
                .GroupBy(x => x.IdProductoNavigation.Nombre)
                .Select(g => new ProductoTopVM
                {
                    Nombre = g.Key,
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(3)
                .ToListAsync();
        }

        public async Task<List<VentasMesVM>> TopMesesVentas()
        {
            return await _context.Pedidos
                .GroupBy(x => new
                {
                    x.FechaCreacion.Year,
                    x.FechaCreacion.Month
                })
                .Select(g => new VentasMesVM
                {
                    Mes = g.Key.Year + "-" + g.Key.Month,
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(3)
                .ToListAsync();
        }

        public async Task<(int Codigo, decimal Total)> GenerarPedidoPersonalizado(
    PedidoPersonalizadoVM vm)
        {
            string fechaTexto = vm.FechaEntrega.ToString("yyyy-MM-dd");

            var esp = await ObtenerEspecificacion(
                fechaTexto,
                vm.FechaEntrega);

            if (esp == null)
                return (-1, 0);

            int consumoTotal = 0;
            decimal precioTotal = 0;

            foreach (var sel in vm.Selecciones)
            {
                if (sel.IdProducto <= 0)
                    continue;

                var producto =
                    await ObtenerProductoPorId(sel.IdProducto);

                if (producto == null)
                    continue;

                consumoTotal += producto.Consumo;
                precioTotal += producto.Precio;
            }

            if (esp.CapacidadActual < consumoTotal)
                return (-1, 0);

            int codigo = await GenerarCodigoPedido();

            var pedido = new Pedido
            {
                Nombre = vm.Nombre,
                Dni = vm.Dni,
                Telefono = vm.Telefono,
                FechaEntrega = vm.FechaEntrega,
                FechaCreacion = DateTime.Now,
                Precio = precioTotal,
                Estado = "1",
                Tipo = "1",
                Comentario = vm.Comentario ?? "",
                Codigo = codigo
            };

            _context.Pedidos.Add(pedido);

            await _context.SaveChangesAsync();

            foreach (var sel in vm.Selecciones)
            {
                if (sel.IdProducto <= 0)
                    continue;

                var detalle = new DetallePersonalizado
                {
                    IdPedido = pedido.IdPedido,
                    IdProducto = sel.IdProducto,
                    Comentario = sel.Comentario ?? "",
                    Posicion = sel.Posicion
                };

                _context.DetallePersonalizados.Add(detalle);
            }

            await _context.SaveChangesAsync();

            var filasActualizadas = await _context.Especificaciones
                .Where(e => e.IdEspecificaciones == esp.IdEspecificaciones)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(
                        e => e.CapacidadActual,
                        e => e.CapacidadActual - consumoTotal));

            Console.WriteLine($"ID especificación: {esp.IdEspecificaciones}");
            Console.WriteLine($"Consumo total: {consumoTotal}");
            Console.WriteLine($"Filas actualizadas: {filasActualizadas}");

            return (codigo, precioTotal);
        }
    }
}