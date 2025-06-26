using DispatchManager.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace DispatchManager.DataAccess
{
    public static class DispatchData
    {
        private static readonly string connStr =
            ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

        public static int GetNextJobNumber()
        {
            string sql = "SELECT ISNULL(MAX(JobNo), 8964) + 1 FROM Dispatch";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        public static int GetLeadTimeOffset(string leadTimeName, int columnIndex)
        {
            string sql = "SELECT OffsetProd, OffsetDetail FROM LookupTableProductionLeadTimes WHERE Name = @leadTime";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@leadTime", leadTimeName);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return columnIndex == 3
                            ? reader.GetInt32(0)
                            : reader.GetInt32(1);
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        public static void InsertDispatchRecord(DispatchRecord r)
        {
            string sql = @"
                INSERT INTO Dispatch (
                    WeekNo, DispatchDate, MaterialsOrderedBy, BenchtopOrderedBy,
                    DesignDate, JobNo, MainContractor, ProjectName, ProjectColour, Qty,
                    Installed, Freight, BenchTopSupplier, BenchTopColour, Installer, Comment,
                    DeliveryAddress, Phone, M3, Amount, OrderNumber, DateOrdered, LeadTime,
                    ProdInput, MaterialsOrdered, ReleasedToFactory, FB, EB, AS
                ) VALUES (
                    @WeekNo, @DispatchDate, @MaterialsOrderedBy, @BenchtopOrderedBy,
                    @DesignDate, @JobNo, @MainContractor, @ProjectName, @ProjectColour, @Qty,
                    @Installed, @Freight, @BenchTopSupplier, @BenchTopColour, @Installer, @Comment,
                    @DeliveryAddress, @Phone, @M3, @Amount, @OrderNumber, @DateOrdered, @LeadTime,
                    @ProdInput, @MaterialsOrdered, @ReleasedToFactory, @FB, @EB, @AS
                )";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@WeekNo", r.WeekNo);
                cmd.Parameters.AddWithValue("@DispatchDate", r.DispatchDate);
                cmd.Parameters.AddWithValue("@MaterialsOrderedBy", (object)r.MaterialsOrderedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BenchtopOrderedBy", (object)r.BenchtopOrderedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DesignDate", r.DesignDate);
                cmd.Parameters.AddWithValue("@JobNo", r.JobNo);
                cmd.Parameters.AddWithValue("@MainContractor", r.MainContractor ?? "");
                cmd.Parameters.AddWithValue("@ProjectName", r.ProjectName ?? "");
                cmd.Parameters.AddWithValue("@ProjectColour", r.ProjectColour ?? "");
                cmd.Parameters.AddWithValue("@Qty", r.Qty);
                cmd.Parameters.AddWithValue("@Installed", r.Installed ?? "");
                cmd.Parameters.AddWithValue("@Freight", r.Freight ?? "");
                cmd.Parameters.AddWithValue("@BenchTopSupplier", r.BenchTopSupplier ?? "");
                cmd.Parameters.AddWithValue("@BenchTopColour", r.BenchTopColour ?? "");
                cmd.Parameters.AddWithValue("@Installer", r.Installer ?? "");
                cmd.Parameters.AddWithValue("@Comment", r.Comment ?? "");
                cmd.Parameters.AddWithValue("@DeliveryAddress", r.DeliveryAddress ?? "");
                cmd.Parameters.AddWithValue("@Phone", r.Phone ?? "");
                cmd.Parameters.AddWithValue("@M3", r.M3 ?? "");
                cmd.Parameters.AddWithValue("@Amount", r.Amount);
                cmd.Parameters.AddWithValue("@OrderNumber", r.OrderNumber);
                cmd.Parameters.AddWithValue("@DateOrdered", r.DateOrdered);
                cmd.Parameters.AddWithValue("@LeadTime", r.LeadTime ?? "");

                cmd.Parameters.AddWithValue("@ProdInput", r.ProdInput ?? "");
                cmd.Parameters.AddWithValue("@MaterialsOrdered", r.MaterialsOrdered ?? "");
                cmd.Parameters.AddWithValue("@ReleasedToFactory", r.ReleasedToFactory ?? "");

                cmd.Parameters.AddWithValue("@FB", r.FB);
                cmd.Parameters.AddWithValue("@EB", r.EB);
                cmd.Parameters.AddWithValue("@AS", r.AS);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static List<DispatchRecord> GetAllDispatchRecords()
        {
            var list = new List<DispatchRecord>();

            const string sql = "SELECT * FROM Dispatch ORDER BY DispatchDate DESC";

            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var rec = new DispatchRecord
                        {
                            WeekNo = rdr.GetInt32(rdr.GetOrdinal("WeekNo")),
                            DispatchDate = rdr.GetDateTime(rdr.GetOrdinal("DispatchDate")),
                            MaterialsOrderedBy = rdr.IsDBNull(rdr.GetOrdinal("MaterialsOrderedBy"))
                                                    ? null : (DateTime?)rdr.GetDateTime(rdr.GetOrdinal("MaterialsOrderedBy")),
                            BenchtopOrderedBy = rdr.IsDBNull(rdr.GetOrdinal("BenchtopOrderedBy"))
                                                    ? null : (DateTime?)rdr.GetDateTime(rdr.GetOrdinal("BenchtopOrderedBy")),
                            DesignDate = rdr.GetDateTime(rdr.GetOrdinal("DesignDate")),
                            JobNo = rdr.GetInt32(rdr.GetOrdinal("JobNo")),
                            MainContractor = rdr["MainContractor"].ToString(),
                            ProjectName = rdr["ProjectName"].ToString(),
                            ProjectColour = rdr["ProjectColour"].ToString(),
                            Qty = rdr.GetInt32(rdr.GetOrdinal("Qty")),
                            Installed = rdr["Installed"].ToString(),
                            Freight = rdr["Freight"].ToString(),
                            BenchTopSupplier = rdr["BenchTopSupplier"].ToString(),
                            BenchTopColour = rdr["BenchTopColour"].ToString(),
                            Installer = rdr["Installer"].ToString(),
                            Comment = rdr["Comment"].ToString(),
                            DeliveryAddress = rdr["DeliveryAddress"].ToString(),
                            Phone = rdr["Phone"].ToString(),
                            M3 = rdr["M3"].ToString(),
                            Amount = rdr.GetDecimal(rdr.GetOrdinal("Amount")),
                            OrderNumber = rdr.GetInt32(rdr.GetOrdinal("OrderNumber")),
                            DateOrdered = rdr.GetDateTime(rdr.GetOrdinal("DateOrdered")),
                            LeadTime = rdr["LeadTime"].ToString(),

                            ProdInput = rdr["ProdInput"].ToString(),
                            MaterialsOrdered = rdr["MaterialsOrdered"].ToString(),
                            ReleasedToFactory = rdr["ReleasedToFactory"].ToString(),

                            FB = rdr.GetBoolean(rdr.GetOrdinal("FB")),
                            EB = rdr.GetBoolean(rdr.GetOrdinal("EB")),
                            AS = rdr.GetBoolean(rdr.GetOrdinal("AS"))
                        };
                        list.Add(rec);
                    }
                }
            }
            return list;
        }

        public static void UpdateProgressInitials(int jobNo, string column, string initials)
        {
            string sql = $"UPDATE Dispatch SET {column} = @val WHERE JobNo = @jobNo";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@val", initials);
                cmd.Parameters.AddWithValue("@jobNo", jobNo);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateTickValue(int jobNo, string column, bool value)
        {
            string sql = $"UPDATE Dispatch SET {column} = @val WHERE JobNo = @jobNo";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@val", value);
                cmd.Parameters.AddWithValue("@jobNo", jobNo);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
