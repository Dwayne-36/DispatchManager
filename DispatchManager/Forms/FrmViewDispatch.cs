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

            // Load data and populate DataGridView
            LoadScheduleData();

            // Style blank rows if they are present
            dgvSchedule.RowPrePaint += (s, ev) =>
            {
                var row = dgvSchedule.Rows[ev.RowIndex];
                if (row.DataBoundItem is DispatchBlankRow)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGray;
                    row.DefaultCellStyle.SelectionBackColor = Color.LightGray;
                    row.DefaultCellStyle.ForeColor = Color.Transparent; // hide text
                    row.ReadOnly = true; // make it uneditable
                }
            };
        }

        //private void FrmViewDispatch_Load(object sender, EventArgs e)
        //{
        //    dtpFrom.Value = Properties.Settings.Default.dtpFromDate;
        //    dtpTo.Value = Properties.Settings.Default.dtpToDate;

        //    LoadScheduleData();
        //}
        private void LoadScheduleData()
        {
            DateTime from = dtpFrom.Value;
            DateTime to = dtpTo.Value;

            fullDispatchList = DispatchData.GetDispatchByDateRange(from, to);

            var groupedList = fullDispatchList
                .OrderBy(d => d.DispatchDate)
                .ThenBy(d => d.JobNo)
                .ToList();

            var withSpacers = new List<DispatchRecord>();
            DateTime? currentDay = null;

            foreach (var record in groupedList)
            {
                if (currentDay != null && record.DispatchDate.Date != currentDay.Value.Date)
                {
                    withSpacers.Add(new DispatchBlankRow()); // Insert blank row between days
                }

                withSpacers.Add(record);
                currentDay = record.DispatchDate.Date;
            }

            dgvSchedule.DataSource = withSpacers;
            dgvSchedule.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvSchedule.Refresh();
        }

        //private void LoadScheduleData()
        //{
        //    DateTime from = dtpFrom.Value;
        //    DateTime to = dtpTo.Value;

        //    fullDispatchList = DispatchData.GetDispatchByDateRange(from, to);
        //    dgvSchedule.DataSource = fullDispatchList;

        //    dgvSchedule.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        //    dgvSchedule.Refresh();
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

