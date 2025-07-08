using DispatchManager.DataAccess;     // so we can call DataAccess.*
using DispatchManager.Models;         // for DispatchRecord
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;


namespace DispatchManager.Forms
{
    public partial class FrmNewProject : Form
    {
        public FrmNewProject()
        {
            InitializeComponent();

            this.Load += FrmNewProject_Load;
            this.tbProjectColour.Leave += tbProjectColour_Leave;
            this.btnAddKeyword.Click += btnAddKeyword_Click;


            btnAddLeadTime.Click += btnAddLeadTime_Click;

            cbxLeadTime.SelectedIndexChanged += cbxLeadTime_SelectedIndexChanged;
            cbxMainContractor.SelectedIndexChanged += cbxMainContractor_SelectedIndexChanged;


            tbDateOrdered.Text = DateTime.Today.ToString("d-MMM");

            // Add Installed options
            cbxInstalled.Items.Clear();
            cbxInstalled.Items.Add("Yes");
            cbxInstalled.Items.Add("No");
        }

        private List<string> deztekKeywords = new List<string>();

        private void FrmNewProject_Load(object sender, EventArgs e)
        {
            LoadComboBox(cbxMainContractor, "SELECT Name FROM MainContractors ORDER BY Name");
            LoadComboBox(cbxLeadTime, "SELECT Description FROM LeadTimes ORDER BY Description");

            LoadDeztekKeywords();
        }

        //private DateTime? dispatchDateValue = null;

        private void cbxLeadTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedLeadTime = cbxLeadTime.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedLeadTime)) return;

            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Get number of days from LeadTimes
                    SqlCommand cmd = new SqlCommand("SELECT [OrderDays] FROM LeadTimes WHERE Description = @desc", conn);
                    cmd.Parameters.AddWithValue("@desc", selectedLeadTime);
                    int daysToAdd = Convert.ToInt32(cmd.ExecuteScalar());

                    // Get all NZ holidays
                    SqlCommand holidayCmd = new SqlCommand("SELECT HolidayDate FROM PublicHolidays", conn);
                    SqlDataReader reader = holidayCmd.ExecuteReader();
                    List<DateTime> holidays = new List<DateTime>();
                    while (reader.Read())
                    {
                        holidays.Add(reader.GetDateTime(0).Date);
                    }
                    reader.Close();

                    // Calculate dispatch date
                    DateTime startDate = DateTime.Today;
                    DateTime dispatchDate = AddWorkingDays(startDate, daysToAdd, holidays);
                    //dispatchDateValue = dispatchDate;



                    tbDispatchDate.Text = dispatchDate.ToString("d-MMM");
                    lblDay1.Text = dispatchDate.ToString("dddd"); // Shows "Monday", "Tuesday", etc.
                    lblWeekNumber1.Text = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        dispatchDate,
                        CalendarWeekRule.FirstFourDayWeek,
                        DayOfWeek.Monday
                    ).ToString();


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error calculating dispatch date: " + ex.Message);
            }
        }

        private void SaveProject(bool closeAfterSave)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string sql = @"
            INSERT INTO Dispatch (
                DispatchDate, MainContractor, ProjectName, ProjectColour, Qty, Freight,
                BenchTopSupplier, BenchTopColour, Installer, Comment,
                DeliveryAddress, Phone, M3, Amount, OrderNumber, DateOrdered, LeadTime, WeekNo, Day, Installed, JobNo
            )
            VALUES (
                @DispatchDate, @MainContractor, @ProjectName, @ProjectColour, @Qty, @Freight,
                @BenchTopSupplier, @BenchTopColour, @Installer, @Comment,
                @DeliveryAddress, @Phone, @M3, @Amount, @OrderNumber, @DateOrdered, @LeadTime, @WeekNo, @Day, @Installed, @JobNo
            )";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@DispatchDate", ParseDate(tbDispatchDate.Text));
                        //if (dispatchDateValue.HasValue)
                        //    cmd.Parameters.AddWithValue("@DispatchDate", dispatchDateValue.Value);
                        //else
                        //    throw new InvalidOperationException("Dispatch date is not set.");

                        cmd.Parameters.AddWithValue("@MainContractor", cbxMainContractor.Text);
                        cmd.Parameters.AddWithValue("@ProjectName", tbProjectName.Text);
                        cmd.Parameters.AddWithValue("@ProjectColour", tbProjectColour.Text);
                        cmd.Parameters.AddWithValue("@Qty", ParseInt(tbQty.Text));
                        cmd.Parameters.AddWithValue("@Freight", tbFreight.Text);
                        cmd.Parameters.AddWithValue("@BenchTopSupplier", tbBenchtopSupplier.Text);
                        cmd.Parameters.AddWithValue("@BenchTopColour", tbBenchtopColour.Text);
                        cmd.Parameters.AddWithValue("@Installer", tbInstaller.Text);
                        cmd.Parameters.AddWithValue("@Comment", tbComment.Text);
                        cmd.Parameters.AddWithValue("@DeliveryAddress", tbDeliveryAddress.Text);
                        cmd.Parameters.AddWithValue("@Phone", tbPhone.Text);
                        cmd.Parameters.AddWithValue("@M3", tbM3.Text);
                        cmd.Parameters.AddWithValue("@Amount", ParseDecimal(tbAmount.Text));
                        cmd.Parameters.AddWithValue("@OrderNumber", ParseInt(tbOrderNumber.Text));
                        cmd.Parameters.AddWithValue("@DateOrdered", ParseDate(tbDateOrdered.Text));
                        cmd.Parameters.AddWithValue("@LeadTime", cbxLeadTime.Text);
                        cmd.Parameters.AddWithValue("@WeekNo", ParseInt(lblWeekNumber1.Text));
                        cmd.Parameters.AddWithValue("@Day", DateTime.Parse(tbDispatchDate.Text).ToString("ddd", CultureInfo.InvariantCulture));
                        cmd.Parameters.AddWithValue("@Installed", cbxInstalled.Text);
                        cmd.Parameters.AddWithValue("@JobNo", lblProjectNumber1.Text);


                        cmd.ExecuteNonQuery();

                        // ✅ Update JobNo for selected main contractor
                        UpdateMainContractorJobNo(cbxMainContractor.Text);
                    }

                    MessageBox.Show("Project saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (closeAfterSave)
                    {
                        this.Close();
                    }
                    else
                    {
                        ClearFormInputs();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving project: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFormInputs()
        {
            tbDispatchDate.Clear();
            cbxMainContractor.SelectedIndex = -1;
            tbProjectName.Clear();
            tbProjectColour.Clear();
            tbQty.Clear();
            tbFreight.Clear();
            tbBenchtopSupplier.Clear();
            tbBenchtopColour.Clear();
            tbInstaller.Clear();
            tbComment.Clear();
            tbDeliveryAddress.Clear();
            tbPhone.Clear();
            tbM3.Clear();
            tbAmount.Clear();
            tbOrderNumber.Clear();
            cbxLeadTime.SelectedIndex = -1;
            cbxInstalled.SelectedIndex = -1;
            lblDay1.Text = "";
            lblWeekNumber1.Text = "";

            // Keep tbDateOrdered unchanged
        }

        private object ParseDate(string input)
        {
            if (DateTime.TryParse(input, out DateTime dt))
                return dt;
            return DBNull.Value;
        }

        private object ParseInt(string input)
        {
            if (int.TryParse(input, out int num))
                return num;
            return DBNull.Value;
        }

        private object ParseDecimal(string input)
        {
            if (decimal.TryParse(input, out decimal dec))
                return dec;
            return DBNull.Value;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
           this.Close();
        }
        private void btnEnterProject_Click(object sender, EventArgs e)
        {
            SaveProject(false); // Save and stay open            
        }
        private void btnEnterClose_Click(object sender, EventArgs e)
        {
            SaveProject(true);  // Save and close
        }

        private void btnAddMainContractor_Click(object sender, EventArgs e)
        {
            AddItemToComboBox(cbxMainContractor, "Main Contractor", "MainContractors", "Name");
        }

        private void btnAddLeadTime_Click(object sender, EventArgs e)
        {
            string description = Interaction.InputBox("Enter Lead Time description:", "Add Lead Time", "");
            if (string.IsNullOrWhiteSpace(description)) return;

            // Prompt for all day values
            string orderStr = Interaction.InputBox("Enter number of days for Order:", "Add Lead Time", "0");
            string productionStr = Interaction.InputBox("Enter number of days for Production:", "Add Lead Time", "0");
            string detailWithBTStr = Interaction.InputBox("Enter number of days for Detail with benchtop:", "Add Lead Time", "0");
            string detailWithoutBTStr = Interaction.InputBox("Enter number of days for Detail without benchtop:", "Add Lead Time", "0");

            // Validate input
            if (!int.TryParse(orderStr, out int orderDays) ||
                !int.TryParse(productionStr, out int productionDays) ||
                !int.TryParse(detailWithBTStr, out int detailWithBTDays) ||
                !int.TryParse(detailWithoutBTStr, out int detailWithoutBTDays))
            {
                MessageBox.Show("Please enter valid numeric values for all day fields.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Insert into database
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"
                INSERT INTO LeadTimes (Description, OrderDays, ProductionDays, DetailWithBenchtopDays, DetailWithoutBenchtopDays)
                VALUES (@Description, @OrderDays, @ProductionDays, @DetailWithBT, @DetailWithoutBT)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@OrderDays", orderDays);
                        cmd.Parameters.AddWithValue("@ProductionDays", productionDays);
                        cmd.Parameters.AddWithValue("@DetailWithBT", detailWithBTDays);
                        cmd.Parameters.AddWithValue("@DetailWithoutBT", detailWithoutBTDays);
                        cmd.ExecuteNonQuery();
                    }
                }

                LoadComboBox(cbxLeadTime, "SELECT Description FROM LeadTimes ORDER BY Description");
                cbxLeadTime.SelectedItem = description;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving new lead time: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void LoadComboBox(ComboBox comboBox, string query)
        {
            comboBox.Items.Clear();

            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            comboBox.Items.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load items: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void AddItemToComboBox(ComboBox comboBox, string itemLabel, string tableName, string columnName)
        {
            string input = Interaction.InputBox($"Enter new {itemLabel}:", $"Add {itemLabel}", "");

            if (!string.IsNullOrWhiteSpace(input))
            {
                if (!comboBox.Items.Contains(input))
                {
                    try
                    {
                        string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

                        using (SqlConnection conn = new SqlConnection(connStr))
                        using (SqlCommand cmd = new SqlCommand($"INSERT INTO {tableName} ({columnName}) VALUES (@value)", conn))
                        {
                            conn.Open();
                            cmd.Parameters.AddWithValue("@value", input);
                            cmd.ExecuteNonQuery();
                        }

                        LoadComboBox(comboBox, $"SELECT {columnName} FROM {tableName} ORDER BY {columnName}");
                        comboBox.SelectedItem = input;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving new {itemLabel.ToLower()}: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"That {itemLabel.ToLower()} already exists.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private DateTime AddWorkingDays(DateTime start, int workDays, List<DateTime> holidays)
        {
            DateTime date = start;
            int addedDays = 0;

            while (addedDays < workDays)
            {
                date = date.AddDays(1);
                if (date.DayOfWeek != DayOfWeek.Saturday &&
                    date.DayOfWeek != DayOfWeek.Sunday &&
                    !holidays.Contains(date.Date))
                {
                    addedDays++;
                }
            }

            return date;
        }
        private void cbxMainContractor_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedContractor = cbxMainContractor.Text;

            if (string.IsNullOrWhiteSpace(selectedContractor))
            {
                lblProjectNumber1.Text = "";
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand cmd = new SqlCommand("SELECT JobNo FROM MainContractors WHERE Name = @Name", conn))
                {
                    cmd.Parameters.AddWithValue("@Name", selectedContractor);
                    conn.Open();

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        lblProjectNumber1.Text = result.ToString();
                    }
                    else
                    {
                        lblProjectNumber1.Text = "(No JobNo found)";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving JobNo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblProjectNumber1.Text = "";
            }
        }
        private void UpdateMainContractorJobNo(string contractorName)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Get current JobNo
                    string selectSql = "SELECT JobNo FROM MainContractors WHERE Name = @Name";
                    using (SqlCommand selectCmd = new SqlCommand(selectSql, conn))
                    {
                        selectCmd.Parameters.AddWithValue("@Name", contractorName);
                        object result = selectCmd.ExecuteScalar();

                        if (result != null && int.TryParse(result.ToString(), out int currentJobNo))
                        {
                            // Increment and adjust if ending in 999
                            int nextJobNo = currentJobNo + 1;
                            if (nextJobNo % 1000 == 999)
                                nextJobNo = (nextJobNo / 1000 + 1) * 1000;

                            // Update new JobNo
                            string updateSql = "UPDATE MainContractors SET JobNo = @NewJobNo WHERE Name = @Name";
                            using (SqlCommand updateCmd = new SqlCommand(updateSql, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@NewJobNo", nextJobNo);
                                updateCmd.Parameters.AddWithValue("@Name", contractorName);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating JobNo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadDeztekKeywords()
        {
            deztekKeywords.Clear();

            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;
            string query = "SELECT Dezignatek FROM KeyWords";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string keyword = reader["Dezignatek"].ToString();
                        deztekKeywords.Add(keyword.ToLower()); // Store lowercase for case-insensitive checks
                    }
                }
            }
        }
        private void tbProjectColour_Leave(object sender, EventArgs e)
        {
            string input = tbProjectColour.Text.ToLower();

            bool matchFound = deztekKeywords.Any(keyword => input.Contains(keyword));

            chkIsDeztek.Checked = matchFound;
        }

        private void btnAddKeyword_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter a new keyword (e.g. 'dezignatek', 'D/Tek', etc):",
                "Add Keyword", "");

            if (string.IsNullOrWhiteSpace(input))
                return;

            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO KeyWords (Dezignatek) VALUES (@Keyword)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Keyword", input.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Keyword added.");
                LoadDeztekKeywords(); // Reload updated list
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to add keyword.\n" + ex.Message);
            }
        }
       

    }

}
