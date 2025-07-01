using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Configuration;

namespace DispatchManager.Forms
{
    public partial class FrmEmployeeDetails : Form
    {
        private int? employeeId = null;

        public FrmEmployeeDetails()
        {
            InitializeComponent();
        }

        public FrmEmployeeDetails(int id)
        {
            InitializeComponent();
            employeeId = id;
        }

        private void FrmEmployeeDetails_Load(object sender, EventArgs e)
        {
            if (employeeId.HasValue)
            {
                LoadEmployeeData(employeeId.Value);
            }
        }

        private void LoadEmployeeData(int id)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM Employees WHERE ID = @ID", conn);
                    cmd.Parameters.AddWithValue("@ID", id);

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        txtInitials.Text = reader["Initials"].ToString();
                        txtFullName.Text = reader["FullName"].ToString();
                        txtEmailWork.Text = reader["EmailWork"].ToString();
                        txtEmailPersonal.Text = reader["EmailPersonal"].ToString();
                        txtPhone.Text = reader["Phone"].ToString();
                        txtAddress.Text = reader["Address"].ToString();
                        txtPassword.Text = reader["Password"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employee data: " + ex.Message);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    SqlCommand cmd = conn.CreateCommand();

                    if (employeeId.HasValue)
                    {
                        // Update existing
                        cmd.CommandText = @"UPDATE Employees SET 
                            Initials = @Initials,
                            FullName = @FullName,
                            EmailWork = @EmailWork,
                            EmailPersonal = @EmailPersonal,
                            Phone = @Phone,
                            Address = @Address,
                            Password = @Password
                            WHERE ID = @ID";

                        cmd.Parameters.AddWithValue("@ID", employeeId.Value);
                    }
                    else
                    {
                        // Insert new
                        cmd.CommandText = @"INSERT INTO Employees 
                            (Initials, FullName, EmailWork, EmailPersonal, Phone, Address, Password)
                            VALUES (@Initials, @FullName, @EmailWork, @EmailPersonal, @Phone, @Address, @Password)";
                    }

                    cmd.Parameters.AddWithValue("@Initials", txtInitials.Text);
                    cmd.Parameters.AddWithValue("@FullName", txtFullName.Text);
                    cmd.Parameters.AddWithValue("@EmailWork", txtEmailWork.Text);
                    cmd.Parameters.AddWithValue("@EmailPersonal", txtEmailPersonal.Text);
                    cmd.Parameters.AddWithValue("@Phone", txtPhone.Text);
                    cmd.Parameters.AddWithValue("@Address", txtAddress.Text);
                    cmd.Parameters.AddWithValue("@Password", txtPassword.Text);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Employee saved successfully.");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving employee: " + ex.Message);
            }
        }
    }
}
