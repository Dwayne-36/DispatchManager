namespace DispatchManager.Forms
{
    partial class FrmViewDispatch
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.dgvSchedule = new System.Windows.Forms.DataGridView();
            this.contextMenuGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.MenuPurchaseOrder = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCopyRow = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDeleteRow = new System.Windows.Forms.ToolStripMenuItem();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.lblFrom = new System.Windows.Forms.Label();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.lblTo = new System.Windows.Forms.Label();
            this.tbSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.lblTotal = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuOptions = new System.Windows.Forms.ToolStripMenuItem();
            this.menuEmployees = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuDeletedProjects = new System.Windows.Forms.ToolStripMenuItem();
            this.newProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.printToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setPrintAreaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.printToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.lblSelectionTotal = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSchedule)).BeginInit();
            this.contextMenuGrid.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvSchedule
            // 
            this.dgvSchedule.AllowUserToAddRows = false;
            this.dgvSchedule.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSchedule.ContextMenuStrip = this.contextMenuGrid;
            this.dgvSchedule.Location = new System.Drawing.Point(1, 134);
            this.dgvSchedule.Name = "dgvSchedule";
            this.dgvSchedule.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSchedule.Size = new System.Drawing.Size(1154, 414);
            this.dgvSchedule.TabIndex = 0;
            // 
            // contextMenuGrid
            // 
            this.contextMenuGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuPurchaseOrder,
            this.menuCopyRow,
            this.menuDeleteRow});
            this.contextMenuGrid.Name = "contextMenuGrid";
            this.contextMenuGrid.Size = new System.Drawing.Size(156, 70);
            // 
            // MenuPurchaseOrder
            // 
            this.MenuPurchaseOrder.Name = "MenuPurchaseOrder";
            this.MenuPurchaseOrder.Size = new System.Drawing.Size(155, 22);
            this.MenuPurchaseOrder.Text = "Purchase Order";
            this.MenuPurchaseOrder.Click += new System.EventHandler(this.MenuPurchaseOrder_Click);
            // 
            // menuCopyRow
            // 
            this.menuCopyRow.Name = "menuCopyRow";
            this.menuCopyRow.Size = new System.Drawing.Size(155, 22);
            this.menuCopyRow.Text = "Copy Row";
            this.menuCopyRow.Click += new System.EventHandler(this.menuCopyRow_Click);
            // 
            // menuDeleteRow
            // 
            this.menuDeleteRow.Name = "menuDeleteRow";
            this.menuDeleteRow.Size = new System.Drawing.Size(155, 22);
            this.menuDeleteRow.Text = "Deleted Row";
            this.menuDeleteRow.Click += new System.EventHandler(this.menuDeleteRow_Click);
            // 
            // dtpFrom
            // 
            this.dtpFrom.Location = new System.Drawing.Point(86, 55);
            this.dtpFrom.Name = "dtpFrom";
            this.dtpFrom.Size = new System.Drawing.Size(200, 20);
            this.dtpFrom.TabIndex = 1;
            // 
            // lblFrom
            // 
            this.lblFrom.AutoSize = true;
            this.lblFrom.Location = new System.Drawing.Point(5, 59);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Size = new System.Drawing.Size(66, 13);
            this.lblFrom.TabIndex = 2;
            this.lblFrom.Text = "Select From:";
            // 
            // dtpTo
            // 
            this.dtpTo.Location = new System.Drawing.Point(86, 82);
            this.dtpTo.Name = "dtpTo";
            this.dtpTo.Size = new System.Drawing.Size(200, 20);
            this.dtpTo.TabIndex = 3;
            // 
            // lblTo
            // 
            this.lblTo.AutoSize = true;
            this.lblTo.Location = new System.Drawing.Point(15, 86);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(56, 13);
            this.lblTo.TabIndex = 4;
            this.lblTo.Text = "Select To:";
            // 
            // tbSearch
            // 
            this.tbSearch.Location = new System.Drawing.Point(86, 108);
            this.tbSearch.Name = "tbSearch";
            this.tbSearch.Size = new System.Drawing.Size(200, 20);
            this.tbSearch.TabIndex = 5;
            this.tbSearch.TextChanged += new System.EventHandler(this.tbSearch_TextChanged);
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(27, 113);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(44, 13);
            this.lblSearch.TabIndex = 6;
            this.lblSearch.Text = "Search:";
            // 
            // lblTotal
            // 
            this.lblTotal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTotal.AutoSize = true;
            this.lblTotal.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotal.ForeColor = System.Drawing.Color.Red;
            this.lblTotal.Location = new System.Drawing.Point(970, 560);
            this.lblTotal.Name = "lblTotal";
            this.lblTotal.Size = new System.Drawing.Size(45, 17);
            this.lblTotal.TabIndex = 10;
            this.lblTotal.Text = "Total";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuOptions,
            this.newProjectToolStripMenuItem,
            this.printToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(8, 28);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(196, 24);
            this.menuStrip1.TabIndex = 11;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuOptions
            // 
            this.menuOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuEmployees,
            this.MenuDeletedProjects});
            this.menuOptions.Name = "menuOptions";
            this.menuOptions.Size = new System.Drawing.Size(61, 20);
            this.menuOptions.Text = "Options";
            // 
            // menuEmployees
            // 
            this.menuEmployees.Name = "menuEmployees";
            this.menuEmployees.Size = new System.Drawing.Size(159, 22);
            this.menuEmployees.Text = "Employees";
            this.menuEmployees.Click += new System.EventHandler(this.menuEmployees_Click);
            // 
            // MenuDeletedProjects
            // 
            this.MenuDeletedProjects.Name = "MenuDeletedProjects";
            this.MenuDeletedProjects.Size = new System.Drawing.Size(159, 22);
            this.MenuDeletedProjects.Text = "Deleted Projects";
            this.MenuDeletedProjects.Click += new System.EventHandler(this.MenuDeletedProjects_Click);
            // 
            // newProjectToolStripMenuItem
            // 
            this.newProjectToolStripMenuItem.Name = "newProjectToolStripMenuItem";
            this.newProjectToolStripMenuItem.Size = new System.Drawing.Size(83, 20);
            this.newProjectToolStripMenuItem.Text = "New Project";
            this.newProjectToolStripMenuItem.Click += new System.EventHandler(this.newProjectToolStripMenuItem_Click);
            // 
            // printToolStripMenuItem
            // 
            this.printToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setPrintAreaToolStripMenuItem,
            this.printToolStripMenuItem1});
            this.printToolStripMenuItem.Name = "printToolStripMenuItem";
            this.printToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.printToolStripMenuItem.Text = "Print";
            // 
            // setPrintAreaToolStripMenuItem
            // 
            this.setPrintAreaToolStripMenuItem.Name = "setPrintAreaToolStripMenuItem";
            this.setPrintAreaToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.setPrintAreaToolStripMenuItem.Text = "Set Print Area";
            this.setPrintAreaToolStripMenuItem.Click += new System.EventHandler(this.setPrintAreaToolStripMenuItem_Click);
            // 
            // printToolStripMenuItem1
            // 
            this.printToolStripMenuItem1.Name = "printToolStripMenuItem1";
            this.printToolStripMenuItem1.Size = new System.Drawing.Size(145, 22);
            this.printToolStripMenuItem1.Text = "Print";
            this.printToolStripMenuItem1.Click += new System.EventHandler(this.printToolStripMenuItem1_Click);
            // 
            // lblSelectionTotal
            // 
            this.lblSelectionTotal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSelectionTotal.AutoSize = true;
            this.lblSelectionTotal.Location = new System.Drawing.Point(842, 108);
            this.lblSelectionTotal.Name = "lblSelectionTotal";
            this.lblSelectionTotal.Size = new System.Drawing.Size(288, 13);
            this.lblSelectionTotal.TabIndex = 15;
            this.lblSelectionTotal.Text = "lblSelectionTotal.Text = \"Selection Total: $0.00 | M3: 0.00\";";
            // 
            // FrmViewDispatch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1155, 600);
            this.Controls.Add(this.lblSelectionTotal);
            this.Controls.Add(this.lblTotal);
            this.Controls.Add(this.lblSearch);
            this.Controls.Add(this.tbSearch);
            this.Controls.Add(this.lblTo);
            this.Controls.Add(this.dtpTo);
            this.Controls.Add(this.lblFrom);
            this.Controls.Add(this.dtpFrom);
            this.Controls.Add(this.dgvSchedule);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FrmViewDispatch";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Haylo View";
            this.Controls.SetChildIndex(this.menuStrip1, 0);
            this.Controls.SetChildIndex(this.dgvSchedule, 0);
            this.Controls.SetChildIndex(this.dtpFrom, 0);
            this.Controls.SetChildIndex(this.lblFrom, 0);
            this.Controls.SetChildIndex(this.dtpTo, 0);
            this.Controls.SetChildIndex(this.lblTo, 0);
            this.Controls.SetChildIndex(this.tbSearch, 0);
            this.Controls.SetChildIndex(this.lblSearch, 0);
            this.Controls.SetChildIndex(this.lblTotal, 0);
            this.Controls.SetChildIndex(this.lblSelectionTotal, 0);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSchedule)).EndInit();
            this.contextMenuGrid.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvSchedule;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.TextBox tbSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.Label lblTotal;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuOptions;
        private System.Windows.Forms.ToolStripMenuItem menuEmployees;
        private System.Windows.Forms.ToolStripMenuItem MenuDeletedProjects;
        private System.Windows.Forms.ContextMenuStrip contextMenuGrid;
        private System.Windows.Forms.ToolStripMenuItem menuDeleteRow;
        private System.Windows.Forms.ToolStripMenuItem menuCopyRow;
        private System.Windows.Forms.ToolStripMenuItem MenuPurchaseOrder;
        private System.Windows.Forms.Label lblSelectionTotal;
        private System.Windows.Forms.ToolStripMenuItem newProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem printToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setPrintAreaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem printToolStripMenuItem1;
    }
}