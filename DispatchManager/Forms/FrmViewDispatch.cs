using System;
using System.Windows.Forms;
using DispatchManager.DataAccess;
using DispatchManager.Models;

namespace DispatchManager.Forms
{
    public partial class FrmViewDispatch : Form
    {

        public FrmViewDispatch()
        {
            InitializeComponent();
            this.Load += FrmViewDispatch_Load;
            this.FormClosing += FrmViewDispatch_FormClosing;
            this.dtpFrom.ValueChanged += dtpFrom_ValueChanged;
            this.dtpTo.ValueChanged += dtpTo_ValueChanged;
        }

        private void FrmViewDispatch_Load(object sender, EventArgs e)
        {
            dtpFrom.Value = Properties.Settings.Default.dtpFromDate;
            dtpTo.Value = Properties.Settings.Default.dtpToDate;

            LoadScheduleData();
        }
        
        private void LoadScheduleData()
        {
            DateTime from = dtpFrom.Value;
            DateTime to = dtpTo.Value;

            var data = DispatchData.GetDispatchByDateRange(from, to);
            dgvSchedule.DataSource = data;

            dgvSchedule.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvSchedule.Refresh();
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




    }
}

