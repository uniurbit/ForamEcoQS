namespace ForamEcoQS
{
    partial class FambiResultsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FambiResultsForm));
            dataGridFAMBI = new DataGridView();
            menuStrip1 = new MenuStrip();
            saveToolStripMenuItem = new ToolStripMenuItem();
            plotToolStripMenuItem = new ToolStripMenuItem();
            ecoGroupsPlotToolStripMenuItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)dataGridFAMBI).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dataGridFAMBI
            // 
            dataGridFAMBI.AllowUserToAddRows = false;
            dataGridFAMBI.AllowUserToDeleteRows = false;
            dataGridFAMBI.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridFAMBI.Dock = DockStyle.Fill;
            dataGridFAMBI.Location = new Point(0, 28);
            dataGridFAMBI.Name = "dataGridFAMBI";
            dataGridFAMBI.ReadOnly = true;
            dataGridFAMBI.RowHeadersWidth = 51;
            dataGridFAMBI.RowTemplate.Height = 29;
            dataGridFAMBI.Size = new Size(1014, 620);
            dataGridFAMBI.TabIndex = 0;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { saveToolStripMenuItem, plotToolStripMenuItem, ecoGroupsPlotToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.RenderMode = ToolStripRenderMode.System;
            menuStrip1.Size = new Size(1014, 28);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(54, 24);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // plotToolStripMenuItem
            // 
            plotToolStripMenuItem.Name = "plotToolStripMenuItem";
            plotToolStripMenuItem.Size = new Size(74, 24);
            plotToolStripMenuItem.Text = "BoxPlot";
            plotToolStripMenuItem.Click += plotToolStripMenuItem_Click;
            // 
            // ecoGroupsPlotToolStripMenuItem
            // 
            ecoGroupsPlotToolStripMenuItem.Name = "ecoGroupsPlotToolStripMenuItem";
            ecoGroupsPlotToolStripMenuItem.Size = new Size(124, 24);
            ecoGroupsPlotToolStripMenuItem.Text = "EcoGroups Plot";
            ecoGroupsPlotToolStripMenuItem.Click += ecoGroupsPlotToolStripMenuItem_Click;
            // 
            // FambiResultsForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1014, 648);
            Controls.Add(dataGridFAMBI);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "FambiResultsForm";
            Text = "FAMBI";
            Load += FambiResultsForm_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridFAMBI).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        public DataGridView dataGridFAMBI;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem plotToolStripMenuItem;
        private ToolStripMenuItem ecoGroupsPlotToolStripMenuItem;
    }
}