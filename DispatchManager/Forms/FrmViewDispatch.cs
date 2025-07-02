using DispatchManager;
using DispatchManager.DataAccess;
using DispatchManager.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;



namespace DispatchManager.Forms
{
    public partial class FrmViewDispatch : Form
    {
        private List<DispatchRecord> fullDispatchList = new List<DispatchRecord>();
        private Dictionary<Guid, Dictionary<string, string>> dispatchColors;

        public FrmViewDispatch()
        {
            InitializeComponent();
            this.Load += FrmViewDispatch_Load;
            this.FormClosing += FrmViewDispatch_FormClosing;
            this.dtpFrom.ValueChanged += dtpFrom_ValueChanged;
            this.dtpTo.ValueChanged += dtpTo_ValueChanged;
            this.btnSearch.Click += btnSearch_Click;
            this.dgvSchedule.CellDoubleClick += dgvSchedule_CellDoubleClick;
        }
        
        private void FrmViewDispatch_Load(object sender, EventArgs e)
        {
            // Load saved date settings
            dtpFrom.Value = Properties.Settings.Default.dtpFromDate;
            dtpTo.Value = Properties.Settings.Default.dtpToDate;

            lblLoggedInUser.Text = $"{Session.CurrentFullName} is logged in";

            dgvSchedule.CellFormatting += dgvSchedule_CellFormatting;
            //dgvSchedule.DataError += (s, ev) => { ev.ThrowException = false; };

            // Load and store colors from database
            dispatchColors = DispatchData.GetDispatchColours();

            // Load data and populate DataGridView
            LoadScheduleData();

            // Disable tooltips (helps reduce flicker)
            dgvSchedule.ShowCellToolTips = false;

            // Optional: Apply double buffering to reduce flicker
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, dgvSchedule, new object[] { true });

        }
        private Dictionary<int, Color> weekColors = new Dictionary<int, Color>();

        private void dgvSchedule_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = dgvSchedule.Rows[e.RowIndex];
            var column = dgvSchedule.Columns[e.ColumnIndex];
            var cell = row.Cells[e.ColumnIndex];

            if (row.DataBoundItem is DispatchRecord record)
            {
                string columnName = column.Name;
                string initials = Session.CurrentInitials;
                string oldValue = cell.Value?.ToString();

                // ✅ Map to matching color column name in DispatchColours table
                string colorColumn = null;
                if (columnName == "ProdInput") colorColumn = "ProdInputColor";
                else if (columnName == "MaterialsOrdered") colorColumn = "MaterialsOrderedColour";
                else if (columnName == "ReleasedtoFactory") colorColumn = "RelesedtoFactoryColour";
                else if (columnName == "MainContractor") colorColumn = "MainContractorColor";
                else if (columnName == "ProjectName") colorColumn = "ProjectNameColour";
                else if (columnName == "Freight") colorColumn = "FreightColor";
                else if (columnName == "Amount") colorColumn = "AmountColor";

                if (oldValue == initials)
                {
                    // ✅ Clear initials and color
                    cell.Value = "";
                    cell.Style.BackColor = Color.White;

                    if (colorColumn != null)
                    {
                        SaveCellColorToDatabase(record.ID, colorColumn, "White");

                        // ✅ Clear from in-memory object
                        if (colorColumn == "ProdInputColor") record.ProdInputColor = null;
                        else if (colorColumn == "MaterialsOrderedColour") record.MaterialsOrderedColor = null;
                        else if (colorColumn == "RelesedtoFactoryColour") record.ReleasedtoFactoryColor = null;
                        else if (colorColumn == "MainContractorColor") record.MainContractorColor = null;
                        else if (colorColumn == "ProjectNameColour") record.ProjectNameColor = null;
                        else if (colorColumn == "FreightColor") record.FreightColor = null;
                        else if (colorColumn == "AmountColor") record.AmountColor = null;
                    }
                }
                else
                {
                    // ✅ Set initials and cycle color
                    cell.Value = initials;
                    Color newColor = oldValue == "" ? Color.Red : Color.FromArgb(146, 208, 80);
                    cell.Style.BackColor = newColor;

                    if (colorColumn != null)
                    {
                        string colorString = $"{newColor.R},{newColor.G},{newColor.B}";
                        SaveCellColorToDatabase(record.ID, colorColumn, colorString);

                        // ✅ Update in-memory object with new color
                        if (colorColumn == "ProdInputColor") record.ProdInputColor = colorString;
                        else if (colorColumn == "MaterialsOrderedColour") record.MaterialsOrderedColor = colorString;
                        else if (colorColumn == "RelesedtoFactoryColour") record.ReleasedtoFactoryColor = colorString;
                        else if (colorColumn == "MainContractorColor") record.MainContractorColor = colorString;
                        else if (colorColumn == "ProjectNameColour") record.ProjectNameColor = colorString;
                        else if (colorColumn == "FreightColor") record.FreightColor = colorString;
                        else if (colorColumn == "AmountColor") record.AmountColor = colorString;
                    }
                }

                // ✅ Always update initials in Dispatch table
                SaveInitialsToDispatch(record.ID, columnName, cell.Value?.ToString());
            }
        }


        //private void dgvSchedule_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

        //    var row = dgvSchedule.Rows[e.RowIndex];
        //    var column = dgvSchedule.Columns[e.ColumnIndex];
        //    var cell = row.Cells[e.ColumnIndex];

        //    if (row.DataBoundItem is DispatchRecord record)
        //    {
        //        string columnName = column.Name;
        //        string initials = Session.CurrentInitials;
        //        string oldValue = cell.Value?.ToString();

        //        // ✅ Map to matching color column name in DispatchColours table
        //        string colorColumn = null;
        //        if (columnName == "ProdInput") colorColumn = "ProdInputColor";
        //        else if (columnName == "MaterialsOrdered") colorColumn = "MaterialsOrderedColour";
        //        else if (columnName == "ReleasedtoFactory") colorColumn = "RelesedtoFactoryColour";
        //        else if (columnName == "MainContractor") colorColumn = "MainContractorColor";
        //        else if (columnName == "ProjectName") colorColumn = "ProjectNameColour";
        //        else if (columnName == "Freight") colorColumn = "FreightColor";
        //        else if (columnName == "Amount") colorColumn = "AmountColor";


        //        // Toggle initials and background color
        //        if (oldValue == initials)
        //        {
        //            cell.Value = "";
        //            cell.Style.BackColor = Color.White;

        //            // Save white color only if it's a valid column
        //            if (colorColumn != null)
        //                SaveCellColorToDatabase(record.ID, colorColumn, "White");
        //        }
        //        else
        //        {
        //            cell.Value = initials;

        //            // 🔁 First click: Red, second click: Green
        //            Color newColor = oldValue == "" ? Color.Red : Color.FromArgb(146, 208, 80);
        //            cell.Style.BackColor = newColor;

        //            if (colorColumn != null)
        //                SaveCellColorToDatabase(record.ID, colorColumn, $"{newColor.R},{newColor.G},{newColor.B}");
        //        }

        //        // ✅ Always update initials in Dispatch table
        //        SaveInitialsToDispatch(record.ID, columnName, cell.Value?.ToString());
        //    }
        //}


        private void SaveCellColorToDatabase(Guid linkId, string columnName, string colorValue)
        {
            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Try to UPDATE first
                string updateSql = $@"UPDATE DispatchColours SET {columnName} = @ColorValue WHERE LinkID = @LinkID";
                using (SqlCommand updateCmd = new SqlCommand(updateSql, conn))
                {
                    updateCmd.Parameters.AddWithValue("@ColorValue", colorValue);
                    updateCmd.Parameters.AddWithValue("@LinkID", linkId);

                    int rowsAffected = updateCmd.ExecuteNonQuery();

                    // If nothing updated, INSERT instead
                    if (rowsAffected == 0)
                    {
                        string insertSql = $@"INSERT INTO DispatchColours (LinkID, {columnName}) VALUES (@LinkID, @ColorValue)";
                        using (SqlCommand insertCmd = new SqlCommand(insertSql, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@LinkID", linkId);
                            insertCmd.Parameters.AddWithValue("@ColorValue", colorValue);
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private void SaveInitialsToDispatch(Guid recordId, string columnName, string initials)
        {
            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Update only if the column exists in Dispatch
                string updateSql = $@"UPDATE Dispatch SET {columnName} = @Initials WHERE ID = @ID";

                using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Initials", string.IsNullOrEmpty(initials) ? (object)DBNull.Value : initials);
                    cmd.Parameters.AddWithValue("@ID", recordId);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        //private void dgvSchedule_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

        //    DataGridViewCell cell = dgvSchedule.Rows[e.RowIndex].Cells[e.ColumnIndex];
        //    string columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

        //    if (columnName == "ProdInput") // Change this later to support more columns
        //    {
        //        var row = dgvSchedule.Rows[e.RowIndex];
        //        if (row.DataBoundItem is DispatchRecord record)
        //        {
        //            string currentValue = cell.Value?.ToString();
        //            Color currentColor = cell.Style.BackColor;

        //            if (!string.IsNullOrEmpty(currentValue) && currentValue != Session.CurrentInitials)
        //            {
        //                // Step 1: Clear unrelated text and reset color
        //                cell.Value = null;
        //                cell.Style.BackColor = dgvSchedule.DefaultCellStyle.BackColor;
        //            }
        //            else if (string.IsNullOrEmpty(currentValue))
        //            {
        //                // Step 2: Add initials, mark as red
        //                cell.Value = Session.CurrentInitials;
        //                cell.Style.BackColor = Color.FromArgb(255, 0, 0);
        //            }
        //            else if (currentValue == Session.CurrentInitials && currentColor == Color.FromArgb(255, 0, 0))
        //            {
        //                // Step 3: Confirm with green
        //                cell.Style.BackColor = Color.FromArgb(146, 208, 80);
        //            }
        //            else if (currentValue == Session.CurrentInitials && currentColor == Color.FromArgb(146, 208, 80))
        //            {
        //                // Step 4: Undo everything
        //                cell.Value = null;
        //                cell.Style.BackColor = dgvSchedule.DefaultCellStyle.BackColor;
        //            }
        //        }
        //    }
        //}
        private void LoadScheduleData()
{
    DateTime from = dtpFrom.Value;
    DateTime to = dtpTo.Value;

    fullDispatchList = DispatchData.GetDispatchByDateRange(from, to);
            dispatchColors = DispatchData.GetDispatchColours();


            var groupedList = fullDispatchList
        .OrderBy(d => d.DispatchDate)
        .ThenBy(d => d.JobNo)
        .ToList();

    var withWeeklyTotals = new List<DispatchRecord>();

    int? currentWeek = null;
    int weeklyTotal = 0;

    for (int i = 0; i < groupedList.Count; i++)
    {
        var record = groupedList[i];

        int recordWeek = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            record.DispatchDate,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);

        // Insert total before new week starts
        if (currentWeek != null && recordWeek != currentWeek)
        {
            withWeeklyTotals.Add(new DispatchBlankRow
            {
                Qty = weeklyTotal,
                BoardETA = $"Total for Week {currentWeek}",
                DispatchDate = DateTime.MinValue,
            });
            weeklyTotal = 0;
        }

        // ✅ Apply colors from DispatchColours table if present
        if (dispatchColors.TryGetValue(record.ID, out var cellColours))
        {
            record.ProdInputColor = cellColours.TryGetValue("ProdInputColor", out string v1) ? v1 : null;
            record.MaterialsOrderedColor = cellColours.TryGetValue("MaterialsOrderedColor", out string v2) ? v2 : null;
            record.ReleasedtoFactoryColor = cellColours.TryGetValue("ReleasedtoFactoryColor", out string v3) ? v3 : null;
            record.MainContractorColor = cellColours.TryGetValue("MainContractorColor", out string v4) ? v4 : null;
            record.ProjectNameColor = cellColours.TryGetValue("ProjectNameColor", out string v5) ? v5 : null;
            record.FreightColor = cellColours.TryGetValue("FreightColor", out string v6) ? v6 : null;
            record.AmountColor = cellColours.TryGetValue("AmountColor", out string v7) ? v7 : null;
        }

        withWeeklyTotals.Add(record);
        weeklyTotal += record.Qty;
        currentWeek = recordWeek;

        // Handle final record
        bool isLast = (i == groupedList.Count - 1);
        if (isLast && weeklyTotal > 0)
        {
            withWeeklyTotals.Add(new DispatchBlankRow
            {
                Qty = weeklyTotal,
                BoardETA = $"Total for Week {currentWeek}",
                WeekNo = -1,
                DispatchDate = DateTime.MinValue,
                JobNo = -1,
                Amount = -1,
                OrderNumber = -1
            });
        }
    }

    // ✅ STEP 2: Assign colors to each week
    Color[] palette = new Color[]
    {
        Color.FromArgb(0, 255, 255),   // Cyan
        Color.FromArgb(255, 128, 128), // Light Red
        Color.FromArgb(0, 255, 0),     // Green
        Color.FromArgb(255, 255, 0),   // Yellow
        Color.FromArgb(255, 0, 255)    // Magenta
    };

    weekColors.Clear();
    int colorIndex = 0;
    foreach (var record in withWeeklyTotals)
    {
        if (record is DispatchBlankRow) continue;

        int week = record.WeekNo;
        if (!weekColors.ContainsKey(week))
        {
            weekColors[week] = palette[colorIndex % palette.Length];
            colorIndex++;
        }
    }

    dgvSchedule.DataSource = null;
    dgvSchedule.Rows.Clear();
    dgvSchedule.Columns.Clear();

    dgvSchedule.DataSource = withWeeklyTotals;
    dgvSchedule.Refresh();

    dgvSchedule.Columns["ID"].Visible = false;
    dgvSchedule.Columns["MaterialsOrderedBy"].Visible = false;
    dgvSchedule.Columns["BenchtopOrderedBy"].Visible = false;

    dgvSchedule.SelectionChanged += dgvSchedule_SelectionChanged;

    foreach (DataGridViewColumn column in dgvSchedule.Columns)
    {
        column.SortMode = DataGridViewColumnSortMode.NotSortable;
    }

    foreach (DataGridViewRow row in dgvSchedule.Rows)
    {
        if (row.DataBoundItem is DispatchBlankRow)
        {
            row.DefaultCellStyle.Font = new Font(dgvSchedule.Font, FontStyle.Bold);
            row.DefaultCellStyle.ForeColor = Color.DarkSlateGray;

            int qtyColumnIndex = dgvSchedule.Columns["Qty"].Index;
            row.Cells[qtyColumnIndex].Style.BackColor = Color.FromArgb(255, 204, 0); // Gold
        }
    }

    UpdateTotalLabel(dgvSchedule.Rows.Cast<DataGridViewRow>());
}


        private void dgvSchedule_SelectionChanged(object sender, EventArgs e)
        {
            var selectedRows = dgvSchedule.SelectedRows.Cast<DataGridViewRow>()
                .Where(row => row.DataBoundItem is DispatchRecord && !(row.DataBoundItem is DispatchBlankRow));

            if (selectedRows.Count() > 1)
            {
                UpdateTotalLabel(selectedRows);
            }
            else
            {
                // Use all rows if 1 or 0 selected
                UpdateTotalLabel(dgvSchedule.Rows.Cast<DataGridViewRow>());
            }
        }
        private void UpdateTotalLabel(IEnumerable<DataGridViewRow> rows)
        {
            int totalQty = 0;

            foreach (var row in rows)
            {
                if (row.DataBoundItem is DispatchRecord record && !(record is DispatchBlankRow))
                {
                    totalQty += record.Qty;
                }
            }

            lblTotal.Text = $"Total Cabinets: {totalQty:N0}";
            
        }

        //Cell formatting to apply custom styles and blanking logic

        private void dgvSchedule_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var row = dgvSchedule.Rows[e.RowIndex];
            string columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

            // Only apply blanking logic to total rows
            if (row.DataBoundItem is DispatchBlankRow)
            {
                // Blank int or decimal values
                if ((columnName == "WeekNo" || columnName == "JobNo" || columnName == "OrderNumber" || columnName == "Qty" || columnName == "Amount")
                    && e.Value is int intVal && intVal <= 0)
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }

                if (columnName == "Amount" && e.Value is decimal decVal && decVal <= 0)
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }

                if (columnName == "DispatchDate" && e.Value is DateTime dt && dt == DateTime.MinValue)
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }
            }

            // ✅ Apply week color to "DispatchDate", "Day", and "JobNo"
            if (columnName == "WeekNo" || columnName == "DispatchDate" || columnName == "Day" || columnName == "JobNo")
            {
                if (row.DataBoundItem is DispatchBlankRow)
                {
                    row.Cells[e.ColumnIndex].Style.BackColor = Color.LightGray;
                    row.Cells[e.ColumnIndex].Style.SelectionBackColor = Color.LightGray;
                    row.Cells[e.ColumnIndex].Style.ForeColor = Color.DarkSlateGray;
                    row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.DarkSlateGray;
                    return;
                }

                if (columnName == "DispatchDate" && e.Value is DateTime dtValue && dtValue != DateTime.MinValue)
                {
                    e.Value = dtValue.ToString("dd-MMM");
                    e.FormattingApplied = true;
                }

                var weekObj = row.Cells["WeekNo"].Value;
                if (weekObj is int week && weekColors.TryGetValue(week, out var color))
                {
                    row.Cells[e.ColumnIndex].Style.BackColor = color;
                    row.Cells[e.ColumnIndex].Style.SelectionBackColor = color;
                    row.Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
                    row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.Black;
                }
            }

            // ✅ Apply saved cell colors using record property values (without switch)
            if (row.DataBoundItem is DispatchRecord rec)
            {
                string colorValue = null;

                if (columnName == "ProdInput")
                    colorValue = rec.ProdInputColor;
                else if (columnName == "MaterialsOrdered")
                    colorValue = rec.MaterialsOrderedColor;
                else if (columnName == "ReleasedtoFactory")
                    colorValue = rec.ReleasedtoFactoryColor;
                else if (columnName == "MainContractor")
                    colorValue = rec.MainContractorColor;
                else if (columnName == "ProjectName")
                    colorValue = rec.ProjectNameColor;
                else if (columnName == "Freight")
                    colorValue = rec.FreightColor;
                else if (columnName == "Amount")
                    colorValue = rec.AmountColor;

                if (!string.IsNullOrWhiteSpace(colorValue))
                {
                    string[] rgb = colorValue.Split(',');
                    if (rgb.Length == 3 &&
                        int.TryParse(rgb[0], out int r) &&
                        int.TryParse(rgb[1], out int g) &&
                        int.TryParse(rgb[2], out int b))
                    {
                        Color dbColor = Color.FromArgb(r, g, b);
                        row.Cells[e.ColumnIndex].Style.BackColor = dbColor;
                        row.Cells[e.ColumnIndex].Style.SelectionBackColor = dbColor;
                    }
                }
            }



        }

        private Color ParseColor(string input)
        {
            try
            {
                // Try HTML format first (e.g., #92D050)
                if (input.StartsWith("#"))
                    return ColorTranslator.FromHtml(input);

                // Try RGB format (e.g., "146,208,80")
                var parts = input.Split(',').Select(p => int.Parse(p.Trim())).ToArray();
                if (parts.Length == 3)
                    return Color.FromArgb(parts[0], parts[1], parts[2]);
            }
            catch
            {
                // Ignore malformed color
            }

            return Color.White; // Default fallback
        }

        //private void dgvSchedule_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        //{
        //    var row = dgvSchedule.Rows[e.RowIndex];
        //    string columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

        //    // Only apply blanking logic to total rows
        //    if (row.DataBoundItem is DispatchBlankRow)
        //    {
        //        // Blank int or decimal values
        //        if ((columnName == "WeekNo" || columnName == "JobNo" || columnName == "OrderNumber" || columnName == "Qty" || columnName == "Amount")
        //            && e.Value is int intVal && intVal <= 0)
        //        {
        //            e.Value = "";
        //            e.FormattingApplied = true;
        //        }

        //        if (columnName == "Amount" && e.Value is decimal decVal && decVal <= 0)
        //        {
        //            e.Value = "";
        //            e.FormattingApplied = true;
        //        }

        //        if (columnName == "DispatchDate" && e.Value is DateTime dt && dt == DateTime.MinValue)
        //        {
        //            e.Value = "";
        //            e.FormattingApplied = true;
        //        }
        //    }

        //    // ✅ Apply week color to "DispatchDate", "Day", and "JobNo"
        //    if (columnName == "WeekNo" || columnName == "DispatchDate" || columnName == "Day" || columnName == "JobNo")
        //    {
        //        if (row.DataBoundItem is DispatchBlankRow)
        //        {
        //            row.Cells[e.ColumnIndex].Style.BackColor = Color.LightGray;
        //            row.Cells[e.ColumnIndex].Style.SelectionBackColor = Color.LightGray;
        //            row.Cells[e.ColumnIndex].Style.ForeColor = Color.DarkSlateGray;
        //            row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.DarkSlateGray;
        //            return;
        //        }

        //        // ✅ Format DispatchDate (inline)
        //        if (columnName == "DispatchDate" && e.Value is DateTime dtValue && dtValue != DateTime.MinValue)
        //        {
        //            e.Value = dtValue.ToString("dd-MMM");
        //            e.FormattingApplied = true;
        //        }

        //        // ✅ Apply week-based color
        //        var weekObj = row.Cells["WeekNo"].Value;
        //        if (weekObj is int week && weekColors.TryGetValue(week, out var color))
        //        {
        //            row.Cells[e.ColumnIndex].Style.BackColor = color;
        //            row.Cells[e.ColumnIndex].Style.SelectionBackColor = color;
        //            row.Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
        //            row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.Black;
        //        }
        //    }
        //}

        private void FrmViewDispatch_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.dtpFromDate = dtpFrom.Value;
            Properties.Settings.Default.dtpToDate = dtpTo.Value;
            Properties.Settings.Default.Save();
        }
        private void dtpFrom_ValueChanged(object sender, EventArgs e)
        {
            LoadScheduleData();
        }

        private void dtpTo_ValueChanged(object sender, EventArgs e)
        {
            LoadScheduleData();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string keyword = tbSearch.Text.ToLower();

            var filteredList = fullDispatchList.Where(d =>
                d.JobNo.ToString().Contains(keyword) ||
                (d.ProjectName?.ToLower().Contains(keyword) ?? false) ||
                (d.MainContractor?.ToLower().Contains(keyword) ?? false) ||
                (d.ProjectColour?.ToLower().Contains(keyword) ?? false) ||
                (d.DispatchDate.ToShortDateString().ToLower().Contains(keyword)) ||
                (d.Comment?.ToLower().Contains(keyword) ?? false)
            ).ToList();

            dgvSchedule.DataSource = filteredList;
            dgvSchedule.Refresh();
        }

        private void menuEmployeesViewAll_Click(object sender, EventArgs e)
        {
            FrmEmployeesList frm = new FrmEmployeesList();
            frm.ShowDialog();
        }

       

       
    }
}

