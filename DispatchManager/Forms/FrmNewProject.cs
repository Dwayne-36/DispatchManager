using System;
using System.Globalization;
using System.Windows.Forms;
using DispatchManager.DataAccess;     // so we can call DataAccess.*
using DispatchManager.Models;         // for DispatchRecord

namespace DispatchManager.Forms
{
    public partial class FrmNewProject : Form
    {
        public FrmNewProject()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            

            try
            {
                // ▼ 13-C-1  Gather basic dates
                DateTime dispatchDate = DateTime.Parse(tbDispatchDate.Text);
                int weekNo = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                dispatchDate,
                                CalendarWeekRule.FirstFourDayWeek,
                                DayOfWeek.Monday);

                // ▼ 13-C-2  Build the new DispatchRecord
                var rec = new DispatchRecord
                {
                    WeekNo = weekNo,
                    DispatchDate = dispatchDate,
                    DesignDate = dispatchDate,
                    MaterialsOrderedBy = null,   // overwritten below
                    BenchtopOrderedBy = null,
                    JobNo = DataAccess.DispatchData.GetNextJobNumber(),
                    MainContractor = cbxMainContractor.Text,
                    ProjectName = tbProjectName.Text,
                    ProjectColour = tbProjectColour.Text,
                    Qty = ParseInt(tbQty.Text),
                    Installed = cbxInstalled.Text,
                    Freight = tbFreight.Text,
                    BenchTopSupplier = tbBenchtopSupplier.Text,
                    BenchTopColour = tbBenchtopColour.Text,
                    Installer = tbInstaller.Text,
                    Comment = tbComment.Text,
                    DeliveryAddress = tbDeliveryAddress.Text,
                    Phone = tbPhone.Text,
                    M3 = tbM3.Text,
                    Amount = ParseDec(tbAmount.Text),
                    OrderNumber = ParseInt(tbOrderNumber.Text),
                    DateOrdered = DateTime.TryParse(tbDateOrdered.Text, out var dOrd) ? dOrd : dispatchDate,
                    LeadTime = cbxLeadTime.Text
                };

                // ▼ 13-C-3  Apply lead-time offsets (if any)
                int prodOff = DataAccess.DispatchData.GetLeadTimeOffset(rec.LeadTime, 3);
                int detailOff = DataAccess.DispatchData.GetLeadTimeOffset(rec.LeadTime, 4);

                rec.MaterialsOrderedBy = dispatchDate.AddDays(-prodOff);
                rec.BenchtopOrderedBy = dispatchDate.AddDays(-detailOff);

                // ▼ 13-C-4  Save to SQL
                DataAccess.DispatchData.InsertDispatchRecord(rec);

                // ▼ 13-C-5  Show success + update label
                lblProjectNumber1.Text = rec.JobNo.ToString();
                MessageBox.Show($"Project saved!\nJob #: {rec.JobNo}",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while saving:\n" + ex.Message,
                                "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 13-B-1  – Safely parse an int TextBox (returns 0 if blank)
        private static int ParseInt(string text) =>
            int.TryParse(text, out int n) ? n : 0;

        // 13-B-2  – Safely parse a decimal TextBox (returns 0 if blank)
        private static decimal ParseDec(string text) =>
            decimal.TryParse(text, out decimal n) ? n : 0m;
    }
}
