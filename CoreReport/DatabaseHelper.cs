using CoreReport.Models;
using System.Data;
using System.Data.SqlClient;

namespace CoreReport
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<List<ReportInfo>> GetReportsAsync()
        {
            var reports = new List<ReportInfo>();
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("SELECT * FROM Reports", conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                reports.Add(new ReportInfo
                {
                    Id = reader.GetInt32(0),
                    ReportName = reader.GetString(1),
                    SP_Name = reader.GetString(2)
                });
            }
            return reports;
        }

        public async Task<DataTable> ExecuteStoredProcedureAsync(string spName, Dictionary<string, object> parameters)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(spName, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            foreach (var param in parameters)
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);

            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }
    }

}
