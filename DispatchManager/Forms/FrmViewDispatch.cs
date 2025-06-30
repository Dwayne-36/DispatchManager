using DispatchManager.DataAccess;
using DispatchManager.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;


namespace DispatchManager.Forms
{
    public partial class FrmViewDispatch : Form
    {
        private List<DispatchRecord> fullDispatchList = new List<DispatchRecord>();

        public FrmViewDispatch()
        {
            InitializeComponent();
            this.Load += FrmViewDispatch_Load;
            this.FormClosing += FrmViewDispatch_FormClosing;
            this.dtpFrom.ValueChanged += dtpFrom_ValueChanged;
            this.dtpTo.ValueChanged += dtpTo_ValueChanged;
            this.btnSearch.Click += btnSearch_Click;

        }
        private void FrmViewDispatch_Load(object sender, EventArgs e)
        {
            // Load saved date settings
            dtpFrom.Value = Properties.Settings.Default.dtpFromDate;
            dtpTo.Value = Properties.Settings.Default.dtpToDate;


            dgvSchedule.CellFormatting += dgvSchedule_CellFormatting;
            //dgvSchedule.DataError += (s, ev) => { ev.ThrowException = false; };

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


        private void LoadScheduleData()
        {
            DateTime from = dtpFrom.Value;
            DateTime to = dtpTo.Value;

            //DateTime from = new DateTime(2025, 4, 25);
            //DateTime to = new DateTime(2025, 6, 15);

            fullDispatchList = DispatchData.GetDispatchByDateRange(from, to);

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
            //dgvSchedule.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvSchedule.Refresh();

            dgvSchedule.Columns["ID"].Visible = false;
            dgvSchedule.Columns["MaterialsOrderedBy"].Visible = false;
            dgvSchedule.Columns["BenchtopOrderedBy"].Visible = false;

            dgvSchedule.SelectionChanged += dgvSchedule_SelectionChanged;

            foreach (DataGridViewColumn column in dgvSchedule.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // 🔍 Highlight total rows to make them stand out
            foreach (DataGridViewRow row in dgvSchedule.Rows)
            {
                if (row.DataBoundItem is DispatchBlankRow)
                {
                    //row.DefaultCellStyle.BackColor = Color.LightGray;
                    //row.DefaultCellStyle.Font = new Font(dgvSchedule.Font, FontStyle.Bold);
                    //row.DefaultCellStyle.ForeColor = Color.DarkSlateGray;
                    row.DefaultCellStyle.Font = new Font(dgvSchedule.Font, FontStyle.Bold);
                    row.DefaultCellStyle.ForeColor = Color.DarkSlateGray;

                    // Change only the Qty cell's background color
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

                // ✅ Format DispatchDate (inline)
                if (columnName == "DispatchDate" && e.Value is DateTime dtValue && dtValue != DateTime.MinValue)
                {
                    e.Value = dtValue.ToString("dd-MMM");
                    e.FormattingApplied = true;
                }

                // ✅ Apply week-based color
                var weekObj = row.Cells["WeekNo"].Value;
                if (weekObj is int week && weekColors.TryGetValue(week, out var color))
                {
                    row.Cells[e.ColumnIndex].Style.BackColor = color;
                    row.Cells[e.ColumnIndex].Style.SelectionBackColor = color;
                    row.Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
                    row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.Black;
                }
            }
        }

        //private void dgvSchedule_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        //{
        //    var row = dgvSchedule.Rows[e.RowIndex];
        //    string columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

        //    // Format DispatchDate for all rows
        //    if (columnName == "DispatchDate" && e.Value is DateTime dtValue && dtValue != DateTime.MinValue)
        //    {
        //        e.Value = dtValue.ToString("dd-MMM");
        //        e.FormattingApplied = true;
        //        return; // Optional: skip other logic
        //    }

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

        //        // Blank DispatchDate if it's MinValue
        //        if (columnName == "DispatchDate" && e.Value is DateTime dt && dt == DateTime.MinValue)
        //        {
        //            e.Value = "";
        //            e.FormattingApplied = true;
        //        }
        //    }

        //    // Apply week color to "Day" column based on WeekNo
        //    if (dgvSchedule.Columns[e.ColumnIndex].Name == "Day")
        //    {
        //        // Ensure we only apply week color to normal data rows
        //        if (row.DataBoundItem is DispatchBlankRow)
        //        {
        //            // Force grey styling for blank row Day cell
        //            row.Cells[e.ColumnIndex].Style.BackColor = Color.LightGray;
        //            row.Cells[e.ColumnIndex].Style.SelectionBackColor = Color.LightGray;
        //            row.Cells[e.ColumnIndex].Style.ForeColor = Color.DarkSlateGray;
        //            row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.DarkSlateGray;
        //            return;
        //        }

        //        // Normal row — apply week-based background color
        //        var weekObj = row.Cells["WeekNo"].Value;
        //        if (weekObj is int week && weekColors.TryGetValue(week, out var color))
        //        {
        //            row.Cells[e.ColumnIndex].Style.BackColor = color;
        //            row.Cells[e.ColumnIndex].Style.SelectionBackColor = color;
        //            row.Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
        //            row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.Black;
        //        }
        //    }


        //    //if (dgvSchedule.Columns[e.ColumnIndex].Name == "Day")
        //    //{
        //    //    var weekProp = row.Cells["WeekNo"].Value;
        //    //    if (weekProp is int week && weekColors.TryGetValue(week, out var color))
        //    //    {
        //    //        row.Cells[e.ColumnIndex].Style.BackColor = color;
        //    //        row.Cells[e.ColumnIndex].Style.SelectionBackColor = color;
        //    //        row.Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
        //    //        row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.Black;
        //    //    }
        //    //}




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
    }
}

