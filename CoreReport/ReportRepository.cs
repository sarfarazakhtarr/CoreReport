using CoreReport.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CoreReport
{
    public class ReportRepository
    {
        private readonly string _connectionString;

        public ReportRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public List<ReportModel> GetAll()
        {
            var reports = new List<ReportModel>();

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("GetAllReports", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reports.Add(new ReportModel
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ReportName = reader["ReportName"].ToString(),
                            SP_Name = reader["SP_Name"].ToString(),
                           
                            Parameters = reader["Parameters"]?.ToString()
                        });
                    }
                }
            }

            return reports;
        }

        public ReportModel GetById(int id)
        {
            ReportModel model = null;

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("GetReportById", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model = new ReportModel
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ReportName = reader["ReportName"].ToString(),
                            SP_Name = reader["SP_Name"].ToString(),
                           
                            Parameters = reader["Parameters"]?.ToString()
                        };
                    }
                }
            }

            return model;
        }

        public void Insert(ReportModel model)
        {
            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("InsertReport", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReportName", model.ReportName);
                cmd.Parameters.AddWithValue("@SP_Name", model.SP_Name);
                cmd.Parameters.AddWithValue("@Parameters", (object?)model.Parameters ?? DBNull.Value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(ReportModel model)
        {
            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("UpdateReport", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@ReportName", model.ReportName);
                cmd.Parameters.AddWithValue("@SP_Name", model.SP_Name);
                cmd.Parameters.AddWithValue("@Parameters", (object?)model.Parameters ?? DBNull.Value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("DeleteReport", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
