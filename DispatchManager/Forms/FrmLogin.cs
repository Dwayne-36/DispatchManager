using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Collections.Specialized.BitVector32;

namespace DispatchManager.Forms
{
    public partial class FrmLogin : Form
    {
        public FrmLogin()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.");
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"SELECT ID, Initials, FullName, IsAdmin FROM Employees 
                                          WHERE Username = @username AND Password = @password", conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    Guid employeeId = reader.GetGuid(0);
                    string initials = reader.GetString(1);
                    string fullName = reader.GetString(2); // Assuming FullName is the same as Initials for this example
                    bool isAdmin = reader.GetBoolean(3);
                    Session.IsAdmin = isAdmin;

                    Session.CurrentUserId = employeeId;
                    Session.CurrentInitials = initials;
                    Session.CurrentFullName = fullName;

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid username or password.");
                }
            }
        }

    }
}
