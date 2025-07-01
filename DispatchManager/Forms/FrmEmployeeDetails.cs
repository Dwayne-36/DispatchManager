using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace DispatchManager.Forms
{
    public partial class FrmEmployeeDetails : Form
    {
        private int? employeeId;

        public FrmEmployeeDetails(int? employeeId = null)
        {
            InitializeComponent();
            this.employeeId = employeeId;
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
            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Employees WHERE ID = @ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    txtFullName.Text = reader["FullName"].ToString();
                    txtInitials.Text = reader["Initials"].ToString();
                    txtEmailWork.Text = reader["WorkEmail"].ToString();
                    txtEmailPrivate.Text = reader["PrivateEmail"].ToString();
                    txtAddress.Text = reader["Address"].ToString();
                    txtPhone.Text = reader["PhoneNumber"].ToString();
                    txtPassword.Text = reader["Password"].ToString();
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd;

                if (employeeId.HasValue)
                {
                    // Update existing employee
                    cmd = new SqlCommand(@"UPDATE Employees SET 
                        FullName = @FullName, Initials = @Initials, WorkEmail = @WorkEmail,
                        PrivateEmail = @PrivateEmail, Address = @Address, 
                        PhoneNumber = @PhoneNumber, Password = @Password 
                        WHERE ID = @ID", conn);
                    cmd.Parameters.AddWithValue("@ID", employeeId.Value);
                }
                else
                {
                    // Add new employee
                    cmd = new SqlCommand(@"INSERT INTO Employees 
                        (FullName, Initials, WorkEmail, PrivateEmail, Address, PhoneNumber, Password) 
                        VALUES 
                        (@FullName, @Initials, @WorkEmail, @PrivateEmail, @Address, @PhoneNumber, @Password)", conn);
                }

                cmd.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                cmd.Parameters.AddWithValue("@Initials", txtInitials.Text.Trim());
                cmd.Parameters.AddWithValue("@WorkEmail", txtEmailWork.Text.Trim());
                cmd.Parameters.AddWithValue("@PrivateEmail", txtEmailPrivate.Text.Trim());
                cmd.Parameters.AddWithValue("@Address", txtAddress.Text.Trim());
                cmd.Parameters.AddWithValue("@PhoneNumber", txtPhone.Text.Trim());
                cmd.Parameters.AddWithValue("@Password", txtPassword.Text.Trim());

                cmd.ExecuteNonQuery();
                MessageBox.Show("Employee saved.");
                this.Close();
            }
        }
    }
}
