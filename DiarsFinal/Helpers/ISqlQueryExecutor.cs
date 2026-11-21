using System.Data;

public interface ISqlQueryExecutor
{
    Task<DataTable> EjecutarConsultaAsync(
        string query,
        Dictionary<string, object> parametros);
}