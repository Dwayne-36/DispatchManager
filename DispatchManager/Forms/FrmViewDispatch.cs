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
        private DataGridViewCell lastDoubleClickedCell = null;


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

            
            dgvSchedule.CellBeginEdit += dgvSchedule_CellBeginEdit;
            



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
            // Store the last double-clicked cell for potential future use
            lastDoubleClickedCell = dgvSchedule.Rows[e.RowIndex].Cells[e.ColumnIndex];


            if (row.DataBoundItem is DispatchRecord record)
            {
                string columnName = column.Name;
                string initials = Session.CurrentInitials;
                string oldValue = cell.Value?.ToString();
                Color currentColor = cell.Style.BackColor;

                string colorColumn = null;
                if (columnName == "ProdInput") colorColumn = "ProdInputColor";
                else if (columnName == "MaterialsOrdered") colorColumn = "MaterialsOrderedColor";
                else if (columnName == "ReleasedToFactory") colorColumn = "ReleasedToFactoryColor";
                else if (columnName == "MainContractor") colorColumn = "MainContractorColor";
                else if (columnName == "Freight") colorColumn = "FreightColor";
                else if (columnName == "Amount") colorColumn = "AmountColor";

                Color red = Color.Red;
                Color green = Color.FromArgb(146, 208, 80);
                Color white = Color.White;
                Color orange = Color.FromArgb(255, 140, 0);  // For ReleasedToFactory & MainContractor
                Color purple = Color.FromArgb(153, 0, 204);  // For Amount

                if (columnName == "ReleasedToFactory")
                {
                    if (string.IsNullOrWhiteSpace(oldValue))
                    {
                        cell.Value = "REL";
                        cell.Style.BackColor = white;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{white.R},{white.G},{white.B}");
                    }
                    else if (oldValue == "REL" && currentColor.ToArgb() == white.ToArgb())
                    {
                        cell.Style.BackColor = orange;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{orange.R},{orange.G},{orange.B}");
                    }
                    else
                    {
                        cell.Value = "";
                        cell.Style.BackColor = white;
                        SaveCellColorToDatabase(record.ID, colorColumn, "White");
                    }

                    cell.Style.ForeColor = Color.Black;
                    cell.Style.SelectionBackColor = cell.Style.BackColor;
                    cell.Style.SelectionForeColor = Color.Black;
                    SaveInitialsToDispatch(record.ID, columnName, cell.Value?.ToString());
                }
                else if (columnName == "MainContractor")
                {
                    if (currentColor.ToArgb() == white.ToArgb())
                    {
                        cell.Style.BackColor = orange;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{orange.R},{orange.G},{orange.B}");
                    }
                    else if (currentColor.ToArgb() == orange.ToArgb())
                    {
                        cell.Style.BackColor = green;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{green.R},{green.G},{green.B}");
                    }
                    else
                    {
                        cell.Style.BackColor = white;
                        SaveCellColorToDatabase(record.ID, colorColumn, "White");
                    }

                    cell.Style.ForeColor = Color.Black;
                    cell.Style.SelectionBackColor = cell.Style.BackColor;
                    cell.Style.SelectionForeColor = Color.Black;
                    // No initials change
                }
                else if (columnName == "Freight")
                {
                    if (currentColor.ToArgb() == white.ToArgb())
                    {
                        cell.Style.BackColor = orange;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{orange.R},{orange.G},{orange.B}");
                    }
                    else
                    {
                        cell.Style.BackColor = white;
                        SaveCellColorToDatabase(record.ID, colorColumn, "White");
                    }

                    cell.Style.ForeColor = Color.Black;
                    cell.Style.SelectionBackColor = cell.Style.BackColor;
                    cell.Style.SelectionForeColor = Color.Black;
                }
                else if (columnName == "Amount")
                {
                    if (currentColor.ToArgb() == white.ToArgb())
                    {
                        cell.Style.BackColor = purple;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{purple.R},{purple.G},{purple.B}");
                    }
                    else
                    {
                        cell.Style.BackColor = white;
                        SaveCellColorToDatabase(record.ID, colorColumn, "White");
                    }

                    cell.Style.ForeColor = Color.Black;
                    cell.Style.SelectionBackColor = cell.Style.BackColor;
                    cell.Style.SelectionForeColor = Color.Black;
                    // ✅ Leave cell.Value alone to keep it user-editable
                }

                else
                {
                    // Steps 1–2 logic (ProdInput, MaterialsOrdered)
                    if (string.IsNullOrWhiteSpace(oldValue))
                    {
                        cell.Value = initials;
                        cell.Style.BackColor = red;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{red.R},{red.G},{red.B}");
                    }
                    else if (oldValue == initials)
                    {
                        if (currentColor.ToArgb() == red.ToArgb())
                        {
                            cell.Style.BackColor = green;
                            SaveCellColorToDatabase(record.ID, colorColumn, $"{green.R},{green.G},{green.B}");
                        }
                        else
                        {
                            cell.Value = "";
                            cell.Style.BackColor = white;
                            SaveCellColorToDatabase(record.ID, colorColumn, "White");
                        }
                    }
                    else
                    {
                        if (currentColor.ToArgb() == red.ToArgb())
                        {
                            cell.Value = initials;
                            cell.Style.BackColor = green;
                            SaveCellColorToDatabase(record.ID, colorColumn, $"{green.R},{green.G},{green.B}");
                        }
                        else
                        {
                            cell.Value = "";
                            cell.Style.BackColor = white;
                            SaveCellColorToDatabase(record.ID, colorColumn, "White");
                        }
                    }

                    cell.Style.ForeColor = Color.Black;
                    cell.Style.SelectionBackColor = cell.Style.BackColor;
                    cell.Style.SelectionForeColor = Color.Black;
                    SaveInitialsToDispatch(record.ID, columnName, cell.Value?.ToString());
                }

                // ✅ Update in-memory record color
                string colorString = cell.Style.BackColor == white ? null : $"{cell.Style.BackColor.R},{cell.Style.BackColor.G},{cell.Style.BackColor.B}";
                if (colorColumn == "ProdInputColor") record.ProdInputColor = colorString;
                else if (colorColumn == "MaterialsOrderedColor") record.MaterialsOrderedColor = colorString;
                else if (colorColumn == "ReleasedToFactoryColor") record.ReleasedToFactoryColor = colorString;
                else if (colorColumn == "MainContractorColor") record.MainContractorColor = colorString;
                 else if (colorColumn == "FreightColor") record.FreightColor = colorString;
                else if (colorColumn == "AmountColor") record.AmountColor = colorString;

                this.BeginInvoke((MethodInvoker)(() => dgvSchedule.InvalidateCell(cell)));
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
        //        Color currentColor = cell.Style.BackColor;

        //        string colorColumn = null;
        //        if (columnName == "ProdInput") colorColumn = "ProdInputColor";
        //        else if (columnName == "MaterialsOrdered") colorColumn = "MaterialsOrderedColor";
        //        else if (columnName == "ReleasedToFactory") colorColumn = "ReleasedToFactoryColor";
        //        else if (columnName == "MainContractor") colorColumn = "MainContractorColor";
        //        else if (columnName == "ProjectName") colorColumn = "ProjectNameColor";
        //        else if (columnName == "Freight") colorColumn = "FreightColor";
        //        else if (columnName == "Amount") colorColumn = "AmountColor";

        //        Color red = Color.Red;
        //        Color green = Color.FromArgb(146, 208, 80);
        //        Color white = Color.White;
        //        Color orange = Color.FromArgb(255, 140, 0); // Step 4 orange

        //        // 🔸 Special logic for ReleasedToFactory
        //        if (columnName == "ReleasedToFactory")
        //        {
        //            if (string.IsNullOrWhiteSpace(oldValue))
        //            {
        //                cell.Value = "REL";
        //                cell.Style.BackColor = white;
        //                SaveCellColorToDatabase(record.ID, colorColumn, $"{white.R},{white.G},{white.B}");
        //            }
        //            else if (oldValue == "REL" && currentColor.ToArgb() == white.ToArgb())
        //            {
        //                cell.Style.BackColor = orange;
        //                SaveCellColorToDatabase(record.ID, colorColumn, $"{orange.R},{orange.G},{orange.B}");
        //            }
        //            else
        //            {
        //                cell.Value = "";
        //                cell.Style.BackColor = white;
        //                SaveCellColorToDatabase(record.ID, colorColumn, "White");
        //            }

        //            cell.Style.ForeColor = Color.Black;
        //            cell.Style.SelectionBackColor = cell.Style.BackColor;
        //            cell.Style.SelectionForeColor = Color.Black;
        //            SaveInitialsToDispatch(record.ID, columnName, cell.Value?.ToString());
        //        }
        //        // 🔸 Special logic for MainContractor (Step 4)
        //        else if (columnName == "MainContractor")
        //        {
        //            if (currentColor.ToArgb() == white.ToArgb())
        //            {
        //                cell.Style.BackColor = orange;
        //                SaveCellColorToDatabase(record.ID, colorColumn, $"{orange.R},{orange.G},{orange.B}");
        //            }
        //            else if (currentColor.ToArgb() == orange.ToArgb())
        //            {
        //                cell.Style.BackColor = green;
        //                SaveCellColorToDatabase(record.ID, colorColumn, $"{green.R},{green.G},{green.B}");
        //            }
        //            else
        //            {
        //                cell.Style.BackColor = white;
        //                SaveCellColorToDatabase(record.ID, colorColumn, "White");
        //            }

        //            cell.Style.ForeColor = Color.Black;
        //            cell.Style.SelectionBackColor = cell.Style.BackColor;
        //            cell.Style.SelectionForeColor = Color.Black;
        //            // No initials change
        //        }
        //        else
        //        {
        //            // 🔁 Normal 3-state logic (ProdInput, MaterialsOrdered, etc.)
        //            if (string.IsNullOrWhiteSpace(oldValue))
        //            {
        //                cell.Value = initials;
        //                cell.Style.BackColor = red;
        //                SaveCellColorToDatabase(record.ID, colorColumn, $"{red.R},{red.G},{red.B}");
        //            }
        //            else if (oldValue == initials)
        //            {
        //                if (currentColor.ToArgb() == red.ToArgb())
        //                {
        //                    cell.Style.BackColor = green;
        //                    SaveCellColorToDatabase(record.ID, colorColumn, $"{green.R},{green.G},{green.B}");
        //                }
        //                else
        //                {
        //                    cell.Value = "";
        //                    cell.Style.BackColor = white;
        //                    SaveCellColorToDatabase(record.ID, colorColumn, "White");
        //                }
        //            }
        //            else
        //            {
        //                if (currentColor.ToArgb() == red.ToArgb())
        //                {
        //                    cell.Value = initials;
        //                    cell.Style.BackColor = green;
        //                    SaveCellColorToDatabase(record.ID, colorColumn, $"{green.R},{green.G},{green.B}");
        //                }
        //                else
        //                {
        //                    cell.Value = "";
        //                    cell.Style.BackColor = white;
        //                    SaveCellColorToDatabase(record.ID, colorColumn, "White");
        //                }
        //            }

        //            cell.Style.ForeColor = Color.Black;
        //            cell.Style.SelectionBackColor = cell.Style.BackColor;
        //            cell.Style.SelectionForeColor = Color.Black;
        //            SaveInitialsToDispatch(record.ID, columnName, cell.Value?.ToString());
        //        }

        //        // ✅ Update in-memory record color
        //        string colorString = cell.Style.BackColor == white ? null : $"{cell.Style.BackColor.R},{cell.Style.BackColor.G},{cell.Style.BackColor.B}";
        //        if (colorColumn == "ProdInputColor") record.ProdInputColor = colorString;
        //        else if (colorColumn == "MaterialsOrderedColor") record.MaterialsOrderedColor = colorString;
        //        else if (colorColumn == "ReleasedToFactoryColor") record.ReleasedToFactoryColor = colorString;
        //        else if (colorColumn == "MainContractorColor") record.MainContractorColor = colorString;
        //        else if (colorColumn == "ProjectNameColor") record.ProjectNameColor = colorString;
        //        else if (colorColumn == "FreightColor") record.FreightColor = colorString;
        //        else if (colorColumn == "AmountColor") record.AmountColor = colorString;

        //        this.BeginInvoke((MethodInvoker)(() => dgvSchedule.InvalidateCell(cell)));
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

                if (dispatchColors.TryGetValue(record.ID, out var cellColours))
                {
                    record.ProdInputColor = cellColours.TryGetValue("ProdInputColor", out string v1) ? v1 : null;
                    record.MaterialsOrderedColor = cellColours.TryGetValue("MaterialsOrderedColor", out string v2) ? v2 : null;
                    record.ReleasedToFactoryColor = cellColours.TryGetValue("ReleasedToFactoryColor", out string v3) ? v3 : null;
                    record.MainContractorColor = cellColours.TryGetValue("MainContractorColor", out string v4) ? v4 : null;
                    record.FreightColor = cellColours.TryGetValue("FreightColor", out string v6) ? v6 : null;
                    record.AmountColor = cellColours.TryGetValue("AmountColor", out string v7) ? v7 : null;
                }

                withWeeklyTotals.Add(record);
                weeklyTotal += record.Qty;
                currentWeek = recordWeek;

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

            Color[] palette = new Color[]
            {
        Color.FromArgb(0, 255, 255),
        Color.FromArgb(255, 128, 128),
        Color.FromArgb(0, 255, 0),
        Color.FromArgb(255, 255, 0),
        Color.FromArgb(255, 0, 255)
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
                    row.Cells[qtyColumnIndex].Style.BackColor = Color.FromArgb(255, 204, 0);
                }
            }

            // ✅ Disable editing for toggleable double-click cells
            string[] toggleColumns = { "ProdInput", "MaterialsOrdered", "ReleasedToFactory" };
            foreach (string colName in toggleColumns)
            {
                if (dgvSchedule.Columns.Contains(colName))
                {
                    dgvSchedule.Columns[colName].ReadOnly = true;
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

            // ✅ Blank total rows
            if (row.DataBoundItem is DispatchBlankRow)
            {
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

                // Grey style
                row.Cells[e.ColumnIndex].Style.BackColor = Color.LightGray;
                row.Cells[e.ColumnIndex].Style.SelectionBackColor = Color.LightGray;
                row.Cells[e.ColumnIndex].Style.ForeColor = Color.DarkSlateGray;
                row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.DarkSlateGray;

                return;
            }

            // ✅ Apply week color
            if (columnName == "WeekNo" || columnName == "DispatchDate" || columnName == "Day" || columnName == "JobNo")
            {
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

            // ✅ Apply saved cell background colors
            if (row.DataBoundItem is DispatchRecord rec)
            {
                string colorValue = null;

                if (columnName == "ProdInput")
                    colorValue = rec.ProdInputColor;
                else if (columnName == "MaterialsOrdered")
                    colorValue = rec.MaterialsOrderedColor;
                else if (columnName == "ReleasedToFactory")
                    colorValue = rec.ReleasedToFactoryColor;
                else if (columnName == "MainContractor")
                    colorValue = rec.MainContractorColor;
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

            // ✅ Force refresh to apply changes immediately
            dgvSchedule.InvalidateCell(row.Cells[e.ColumnIndex]);
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

        //        if (columnName == "DispatchDate" && e.Value is DateTime dtValue && dtValue != DateTime.MinValue)
        //        {
        //            e.Value = dtValue.ToString("dd-MMM");
        //            e.FormattingApplied = true;
        //        }

        //        var weekObj = row.Cells["WeekNo"].Value;
        //        if (weekObj is int week && weekColors.TryGetValue(week, out var color))
        //        {
        //            row.Cells[e.ColumnIndex].Style.BackColor = color;
        //            row.Cells[e.ColumnIndex].Style.SelectionBackColor = color;
        //            row.Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
        //            row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.Black;
        //        }
        //    }

        //    // ✅ Apply saved cell colors using record property values (without switch)
        //    if (row.DataBoundItem is DispatchRecord rec)
        //    {
        //        string colorValue = null;

        //        if (columnName == "ProdInput")
        //            colorValue = rec.ProdInputColor;
        //        else if (columnName == "MaterialsOrdered")
        //            colorValue = rec.MaterialsOrderedColor;
        //        else if (columnName == "ReleasedToFactory")
        //            colorValue = rec.ReleasedToFactoryColor;
        //        else if (columnName == "MainContractor")
        //            colorValue = rec.MainContractorColor;
        //        else if (columnName == "ProjectName")
        //            colorValue = rec.ProjectNameColor;
        //        else if (columnName == "Freight")
        //            colorValue = rec.FreightColor;
        //        else if (columnName == "Amount")
        //            colorValue = rec.AmountColor;

        //        if (!string.IsNullOrWhiteSpace(colorValue))
        //        {
        //            string[] rgb = colorValue.Split(',');
        //            if (rgb.Length == 3 &&
        //                int.TryParse(rgb[0], out int r) &&
        //                int.TryParse(rgb[1], out int g) &&
        //                int.TryParse(rgb[2], out int b))
        //            {
        //                Color dbColor = Color.FromArgb(r, g, b);
        //                row.Cells[e.ColumnIndex].Style.BackColor = dbColor;
        //                row.Cells[e.ColumnIndex].Style.SelectionBackColor = dbColor;
        //            }
        //        }
        //    }
        //}

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
        //private void dgvSchedule_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        //{
        //    var columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

        //    // Cancel edit mode for the custom-handled columns
        //    if (columnName == "ProdInput" || columnName == "MaterialsOrdered" || columnName == "ReleasedToFactory" || columnName == "MainContractor" || columnName == "Amount" || columnName == "Freight")
        //    {
        //        e.Cancel = true;
        //    }
        //}
        private void dgvSchedule_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var cell = dgvSchedule.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

            bool isCustomColumn = columnName == "ProdInput"
                               || columnName == "MaterialsOrdered"
                               || columnName == "ReleasedToFactory"
                               || columnName == "MainContractor";

            bool isColorClickOnlyColumn = columnName == "Freight"
                                       || columnName == "Amount";

            if (isCustomColumn)
            {
                // Fully block editing
                e.Cancel = true;
            }
            else if (isColorClickOnlyColumn)
            {
                // Block editing only on the double-click
                if (lastDoubleClickedCell == cell)
                {
                    e.Cancel = true;
                    lastDoubleClickedCell = null; // reset after blocking
                }
            }
        }




    }
}

