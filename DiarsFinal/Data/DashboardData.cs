using DiarsFinal.ViewModels.DiarsFinal.ViewModels;
using Microsoft.Data.SqlClient;
using System.Data;
using DiarsFinal.Helpers;

namespace DiarsFinal.Data
{
    public class DashboardData
    {
        private readonly string? _connectionString;
        private readonly ISqlQueryExecutor? _executor;

        public DashboardData(IConfiguration config)
        {
            _connectionString =
                config.GetConnectionString("DefaultConnection");
        }

        // Constructor adicional: permite usar Moq
        public DashboardData(ISqlQueryExecutor executor)
        {
            _executor = executor;
        }

        public async Task<List<ProductoTopVM>> TopProductosFinales(
            int top)
        {
            string query = @"
                SELECT TOP (@top)
                    p.Nombre,
                    COUNT(*) AS Cantidad
                FROM DetallePedido d
                INNER JOIN Producto p ON p.IdProducto = d.IdProducto
                INNER JOIN Pedido pe ON pe.IdPedido = d.IdPedido
                WHERE pe.Tipo = '0'
                GROUP BY p.Nombre
                ORDER BY COUNT(*) DESC";

            if (_executor != null)
            {
                var tabla = await _executor.EjecutarConsultaAsync(
                    query,
                    new Dictionary<string, object>
                    {
                        ["@top"] = top
                    });

                return tabla.AsEnumerable()
                    .Select(fila => new ProductoTopVM
                    {
                        Nombre = fila["Nombre"].ToString()!,
                        Cantidad = Convert.ToInt32(fila["Cantidad"])
                    })
                    .ToList();
            }

            // Ejecución SQL original
            var lista = new List<ProductoTopVM>();

            await using var conn =
                new Microsoft.Data.SqlClient.SqlConnection(
                    _connectionString);

            await conn.OpenAsync();

            await using var cmd =
                new Microsoft.Data.SqlClient.SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@top", top);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new ProductoTopVM
                {
                    Nombre = reader["Nombre"].ToString()!,
                    Cantidad = (int)reader["Cantidad"]
                });
            }

            return lista;
        }

        public async Task<List<ProductoTopVM>> TopProductosPersonalizados(int top)
        {
            string query = @"
        SELECT TOP (@top)
            p.Nombre,
            COUNT(*) AS Cantidad
        FROM DetallePersonalizado d
        INNER JOIN Producto p ON p.IdProducto = d.IdProducto
        INNER JOIN Pedido pe ON pe.IdPedido = d.IdPedido
        WHERE pe.Tipo = '1'
        GROUP BY p.Nombre
        ORDER BY COUNT(*) DESC";

            if (_executor != null)
            {
                var tabla = await _executor.EjecutarConsultaAsync(
                    query,
                    new Dictionary<string, object>
                    {
                        ["@top"] = top
                    });

                return tabla.AsEnumerable()
                    .Select(fila => new ProductoTopVM
                    {
                        Nombre = fila["Nombre"].ToString()!,
                        Cantidad = Convert.ToInt32(fila["Cantidad"])
                    })
                    .ToList();
            }

            var lista = new List<ProductoTopVM>();

            await using var conn =
                new Microsoft.Data.SqlClient.SqlConnection(_connectionString);

            await conn.OpenAsync();

            await using var cmd =
                new Microsoft.Data.SqlClient.SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@top", top);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new ProductoTopVM
                {
                    Nombre = reader["Nombre"].ToString()!,
                    Cantidad = (int)reader["Cantidad"]
                });
            }

            return lista;
        }

        public async Task<List<VentasMesVM>> TopMesesVentas(int top)
        {
            string query = @"
        SELECT TOP (@top)
            FORMAT(FechaCreacion, 'yyyy-MM') AS Mes,
            COUNT(*) AS Cantidad
        FROM Pedido
        GROUP BY FORMAT(FechaCreacion, 'yyyy-MM')
        ORDER BY COUNT(*) DESC";

            if (_executor != null)
            {
                var tabla = await _executor.EjecutarConsultaAsync(
                    query,
                    new Dictionary<string, object>
                    {
                        ["@top"] = top
                    });

                return tabla.AsEnumerable()
                    .Select(fila => new VentasMesVM
                    {
                        Mes = fila["Mes"].ToString()!,
                        Cantidad = Convert.ToInt32(fila["Cantidad"])
                    })
                    .ToList();
            }

            var lista = new List<VentasMesVM>();

            await using var conn =
                new Microsoft.Data.SqlClient.SqlConnection(_connectionString);

            await conn.OpenAsync();

            await using var cmd =
                new Microsoft.Data.SqlClient.SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@top", top);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new VentasMesVM
                {
                    Mes = reader["Mes"].ToString()!,
                    Cantidad = (int)reader["Cantidad"]
                });
            }

            return lista;
        }
    }
}
