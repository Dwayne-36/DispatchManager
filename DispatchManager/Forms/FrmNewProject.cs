using DispatchManager.DataAccess;     // so we can call DataAccess.*
using DispatchManager.Models;         // for DispatchRecord
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows.Forms;


namespace DispatchManager.Forms
{
    public partial class FrmNewProject : Form
    {
        public FrmNewProject()
        {
            InitializeComponent();

            this.Load += FrmNewProject_Load;             
            btnEnterProject.Click += btnEnterProject_Click;
            
            //btnEnterClose.Click += btnEnterClose_Click;
            //btnClose.Click += btnClose_Click;
            btnAddLeadTime.Click += btnAddLeadTime_Click;

            cbxLeadTime.SelectedIndexChanged += cbxLeadTime_SelectedIndexChanged;



        }
        private void FrmNewProject_Load(object sender, EventArgs e)
        {
            LoadComboBox(cbxMainContractor, "SELECT Name FROM MainContractors ORDER BY Name");
            LoadComboBox(cbxLeadTime, "SELECT Description FROM LeadTimes ORDER BY Description");
        }

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



        private void btnEnterProject_Click(object sender, EventArgs e)
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
                    DeliveryAddress, Phone, M3, Amount, OrderNumber, DateOrdered, LeadTime
                )
                VALUES (
                    @DispatchDate, @MainContractor, @ProjectName, @ProjectColour, @Qty, @Freight,
                    @BenchTopSupplier, @BenchTopColour, @Installer, @Comment,
                    @DeliveryAddress, @Phone, @M3, @Amount, @OrderNumber, @DateOrdered, @LeadTime
                )";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@DispatchDate", ParseDate(tbDispatchDate.Text));
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

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Project saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close(); // Close the form after save
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving project: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void btnEnterClose_Click(object sender, EventArgs e)
        {            
            this.Close();
        }

        private void btnAddMainContractor_Click(object sender, EventArgs e)
        {
            AddItemToComboBox(cbxMainContractor, "Main Contractor", "MainContractors", "Name");
        }

        private void btnAddLeadTime_Click(object sender, EventArgs e)
        {
            AddItemToComboBox(cbxLeadTime, "Lead Time", "LeadTimes", "Description");
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





    }

}
