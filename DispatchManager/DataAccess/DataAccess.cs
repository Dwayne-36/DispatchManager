using DispatchManager.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace DispatchManager.DataAccess
{
    public static class DispatchData
    {
        private static readonly string connStr =
            ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

        public static List<DispatchRecord> GetDispatchByDateRange(DateTime fromDate, DateTime toDate)
        {
            List<DispatchRecord> records = new List<DispatchRecord>();

            string query = @"
        SELECT * FROM Dispatch
        WHERE DispatchDate >= @FromDate AND DispatchDate <= @ToDate
        ORDER BY DispatchDate";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FromDate", fromDate);
                cmd.Parameters.AddWithValue("@ToDate", toDate);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            var record = new DispatchRecord
                            {
                                ID = SafeRead<Guid>(reader, "ID"),
                                WeekNo = SafeRead<int>(reader, "WeekNo"),
                                DispatchDate = SafeRead<DateTime>(reader, "DispatchDate"),
                                MaterialsOrderedBy = reader["MaterialsOrderedBy"] == DBNull.Value ? null : (DateTime?)reader["MaterialsOrderedBy"],
                                BenchtopOrderedBy = reader["BenchtopOrderedBy"] == DBNull.Value ? null : (DateTime?)reader["BenchtopOrderedBy"],
                                Day = SafeRead<string>(reader, "Day"),
                                JobNo = SafeRead<int>(reader, "JobNo"),
                                ProdInput = SafeRead<string>(reader, "ProdInput"),
                                MaterialsOrdered = SafeRead<string>(reader, "MaterialsOrdered"),
                                ReleasedToFactory = SafeRead<string>(reader, "ReleasedToFactory"),
                                MainContractor = SafeRead<string>(reader, "MainContractor"),
                                ProjectName = SafeRead<string>(reader, "ProjectName"),
                                ProjectColour = SafeRead<string>(reader, "ProjectColour"),
                                Qty = SafeRead<int>(reader, "Qty"),
                                FB = SafeRead<bool>(reader, "FB"),
                                EB = SafeRead<bool>(reader, "EB"),
                                ASS = SafeRead<bool>(reader, "ASS"),
                                BoardETA = SafeRead<string>(reader, "BoardETA"),
                                Installed = SafeRead<string>(reader, "Installed"),
                                Freight = SafeRead<string>(reader, "Freight"),
                                BenchTopSupplier = SafeRead<string>(reader, "BenchTopSupplier"),
                                BenchTopColour = SafeRead<string>(reader, "BenchTopColour"),
                                Installer = SafeRead<string>(reader, "Installer"),
                                Comment = SafeRead<string>(reader, "Comment"),
                                DeliveryAddress = SafeRead<string>(reader, "DeliveryAddress"),
                                Phone = SafeRead<string>(reader, "Phone"),
                                M3 = SafeRead<string>(reader, "M3"),
                                Amount = SafeRead<decimal>(reader, "Amount"),
                                OrderNumber = SafeRead<int>(reader, "OrderNumber"),
                                DateOrdered = SafeRead<DateTime>(reader, "DateOrdered"),
                                LeadTime = SafeRead<string>(reader, "LeadTime")
                            };

                            records.Add(record);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error reading a Dispatch record:\n{ex.Message}", "Data Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            return records;
        }

        public static Dictionary<Guid, Dictionary<string, string>> GetDispatchColours()
        {
            var dispatchColors = new Dictionary<Guid, Dictionary<string, string>>();

            string query = "SELECT * FROM DispatchColours";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Guid linkId = reader["LinkID"] != DBNull.Value ? (Guid)reader["LinkID"] : Guid.Empty;
                        if (!dispatchColors.ContainsKey(linkId))
                            dispatchColors[linkId] = new Dictionary<string, string>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);
                            if (columnName != "LinkID")
                            {
                                dispatchColors[linkId][columnName] = reader[columnName]?.ToString();
                            }
                        }
                    }
                }
            }

            return dispatchColors;
        }


        private static T SafeRead<T>(SqlDataReader reader, string column)
        {
            object value = reader[column];
            if (value == DBNull.Value)
                return default(T);
            return (T)Convert.ChangeType(value, typeof(T));
        }

    }
}
