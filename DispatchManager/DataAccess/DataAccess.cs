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

        public static void CopyDispatchColours(Guid originalId, Guid newId)
        {
            string selectQuery = "SELECT * FROM DispatchColours WHERE LinkID = @OriginalID";
            string insertQuery = @"
        INSERT INTO DispatchColours 
        (ID, LinkID, ProdInputColor, MaterialsOrderedColor, ReleasedToFactoryColor, 
         MainContractorColor, ProjectNameColor, FreightColor, AmountColor)
        VALUES
        (@ID, @LinkID, @ProdInputColor, @MaterialsOrderedColor, @ReleasedToFactoryColor, 
         @MainContractorColor, @ProjectNameColor, @FreightColor, @AmountColor)";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                object prodInputColor = DBNull.Value;
                object materialsOrderedColor = DBNull.Value;
                object releasedToFactoryColor = DBNull.Value;
                object mainContractorColor = DBNull.Value;
                object projectNameColor = DBNull.Value;
                object freightColor = DBNull.Value;
                object amountColor = DBNull.Value;

                // ✅ STEP 1: Read from old row
                using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@OriginalID", originalId);

                    using (SqlDataReader reader = selectCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            prodInputColor = reader["ProdInputColor"] ?? DBNull.Value;
                            materialsOrderedColor = reader["MaterialsOrderedColor"] ?? DBNull.Value;
                            releasedToFactoryColor = reader["ReleasedToFactoryColor"] ?? DBNull.Value;
                            mainContractorColor = reader["MainContractorColor"] ?? DBNull.Value;
                            projectNameColor = reader["ProjectNameColor"] ?? DBNull.Value;
                            freightColor = reader["FreightColor"] ?? DBNull.Value;
                            amountColor = reader["AmountColor"] ?? DBNull.Value;
                        }
                    }
                }

                // ✅ STEP 2: Insert new row with color values
                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@ID", Guid.NewGuid());
                    insertCmd.Parameters.AddWithValue("@LinkID", newId);
                    insertCmd.Parameters.AddWithValue("@ProdInputColor", prodInputColor);
                    insertCmd.Parameters.AddWithValue("@MaterialsOrderedColor", materialsOrderedColor);
                    insertCmd.Parameters.AddWithValue("@ReleasedToFactoryColor", releasedToFactoryColor);
                    insertCmd.Parameters.AddWithValue("@MainContractorColor", mainContractorColor);
                    insertCmd.Parameters.AddWithValue("@ProjectNameColor", projectNameColor);
                    insertCmd.Parameters.AddWithValue("@FreightColor", freightColor);
                    insertCmd.Parameters.AddWithValue("@AmountColor", amountColor);

                    insertCmd.ExecuteNonQuery();
                }
            }
        }





    }
}
