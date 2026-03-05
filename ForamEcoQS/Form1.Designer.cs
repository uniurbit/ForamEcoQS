namespace ForamEcoQS
{
    partial class ForamEcoQS
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ForamEcoQS));
            dataGridView1 = new DataGridView();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            newToolStripMenuItem = new ToolStripMenuItem();
            newEmptyDatasetToolStripMenuItem = new ToolStripMenuItem();
            clearWorkspaceToolStripMenuItem = new ToolStripMenuItem();
            createTemplateFromDatabankToolStripMenuItem = new ToolStripMenuItem();
            createFSITemplateToolStripMenuItem = new ToolStripMenuItem();
            createTSIMedTemplateToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            loadFAMBIDataToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            editDataToolStripMenuItem = new ToolStripMenuItem();
            newSampleToolStripMenuItem = new ToolStripMenuItem();
            removeSelectedSampleToolStripMenuItem = new ToolStripMenuItem();
            renameSelectedSampleToolStripMenuItem = new ToolStripMenuItem();
            undoToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            groupBox1 = new GroupBox();
            showDatabankButton = new Button();
            comboBox1 = new ComboBox();
            compareDatabankButton = new Button();
            groupBox2 = new GroupBox();
            label2 = new Label();
            listBox1 = new ListBox();
            label1 = new Label();
            calculateFambiButton = new Button();
            btnCalcFSI = new Button();
            btnCalcNQI = new Button();
            btnCalcExpH = new Button();
            cleanNormalizeButton = new Button();
            exportStatsButton = new Button();
            overrideClassificationCheckBox = new CheckBox();
            groupBox3 = new GroupBox();
            compositePlotButton = new Button();
            plotIndicesButton = new Button();
            advancedIndicesButton = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            menuStrip1.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(12, 63);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.RowTemplate.Height = 29;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.Size = new Size(1136, 705);
            dataGridView1.TabIndex = 0;
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = SystemColors.Control;
            menuStrip1.ImageScalingSize = new Size(20, 20);
            advancedIndicesToolStripMenuItem = new ToolStripMenuItem();
            advancedIndicesToolStripMenuItem.Name = "advancedIndicesToolStripMenuItem";
            advancedIndicesToolStripMenuItem.Size = new Size(140, 24);
            advancedIndicesToolStripMenuItem.Text = "Advanced Indices";
            advancedIndicesToolStripMenuItem.Click += advancedIndicesToolStripMenuItem_Click;
            advancedIndicesToolStripMenuItem.Enabled = false;
            toolsToolStripMenuItem = new ToolStripMenuItem();
            fsiDatabankManagerToolStripMenuItem = new ToolStripMenuItem();
            foramAMBIDatabankManagerToolStripMenuItem = new ToolStripMenuItem();
            geographicAreasDatabankToolStripMenuItem = new ToolStripMenuItem();
            indexSettingsToolStripMenuItem = new ToolStripMenuItem();
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editDataToolStripMenuItem, undoToolStripMenuItem, advancedIndicesToolStripMenuItem, toolsToolStripMenuItem, aboutToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.RenderMode = ToolStripRenderMode.System;
            menuStrip1.Size = new Size(1447, 28);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newToolStripMenuItem, createTemplateFromDatabankToolStripMenuItem, createFSITemplateToolStripMenuItem, createTSIMedTemplateToolStripMenuItem, openToolStripMenuItem, loadFAMBIDataToolStripMenuItem, saveToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "File";
            //
            // newToolStripMenuItem
            //
            newToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newEmptyDatasetToolStripMenuItem, clearWorkspaceToolStripMenuItem });
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.Size = new Size(312, 26);
            newToolStripMenuItem.Text = "New";
            //
            // newEmptyDatasetToolStripMenuItem
            //
            newEmptyDatasetToolStripMenuItem.Name = "newEmptyDatasetToolStripMenuItem";
            newEmptyDatasetToolStripMenuItem.Size = new Size(194, 26);
            newEmptyDatasetToolStripMenuItem.Text = "Empty Dataset";
            newEmptyDatasetToolStripMenuItem.Click += newEmptyDatasetToolStripMenuItem_Click;
            //
            // clearWorkspaceToolStripMenuItem
            //
            clearWorkspaceToolStripMenuItem.Name = "clearWorkspaceToolStripMenuItem";
            clearWorkspaceToolStripMenuItem.Size = new Size(194, 26);
            clearWorkspaceToolStripMenuItem.Text = "Clear Workspace";
            clearWorkspaceToolStripMenuItem.Click += newToolStripMenuItem_Click;
            //
            // createTemplateFromDatabankToolStripMenuItem
            //
            createTemplateFromDatabankToolStripMenuItem.Name = "createTemplateFromDatabankToolStripMenuItem";
            createTemplateFromDatabankToolStripMenuItem.Size = new Size(312, 26);
            createTemplateFromDatabankToolStripMenuItem.Text = "Create Template from Databank";
            createTemplateFromDatabankToolStripMenuItem.Click += createTemplateFromDatabankToolStripMenuItem_Click;
            //
            // createFSITemplateToolStripMenuItem
            //
            createFSITemplateToolStripMenuItem.Name = "createFSITemplateToolStripMenuItem";
            createFSITemplateToolStripMenuItem.Size = new Size(312, 26);
            createFSITemplateToolStripMenuItem.Text = "Create FSI Template";
            createFSITemplateToolStripMenuItem.Click += createFSITemplateToolStripMenuItem_Click;
            //
            // createTSIMedTemplateToolStripMenuItem
            //
            createTSIMedTemplateToolStripMenuItem.Name = "createTSIMedTemplateToolStripMenuItem";
            createTSIMedTemplateToolStripMenuItem.Size = new Size(312, 26);
            createTSIMedTemplateToolStripMenuItem.Text = "Create TSI-Med Template";
            createTSIMedTemplateToolStripMenuItem.Click += createTSIMedTemplateToolStripMenuItem_Click;
            //
            // openToolStripMenuItem
            //
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(312, 26);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            //
            // loadFAMBIDataToolStripMenuItem
            //
            loadFAMBIDataToolStripMenuItem.Name = "loadFAMBIDataToolStripMenuItem";
            loadFAMBIDataToolStripMenuItem.Size = new Size(312, 26);
            loadFAMBIDataToolStripMenuItem.Text = "Load Indices for Graphs...";
            loadFAMBIDataToolStripMenuItem.Click += loadFAMBIDataToolStripMenuItem_Click;
            //
            // saveToolStripMenuItem
            //
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(312, 26);
            saveToolStripMenuItem.Text = "Save Samples";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            //
            // exitToolStripMenuItem
            //
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(312, 26);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // editDataToolStripMenuItem
            // 
            editDataToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newSampleToolStripMenuItem, removeSelectedSampleToolStripMenuItem, renameSelectedSampleToolStripMenuItem });
            editDataToolStripMenuItem.Name = "editDataToolStripMenuItem";
            editDataToolStripMenuItem.Size = new Size(85, 24);
            editDataToolStripMenuItem.Text = "Edit Data";
            editDataToolStripMenuItem.Click += editDataToolStripMenuItem_Click;
            // 
            // newSampleToolStripMenuItem
            // 
            newSampleToolStripMenuItem.Name = "newSampleToolStripMenuItem";
            newSampleToolStripMenuItem.Size = new Size(261, 26);
            newSampleToolStripMenuItem.Text = "New Sample";
            newSampleToolStripMenuItem.Click += newSampleToolStripMenuItem_Click;
            // 
            // removeSelectedSampleToolStripMenuItem
            // 
            removeSelectedSampleToolStripMenuItem.Name = "removeSelectedSampleToolStripMenuItem";
            removeSelectedSampleToolStripMenuItem.Size = new Size(261, 26);
            removeSelectedSampleToolStripMenuItem.Text = "Remove Selected Sample";
            removeSelectedSampleToolStripMenuItem.Click += removeSelectedSampleToolStripMenuItem_Click;
            //
            // renameSelectedSampleToolStripMenuItem
            //
            renameSelectedSampleToolStripMenuItem.Name = "renameSelectedSampleToolStripMenuItem";
            renameSelectedSampleToolStripMenuItem.Size = new Size(261, 26);
            renameSelectedSampleToolStripMenuItem.Text = "Rename Selected Sample";
            renameSelectedSampleToolStripMenuItem.Click += renameSelectedSampleToolStripMenuItem_Click;
            // 
            // undoToolStripMenuItem
            // 
            undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            undoToolStripMenuItem.Size = new Size(59, 24);
            undoToolStripMenuItem.Text = "Undo";
            undoToolStripMenuItem.Click += undoToolStripMenuItem_Click;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(64, 24);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            //
            // toolsToolStripMenuItem
            //
            userCustomListsManagerToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fsiDatabankManagerToolStripMenuItem, foramAMBIDatabankManagerToolStripMenuItem, geographicAreasDatabankToolStripMenuItem, userCustomListsManagerToolStripMenuItem, new ToolStripSeparator(), indexSettingsToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(58, 24);
            toolsToolStripMenuItem.Text = "Tools";
            //
            // userCustomListsManagerToolStripMenuItem
            //
            userCustomListsManagerToolStripMenuItem.Name = "userCustomListsManagerToolStripMenuItem";
            userCustomListsManagerToolStripMenuItem.Size = new Size(280, 26);
            userCustomListsManagerToolStripMenuItem.Text = "User Custom Lists Manager...";
            userCustomListsManagerToolStripMenuItem.Click += userCustomListsManagerToolStripMenuItem_Click;
            //
            // fsiDatabankManagerToolStripMenuItem
            //
            fsiDatabankManagerToolStripMenuItem.Name = "fsiDatabankManagerToolStripMenuItem";
            fsiDatabankManagerToolStripMenuItem.Size = new Size(280, 26);
            fsiDatabankManagerToolStripMenuItem.Text = "FSI Databank Manager...";
            fsiDatabankManagerToolStripMenuItem.Click += fsiDatabankManagerToolStripMenuItem_Click;
            //
            // foramAMBIDatabankManagerToolStripMenuItem
            //
            foramAMBIDatabankManagerToolStripMenuItem.Name = "foramAMBIDatabankManagerToolStripMenuItem";
            foramAMBIDatabankManagerToolStripMenuItem.Size = new Size(280, 26);
            foramAMBIDatabankManagerToolStripMenuItem.Text = "Foram-AMBI Databank Manager...";
            foramAMBIDatabankManagerToolStripMenuItem.Click += foramAMBIDatabankManagerToolStripMenuItem_Click;
            //
            // geographicAreasDatabankToolStripMenuItem
            //
            geographicAreasDatabankToolStripMenuItem.Name = "geographicAreasDatabankToolStripMenuItem";
            geographicAreasDatabankToolStripMenuItem.Size = new Size(280, 26);
            geographicAreasDatabankToolStripMenuItem.Text = "Geographic Areas Database...";
            geographicAreasDatabankToolStripMenuItem.Click += geographicAreasDatabankToolStripMenuItem_Click;
            //
            // indexSettingsToolStripMenuItem
            //
            indexSettingsToolStripMenuItem.Name = "indexSettingsToolStripMenuItem";
            indexSettingsToolStripMenuItem.Size = new Size(280, 26);
            indexSettingsToolStripMenuItem.Text = "Index Calculation Settings...";
            indexSettingsToolStripMenuItem.Click += indexSettingsToolStripMenuItem_Click;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox1.Controls.Add(showDatabankButton);
            groupBox1.Controls.Add(comboBox1);
            groupBox1.Location = new Point(1163, 63);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(269, 119);
            groupBox1.TabIndex = 2;
            groupBox1.TabStop = false;
            groupBox1.Text = "Databank (Foram-AMBI)";
            // 
            // showDatabankButton
            //
            showDatabankButton.FlatStyle = FlatStyle.Flat;
            showDatabankButton.BackColor = Color.FromArgb(95, 158, 160);
            showDatabankButton.ForeColor = Color.White;
            showDatabankButton.Location = new Point(8, 72);
            showDatabankButton.Name = "showDatabankButton";
            showDatabankButton.Size = new Size(255, 33);
            showDatabankButton.TabIndex = 1;
            showDatabankButton.Text = "Show DataBank";
            showDatabankButton.Click += showDatabankButton_Click;
            // 
            // comboBox1
            // 
            comboBox1.FlatStyle = FlatStyle.Flat;
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(6, 35);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(257, 28);
            comboBox1.TabIndex = 0;
            // 
            // compareDatabankButton
            //
            compareDatabankButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            compareDatabankButton.FlatStyle = FlatStyle.Flat;
            compareDatabankButton.BackColor = Color.FromArgb(70, 130, 180);
            compareDatabankButton.ForeColor = Color.White;
            compareDatabankButton.Location = new Point(1220, 190);
            compareDatabankButton.Name = "compareDatabankButton";
            compareDatabankButton.Size = new Size(148, 39);
            compareDatabankButton.TabIndex = 3;
            compareDatabankButton.Text = "Compare";
            compareDatabankButton.Click += compareDatabankButton_Click;
            //
            // groupBox3
            //
            groupBox3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox3.Controls.Add(advancedIndicesButton);
            groupBox3.Controls.Add(plotIndicesButton);
            groupBox3.Controls.Add(compositePlotButton);
            groupBox3.Location = new Point(1163, 275);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(269, 180);
            groupBox3.TabIndex = 9;
            groupBox3.TabStop = false;
            groupBox3.Text = "Indices & Plots";
            //
            // advancedIndicesButton
            //
            advancedIndicesButton.BackColor = Color.FromArgb(46, 139, 87);
            advancedIndicesButton.Enabled = false;
            advancedIndicesButton.FlatStyle = FlatStyle.Flat;
            advancedIndicesButton.ForeColor = Color.White;
            advancedIndicesButton.Location = new Point(8, 30);
            advancedIndicesButton.Name = "advancedIndicesButton";
            advancedIndicesButton.Size = new Size(255, 35);
            advancedIndicesButton.TabIndex = 4;
            advancedIndicesButton.Text = "Calculate Indices";
            advancedIndicesButton.UseVisualStyleBackColor = false;
            advancedIndicesButton.Click += advancedIndicesButton_Click;
            //
            // plotIndicesButton
            //
            plotIndicesButton.BackColor = Color.FromArgb(70, 130, 180);
            plotIndicesButton.Enabled = false;
            plotIndicesButton.FlatStyle = FlatStyle.Flat;
            plotIndicesButton.ForeColor = Color.White;
            plotIndicesButton.Location = new Point(8, 75);
            plotIndicesButton.Name = "plotIndicesButton";
            plotIndicesButton.Size = new Size(255, 35);
            plotIndicesButton.TabIndex = 5;
            plotIndicesButton.Text = "Open Plot Options";
            plotIndicesButton.UseVisualStyleBackColor = false;
            plotIndicesButton.Click += plotIndicesButton_Click;
            //
            // compositePlotButton
            //
            compositePlotButton.BackColor = Color.FromArgb(100, 149, 237);
            compositePlotButton.Enabled = false;
            compositePlotButton.FlatStyle = FlatStyle.Flat;
            compositePlotButton.ForeColor = Color.White;
            compositePlotButton.Location = new Point(8, 120);
            compositePlotButton.Name = "compositePlotButton";
            compositePlotButton.Size = new Size(255, 35);
            compositePlotButton.TabIndex = 6;
            compositePlotButton.Text = "Open Composite Dashboard";
            compositePlotButton.UseVisualStyleBackColor = false;
            compositePlotButton.Click += compositePlotButton_Click;
            //
            // groupBox2
            //
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(listBox1);
            groupBox2.Controls.Add(label1);
            groupBox2.Location = new Point(1163, 470);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(269, 220);
            groupBox2.TabIndex = 4;
            groupBox2.TabStop = false;
            groupBox2.Text = "Statistics";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(5, 22);
            label2.Name = "label2";
            label2.Size = new Size(222, 20);
            label2.TabIndex = 2;
            label2.Text = "Click on a sample to see its stats";
            // 
            // listBox1
            // 
            listBox1.BorderStyle = BorderStyle.FixedSingle;
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 20;
            listBox1.Location = new Point(6, 47);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(247, 82);
            listBox1.TabIndex = 1;
            listBox1.Visible = false;
            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged_1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(15, 135);
            label1.Name = "label1";
            label1.Size = new Size(45, 20);
            label1.TabIndex = 0;
            label1.Text = "None";
            //
            // cleanNormalizeButton
            //
            cleanNormalizeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cleanNormalizeButton.FlatStyle = FlatStyle.Flat;
            cleanNormalizeButton.BackColor = Color.FromArgb(255, 165, 0);
            cleanNormalizeButton.ForeColor = Color.White;
            cleanNormalizeButton.Location = new Point(1171, 740);
            cleanNormalizeButton.Name = "cleanNormalizeButton";
            cleanNormalizeButton.Size = new Size(180, 35);
            cleanNormalizeButton.TabIndex = 6;
            cleanNormalizeButton.Text = "Clean and normalize";
            cleanNormalizeButton.Click += cleanNormalizeButton_Click;
            //
            // exportStatsButton
            //
            exportStatsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            exportStatsButton.Enabled = false;
            exportStatsButton.FlatStyle = FlatStyle.Flat;
            exportStatsButton.BackColor = Color.FromArgb(100, 149, 237);
            exportStatsButton.ForeColor = Color.White;
            exportStatsButton.Location = new Point(1171, 700);
            exportStatsButton.Name = "exportStatsButton";
            exportStatsButton.Size = new Size(180, 35);
            exportStatsButton.TabIndex = 7;
            exportStatsButton.Text = "Export Stats";
            exportStatsButton.Click += exportStatsButton_Click;
            //
            // overrideClassificationCheckBox
            //
            overrideClassificationCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            overrideClassificationCheckBox.AutoSize = true;
            overrideClassificationCheckBox.Location = new Point(1163, 235);
            overrideClassificationCheckBox.Name = "overrideClassificationCheckBox";
            overrideClassificationCheckBox.Size = new Size(180, 24);
            overrideClassificationCheckBox.TabIndex = 8;
            overrideClassificationCheckBox.Text = "Include unassigned taxa";
            overrideClassificationCheckBox.UseVisualStyleBackColor = true;
            // 
            // ForamEcoQS
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(1447, 780);
            Controls.Add(overrideClassificationCheckBox);
            Controls.Add(exportStatsButton);
            Controls.Add(cleanNormalizeButton);
            Controls.Add(groupBox2);
            Controls.Add(groupBox3);
            Controls.Add(compareDatabankButton);
            Controls.Add(groupBox1);
            Controls.Add(dataGridView1);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "ForamEcoQS";
            Text = "ForamEcoQS";
            WindowState = FormWindowState.Maximized;
            Load += ForamEcoQS_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dataGridView1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private ToolStripMenuItem newEmptyDatasetToolStripMenuItem;
        private ToolStripMenuItem clearWorkspaceToolStripMenuItem;
        private ToolStripMenuItem createTemplateFromDatabankToolStripMenuItem;
        private ToolStripMenuItem createFSITemplateToolStripMenuItem;
        private ToolStripMenuItem createTSIMedTemplateToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem editDataToolStripMenuItem;
        private ToolStripMenuItem newSampleToolStripMenuItem;
        private ToolStripMenuItem removeSelectedSampleToolStripMenuItem;
        private ToolStripMenuItem renameSelectedSampleToolStripMenuItem;
        private GroupBox groupBox1;
        private ComboBox comboBox1;
        private ToolStripMenuItem undoToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private Button showDatabankButton;
        private Button compareDatabankButton;
        private GroupBox groupBox2;
        private Label label1;
        private Label label2;
        private ListBox listBox1;
        private ToolStripMenuItem loadFAMBIDataToolStripMenuItem;
        private Button cleanNormalizeButton;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private Button exportStatsButton;
        private CheckBox overrideClassificationCheckBox;
        private GroupBox groupBox3;
        private Button compositePlotButton;
        private Button plotIndicesButton;
        private Button advancedIndicesButton;
        private ToolStripMenuItem advancedIndicesToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem fsiDatabankManagerToolStripMenuItem;
        private ToolStripMenuItem foramAMBIDatabankManagerToolStripMenuItem;
        private ToolStripMenuItem geographicAreasDatabankToolStripMenuItem;
        private ToolStripMenuItem indexSettingsToolStripMenuItem;
        private ToolStripMenuItem userCustomListsManagerToolStripMenuItem;
        private Button calculateFambiButton;
        private Button btnCalcFSI;
        private Button btnCalcNQI;
        private Button btnCalcExpH;
    }
}