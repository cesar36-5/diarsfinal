using System.Data;

namespace DiarsFinal.Helpers
{
    public interface ISqlQueryExecutor
    {
        Task<DataTable> EjecutarConsultaAsync(
            string query,
            Dictionary<string, object> parametros);
    }
}
