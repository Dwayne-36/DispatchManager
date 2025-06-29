using DispatchManager.DataAccess;
using DispatchManager.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

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
        private void LoadScheduleData()
        {
            DateTime from = dtpFrom.Value;
            DateTime to = dtpTo.Value;

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
                    //MessageBox.Show($"Inserted total: Week {currentWeek} = Qty {weeklyTotal}", "DEBUG");

                    withWeeklyTotals.Add(new DispatchBlankRow
                    {
                        Qty = weeklyTotal,
                        BoardETA = $"Total for Week {currentWeek}",
                        DispatchDate = DateTime.MinValue, // or lastWeekDate if you're tracking that

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
                    //MessageBox.Show($"Inserted total: Week {currentWeek} = Qty {weeklyTotal}", "DEBUG");

                    withWeeklyTotals.Add(new DispatchBlankRow
                    {
                        Qty = weeklyTotal,
                        BoardETA = $"Total for Week {currentWeek}",
                        WeekNo = -1,                       // Sentinel value to handle in cell formatting
                        DispatchDate = DateTime.MinValue, // Will show as blank if formatted
                        JobNo = -1,
                        Amount = -1,
                        OrderNumber = -1
                    });
                }
            }

            dgvSchedule.DataSource = null;
            dgvSchedule.Rows.Clear(); // Just in case
            dgvSchedule.Columns.Clear();
           
            dgvSchedule.DataSource = withWeeklyTotals;
            dgvSchedule.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvSchedule.Refresh();

            dgvSchedule.Columns["ID"].Visible = false; // Hide ID column
            dgvSchedule.Columns["MaterialsOrderedBy"].Visible = false; // Hide ID column
            dgvSchedule.Columns["BenchtopOrderedBy"].Visible = false; // Hide ID column

            foreach (DataGridViewColumn column in dgvSchedule.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // 🔍 Highlight total rows to make them stand out
            foreach (DataGridViewRow row in dgvSchedule.Rows)
            {
                if (row.DataBoundItem is DispatchBlankRow)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGray;
                    row.DefaultCellStyle.Font = new Font(dgvSchedule.Font, FontStyle.Bold);
                    row.DefaultCellStyle.ForeColor = Color.DarkSlateGray;

                    // 🧪 Debug output: write total to console
                    Console.WriteLine($"Blank row - Qty: {row.Cells["Qty"].Value}, Comment: {row.Cells["Comment"].Value}");
                }
            }

        }

        private void dgvSchedule_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var row = dgvSchedule.Rows[e.RowIndex];

            if (row.DataBoundItem is DispatchBlankRow)
            {
                string columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

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

                // Blank DispatchDate
                if (columnName == "DispatchDate" && e.Value is DateTime dt && dt == DateTime.MinValue)
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }
            }
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
    }
}

