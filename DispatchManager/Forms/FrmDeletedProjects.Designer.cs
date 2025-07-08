namespace DispatchManager.Forms
{
    partial class FrmDeletedProjects
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
            this.dgvDeleted = new System.Windows.Forms.DataGridView();
            this.menuDeleted = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuRevertProject = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeleted)).BeginInit();
            this.menuDeleted.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvDeleted
            // 
            this.dgvDeleted.AllowUserToAddRows = false;
            this.dgvDeleted.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDeleted.ContextMenuStrip = this.menuDeleted;
            this.dgvDeleted.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDeleted.Location = new System.Drawing.Point(0, 0);
            this.dgvDeleted.Name = "dgvDeleted";
            this.dgvDeleted.ReadOnly = true;
            this.dgvDeleted.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDeleted.Size = new System.Drawing.Size(2354, 661);
            this.dgvDeleted.TabIndex = 0;
            // 
            // menuDeleted
            // 
            this.menuDeleted.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuRevertProject});
            this.menuDeleted.Name = "menuDeleted";
            this.menuDeleted.Size = new System.Drawing.Size(181, 48);
            // 
            // menuRevertProject
            // 
            this.menuRevertProject.Name = "menuRevertProject";
            this.menuRevertProject.Size = new System.Drawing.Size(180, 22);
            this.menuRevertProject.Text = "Revert Project";
            this.menuRevertProject.Click += new System.EventHandler(this.menuRevertProject_Click);
            // 
            // FrmDeletedProjects
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2354, 661);
            this.Controls.Add(this.dgvDeleted);
            this.Name = "FrmDeletedProjects";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FrmDeletedProjects";
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeleted)).EndInit();
            this.menuDeleted.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvDeleted;
        private System.Windows.Forms.ContextMenuStrip menuDeleted;
        private System.Windows.Forms.ToolStripMenuItem menuRevertProject;
    }
}