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
        }

        private void FrmViewDispatch_Load(object sender, EventArgs e)
        {
            try
            {
                var list = DispatchData.GetAllDispatchRecords();
                dgvDispatch.DataSource = list;

                // show ticks as checkboxes
                foreach (var name in new[] { "FB", "EB", "AS" })
                {
                    var col = (DataGridViewCheckBoxColumn)dgvDispatch.Columns[name];
                    col.HeaderText = name;
                    col.ReadOnly = false;
                }

                // initials columns editable
                foreach (var name in new[] { "ProdInput", "MaterialsOrdered", "ReleasedToFactory" })
                {
                    dgvDispatch.Columns[name].ReadOnly = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load failed");
            }
        }
        // Put your current user initials here, or grab from login
        private const string CurrentUser = "DK";

        private void dgvDispatch_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var colName = dgvDispatch.Columns[e.ColumnIndex].Name;
            var rec = (DispatchRecord)dgvDispatch.Rows[e.RowIndex].DataBoundItem;

            // tick columns
            if (colName == "FB" || colName == "EB" || colName == "AS")
            {
                bool newVal = !(bool)dgvDispatch[e.ColumnIndex, e.RowIndex].Value;
                dgvDispatch[e.ColumnIndex, e.RowIndex].Value = newVal;
                DispatchData.UpdateTickValue(rec.JobNo, colName, newVal);
            }
        }

        // for initials: detect double-click
        private void dgvDispatch_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var colName = dgvDispatch.Columns[e.ColumnIndex].Name;
            if (colName == "ProdInput" || colName == "MaterialsOrdered" || colName == "ReleasedToFactory")
            {
                var rec = (DispatchRecord)dgvDispatch.Rows[e.RowIndex].DataBoundItem;
                dgvDispatch[e.ColumnIndex, e.RowIndex].Value = CurrentUser;
                DispatchData.UpdateProgressInitials(rec.JobNo, colName, CurrentUser);
            }
        }

    }
}

