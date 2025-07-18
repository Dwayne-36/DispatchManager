using ClosedXML.Excel;
using DispatchManager;
using DispatchManager.DataAccess;
using DispatchManager.Models;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace DispatchManager.Forms
{
    public partial class FrmViewDispatch : Form
    {
        private List<DispatchRecord> fullDispatchList = new List<DispatchRecord>();
        private Dictionary<Guid, Dictionary<string, string>> dispatchColors;
        private DataGridViewCell lastDoubleClickedCell = null;
        private List<string> deztekKeywords = new List<string>();
        private DateTimePicker dtpDispatch = new DateTimePicker();
        private DataGridViewCell currentDateCell = null;
        private bool isSelectingPrintArea = false;
        private List<DataGridViewCell> selectedPrintCells = new List<DataGridViewCell>();
        private Timer antTimer = new Timer();
        private float dashOffset = 0f;
        private Dictionary<int, Color> weekColors = new Dictionary<int, Color>();

        // Place this at the top of your FrmViewDispatch form
        private readonly HashSet<string> editableColumns = new HashSet<string>
{
    "DispatchDate",
    "ProdInput",
    "MaterialsOrdered",
    "ReleasedToFactory",
    "ProjectName",
    "ProjectColour",
    "Qty",
    "FB",
    "EB",
    "ASS",
    "BoardETA",
    "Installed",
    "Freight",
    "BenchTopSupplier",
    "BenchTopColour",
    "Installer",
    "Comment",
    "DeliveryAddress",
    "Phone",
    "M3",
    "Amount",
    "OrderNumber",
    "FlatBedColour",
    "EdgeColour",
    "PreAssemble",
    "CarcassAssemble",
    "Invoiced",
    "Stacked"
};

        public FrmViewDispatch()
        {
            InitializeComponent();

            // ✅ Allow user to reorder columns
            dgvSchedule.AllowUserToOrderColumns = true;
                       
            // Form Level Events
            this.Load += FrmViewDispatch_Load;
            this.FormClosing += FrmViewDispatch_FormClosing;

            // Control Level Events
            this.dtpFrom.ValueChanged += dtpFrom_ValueChanged;
            this.dtpTo.ValueChanged += dtpTo_ValueChanged;                                          

            //DataGridView Events
            this.dgvSchedule.CellDoubleClick += dgvSchedule_CellDoubleClick;
            this.dgvSchedule.CellEndEdit += dgvSchedule_CellEndEdit;
            dgvSchedule.CellBeginEdit += dgvSchedule_CellBeginEdit;
            dgvSchedule.CellFormatting += dgvSchedule_CellFormatting;
            dgvSchedule.SelectionChanged += dgvSchedule_SelectionChanged;
            dgvSchedule.CellClick += dgvSchedule_CellClick;
            dgvSchedule.CellMouseDown += dgvSchedule_CellMouseDown;

            dgvSchedule.Paint += dgvSchedule_Paint;

            this.KeyPreview = true;
            this.KeyDown += FrmViewDispatch_KeyDown;

            dgvSchedule.CellPainting += dgvSchedule_CellPainting;

            dgvSchedule.SelectionChanged += (s, e) =>
            {
                foreach (DataGridViewRow row in dgvSchedule.Rows)
                    dgvSchedule.InvalidateRow(row.Index);
            };

            dtpDispatch.CloseUp += (s, ev) =>
            {
                dtpDispatch.Visible = false;
            };


            //colour and style events
            dgvSchedule.RowPostPaint += dgvSchedule_RowPostPaint;

            
             // Remove blue selection highlight
            dgvSchedule.DefaultCellStyle.SelectionBackColor = dgvSchedule.DefaultCellStyle.BackColor;
            dgvSchedule.DefaultCellStyle.SelectionForeColor = dgvSchedule.DefaultCellStyle.ForeColor;

            //Optional: supress tooltips to reduce flicker
            dgvSchedule.ShowCellToolTips = false;
        }
        private void FrmViewDispatch_Load(object sender, EventArgs e)
        {
            dtpDispatch.Visible = false;
            dtpDispatch.Format = DateTimePickerFormat.Custom;
            dtpDispatch.CustomFormat = "d-MMM"; // This will format like 8-Jul
            dtpDispatch.TextChanged += DtpDispatch_TextChanged;
            this.Controls.Add(dtpDispatch);

            this.MouseDown += FrmViewDispatch_MouseDown;

            // Load saved date settings
            dtpFrom.Value = Properties.Settings.Default.dtpFromDate;
            dtpTo.Value = Properties.Settings.Default.dtpToDate;

            // Set the logged-in user label
            lblLoggedInUser.Text = $"{Session.CurrentFullName} is logged in";

            // Load and store colors from database
            dispatchColors = DispatchData.GetDispatchColours();

            // ⛔ Prevent flicker during data load
            dgvSchedule.SuspendLayout();

            // Load data and populate DataGridView
            LoadScheduleData();

            dgvSchedule.ResumeLayout();

            // ✅ Hook event to reposition label when columns are reordered
            dgvSchedule.ColumnDisplayIndexChanged += (s, args) => PositionTotalLabelNextToQty();
            dgvSchedule.ColumnWidthChanged += (s, args) => PositionTotalLabelNextToQty();

            // ✅ Hook event to handle editing control showing for custom columns
            dgvSchedule.EditingControlShowing += dgvSchedule_EditingControlShowing;

            dgvSchedule.DataBindingComplete += dgvSchedule_DataBindingComplete;

            dgvSchedule.CellMouseClick += dgvSchedule_CellMouseClick;

            //dgvSchedule.CellValueChanged += dgvSchedule_CellValueChanged;

            dgvSchedule.CurrentCellDirtyStateChanged += dgvSchedule_CurrentCellDirtyStateChanged;

            dgvSchedule.CellValueChanged += dgvSchedule_CellValueChanged;

            dgvSchedule.EnableHeadersVisualStyles = false;
            dgvSchedule.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvSchedule.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgvSchedule.ColumnHeadersDefaultCellStyle.BackColor;



            // ✅ Restore layout
            RestoreColumnSettings();

            // ✅ Optional: Apply double buffering to reduce flicker
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, dgvSchedule, new object[] { true });

            antTimer.Interval = 100;
            antTimer.Tick += AntTimer_Tick;

            dgvSchedule.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvSchedule.ColumnHeadersHeight = 140; // or taller if needed

            // ✅ Start SQL Dependency monitoring
            
            RegisterDispatchNotification();
            RegisterDispatchColoursNotification();

        }

        private void RegisterDispatchNotification()
        {
            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT ID, DispatchDate, ProdInput, MaterialsOrdered, ReleasedtoFactory, ProjectName, 
                            ProjectColour, FB, EB, ASS, FlatBedColour, EdgeColour, PreAssemble, CarcassAssemble, 
                            Invoiced, Stacked, BoardETA, BenchTopSupplier, BenchTopColour, Installer, 
                            DeliveryAddress, Phone, M3, Amount, OrderNumber, 
                            Installed, Comment, Qty

                        FROM dbo.Dispatch", conn)) // 🔸 Add here any column that users change
            {
                cmd.Notification = null;

                SqlDependency dependency = new SqlDependency(cmd);
                dependency.OnChange += new OnChangeEventHandler(OnDispatchTableChanged);

                conn.Open();
                cmd.ExecuteReader(CommandBehavior.CloseConnection);
            }
        }
        private void RegisterDispatchColoursNotification()
        {
            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT LINKID, ProdInputColor, MaterialsOrderedColor, ReleasedToFactoryColor,
                 MainContractorColor, FreightColor, AmountColor, ProjectNameColor
          FROM dbo.DispatchColours", conn))
            {
                cmd.Notification = null;

                SqlDependency dependency = new SqlDependency(cmd);
                dependency.OnChange += new OnChangeEventHandler(OnDispatchColoursChanged);

                conn.Open();
                cmd.ExecuteReader(CommandBehavior.CloseConnection);
            }
        }
        private void OnDispatchTableChanged(object sender, SqlNotificationEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new System.Action(() => OnDispatchTableChanged(sender, e)));
                return;
            }

            RegisterDispatchNotification(); // Re-register dependency

            var latestRecords = DispatchData.GetDispatchByDateRange(dtpFrom.Value, dtpTo.Value);
            bool reloadNeeded = false;

            foreach (DataGridViewRow row in dgvSchedule.Rows)
            {
                if (row.DataBoundItem is DispatchRecord oldRecord)
                {
                    var updatedRecord = latestRecords.FirstOrDefault(r => r.ID == oldRecord.ID);
                    if (updatedRecord == null) continue;

                    if (!Equals(oldRecord.DispatchDate, updatedRecord.DispatchDate))
                    {
                        reloadNeeded = true;
                        break;
                    }
                }
            }

            if (reloadNeeded)
            {
                LoadScheduleData();
                return;
            }

            foreach (DataGridViewRow row in dgvSchedule.Rows)
            {
                if (row.DataBoundItem is DispatchRecord oldRecord)
                {
                    var updatedRecord = latestRecords.FirstOrDefault(r => r.ID == oldRecord.ID);
                    if (updatedRecord == null) continue;

                    void TryUpdateCell(string columnName, object newValue)
                    {
                        if (dgvSchedule.Columns.Contains(columnName))
                        {
                            var cell = row.Cells[columnName];
                            if (!Equals(cell.Value, newValue))
                            {
                                cell.Value = newValue;
                            }
                        }
                    }

                    TryUpdateCell("ProdInput", updatedRecord.ProdInput);
                    TryUpdateCell("MaterialsOrdered", updatedRecord.MaterialsOrdered);
                    TryUpdateCell("ReleasedtoFactory", updatedRecord.ReleasedToFactory);
                    TryUpdateCell("ProjectName", updatedRecord.ProjectName);
                    TryUpdateCell("ProjectColour", updatedRecord.ProjectColour);
                    TryUpdateCell("FB", updatedRecord.FB);
                    TryUpdateCell("EB", updatedRecord.EB);
                    TryUpdateCell("ASS", updatedRecord.ASS);
                    TryUpdateCell("FlatBedColour", updatedRecord.FlatBedColour);
                    TryUpdateCell("EdgeColour", updatedRecord.EdgeColour);
                    TryUpdateCell("PreAssemble", updatedRecord.PreAssemble);
                    TryUpdateCell("CarcassAssemble", updatedRecord.CarcassAssemble);
                    TryUpdateCell("Invoiced", updatedRecord.Invoiced);
                    TryUpdateCell("Stacked", updatedRecord.Stacked);
                    TryUpdateCell("BoardETA", updatedRecord.BoardETA);
                    TryUpdateCell("BenchTopSupplier", updatedRecord.BenchTopSupplier);
                    TryUpdateCell("BenchTopColour", updatedRecord.BenchTopColour);
                    TryUpdateCell("Installer", updatedRecord.Installer);
                    TryUpdateCell("DeliveryAddress", updatedRecord.DeliveryAddress);
                    TryUpdateCell("Phone", updatedRecord.Phone);
                    TryUpdateCell("M3", updatedRecord.M3);
                    TryUpdateCell("Amount", updatedRecord.Amount);
                    TryUpdateCell("OrderNumber", updatedRecord.OrderNumber);
                    TryUpdateCell("Installed", updatedRecord.Installed);
                    TryUpdateCell("Comment", updatedRecord.Comment);
                    TryUpdateCell("Qty", updatedRecord.Qty);

                }
            }
        }

        private void OnDispatchColoursChanged(object sender, SqlNotificationEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new System.Action(() => OnDispatchColoursChanged(sender, e)));
                return;
            }

            // Re-register dependency
            RegisterDispatchColoursNotification();

            // ✅ Always refresh latest color mapping
            dispatchColors = DispatchData.GetDispatchColours();

            // ✅ Reload just cell colors (not full dispatch list)
            foreach (DataGridViewRow row in dgvSchedule.Rows)
            {
                if (row.DataBoundItem is DispatchRecord record)
                {
                    if (dispatchColors.TryGetValue(record.ID, out var colors))
                    {
                        if (colors.TryGetValue("ProdInputColor", out string prodColor))
                            row.Cells["ProdInput"].Style.BackColor = ColorTranslator.FromHtml(prodColor);

                        if (colors.TryGetValue("MaterialsOrderedColor", out string matColor))
                            row.Cells["MaterialsOrdered"].Style.BackColor = ColorTranslator.FromHtml(matColor);

                        if (colors.TryGetValue("ReleasedToFactoryColor", out string relColor))
                            row.Cells["ReleasedtoFactory"].Style.BackColor = ColorTranslator.FromHtml(relColor);

                        if (colors.TryGetValue("MainContractorColor", out string mainColor))
                            row.Cells["MainContractor"].Style.BackColor = ColorTranslator.FromHtml(mainColor);

                        if (colors.TryGetValue("FreightColor", out string freightColor))
                            row.Cells["Freight"].Style.BackColor = ColorTranslator.FromHtml(freightColor);

                        if (colors.TryGetValue("AmountColor", out string amountColor))
                            row.Cells["Amount"].Style.BackColor = ColorTranslator.FromHtml(amountColor);

                        if (colors.TryGetValue("ProjectNameColor", out string projColor))
                            row.Cells["ProjectName"].Style.BackColor = ColorTranslator.FromHtml(projColor);
                    }
                }
            }

        }
        private void AntTimer_Tick(object sender, EventArgs e)
        {
            dashOffset += 1f;
            if (dashOffset > 10f) dashOffset = 0f;
            dgvSchedule.Invalidate(); // Force border redraw
        }
        private void dgvSchedule_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvSchedule.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
            {
                dgvSchedule.CommitEdit(DataGridViewDataErrorContexts.Commit);

                // Save the updated value to SQL
                var row = dgvSchedule.Rows[e.RowIndex];
                if (row.DataBoundItem is DispatchRecord record)
                {
                    DispatchData.UpdateSingleField(record.ID, dgvSchedule.Columns[e.ColumnIndex].Name, row.Cells[e.ColumnIndex].Value);
                }
            }
        }

        

        private void dgvSchedule_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvSchedule.IsCurrentCellDirty &&
                dgvSchedule.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvSchedule.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }


        private void DtpDispatch_TextChanged(object sender, EventArgs e)
        {
            if (currentDateCell == null) return;

            int rowIndex = currentDateCell.RowIndex;
            if (rowIndex < 0 || rowIndex >= dgvSchedule.Rows.Count) return;

            var row = dgvSchedule.Rows[rowIndex];

            if (row.DataBoundItem is DispatchRecord record)
            {
                DateTime oldDate = record.DispatchDate;
                int oldWeek = record.WeekNo;

                DateTime newDate = dtpDispatch.Value;
                CultureInfo culture = CultureInfo.CurrentCulture;
                int newWeek = culture.Calendar.GetWeekOfYear(
                    newDate,
                    CalendarWeekRule.FirstFourDayWeek,
                    DayOfWeek.Monday);

                string newDay = newDate.ToString("ddd");

                record.DispatchDate = newDate;
                record.WeekNo = newWeek;
                record.Day = newDay;

                // Update DataGridView cells
                row.Cells["DispatchDate"].Value = newDate;
                row.Cells["WeekNo"].Value = newWeek;
                row.Cells["Day"].Value = newDay;

                // Update SQL
                DispatchData.UpdateDispatchField(record.ID, "DispatchDate", newDate);
                DispatchData.UpdateDispatchField(record.ID, "WeekNo", newWeek);
                DispatchData.UpdateDispatchField(record.ID, "Day", newDay);

                // 🔁 Only reload layout if week number changed (row might move)
                if (newWeek != oldWeek)
                {
                    LoadScheduleData(); // Refresh to regroup rows and totals
                }
            }

            dtpDispatch.Visible = false;
        }
        private void dgvSchedule_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        private void dgvSchedule_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = dgvSchedule.Rows[e.RowIndex];
            var column = dgvSchedule.Columns[e.ColumnIndex];
            var cell = row.Cells[e.ColumnIndex];
            string columnName = column.Name;

            // ✅ Show DateTimePicker for DispatchDate
            if (columnName == "DispatchDate")
            {
                var dispatchDateCell = dgvSchedule.Rows[e.RowIndex].Cells[e.ColumnIndex];
                currentDateCell = dispatchDateCell;

                // ⛔ Cancel default cell edit behavior
                dgvSchedule.CurrentCell = null;

                System.Drawing.Rectangle rect = dgvSchedule.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                int dtpWidth = 120;
                int dtpHeight = 30;
                int dtpX = rect.X + (rect.Width - dtpWidth) / 2;
                int dtpY = rect.Y + (rect.Height - dtpHeight) / 2;

                dtpDispatch.Value = DateTime.TryParse(dispatchDateCell.Value?.ToString(), out DateTime dateVal)
                    ? dateVal
                    : DateTime.Today;

                dtpDispatch.Bounds = new System.Drawing.Rectangle(dtpX, dtpY, dtpWidth, dtpHeight);
                dtpDispatch.Visible = true;
                dtpDispatch.BringToFront();
                dtpDispatch.Focus();
                SendKeys.Send("%{DOWN}");
                return;
            }

            // ✅ Store cell for future use
            lastDoubleClickedCell = cell;

            if (row.DataBoundItem is DispatchRecord record)
            {
                string initials = Session.CurrentInitials;
                string oldValue = cell.Value?.ToString();
                Color currentColor = cell.Style.BackColor;

                // ✅ Toggle ProjectName, Freight, and Amount columns with specific colors
                if (columnName == "ProjectName" || columnName == "Freight" || columnName == "Amount")
                {
                    Color toggleColor;
                    string dbColorColumn;

                    if (columnName == "ProjectName")
                    {
                        toggleColor = Color.FromArgb(0, 176, 80); // Green
                        dbColorColumn = "ProjectNameColor";
                    }
                    else if (columnName == "Freight")
                    {
                        toggleColor = Color.FromArgb(255, 140, 0); // Orange
                        dbColorColumn = "FreightColor";
                    }
                    else // Amount
                    {
                        toggleColor = Color.FromArgb(153, 0, 204); // Purple
                        dbColorColumn = "AmountColor";
                    }

                    Color white = Color.White;

                    // Toggle between target color and white
                    if (currentColor.ToArgb() == toggleColor.ToArgb())
                    {
                        cell.Style.BackColor = white;
                        SaveCellColorToDatabase(record.ID, dbColorColumn, "White");
                    }
                    else
                    {
                        cell.Style.BackColor = toggleColor;
                        SaveCellColorToDatabase(record.ID, dbColorColumn, $"{toggleColor.R},{toggleColor.G},{toggleColor.B}");
                    }

                    cell.Style.ForeColor = Color.Black;
                    cell.Style.SelectionBackColor = cell.Style.BackColor;
                    cell.Style.SelectionForeColor = Color.Black;

                    // Save color to DispatchRecord object
                    string colorStr = cell.Style.BackColor == white ? null : $"{toggleColor.R},{toggleColor.G},{toggleColor.B}";
                    if (columnName == "ProjectName") record.ProjectNameColor = colorStr;
                    else if (columnName == "Freight") record.FreightColor = colorStr;
                    else if (columnName == "Amount") record.AmountColor = colorStr;

                    // Shift focus to right cell to avoid edit mode
                    int nextCol = e.ColumnIndex + 1;
                    if (nextCol < dgvSchedule.ColumnCount)
                        dgvSchedule.CurrentCell = dgvSchedule.Rows[e.RowIndex].Cells[nextCol];

                    // Invalidate visual update
                    this.BeginInvoke((MethodInvoker)(() => dgvSchedule.InvalidateCell(cell)));
                    return;
                }


                string colorColumn = null;
                if (columnName == "ProdInput") colorColumn = "ProdInputColor";
                else if (columnName == "MaterialsOrdered") colorColumn = "MaterialsOrderedColor";
                else if (columnName == "ReleasedToFactory") colorColumn = "ReleasedToFactoryColor";
                else if (columnName == "MainContractor") colorColumn = "MainContractorColor";
                else if (columnName == "Freight") colorColumn = "FreightColor";
                else if (columnName == "Amount") colorColumn = "AmountColor";

                if (colorColumn == null) return;

                Color red = Color.Red;
                Color green2 = Color.FromArgb(146, 208, 80);
                Color white2 = Color.White;
                Color orange = Color.FromArgb(255, 140, 0);
                Color purple = Color.FromArgb(153, 0, 204);

                if (columnName == "ReleasedToFactory")
                {
                    if (string.IsNullOrWhiteSpace(oldValue))
                    {
                        cell.Value = "REL";
                        cell.Style.BackColor = white2;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{white2.R},{white2.G},{white2.B}");
                    }
                    else if (oldValue == "REL" && currentColor.ToArgb() == white2.ToArgb())
                    {
                        cell.Style.BackColor = orange;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{orange.R},{orange.G},{orange.B}");
                    }
                    else
                    {
                        cell.Value = "";
                        cell.Style.BackColor = white2;
                        SaveCellColorToDatabase(record.ID, colorColumn, "White");
                    }

                    cell.Style.ForeColor = Color.Black;
                    cell.Style.SelectionBackColor = cell.Style.BackColor;
                    cell.Style.SelectionForeColor = Color.Black;
                    SaveInitialsToDispatch(record.ID, columnName, cell.Value?.ToString());
                }
                else if (columnName == "MainContractor")
                {
                    if (currentColor.ToArgb() == white2.ToArgb())
                    {
                        cell.Style.BackColor = orange;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{orange.R},{orange.G},{orange.B}");
                    }
                    else if (currentColor.ToArgb() == orange.ToArgb())
                    {
                        cell.Style.BackColor = green2;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{green2.R},{green2.G},{green2.B}");
                    }
                    else
                    {
                        cell.Style.BackColor = white2;
                        SaveCellColorToDatabase(record.ID, colorColumn, "White");
                    }

                    cell.Style.ForeColor = Color.Black;
                    cell.Style.SelectionBackColor = cell.Style.BackColor;
                    cell.Style.SelectionForeColor = Color.Black;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(oldValue))
                    {
                        cell.Value = initials;
                        cell.Style.BackColor = red;
                        SaveCellColorToDatabase(record.ID, colorColumn, $"{red.R},{red.G},{red.B}");
                    }
                    else if (oldValue == initials)
                    {
                        if (currentColor.ToArgb() == red.ToArgb())
                        {
                            cell.Style.BackColor = green2;
                            SaveCellColorToDatabase(record.ID, colorColumn, $"{green2.R},{green2.G},{green2.B}");
                        }
                        else
                        {
                            cell.Value = "";
                            cell.Style.BackColor = white2;
                            SaveCellColorToDatabase(record.ID, colorColumn, "White");
                        }
                    }
                    else
                    {
                        if (currentColor.ToArgb() == red.ToArgb())
                        {
                            cell.Value = initials;
                            cell.Style.BackColor = green2;
                            SaveCellColorToDatabase(record.ID, colorColumn, $"{green2.R},{green2.G},{green2.B}");
                        }
                        else
                        {
                            cell.Value = "";
                            cell.Style.BackColor = white2;
                            SaveCellColorToDatabase(record.ID, colorColumn, "White");
                        }
                    }

                    SaveInitialsToDispatch(record.ID, columnName, cell.Value?.ToString());
                    cell.Style.ForeColor = Color.Black;
                    cell.Style.SelectionBackColor = cell.Style.BackColor;
                    cell.Style.SelectionForeColor = Color.Black;
                }

                string colorString = cell.Style.BackColor == white2 ? null : $"{cell.Style.BackColor.R},{cell.Style.BackColor.G},{cell.Style.BackColor.B}";
                if (colorColumn == "ProdInputColor") record.ProdInputColor = colorString;
                else if (colorColumn == "MaterialsOrderedColor") record.MaterialsOrderedColor = colorString;
                else if (colorColumn == "ReleasedToFactoryColor") record.ReleasedToFactoryColor = colorString;
                else if (colorColumn == "MainContractorColor") record.MainContractorColor = colorString;
                else if (colorColumn == "FreightColor") record.FreightColor = colorString;
                else if (colorColumn == "AmountColor") record.AmountColor = colorString;
                else if (colorColumn == "ProjectNameColor") record.ProjectColour = colorString;

                this.BeginInvoke((MethodInvoker)(() => dgvSchedule.InvalidateCell(cell)));
            }
        }

        private void SaveCellColorToDatabase(Guid linkId, string columnName, string colorValue)
        {
            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Try to UPDATE first
                string updateSql = $@"UPDATE DispatchColours SET {columnName} = @ColorValue WHERE LinkID = @LinkID";
                using (SqlCommand updateCmd = new SqlCommand(updateSql, conn))
                {
                    updateCmd.Parameters.AddWithValue("@ColorValue", colorValue);
                    updateCmd.Parameters.AddWithValue("@LinkID", linkId);

                    int rowsAffected = updateCmd.ExecuteNonQuery();

                    // If nothing updated, INSERT instead
                    if (rowsAffected == 0)
                    {
                        string insertSql = $@"INSERT INTO DispatchColours (LinkID, {columnName}) VALUES (@LinkID, @ColorValue)";
                        using (SqlCommand insertCmd = new SqlCommand(insertSql, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@LinkID", linkId);
                            insertCmd.Parameters.AddWithValue("@ColorValue", colorValue);
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private void SaveInitialsToDispatch(Guid recordId, string columnName, string initials)
        {
            string connStr = ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Update only if the column exists in Dispatch
                string updateSql = $@"UPDATE Dispatch SET {columnName} = @Initials WHERE ID = @ID";

                using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Initials", string.IsNullOrEmpty(initials) ? (object)DBNull.Value : initials);
                    cmd.Parameters.AddWithValue("@ID", recordId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ReloadSchedule()
        {
            LoadScheduleData(); // Or whatever method you use to repopulate dgvSchedule

        }
        private void LoadScheduleData()
        {
            // ✅ Store scroll position and current cell
            int firstVisibleRow = dgvSchedule.FirstDisplayedScrollingRowIndex;
            DataGridViewCell currentCell = dgvSchedule.CurrentCell;

            DateTime from = dtpFrom.Value;
            DateTime to = dtpTo.Value;

            fullDispatchList = DispatchData.GetDispatchByDateRange(from, to);
            dispatchColors = DispatchData.GetDispatchColours();

            var groupedList = fullDispatchList
                .OrderBy(d => d.DispatchDate)
                .ThenBy(d => d.JobNo)
                .ToList();

            var withWeeklyTotals = new List<DispatchRecord>();
            int? currentWeek = null;
            int weeklyTotal = 0;

            for (int i = 0; i < groupedList.Count; i++)
            {
                var record = groupedList[i];

                int recordWeek = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    record.DispatchDate,
                    System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                    DayOfWeek.Monday);

                if (currentWeek != null && recordWeek != currentWeek)
                {
                    withWeeklyTotals.Add(new DispatchBlankRow
                    {
                        Qty = weeklyTotal,
                        ProjectColour = $"Total for Week {currentWeek}",
                        DispatchDate = DateTime.MinValue,
                    });
                    weeklyTotal = 0;
                }

                if (dispatchColors.TryGetValue(record.ID, out var cellColours))
                {
                    record.ProdInputColor = cellColours.TryGetValue("ProdInputColor", out string v1) ? v1 : null;
                    record.MaterialsOrderedColor = cellColours.TryGetValue("MaterialsOrderedColor", out string v2) ? v2 : null;
                    record.ReleasedToFactoryColor = cellColours.TryGetValue("ReleasedToFactoryColor", out string v3) ? v3 : null;
                    record.MainContractorColor = cellColours.TryGetValue("MainContractorColor", out string v4) ? v4 : null;
                    record.FreightColor = cellColours.TryGetValue("FreightColor", out string v6) ? v6 : null;
                    record.AmountColor = cellColours.TryGetValue("AmountColor", out string v7) ? v7 : null;
                    record.ProjectNameColor = cellColours.TryGetValue("ProjectNameColor", out string v5) ? v5 : null;
                }

                withWeeklyTotals.Add(record);
                weeklyTotal += record.Qty;
                currentWeek = recordWeek;

                bool isLast = (i == groupedList.Count - 1);
                if (isLast && weeklyTotal > 0)
                {
                    withWeeklyTotals.Add(new DispatchBlankRow
                    {
                        Qty = weeklyTotal,
                        ProjectColour = $"Total for Week {currentWeek}",
                        WeekNo = -1,
                        DispatchDate = DateTime.MinValue,
                        JobNo = -1,
                        Amount = -1,
                        OrderNumber = -1
                    });
                }
            }

                Color[] palette = new Color[]
            {
                Color.FromArgb(0, 255, 255),
                Color.FromArgb(255, 128, 128),
                Color.FromArgb(0, 255, 0),
                Color.FromArgb(255, 255, 0),
                Color.FromArgb(255, 0, 255)
            };

            weekColors.Clear();
            int colorIndex = 0;
            foreach (var record in withWeeklyTotals)
            {
                if (record is DispatchBlankRow) continue;
                int week = record.WeekNo;
                if (!weekColors.ContainsKey(week))
                {
                    weekColors[week] = palette[colorIndex % palette.Length];
                    colorIndex++;
                }
            }

            dgvSchedule.SuspendLayout(); // ✅ Start suspend layout
            dgvSchedule.DataSource = null;
            dgvSchedule.Rows.Clear();
            dgvSchedule.Columns.Clear();

            dgvSchedule.DataSource = withWeeklyTotals;

           


            foreach (DataGridViewRow row in dgvSchedule.Rows)
            {
                if (row.DataBoundItem is DispatchBlankRow)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGray;
                    row.Cells["ProjectColour"].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    row.DefaultCellStyle.Font = new System.Drawing.Font(dgvSchedule.Font, FontStyle.Bold);
                    row.DefaultCellStyle.ForeColor = Color.DarkSlateGray;

                    int qtyColumnIndex = dgvSchedule.Columns["Qty"].Index;
                    row.Cells[qtyColumnIndex].Style.BackColor = Color.FromArgb(255, 204, 0);
                }
                    }
                    string[] hiddenColumns = {
                    "ID", "MaterialsOrderedBy", "BenchtopOrderedBy",
                    "ProdInputColor", "MaterialsOrderedColor", "ReleasedToFactoryColor",
                    "MainContractorColor", "FreightColor", "AmountColor", "LinkID", "ProjectNameColor"
                     };

            foreach (string col in hiddenColumns)
            {
                if (dgvSchedule.Columns.Contains(col))
                {
                    dgvSchedule.Columns[col].Visible = false;
                }
            }

            foreach (DataGridViewColumn column in dgvSchedule.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.ReadOnly = !editableColumns.Contains(column.Name);
            }

            // Disable editing for double-click toggle columns
            string[] toggleColumns = { "ProdInput", "MaterialsOrdered", "ReleasedToFactory" };
            foreach (string colName in toggleColumns)
            {
                if (dgvSchedule.Columns.Contains(colName))
                {
                    dgvSchedule.Columns[colName].ReadOnly = true;
                }
            }

            UpdateTotalLabel(dgvSchedule.Rows.Cast<DataGridViewRow>());
            PositionTotalLabelNextToQty();

            LoadDeztekKeywords();
            HighlightDeztekRows();

            dgvSchedule.Columns["DispatchDate"].HeaderText = " Dispatch Date";
            dgvSchedule.Columns["ProdInput"].HeaderText = " Production Input";
            dgvSchedule.Columns["MaterialsOrdered"].HeaderText = " Materials Ordered";
            dgvSchedule.Columns["ReleasedtoFactory"].HeaderText = " Released to Factory";
            dgvSchedule.Columns["FB"].HeaderText = " Flatbed White";
            dgvSchedule.Columns["EB"].HeaderText = " Edge White";
            dgvSchedule.Columns["ASS"].HeaderText = " Fit-Out";
            dgvSchedule.Columns["Installer"].HeaderText = " Installer Name";
            dgvSchedule.Columns["Qty"].HeaderText = " Quantity";
            dgvSchedule.Columns["Amount"].HeaderText = " Invoice Amount ($)";
            dgvSchedule.Columns["ProjectName"].HeaderText = " Project Name";
            dgvSchedule.Columns["ProjectColour"].HeaderText = " Project Colour";
            dgvSchedule.Columns["BenchTopSupplier"].HeaderText = " Bench Top Supplier";
            dgvSchedule.Columns["BenchTopColour"].HeaderText = " Bench Top Colour";
            dgvSchedule.Columns["DeliveryAddress"].HeaderText = " Delivery Address";
            dgvSchedule.Columns["Phone"].HeaderText = " Contact Phone";
            dgvSchedule.Columns["Comment"].HeaderText = " Comment";
            dgvSchedule.Columns["OrderNumber"].HeaderText = " Order Number";
            dgvSchedule.Columns["Day"].HeaderText = " Day";
            dgvSchedule.Columns["WeekNo"].HeaderText = " Week No";
            dgvSchedule.Columns["DateOrdered"].HeaderText = " Date Ordered";
            dgvSchedule.Columns["LeadTime"].HeaderText = " Lead Time";
            dgvSchedule.Columns["JobNo"].HeaderText = " Job No";
            dgvSchedule.Columns["M3"].HeaderText = " Volume (m³)";
            dgvSchedule.Columns["BoardETA"].HeaderText = " Assembler";
            dgvSchedule.Columns["MainContractor"].HeaderText = " Main Contractor";
            dgvSchedule.Columns["FlatBedColour"].HeaderText = " Flatbed Colour";
            dgvSchedule.Columns["EdgeColour"].HeaderText = " Edge Colour";
            dgvSchedule.Columns["PreAssemble"].HeaderText = " Pre-Assemble";
            dgvSchedule.Columns["CarcassAssemble"].HeaderText = " Carcass Assemble";
            dgvSchedule.Columns["Invoiced"].HeaderText = " Invoiced";
            dgvSchedule.Columns["Stacked"].HeaderText = " Dipatched";

            dgvSchedule.ResumeLayout(); // ✅ Resume layout
            dgvSchedule.Refresh();

            // ✅ Restore scroll and selection if valid
            try
            {
                if (firstVisibleRow >= 0 && firstVisibleRow < dgvSchedule.RowCount)
                    dgvSchedule.FirstDisplayedScrollingRowIndex = firstVisibleRow;

                if (currentCell != null &&
                    currentCell.RowIndex < dgvSchedule.RowCount &&
                    currentCell.ColumnIndex < dgvSchedule.ColumnCount)
                {
                    dgvSchedule.CurrentCell = dgvSchedule.Rows[currentCell.RowIndex].Cells[currentCell.ColumnIndex];
                }
            }
            catch
            {
                // Ignore if restoring position fails
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
                        if (!string.IsNullOrWhiteSpace(keyword))
                            deztekKeywords.Add(keyword.ToLower());
                    }
                }
            }
        }

        private void HighlightDeztekRows()
        {
            foreach (DataGridViewRow row in dgvSchedule.Rows)
            {
                if (row.Cells["ProjectColour"].Value != null)
                {
                    string colourValue = row.Cells["ProjectColour"].Value.ToString().ToLower();

                    // Check against all keywords
                    foreach (string keyword in deztekKeywords)
                    {
                        if (colourValue.Contains(keyword))
                        {
                            row.Cells["ProjectColour"].Style.BackColor = Color.FromArgb(127, 247, 247);
                            break;
                        }
                    }
                }
            }
        }

        private void dgvSchedule_SelectionChanged(object sender, EventArgs e)
        {
            

            var selectedRows = dgvSchedule.SelectedRows.Cast<DataGridViewRow>()
                .Where(row => row.DataBoundItem is DispatchRecord && !(row.DataBoundItem is DispatchBlankRow));

            if (selectedRows.Count() > 1)
            {
                UpdateTotalLabel(selectedRows);
            }
            else
            {
                // Use all rows if 1 or 0 selected
                UpdateTotalLabel(dgvSchedule.Rows.Cast<DataGridViewRow>());
            }
        }
        private void UpdateTotalLabel(IEnumerable<DataGridViewRow> rows)
        {
            int totalQty = 0;

            foreach (var row in rows)
            {
                if (row.DataBoundItem is DispatchRecord record && !(record is DispatchBlankRow))
                {
                    totalQty += record.Qty;
                }
            }

            lblTotal.Text = $"Total Cabinets: {totalQty:N0}";
            lblTotal.AutoSize = true; // Ensures accurate width


        }

        private void dgvSchedule_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var row = dgvSchedule.Rows[e.RowIndex];
            string columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

            // ✅ Blank total rows
            if (row.DataBoundItem is DispatchBlankRow)
            {
                if ((columnName == "WeekNo" || columnName == "JobNo" || columnName == "OrderNumber" || columnName == "Qty")
                    && e.Value is int intVal && intVal <= 0)
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }

                if (columnName == "Amount" && e.Value is decimal decVal && decVal <= 0)
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }

                if (columnName == "DispatchDate" && e.Value is DateTime dt && dt == DateTime.MinValue)
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }

                // ✅ Hide checkboxes for DispatchBlankRow
                string[] checkboxCols = {
            "FB", "EB", "ASS", "FlatBedColour", "EdgeColour",
            "PreAssemble", "CarcassAssemble", "FitOut", "Stacked"
        };

                if (checkboxCols.Contains(columnName))
                {
                    e.FormattingApplied = false; // Let it keep the value, don't break binding
                }


                // Make totals yellow ONLY in Qty column
                if (columnName == "Qty" || columnName == "ProjectColour")
                {
                    row.Cells[e.ColumnIndex].Style.BackColor = Color.FromArgb(255, 204, 0); // Yellow
                    row.Cells[e.ColumnIndex].Style.SelectionBackColor = Color.FromArgb(255, 204, 0);
                    row.Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
                    row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.Black;
                }
                else
                {
                    row.Cells[e.ColumnIndex].Style.BackColor = Color.LightGray;
                    row.Cells[e.ColumnIndex].Style.SelectionBackColor = Color.LightGray;
                    row.Cells[e.ColumnIndex].Style.ForeColor = Color.DarkSlateGray;
                    row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.DarkSlateGray;
                }
                return;
            }

            // ✅ Apply week color
            if (columnName == "Day")
            {
                if (columnName == "DispatchDate" && e.Value is DateTime dtValue && dtValue != DateTime.MinValue)
                {
                    e.Value = dtValue.ToString("dd-MMM");
                    e.FormattingApplied = true;
                }

                var weekObj = row.Cells["WeekNo"].Value;
                if (weekObj is int week && weekColors.TryGetValue(week, out var color))
                {
                    row.Cells[e.ColumnIndex].Style.BackColor = color;
                    row.Cells[e.ColumnIndex].Style.SelectionBackColor = color;
                    row.Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
                    row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.Black;
                }
            }

            if ((columnName == "DispatchDate" || columnName == "DateOrdered") &&
                 e.Value is DateTime realDate && realDate != DateTime.MinValue)
            {
                e.Value = realDate.ToString("dd-MMM");
                e.FormattingApplied = true;
            }

            // ✅ Apply saved cell background colors
            if (row.DataBoundItem is DispatchRecord rec)
            {
                string colorValue = null;

                if (columnName == "ProdInput")
                    colorValue = rec.ProdInputColor;
                else if (columnName == "MaterialsOrdered")
                    colorValue = rec.MaterialsOrderedColor;
                else if (columnName == "ReleasedToFactory")
                    colorValue = rec.ReleasedToFactoryColor;
                else if (columnName == "MainContractor")
                    colorValue = rec.MainContractorColor;
                else if (columnName == "Freight")
                    colorValue = rec.FreightColor;
                else if (columnName == "Amount")
                    colorValue = rec.AmountColor;
                else if (columnName == "ProjectName")
                    colorValue = rec.ProjectNameColor;

                if (!string.IsNullOrWhiteSpace(colorValue))
                {
                    string[] rgb = colorValue.Split(',');
                    if (rgb.Length == 3 &&
                        int.TryParse(rgb[0], out int r) &&
                        int.TryParse(rgb[1], out int g) &&
                        int.TryParse(rgb[2], out int b))
                    {
                        Color dbColor = Color.FromArgb(r, g, b);
                        row.Cells[e.ColumnIndex].Style.BackColor = dbColor;
                        row.Cells[e.ColumnIndex].Style.SelectionBackColor = dbColor;
                    }
                }
            }
        }


        //private void dgvSchedule_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        //{
        //    var row = dgvSchedule.Rows[e.RowIndex];
        //    string columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

        //    // ✅ Blank total rows
        //    if (row.DataBoundItem is DispatchBlankRow)
        //    {
        //        if ((columnName == "WeekNo" || columnName == "JobNo" || columnName == "OrderNumber" || columnName == "Qty")
        //            && e.Value is int intVal && intVal <= 0)
        //        {
        //            e.Value = "";
        //            e.FormattingApplied = true;
        //        }

        //        if (columnName == "Amount" && e.Value is decimal decVal && decVal <= 0)
        //        {
        //            e.Value = "";
        //            e.FormattingApplied = true;
        //        }

        //        if (columnName == "DispatchDate" && e.Value is DateTime dt && dt == DateTime.MinValue)
        //        {
        //            e.Value = "";
        //            e.FormattingApplied = true;
        //        }

        //        // Make totals yellow ONLY in Qty column
        //        if (columnName == "Qty" || columnName == "ProjectColour")
        //        {
        //            row.Cells[e.ColumnIndex].Style.BackColor = Color.FromArgb(255, 204, 0); // Yellow
        //            row.Cells[e.ColumnIndex].Style.SelectionBackColor = Color.FromArgb(255, 204, 0);
        //            row.Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
        //            row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.Black;
        //        }
        //        else
        //        {
        //            row.Cells[e.ColumnIndex].Style.BackColor = Color.LightGray;
        //            row.Cells[e.ColumnIndex].Style.SelectionBackColor = Color.LightGray;
        //            row.Cells[e.ColumnIndex].Style.ForeColor = Color.DarkSlateGray;
        //            row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.DarkSlateGray;
        //        }
        //        return;
        //    }

        //    // ✅ Apply week color
        //    if (columnName == "Day")
        //    {
        //        if (columnName == "DispatchDate" && e.Value is DateTime dtValue && dtValue != DateTime.MinValue)
        //        {
        //            e.Value = dtValue.ToString("dd-MMM");
        //            e.FormattingApplied = true;
        //        }

        //        var weekObj = row.Cells["WeekNo"].Value;
        //        if (weekObj is int week && weekColors.TryGetValue(week, out var color))
        //        {
        //            row.Cells[e.ColumnIndex].Style.BackColor = color;
        //            row.Cells[e.ColumnIndex].Style.SelectionBackColor = color;
        //            row.Cells[e.ColumnIndex].Style.ForeColor = Color.Black;
        //            row.Cells[e.ColumnIndex].Style.SelectionForeColor = Color.Black;
        //        }
        //    }

        //    if ((columnName == "DispatchDate" || columnName == "DateOrdered") &&
        //         e.Value is DateTime realDate && realDate != DateTime.MinValue)
        //    {
        //        e.Value = realDate.ToString("dd-MMM");
        //        e.FormattingApplied = true;
        //    }

        //    // ✅ Apply saved cell background colors
        //    if (row.DataBoundItem is DispatchRecord rec)
        //    {
        //        string colorValue = null;

        //        if (columnName == "ProdInput")
        //            colorValue = rec.ProdInputColor;
        //        else if (columnName == "MaterialsOrdered")
        //            colorValue = rec.MaterialsOrderedColor;
        //        else if (columnName == "ReleasedToFactory")
        //            colorValue = rec.ReleasedToFactoryColor;
        //        else if (columnName == "MainContractor")
        //            colorValue = rec.MainContractorColor;
        //        else if (columnName == "Freight")
        //            colorValue = rec.FreightColor;
        //        else if (columnName == "Amount")
        //            colorValue = rec.AmountColor;
        //        else if (columnName == "ProjectName")
        //            colorValue = rec.ProjectNameColor;

        //        if (!string.IsNullOrWhiteSpace(colorValue))
        //        {
        //            string[] rgb = colorValue.Split(',');
        //            if (rgb.Length == 3 &&
        //                int.TryParse(rgb[0], out int r) &&
        //                int.TryParse(rgb[1], out int g) &&
        //                int.TryParse(rgb[2], out int b))
        //            {
        //                Color dbColor = Color.FromArgb(r, g, b);
        //                row.Cells[e.ColumnIndex].Style.BackColor = dbColor;
        //                row.Cells[e.ColumnIndex].Style.SelectionBackColor = dbColor;
        //            }
        //        }
        //    }
        //}

        private Color ParseColor(string input)
        {
            try
            {
                // Try HTML format first (e.g., #92D050)
                if (input.StartsWith("#"))
                    return ColorTranslator.FromHtml(input);

                // Try RGB format (e.g., "146,208,80")
                var parts = input.Split(',').Select(p => int.Parse(p.Trim())).ToArray();
                if (parts.Length == 3)
                    return Color.FromArgb(parts[0], parts[1], parts[2]);
            }
            catch
            {
                // Ignore malformed color
            }

            return Color.White; // Default fallback
        }
                

        
        private void dtpFrom_ValueChanged(object sender, EventArgs e)
        {
            LoadScheduleData();
            // ✅ Restore layout
            RestoreColumnSettings();
        }

        private void dtpTo_ValueChanged(object sender, EventArgs e)
        {
            LoadScheduleData();
            // ✅ Restore layout
            RestoreColumnSettings();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string keyword = tbSearch.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                // 🔁 If search is empty, restore full schedule with total rows
                LoadScheduleData();
                return;
            }

            // 🔍 Filter based on search keyword
            var filteredList = fullDispatchList.Where(d =>
                d.JobNo.ToString().Contains(keyword) ||
                (d.ProjectName?.ToLower().Contains(keyword) ?? false) ||
                (d.MainContractor?.ToLower().Contains(keyword) ?? false) ||
                (d.ProjectColour?.ToLower().Contains(keyword) ?? false) ||
                (d.DispatchDate.ToShortDateString().ToLower().Contains(keyword)) ||
                (d.Comment?.ToLower().Contains(keyword) ?? false)
            ).ToList();

            dgvSchedule.DataSource = filteredList;
            RestoreColumnSettings(); // 🛠️ Keep this to reapply column width/order
            dgvSchedule.Refresh();
        }
                       
        private void menuEmployees_Click(object sender, EventArgs e)
        {
            FrmEmployeesList frm = new FrmEmployeesList();
            frm.ShowDialog();
        }
        private void dgvSchedule_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var cell = dgvSchedule.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

            bool isCustomColumn = columnName == "ProdInput"
                               || columnName == "MaterialsOrdered"
                               || columnName == "ReleasedToFactory"
                               || columnName == "MainContractor";

            bool isColorClickOnlyColumn = columnName == "Freight"
                                       || columnName == "Amount";

            if (isCustomColumn)
            {
                e.Cancel = true;
            }
            else if (isColorClickOnlyColumn)
            {
                if (lastDoubleClickedCell == cell)
                {
                    e.Cancel = true;
                    lastDoubleClickedCell = null;
                }
            }
            else if (columnName == "DispatchDate")
            {
                // ✅ Prevent edit mode if DateTimePicker is already visible
                e.Cancel = true;
            }

            // ✅ Ensure cell is not in edit mode
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var row = dgvSchedule.Rows[e.RowIndex];
            var colName = dgvSchedule.Columns[e.ColumnIndex].Name;

            string[] checkboxCols = {
                "FB", "EB", "ASS", "FlatBedColour", "EdgeColour",
                "PreAssemble", "CarcassAssemble", "FitOut", "Stacked"
              };

            if (row.DataBoundItem is DispatchBlankRow && checkboxCols.Contains(colName))
            {
                e.Cancel = true; // ✅ Prevent checkbox from being toggled
            }
        }

        private void dgvSchedule_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            string columnName = dgvSchedule.Columns[e.ColumnIndex].Name;

            // ✅ Bail if not editable
            if (!editableColumns.Contains(columnName))
                return;

            var row = dgvSchedule.Rows[e.RowIndex];

            if (row.IsNewRow || row.Cells["ID"].Value == null)
                return;

            if (!Guid.TryParse(row.Cells["ID"].Value.ToString(), out Guid id))
                return;

            object newValue = row.Cells[columnName].Value ?? DBNull.Value;

            bool success = DispatchData.UpdateDispatchField(id, columnName, newValue);

            if (!success)
            {
                MessageBox.Show($"Could not update {columnName}.", "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // ✅ Extra: Update cell color if editing ProjectColour
            if (columnName == "ProjectColour")
            {
                string cellValue = newValue?.ToString().ToLower() ?? "";

                bool isDeztek = deztekKeywords.Any(keyword => cellValue.Contains(keyword));

                if (isDeztek)
                {
                    row.Cells["ProjectColour"].Style.BackColor = Color.FromArgb(127, 247, 247); // match
                }
                else
                {
                    row.Cells["ProjectColour"].Style.BackColor = Color.White; // no match
                }
            }
        }

        private void dgvSchedule_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            if (isSelectingPrintArea)
                return; // ⛔ Skip row borders during print selection

            var grid = dgvSchedule;
            if (grid.SelectedCells.Count == 0) return;

            using (Pen pinkPen = new Pen(Color.DeepPink, 2))
            {
                var selectedRowIndexes = grid.SelectedCells
                    .Cast<DataGridViewCell>()
                    .Select(c => c.RowIndex)
                    .Distinct()
                    .OrderBy(i => i)
                    .ToList();

                bool isContiguous =
                    selectedRowIndexes.Count > 1 &&
                    selectedRowIndexes.Last() - selectedRowIndexes.First() + 1 == selectedRowIndexes.Count;

                if (isContiguous)
                {
                    int topRow = selectedRowIndexes.First();
                    int bottomRow = selectedRowIndexes.Last();

                    // Only draw once — on the last row
                    if (e.RowIndex == bottomRow)
                    {
                        System.Drawing.Rectangle topRect = grid.GetRowDisplayRectangle(topRow, true);
                        System.Drawing.Rectangle bottomRect = grid.GetRowDisplayRectangle(bottomRow, true);

                        int left = grid.RowHeadersWidth;
                        int width = grid.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);

                        // Draw top border
                        e.Graphics.DrawLine(pinkPen, left, topRect.Top + 1, left + width, topRect.Top + 1);

                        // Draw bottom border
                        e.Graphics.DrawLine(pinkPen, left, bottomRect.Bottom - 2, left + width, bottomRect.Bottom - 2);
                    }
                }
                else
                {
                    // Non-contiguous selection
                    if (grid.Rows[e.RowIndex].Selected)
                    {
                        System.Drawing.Rectangle rowRect = grid.GetRowDisplayRectangle(e.RowIndex, true);
                        int left = grid.RowHeadersWidth;
                        int width = grid.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);

                        e.Graphics.DrawRectangle(pinkPen, new System.Drawing.Rectangle(
                            left + 1,
                            rowRect.Top + 1,
                            width - 3,
                            rowRect.Height - 3));
                    }
                }
            }
        }
        private void PositionTotalLabelNextToQty()
        {
            if (dgvSchedule.Columns.Contains("Qty"))
            {
                var qtyColumn = dgvSchedule.Columns["Qty"];
                int displayIndex = qtyColumn.DisplayIndex;

                // Get the rectangle of the Qty column header
                var qtyColumnRect = dgvSchedule.GetCellDisplayRectangle(displayIndex, -1, true);

                // Measure the text width using the current font
                SizeF textSize;
                using (Graphics g = lblTotal.CreateGraphics())
                {
                    textSize = g.MeasureString(lblTotal.Text, lblTotal.Font);
                }

                // Center the label text under the column
                int centerOfQtyColumn = qtyColumnRect.Left + (qtyColumnRect.Width / 2);
                int labelLeft = dgvSchedule.Left + centerOfQtyColumn - (int)(textSize.Width / 2);

                lblTotal.Left = labelLeft;
                lblTotal.Top = dgvSchedule.Bottom + 5;
            }
        }

        private void dgvSchedule_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is System.Windows.Forms.TextBox tb)
            {
                // Remove any existing handler to prevent multiple subscriptions
                tb.Enter -= TextBox_EnterMoveToEnd;
                tb.Enter += TextBox_EnterMoveToEnd;
            }
        }

        private void TextBox_EnterMoveToEnd(object sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.TextBox tb)
            {
                // Delay placing the caret at the end to let the DataGridView finish processing
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    tb.SelectionStart = tb.Text.Length;
                    tb.SelectionLength = 0;
                }));
            }
        }
        private void dgvSchedule_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (isSelectingPrintArea)
            {
                // 🛑 Ignore header clicks during print area selection
                if (e.RowIndex == -1 || e.ColumnIndex == -1)
                {
                    dgvSchedule.ClearSelection();
                    return;
                }

                var cell = dgvSchedule[e.ColumnIndex, e.RowIndex];
                if (!selectedPrintCells.Contains(cell))
                    selectedPrintCells.Add(cell);

                dgvSchedule.ClearSelection(); // optional: don't show highlight
                dgvSchedule.Invalidate();
            }
        }


      

        private void dgvSchedule_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (!isSelectingPrintArea) return;

            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                dgvSchedule.CurrentCell = dgvSchedule.Rows[e.RowIndex].Cells[e.ColumnIndex];
                dgvSchedule.BeginEdit(true);  // Start editing immediately

                if (dgvSchedule.EditingControl is System.Windows.Forms.TextBox tb)
                {
                    // Delay cursor placement until control is ready
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        tb.SelectionStart = tb.Text.Length;
                        tb.SelectionLength = 0;
                    }));
                }
            }
        }
        private void RestoreColumnSettings()
        {
            // Restore saved column widths
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.ColumnWidths))
            {
                string[] widthPairs = Properties.Settings.Default.ColumnWidths.Split('|');
                foreach (string pair in widthPairs)
                {
                    string[] parts = pair.Split(':');
                    if (parts.Length == 2)
                    {
                        string colName = parts[0];
                        if (int.TryParse(parts[1], out int width) && dgvSchedule.Columns.Contains(colName))
                        {
                            dgvSchedule.Columns[colName].Width = width;
                        }
                    }
                }
            }

            // Restore saved column order
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.ColumnOrder))
            {
                string[] orderPairs = Properties.Settings.Default.ColumnOrder.Split('|');
                foreach (string pair in orderPairs)
                {
                    string[] parts = pair.Split(':');
                    if (parts.Length == 2)
                    {
                        string colName = parts[0];
                        if (int.TryParse(parts[1], out int displayIndex) && dgvSchedule.Columns.Contains(colName))
                        {
                            dgvSchedule.Columns[colName].DisplayIndex = displayIndex;
                        }
                    }
                }
            }
        }
        private void FrmViewDispatch_FormClosing(object sender, FormClosingEventArgs e)
        {
            List<string> widthList = new List<string>();

            foreach (DataGridViewColumn col in dgvSchedule.Columns)
            {
                widthList.Add($"{col.Name}:{col.Width}");
            }
            //Properties.Settings.Default.ColumnWidths = string.Join("|", widthList);

            // ✅ Save column order
            List<string> orderPairs = new List<string>();
            foreach (DataGridViewColumn col in dgvSchedule.Columns)
            {
                orderPairs.Add($"{col.Name}:{col.DisplayIndex}");
            }
            
            Properties.Settings.Default.dtpFromDate = dtpFrom.Value;
            Properties.Settings.Default.dtpToDate = dtpTo.Value;
            Properties.Settings.Default.ColumnWidths = string.Join("|", widthList);
            Properties.Settings.Default.ColumnOrder = string.Join("|", orderPairs);
            Properties.Settings.Default.Save();
        }

        private void dgvSchedule_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            RestoreColumnSettings();
        }

        private void tbSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = tbSearch.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                LoadScheduleData(); // brings back full data + blank rows
                return;
            }

            var filteredList = fullDispatchList.Where(d =>
                d.JobNo.ToString().Contains(keyword) ||
                (d.ProjectName?.ToLower().Contains(keyword) ?? false) ||
                (d.MainContractor?.ToLower().Contains(keyword) ?? false) ||
                (d.ProjectColour?.ToLower().Contains(keyword) ?? false) ||
                (d.DispatchDate.ToShortDateString().ToLower().Contains(keyword)) ||
                (d.Comment?.ToLower().Contains(keyword) ?? false)
            ).ToList();

            dgvSchedule.DataSource = filteredList;
        }

        private void btnNewProject_Click(object sender, EventArgs e)
        {
            using (var newProjectForm = new FrmNewProject())
            {
                newProjectForm.ShowDialog();

                // Optionally refresh the main view after closing
                LoadScheduleData(); // Reload to reflect new project
                RestoreColumnSettings(); // Keep column widths/order
            }
        }


        private void AppendDeletedRowToCsv(DataGridViewRow row)
        {
            try
            {
                string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string logPath = Path.Combine(documentsFolder, "DeletedRowsLog.csv");

                List<string> values = new List<string>();

                // Capture the row data

                    foreach (DataGridViewColumn col in dgvSchedule.Columns)
                    {
                        var cellValue = row.Cells[col.Index].Value?.ToString().Replace("\"", "\"\"") ?? "";
                        values.Add($"\"{cellValue}\"");
                    }


                // Add separate metadata fields
                string deletedBy = Session.CurrentFullName;
                string deletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                values.Add($"\"{deletedBy}\"");
                values.Add($"\"{deletedAt}\"");

                // Add headers if file is new
                if (!File.Exists(logPath))
                {
                    using (StreamWriter sw = new StreamWriter(logPath, true))
                    {
                        List<string> headers = new List<string>();
                        foreach (DataGridViewColumn col in dgvSchedule.Columns)
                            headers.Add($"\"{col.HeaderText}\"");

                        headers.Add("\"Deleted By\"");
                        headers.Add("\"Date Deleted\"");

                        sw.WriteLine(string.Join(",", headers));
                    }
                }

                // Append row
                File.AppendAllText(logPath, string.Join(",", values) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to write deleted row log.\n" + ex.Message);
            }
        }


        private void dgvSchedule_MouseDown(object sender, MouseEventArgs e)
        {
            if (!isSelectingPrintArea) return;

            if (e.Button == MouseButtons.Right)
            {
                var hitTest = dgvSchedule.HitTest(e.X, e.Y);
                if (hitTest.RowIndex >= 0)
                {
                    dgvSchedule.ClearSelection();
                    dgvSchedule.Rows[hitTest.RowIndex].Selected = true;
                }
            }
        }

        private void menuDeleteRow_Click(object sender, EventArgs e)
        {
            if (!Session.IsAdmin)
            {
                MessageBox.Show("Only administrators can delete rows.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvSchedule.SelectedRows.Count == 0)
                return;

            var result = MessageBox.Show("Are you sure you want to delete this row?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;

            var selectedRow = dgvSchedule.SelectedRows[0];

            // Log to CSV
            AppendDeletedRowToCsv(selectedRow);

            // Delete from DB
            if (selectedRow.Cells["ID"].Value is Guid id)
            {
                // Backup to CSV first (already done before this)

                // Delete from DB
                DispatchData.DeleteById(id);

                // Refresh the grid to reflect changes
                LoadScheduleData(); // Or whatever method you're using to reload the data
            }

        }

        private void MenuDeletedProjects_Click(object sender, EventArgs e)
        {
            FrmDeletedProjects frm = new FrmDeletedProjects();
            frm.ShowDialog();
        }


        private void menuCopyRow_Click(object sender, EventArgs e)
        {
            if (dgvSchedule.SelectedRows.Count == 0)
                return;

            var originalRow = dgvSchedule.SelectedRows[0];
            CopyRowToDatabase(originalRow);
            LoadScheduleData(); // Refresh grid to show the new copied row
        }


        private void CopyRowToDatabase(DataGridViewRow row)
        {
            try
            {
                string[] columns = new string[]
                {
                    "WeekNo", "DispatchDate", "MaterialsOrderedBy", "BenchtopOrderedBy", "Day", "JobNo",
                    "ProdInput", "MaterialsOrdered", "ReleasedtoFactory", "MainContractor", "ProjectName", "ProjectColour", "Qty",
                    "FB", "EB", "ASS", "Installed", "Freight", "BenchTopSupplier", "BenchTopColour", "Installer",
                    "Comment", "DeliveryAddress", "Phone", "M3", "Amount", "OrderNumber", "DateOrdered", "LeadTime", "ID", "LinkId",
                    "FlatBedColour", "EdgeColour", "PreAssemble", "CarcassAssemble", "Invoiced", "Stacked"
                };

                var values = new List<string>();

                Guid newID = Guid.NewGuid(); // ✅ new ID to use in query
                Guid originalID = (Guid)row.Cells["ID"].Value; // ✅ original row's ID

                foreach (string column in columns)
                {
                    object val = row.Cells[column]?.Value;

                    // ✅ Clear specific field values
                    if (column == "ProdInput" || column == "MaterialsOrdered" || column == "ReleasedtoFactory")
                    {
                        values.Add("NULL");
                    }
                    else if (column == "FB" || column == "EB" || column == "ASS"
                          || column == "FlatBedColour" || column == "EdgeColour"
                          || column == "PreAssemble" || column == "CarcassAssemble"
                          || column == "Invoiced" || column == "Stacked")

                    {
                        values.Add("0"); // false
                    }
                    else if (column == "ID")
                    {
                        values.Add($"'{newID}'"); // use new unique ID
                    }
                    else if (column == "LinkId")
                    {
                        values.Add($"'{newID}'"); // use same ID for colour reference
                    }
                    else if (column == "Day")
                    {
                        if (DateTime.TryParse(row.Cells["DispatchDate"]?.Value?.ToString(), out DateTime dd))
                        {
                            values.Add($"'{dd:ddd}'"); // Mon, Tue, etc.
                        }
                        else
                        {
                            values.Add("NULL");
                        }
                    }
                    else if (val == null || string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        values.Add("NULL");
                    }
                    else if (DateTime.TryParse(val.ToString(), out DateTime dt))
                    {
                        values.Add($"'{dt:yyyy-MM-dd HH:mm:ss}'");
                    }
                    else
                    {
                        values.Add($"'{val.ToString().Replace("'", "''")}'");
                    }
                }

                string query = $"INSERT INTO Dispatch ({string.Join(",", columns)}) VALUES ({string.Join(",", values)})";

                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["HayloSync"].ConnectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // ✅ Copy colour data with cleared entries for copied fields
                    DispatchData.CopyDispatchColours(originalID, newID, clearKeys: new[] {
                "ProdInputColor", "MaterialsOrderedColor", "ReleasedToFactoryColor"
            });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to copy row to database.\n" + ex.Message);
            }
        }

        private void DtpDispatch_ValueChanged(object sender, EventArgs e)
        {
            if (currentDateCell == null) return;

            var row = dgvSchedule.Rows[currentDateCell.RowIndex];
            DateTime newDate = dtpDispatch.Value;

            // Update DispatchDate cell visually
            currentDateCell.Value = newDate.ToString("d-MMM");  // Example: 8-Jul

            // Update underlying DispatchRecord if bound
            if (row.DataBoundItem is DispatchRecord record)
            {
                record.DispatchDate = newDate;

                // Update WeekNo and Day fields
                record.WeekNo = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    newDate,
                    System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                    DayOfWeek.Monday);

                record.Day = newDate.ToString("ddd"); // e.g. "Mon", "Tue"


                // Update cells in the DataGridView
                if (dgvSchedule.Columns.Contains("WeekNo"))
                    row.Cells["WeekNo"].Value = record.WeekNo;
                if (dgvSchedule.Columns.Contains("Day"))
                    row.Cells["Day"].Value = newDate.ToString("ddd"); // Mon, Tue, etc.

                // Save to SQL
                DispatchData.UpdateDispatchField(record.ID, "DispatchDate", newDate);
                DispatchData.UpdateDispatchField(record.ID, "WeekNo", record.WeekNo);
                DispatchData.UpdateDispatchField(record.ID, "Day", newDate);
            }

            // Hide DateTimePicker after selection
            dtpDispatch.Visible = false;
            dgvSchedule.Focus();
        }

        private void MenuPurchaseOrder_Click(object sender, EventArgs e)
        {            
                if (dgvSchedule.SelectedCells.Count == 0) return;

                var cell = dgvSchedule.SelectedCells[0];
                var row = dgvSchedule.Rows[cell.RowIndex];
                var columnName = dgvSchedule.Columns[cell.ColumnIndex].Name;

                if (row.DataBoundItem is DispatchRecord record)
                {
                    string filePath = @"X:\Purchase Orders\Files\Purchase order.xlsm";
                    if (!File.Exists(filePath))
                    {
                        MessageBox.Show($"File not found:\n{filePath}", "File Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        var excelApp = new Excel.Application();
                        excelApp.Visible = true;
                        var workbooks = excelApp.Workbooks;
                        var workbook = workbooks.Open(filePath);

                        Excel.Worksheet worksheet = workbook.Sheets["Purchase Orders"] as Excel.Worksheet;
                        int lastRow = worksheet.Cells[worksheet.Rows.Count, 1].End(Excel.XlDirection.xlUp).Row + 1;

                        worksheet.Cells[lastRow, 1].Value = record.ProjectName;
                        worksheet.Cells[lastRow, 2].Value = record.JobNo;

                        Marshal.ReleaseComObject(worksheet);
                        Marshal.ReleaseComObject(workbook);
                        Marshal.ReleaseComObject(workbooks);
                        Marshal.ReleaseComObject(excelApp);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error writing to Excel file:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

        private void btnSetPrintArea_Click(object sender, EventArgs e)
        {
            isSelectingPrintArea = true;
            selectedPrintCells.Clear();

            dgvSchedule.SelectionMode = DataGridViewSelectionMode.CellSelect; // ✅ allows multi-cell drag
            dgvSchedule.MultiSelect = true;

            dgvSchedule.ClearSelection();
            dgvSchedule.CurrentCell = null;

            dashOffset = 0f;
            antTimer.Start();

            dgvSchedule.Invalidate();
        }

        private void ExitPrintAreaMode()
        {
            isSelectingPrintArea = false;
            antTimer.Stop();
            dgvSchedule.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSchedule.ClearSelection();
            dgvSchedule.Invalidate();
        }

        private void dgvSchedule_Paint(object sender, PaintEventArgs e)
        {
            if (!isSelectingPrintArea || dgvSchedule.SelectedCells.Count == 0) return;

            var selectedCells = dgvSchedule.SelectedCells
                .Cast<DataGridViewCell>()
                .ToList();

            int minRow = selectedCells.Min(c => c.RowIndex);
            int maxRow = selectedCells.Max(c => c.RowIndex);
            int minCol = selectedCells.Min(c => c.ColumnIndex);
            int maxCol = selectedCells.Max(c => c.ColumnIndex);

            System.Drawing.Rectangle topLeft = dgvSchedule.GetCellDisplayRectangle(minCol, minRow, true);
            System.Drawing.Rectangle bottomRight = dgvSchedule.GetCellDisplayRectangle(maxCol, maxRow, true);

            if (topLeft.IsEmpty || bottomRight.IsEmpty)
                return;

            System.Drawing.Rectangle borderRect = new System.Drawing.Rectangle(
                topLeft.X,
                topLeft.Y,
                bottomRight.Right - topLeft.Left - 1,
                bottomRight.Bottom - topLeft.Top - 1
            );

            using (Pen pinkPen = new Pen(Color.DeepPink, 2))
            {
                pinkPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                pinkPen.DashOffset = dashOffset; // animate here
                e.Graphics.DrawRectangle(pinkPen, borderRect);
            }
        }
        private void FrmViewDispatch_KeyDown(object sender, KeyEventArgs e)
        {
            if (isSelectingPrintArea && e.KeyCode == Keys.Escape)
            {
                ExitPrintAreaMode();
                e.Handled = true;
            }
        }
        private void FrmViewDispatch_MouseDown(object sender, MouseEventArgs e)
        {
            if (isSelectingPrintArea)
            {
                // Check if clicked outside the DataGridView
                var mousePos = dgvSchedule.PointToClient(Cursor.Position);
                if (!dgvSchedule.ClientRectangle.Contains(mousePos))
                {
                    ExitPrintAreaMode();
                }
            }
        }

        private void dgvSchedule_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // === HEADER STYLE: Vertical Header Font and Text Formatting ===
            if (e.RowIndex == -1 && e.ColumnIndex >= 0)
            {
                e.PaintBackground(e.CellBounds, false);

                string headerText = e.FormattedValue?.ToString() ?? "";

                // Save original settings to restore later
                var oldHint = e.Graphics.TextRenderingHint;
                var oldOffset = e.Graphics.PixelOffsetMode;

                // Apply crisp text settings just for header
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using (System.Drawing.Font font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold))
                using (System.Drawing.Brush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                using (System.Drawing.StringFormat format = new System.Drawing.StringFormat())
                {
                    format.Alignment = System.Drawing.StringAlignment.Near;     // Left aligned (after rotate)
                    format.LineAlignment = System.Drawing.StringAlignment.Center; // Vertically centered

                    // Rotate origin to bottom-left of cell
                    e.Graphics.TranslateTransform(e.CellBounds.Left, e.CellBounds.Bottom);
                    e.Graphics.RotateTransform(-90);

                    System.Drawing.RectangleF rect = new System.Drawing.RectangleF(0, 0, e.CellBounds.Height, e.CellBounds.Width);
                    e.Graphics.DrawString(headerText, font, brush, rect, format);

                    e.Graphics.ResetTransform();
                }

                // Restore original graphics settings
                e.Graphics.TextRenderingHint = oldHint;
                e.Graphics.PixelOffsetMode = oldOffset;

                e.Handled = true;
                return;
            }

            // === HIDE CHECKBOXES for DispatchBlankRow in boolean columns ===
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var row = dgvSchedule.Rows[e.RowIndex];
                string colName = dgvSchedule.Columns[e.ColumnIndex].Name;

                string[] checkboxCols = {
            "FB", "EB", "ASS", "FlatBedColour", "EdgeColour",
            "PreAssemble", "CarcassAssemble", "FitOut", "Stacked"
        };

                if (row.DataBoundItem is DispatchBlankRow && checkboxCols.Contains(colName))
                {
                    // Only draw background — skip the checkbox rendering
                    e.PaintBackground(e.ClipBounds, true);
                    e.Handled = true;
                }
            }
        }


        //private void dgvSchedule_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        //{
        //    // === HEADER STYLE: Vertical Header Font and Text Formatting ===
        //    if (e.RowIndex == -1 && e.ColumnIndex >= 0)
        //    {
        //        e.PaintBackground(e.CellBounds, false);

        //        string headerText = e.FormattedValue?.ToString() ?? "";

        //        // Save original settings to restore later
        //        var oldHint = e.Graphics.TextRenderingHint;
        //        var oldOffset = e.Graphics.PixelOffsetMode;

        //        // Apply crisp text settings just for header
        //        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        //        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        //        using (System.Drawing.Font font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold))
        //        using (System.Drawing.Brush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
        //        using (System.Drawing.StringFormat format = new System.Drawing.StringFormat())
        //        {
        //            format.Alignment = System.Drawing.StringAlignment.Near;     // Left aligned (after rotate)
        //            format.LineAlignment = System.Drawing.StringAlignment.Center; // Vertically centered

        //            // Rotate origin to bottom-left of cell
        //            e.Graphics.TranslateTransform(e.CellBounds.Left, e.CellBounds.Bottom);
        //            e.Graphics.RotateTransform(-90);

        //            System.Drawing.RectangleF rect = new System.Drawing.RectangleF(0, 0, e.CellBounds.Height, e.CellBounds.Width);
        //            e.Graphics.DrawString(headerText, font, brush, rect, format);

        //            e.Graphics.ResetTransform();
        //        }

        //        // Restore original graphics settings
        //        e.Graphics.TextRenderingHint = oldHint;
        //        e.Graphics.PixelOffsetMode = oldOffset;

        //        e.Handled = true;
        //    }
        //}






        //private void dgvSchedule_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        //{
        //    if (e.RowIndex == -1 && e.ColumnIndex >= 0)
        //    {
        //        e.PaintBackground(e.CellBounds, false);

        //        string headerText = e.FormattedValue?.ToString() ?? "";
        //        using (Brush brush = new SolidBrush(e.CellStyle.ForeColor))
        //        using (StringFormat format = new StringFormat())
        //        {
        //            format.Alignment = StringAlignment.Near; // Left aligned
        //            format.LineAlignment = StringAlignment.Center; // Vertically centered

        //            // Rotate origin to bottom-left of cell
        //            e.Graphics.TranslateTransform(e.CellBounds.Left, e.CellBounds.Bottom);
        //            e.Graphics.RotateTransform(-90);

        //            RectangleF rect = new RectangleF(0, 0, e.CellBounds.Height, e.CellBounds.Width);

        //            e.Graphics.DrawString(
        //                headerText,
        //                e.CellStyle.Font,
        //                brush,
        //                rect,
        //                format
        //            );

        //            e.Graphics.ResetTransform();
        //        }

        //        e.Handled = true;
        //    }
        //}
        private void ExportSelectionToExcel()
        {
            if (dgvSchedule.SelectedCells.Count == 0)
            {
                MessageBox.Show("No cells selected to export.");
                return;
            }

            var visibleSelectedCells = dgvSchedule.SelectedCells
                .Cast<DataGridViewCell>()
                .Where(c => dgvSchedule.Columns[c.ColumnIndex].Visible)
                .OrderBy(c => dgvSchedule.Columns[c.ColumnIndex].DisplayIndex)
                .ThenBy(c => c.RowIndex)
                .ToList();

            if (!visibleSelectedCells.Any())
            {
                MessageBox.Show("No visible selected cells to export.");
                return;
            }

            var groupedRows = visibleSelectedCells
                .GroupBy(c => c.RowIndex)
                .OrderBy(g => g.Key);

            string tempPath = Path.GetTempPath();
            string excelPath = Path.Combine(tempPath, "Dispatch_Print.xlsx");

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("PrintArea");

                var colIndexes = visibleSelectedCells
                    .Select(c => c.ColumnIndex)
                    .Distinct()
                    .OrderBy(c => dgvSchedule.Columns[c].DisplayIndex)
                    .ToList();

                for (int i = 0; i < colIndexes.Count; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = " " + dgvSchedule.Columns[colIndexes[i]].HeaderText;
                    cell.Style.Alignment.TextRotation = 90;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(100, 100, 100);
                }

                int rowOffset = 2;
                foreach (var rowGroup in groupedRows)
                {
                    // Check if this is a weekly total row (ProjectColour contains "Total for Week")
                    bool isWeeklyTotalRow = rowGroup.Any(c =>
                    {
                        var col = dgvSchedule.Columns[c.ColumnIndex];
                        return col.Name == "ProjectColour" && (c.Value?.ToString()?.ToLower().Contains("total for week") ?? false);
                    });

                    for (int i = 0; i < colIndexes.Count; i++)
                    {
                        var cell = rowGroup.FirstOrDefault(c => c.ColumnIndex == colIndexes[i]);
                        if (cell != null)
                        {
                            var value = cell.Value;
                            var xlCell = ws.Cell(rowOffset, i + 1);
                            string colName = dgvSchedule.Columns[cell.ColumnIndex].Name;

                            if (isWeeklyTotalRow)
                            {
                                if (colName == "Qty" || colName == "ProjectColour" || colName == "Amount")
                                {
                                    xlCell.Value = value?.ToString() ?? "";
                                }
                                else
                                {
                                    xlCell.Value = value?.ToString() ?? "";
                                    xlCell.Style.Font.FontColor = XLColor.LightGray;
                                }

                                // Vertical border logic for total rows
                                bool isFirst = (i == 0);
                                bool isLast = (i == colIndexes.Count - 1);
                                bool isKeyColumn = (colName == "Qty" || colName == "ProjectColour" || colName == "Amount");

                                // Top/Bottom borders always applied
                                xlCell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
                                xlCell.Style.Border.TopBorderColor = XLColor.FromArgb(100, 100, 100);
                                xlCell.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                                xlCell.Style.Border.BottomBorderColor = XLColor.FromArgb(100, 100, 100);

                                // Conditionally apply Left/Right borders
                                xlCell.Style.Border.LeftBorder = (isFirst || isKeyColumn) ? XLBorderStyleValues.Hair : XLBorderStyleValues.None;
                                xlCell.Style.Border.LeftBorderColor = XLColor.FromArgb(100, 100, 100);
                                xlCell.Style.Border.RightBorder = (isLast || isKeyColumn) ? XLBorderStyleValues.Hair : XLBorderStyleValues.None;
                                xlCell.Style.Border.RightBorderColor = XLColor.FromArgb(100, 100, 100);
                            }
                            else
                            {
                                if (colName == "DispatchDate" && DateTime.TryParse(value?.ToString(), out DateTime dt))
                                {
                                    xlCell.Value = dt;
                                    xlCell.Style.DateFormat.Format = "dd-mmm";
                                }
                                else if (colName == "FB" || colName == "EB" || colName == "ASS" ||
                                         colName == "FlatBedColour" || colName == "EdgeColour" ||
                                         colName == "PreAssemble" || colName == "CarcassAssemble" ||
                                         colName == "Invoiced" || colName == "Stacked")

                                {
                                    xlCell.Value = value != null && value.ToString().ToLower() == "true" ? "✓" : "";
                                }
                                else
                                {
                                    xlCell.Value = value?.ToString() ?? "";
                                }

                                xlCell.Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                                xlCell.Style.Border.OutsideBorderColor = XLColor.FromArgb(100, 100, 100);
                            }

                            // Background color
                            var backColor = cell.Style.BackColor;
                            if (backColor.A > 0)
                            {
                                xlCell.Style.Fill.BackgroundColor = XLColor.FromColor(backColor);
                            }

                            // Alignment
                            xlCell.Style.Alignment.Horizontal =
                                cell.Style.Alignment == DataGridViewContentAlignment.MiddleRight ? XLAlignmentHorizontalValues.Right :
                                cell.Style.Alignment == DataGridViewContentAlignment.MiddleCenter ? XLAlignmentHorizontalValues.Center :
                                XLAlignmentHorizontalValues.Left;
                        }
                    }
                    rowOffset++;
                }

                for (int i = 0; i < colIndexes.Count; i++)
                {
                    int dgvWidth = dgvSchedule.Columns[colIndexes[i]].Width;
                    double excelWidth = dgvWidth * 0.142;
                    ws.Column(i + 1).Width = excelWidth;
                }

                workbook.SaveAs(excelPath);
            }

            string pdfPath = Path.Combine(Path.GetTempPath(), "Dispatch_Print.pdf");
            ConvertExcelToPdf(excelPath, pdfPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = pdfPath,
                UseShellExecute = true
            });
        }
        private void ConvertExcelToPdf(string excelPath, string pdfPath)
        {
            var excelApp = new Microsoft.Office.Interop.Excel.Application();
            Workbook workbook = null;

            try
            {
                workbook = excelApp.Workbooks.Open(excelPath);
                var sheet = (Microsoft.Office.Interop.Excel.Worksheet)workbook.Sheets[1];

                // Check if A4 is supported by the default printer
                bool a4Supported = false;
                PrinterSettings settings = new PrinterSettings();
                foreach (PaperSize size in settings.PaperSizes)
                {
                    if (size.Kind == PaperKind.A4)
                    {
                        a4Supported = true;
                        break;
                    }
                }

                // Only set to A4 if supported
                if (a4Supported)
                {
                    sheet.PageSetup.PaperSize = Microsoft.Office.Interop.Excel.XlPaperSize.xlPaperA4;
                }

                sheet.PageSetup.Orientation = Microsoft.Office.Interop.Excel.XlPageOrientation.xlLandscape;

                // Margins (5mm = 14.17 points)
                float marginInPoints = 14.17f;
                sheet.PageSetup.TopMargin = marginInPoints;
                sheet.PageSetup.BottomMargin = marginInPoints;
                sheet.PageSetup.LeftMargin = marginInPoints;
                sheet.PageSetup.RightMargin = marginInPoints;

                // Fit to one page wide
                sheet.PageSetup.FitToPagesWide = 1;
                sheet.PageSetup.FitToPagesTall = false;

                // Convert to PDF
                workbook.ExportAsFixedFormat(
                    Microsoft.Office.Interop.Excel.XlFixedFormatType.xlTypePDF,
                    pdfPath);

                workbook.Close(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to convert to PDF: " + ex.Message);
            }
            finally
            {
                if (workbook != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                }

                excelApp.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
            }
        }



        public static class PrinterHelper
        {
            [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool SetDefaultPrinter(string Name);

            public static string GetDefaultPrinter()
            {
                string defaultPrinter = "";
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer WHERE Default = true"))
                {
                    foreach (ManagementObject printer in searcher.Get())
                    {
                        defaultPrinter = printer["Name"].ToString();
                        break;
                    }
                }
                return defaultPrinter;
            }
        }
        private string SetTemporaryPrinter(string preferredPrinter = "Microsoft XPS Document Writer")
        {
            var excelApp = new Microsoft.Office.Interop.Excel.Application();
            string originalPrinter = excelApp.ActivePrinter;

            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                if (printer.Contains(preferredPrinter))
                {
                    excelApp.ActivePrinter = printer;
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                    return originalPrinter; // Save old printer so we can restore it later
                }
            }

            // If preferred printer not found, return original
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
            return originalPrinter;
        }


        private void btnPrint_Click(object sender, EventArgs e)
        {
            ExportSelectionToExcel();
            ExitPrintAreaMode();
        }
    }
}


