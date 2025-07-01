// FrmEmployeesList.cs
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Configuration;

namespace DispatchManager.Forms
{
    public partial class FrmEmployeesList : Form
    {
        private bool editMode = false;

        public FrmEmployeesList(bool editMode = false)
        {
            InitializeComponent();
            this.editMode = editMode;
        }

        private void FrmEmployeesList_Load(object sender, EventArgs e)
        {
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Employees ORDER BY FullName", conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgvEmployees.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employees: " + ex.Message);
            }
        }

        private void btnEditSelected_Click(object sender, EventArgs e)
        {
            if (dgvEmployees.CurrentRow != null)
            {
                Guid selectedId = (Guid)dgvEmployees.CurrentRow.Cells["ID"].Value;
                FrmEmployeeDetails frm = new FrmEmployeeDetails(selectedId);
                frm.ShowDialog();
                LoadEmployees(); // Refresh after edit
            }
        }

        private void btnEditSelected_Click_1(object sender, EventArgs e)
        {
            if (dgvEmployees.CurrentRow != null)
            {
                Guid selectedId = (Guid)dgvEmployees.CurrentRow.Cells["ID"].Value;
                FrmEmployeeDetails frm = new FrmEmployeeDetails(selectedId);          // Pass selected ID
                frm.ShowDialog();
                LoadEmployees(); // Refresh list after editing
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
