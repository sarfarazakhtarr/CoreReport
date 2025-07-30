using CoreReport.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Data;
using System.Data.SqlClient;
using System.Text;

public class HomeController : Controller
{
    private readonly string _connectionString;

    public HomeController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public IActionResult Index()
    {
        var reports = new List<ReportModel>();

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand("SELECT Id, ReportName, SP_Name FROM Mst_Reports", conn);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                reports.Add(new ReportModel
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    ReportName = reader["ReportName"].ToString(),
                    SP_Name = reader["SP_Name"].ToString()
                });
            }
        }

        return View(reports);
    }

    [HttpGet]
    public IActionResult GetSPParameters(string spName)
    {
        var parameters = new List<SPParameterModel>();

        using (var conn = new SqlConnection(_connectionString))
        {
            using (var cmd = new SqlCommand(spName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();
                SqlCommandBuilder.DeriveParameters(cmd);

                foreach (SqlParameter param in cmd.Parameters)
                {
                    if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                    {
                        parameters.Add(new SPParameterModel
                        {
                            ParameterName = param.ParameterName,
                            DataType = param.SqlDbType.ToString()
                        });
                    }
                }
            }
        }

        return Json(parameters);
    }

    [HttpPost]
    public IActionResult ExecuteSP(string spName, [FromForm] Dictionary<string, string> parameters)
    {
        DataTable dt = new DataTable();

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            SqlCommand cmd = new SqlCommand(spName, conn);
            cmd.CommandType = CommandType.StoredProcedure;

            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value.ToString());
            }

            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
        }

        // Convert DataTable to CSV
        var csv = new StringBuilder();

        // Header
        for (int i = 0; i < dt.Columns.Count; i++)
        {
            csv.Append(dt.Columns[i].ColumnName + (i < dt.Columns.Count - 1 ? "," : "\n"));
        }

        // Rows
        foreach (DataRow row in dt.Rows)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                csv.Append(row[i].ToString() + (i < dt.Columns.Count - 1 ? "," : "\n"));
            }
        }

        byte[] buffer = Encoding.UTF8.GetBytes(csv.ToString());
        return File(buffer, "text/csv", $"{spName}_Report.csv");
    }
}
