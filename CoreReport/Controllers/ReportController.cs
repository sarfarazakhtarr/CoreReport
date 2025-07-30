using CoreReport.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Formats.Asn1;
using System.Globalization;
using System.Text;
using System.Text.Json;

public class ReportController : Controller
{
    private readonly string _connectionString;

    public ReportController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public IActionResult Index()
    {
        var reportList = new List<ReportModel>();

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand("dbo.GetReportList", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var rawParams = reader["Parameters"] == DBNull.Value ? null : reader["Parameters"].ToString();

                        List<Parameters> paramList = null;
                        if (!string.IsNullOrWhiteSpace(rawParams))
                        {
                            try
                            {
                                paramList = JsonSerializer.Deserialize<List<Parameters>>(rawParams, _jsonOptions);
                            }
                            catch
                            {
                                paramList = new List<Parameters>();
                            }
                        }
                        else
                        {
                            paramList = new List<Parameters>();
                        }

                        reportList.Add(new ReportModel
                        {
                            Id = (int)reader["Id"],
                            ReportName = reader["ReportName"].ToString(),
                            SP_Name = reader["SP_Name"].ToString(),
                            Parameters = rawParams,
                            ParameterList = paramList
                        });
                    }
                }
            }
        }

        return View(reportList);
    }




    [HttpPost]
    public IActionResult GenerateReport(int reportId, string spName, Dictionary<string, string> parameters)
    {
        // Optional: Get parameter definitions (metadata) from DB for validation
        List<Parameters> parameterMeta = null;
        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using var cmd = new SqlCommand("dbo.GetReportParameters", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", reportId);

            var rawParams = cmd.ExecuteScalar()?.ToString();

            if (!string.IsNullOrWhiteSpace(rawParams))
            {
                parameterMeta = JsonSerializer.Deserialize<List<Parameters>>(rawParams, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }

        // Convert and validate parameters using the metadata (optional but good)
        var sqlParameters = new List<SqlParameter>();

        if (parameterMeta != null)
        {
            foreach (var meta in parameterMeta)
            {
                if (string.IsNullOrWhiteSpace(meta?.Name))
                    continue;

                if (!parameters.TryGetValue(meta.Name, out var rawValue))
                    rawValue = null;

                if (meta.IsRequired && string.IsNullOrWhiteSpace(rawValue))
                    return BadRequest($"Parameter '{meta.Name}' is required.");

                var sqlParam = new SqlParameter("@" + meta.Name, MapSqlDbType(meta.Type));

                sqlParam.Value = string.IsNullOrWhiteSpace(rawValue)
                    ? DBNull.Value
                    : ConvertToType(rawValue, meta.Type);

                sqlParameters.Add(sqlParam);
            }
        }
        else
        {
            // If no metadata, just use whatever came from the form
            foreach (var kvp in parameters)
            {
                sqlParameters.Add(new SqlParameter("@" + kvp.Key, kvp.Value ?? (object)DBNull.Value));
            }
        }

        var reportData = ExecuteStoredProcedure(spName, sqlParameters);

        string fileName = $"Report_{reportId}_{DateTime.Now:yyyyMMddHHmmss}.csv";
        return File(reportData, "text/csv", fileName);
    }

    // Map your metadata type string to SqlDbType
    private SqlDbType MapSqlDbType(string type)
    {
        return type.ToUpper() switch
        {
            "INT" => SqlDbType.Int,
            "VARCHAR" => SqlDbType.VarChar,
            "NVARCHAR" => SqlDbType.NVarChar,
            "DATETIME" => SqlDbType.DateTime,
            "DATE" => SqlDbType.Date,
            "BIT" => SqlDbType.Bit,
            "DECIMAL" => SqlDbType.Decimal,
            _ => throw new NotSupportedException($"Unsupported SQL type: {type}")
        };
    }


    // Convert string input to correct .NET type for SQL parameter
    private object ConvertToType(string value, string type)
    {
        switch (type.ToUpper())
        {
            case "INT":
                return int.Parse(value);
            case "BIT":
                return bool.Parse(value);
            case "DATETIME":
                return DateTime.Parse(value);
            case "DECIMAL":
                return decimal.Parse(value);
            case "VARCHAR":
            case "NVARCHAR":
                return value;
            default:
                throw new NotSupportedException($"Unsupported type: {type}");
        }
    }


    private byte[] ExecuteStoredProcedure(string spName, List<SqlParameter> parameters)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        using var cmd = new SqlCommand(spName, conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddRange(parameters.ToArray());

        using var reader = cmd.ExecuteReader();
        var dataTable = new DataTable();
        dataTable.Load(reader);

        return DataTableToCsv(dataTable); // <-- convert to CSV format
    }

    private byte[] DataTableToCsv(DataTable dt)
    {
        StringBuilder csvContent = new StringBuilder();

        for (int i = 0; i < dt.Columns.Count; i++)
        {
            csvContent.Append(dt.Columns[i]);
            if (i < dt.Columns.Count - 1) csvContent.Append(",");
        }
        csvContent.AppendLine();

        foreach (DataRow row in dt.Rows)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                csvContent.Append(row[i].ToString());
                if (i < dt.Columns.Count - 1) csvContent.Append(",");
            }
            csvContent.AppendLine();
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }
}
