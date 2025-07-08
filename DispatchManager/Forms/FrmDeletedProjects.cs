using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DispatchManager.Forms
{
    public partial class FrmDeletedProjects : Form
    {
        public FrmDeletedProjects()
        {
            InitializeComponent();
            LoadDeletedData();
        }
        private void LoadDeletedData()
        {
            try
            {
                string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string logPath = Path.Combine(documentsFolder, "DeletedRowsLog.csv");

                if (!File.Exists(logPath))
                {
                    MessageBox.Show("No deleted log file found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string[] lines = File.ReadAllLines(logPath);
                if (lines.Length == 0) return;

                var dt = new DataTable();

                // Parse headers
                string[] headers = ParseCsvLine(lines[0]);
                foreach (string header in headers)
                    dt.Columns.Add(header);

                // Parse rows
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] fields = ParseCsvLine(lines[i]);

                    if (fields.Length == dt.Columns.Count)
                        dt.Rows.Add(fields);

                }

                dgvDeleted.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load deleted projects.\n" + ex.Message);
            }
        }
        private string[] ParseCsvLine(string line)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;
            string value = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        value += '\"';
                        i++; // Skip second quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(value);
                    value = "";
                }
                else
                {
                    value += c;
                }
            }

            values.Add(value); // last value
            return values.ToArray();
        }
        private void dgvDeleted_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hitTest = dgvDeleted.HitTest(e.X, e.Y);
                if (hitTest.RowIndex >= 0)
                {
                    dgvDeleted.ClearSelection();
                    dgvDeleted.Rows[hitTest.RowIndex].Selected = true;
                }
            }
        }

        private void menuRevertProject_Click(object sender, EventArgs e)
        {
            if (dgvDeleted.SelectedRows.Count == 0)
                return;

            var selectedRow = dgvDeleted.SelectedRows[0];
            var result = MessageBox.Show("Revert this project to Dispatch table?", "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            try
            {
                // Dispatch table columns (in correct order)
                string[] dispatchColumns = new string[]
                {
            "WeekNo", "DispatchDate", "MaterialsOrderedBy", "BenchtopOrderedBy", "Day", "JobNo",
            "ProdInput", "MaterialsOrdered", "ReleasedtoFactory", "MainContractor", "ProjectName", "ProjectColour", "Qty",
            "FB", "EB", "ASS", "Installed", "Freight", "BenchTopSupplier", "BenchTopColour", "Installer",
            "Comment", "DeliveryAddress", "Phone", "M3", "Amount", "OrderNumber", "DateOrdered", "LeadTime", "ID", "LinkID"
                };

                var values = new List<string>();

                foreach (string column in dispatchColumns)
                {
                    string raw;

                    if (dgvDeleted.Columns.Contains(column))
                    {
                        raw = selectedRow.Cells[column].Value?.ToString().Trim() ?? "";
                    }
                    else
                    {
                        raw = "";
                    }

                    // Handle actual date columns
                    string[] dateColumns = { "DispatchDate", "MaterialsOrderedBy", "BenchtopOrderedBy", "DateOrdered" };

                    if (dateColumns.Contains(column))
                    {
                        if (DateTime.TryParse(raw, out DateTime parsedDate))
                        {
                            raw = parsedDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            raw = null;
                        }
                    }

                    // Handle Day as formatted version of DispatchDate
                    if (column == "Day")
                    {
                        string dispatchDateRaw = dgvDeleted.Columns.Contains("DispatchDate")
                            ? selectedRow.Cells["DispatchDate"].Value?.ToString().Trim() ?? ""
                            : "";

                        if (DateTime.TryParse(dispatchDateRaw, out DateTime dispatchDate))
                        {
                            raw = dispatchDate.ToString("ddd"); // gives Mon, Tue, etc.
                        }
                        else
                        {
                            raw = null;
                        }
                    }

                    // Handle ID
                    if (column == "ID" && !Guid.TryParse(raw, out _))
                    {
                        raw = Guid.NewGuid().ToString();
                    }

                    values.Add(raw != null ? $"'{raw}'" : "NULL");


                }

                string query = $"INSERT INTO Dispatch ({string.Join(",", dispatchColumns)}) VALUES ({string.Join(",", values)})";

                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                RemoveRowFromCsv(selectedRow);
                dgvDeleted.Rows.Remove(selectedRow);

                MessageBox.Show("Project successfully restored.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to restore project.\n" + ex.Message);
            }

            // Find and refresh the main form if open
            foreach (Form openForm in Application.OpenForms)
            {
                if (openForm is FrmViewDispatch mainForm)
                {
                    mainForm.ReloadSchedule(); // ✅ refresh the main grid
                    break;
                }
            }

        }



        private void RemoveRowFromCsv(DataGridViewRow rowToRemove)
        {
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string logPath = Path.Combine(documentsFolder, "DeletedRowsLog.csv");

            if (!File.Exists(logPath)) return;

            var lines = File.ReadAllLines(logPath).ToList();
            if (lines.Count < 2) return;

            string header = lines[0];
            lines.RemoveAt(0);

            string idToRemove = rowToRemove.Cells["ID"].Value.ToString();

            // Remove line with matching ID in first column (regardless of quotes)
            var filteredLines = lines.Where(line =>
            {
                string[] columns = line.Split(',');
                if (columns.Length == 0) return true;
                string firstCol = columns[0].Trim('"'); // Trim quotes just in case
                return firstCol != idToRemove;
            }).ToList();

            filteredLines.Insert(0, header);
            File.WriteAllLines(logPath, filteredLines);
        }



    }
}
