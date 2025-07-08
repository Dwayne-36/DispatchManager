using DispatchManager.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
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
                                LeadTime = SafeRead<string>(reader, "LeadTime"),
                                LinkId = SafeRead<string>(reader, "LinkId")

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

        public static bool UpdateDispatchField(Guid id, string columnName, object value)
        {
            // Only allow updates to these fields
            var allowedColumns = new HashSet<string> {
        "DispatchDate", "ProdInput", "MaterialsOrdered", "ReleasedToFactory", "ProjectName",
        "ProjectColour", "Qty", "FB", "EB", "ASS", "BoardETA", "Installed", "Freight",
        "BenchTopSupplier", "BenchTopColour", "Installer", "Comment", "DeliveryAddress",
        "Phone", "M3", "Amount", "OrderNumber"
    };

            if (!allowedColumns.Contains(columnName))
                return false;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string sql = $"UPDATE Dispatch SET {columnName} = @Value WHERE ID = @ID";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Value", value ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ID", id);

                    try
                    {
                        conn.Open();
                        return cmd.ExecuteNonQuery() == 1;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to update '{columnName}' for ID {id}.\n{ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
        }

        public static void DeleteById(Guid id)
        {
            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;
            string query = "DELETE FROM Dispatch WHERE ID = @ID";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void CopyDispatchColours(Guid fromId, Guid toId, string[] clearKeys = null)
        {
            string query = "SELECT * FROM DispatchColours WHERE LinkID = @FromID";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FromID", fromId);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var colorDict = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string col = reader.GetName(i);
                            if (col != "ID" && col != "LinkID")
                            {
                                object val = reader[col];

                                // Clear if in clearKeys
                                if (clearKeys != null && clearKeys.Contains(col))
                                    colorDict[col] = DBNull.Value;
                                else
                                    colorDict[col] = val == DBNull.Value ? DBNull.Value : val;
                            }
                        }

                        reader.Close(); // ✅ Must close reader before issuing another command

                        // Build insert command
                        List<string> columns = colorDict.Keys.ToList();
                        List<string> parameters = columns.Select(c => "@" + c).ToList();

                        string insertSql = $@"
                    INSERT INTO DispatchColours (LinkID, {string.Join(",", columns)})
                    VALUES (@ToID, {string.Join(",", parameters)})
                ";

                        using (SqlCommand insertCmd = new SqlCommand(insertSql, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@ToID", toId);
                            foreach (var col in columns)
                            {
                                insertCmd.Parameters.AddWithValue("@" + col, colorDict[col] ?? DBNull.Value);
                            }

                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }






    }
}
