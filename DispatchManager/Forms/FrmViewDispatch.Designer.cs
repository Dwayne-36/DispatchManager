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
            this.dgvDispatch = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDispatch)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvDispatch
            // 
            this.dgvDispatch.AllowUserToAddRows = false;
            this.dgvDispatch.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDispatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDispatch.Location = new System.Drawing.Point(0, 0);
            this.dgvDispatch.Name = "dgvDispatch";
            this.dgvDispatch.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDispatch.Size = new System.Drawing.Size(800, 450);
            this.dgvDispatch.TabIndex = 0;
            // 
            // FrmViewDispatch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.dgvDispatch);
            this.Name = "FrmViewDispatch";
            this.Text = "FrmViewDispatch";
            ((System.ComponentModel.ISupportInitialize)(this.dgvDispatch)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvDispatch;
    }
}