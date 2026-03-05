//MIT License
// ForamAMBIDatabankManagerForm.cs - Utility for managing Foram-AMBI ecological group databank

using ClosedXML.Excel;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ForamEcoQS
{
    /// <summary>
    /// Form for managing custom Foram-AMBI ecological group (EG1-EG5) databank
    /// Similar to FSIDatabankManagerForm but for ecological groups
    /// </summary>
    public partial class ForamAMBIDatabankManagerForm : Form
    {
        private DataGridView databankGrid;
        private TextBox searchBox;
        private Label statsLabel;
        private Button addSpeciesButton;
        private Button removeSpeciesButton;
        private Button saveButton;
        private Button resetButton;
        private Button closeButton;
        private ComboBox filterCombo;
        private ComboBox baseDatabankCombo;
        private CheckBox useCustomCheckbox;
        private MenuStrip menuStrip;

        private DataTable databankTable;
        private string userDatabankPath;
        private string currentBaseDatabankName;
        private bool hasUnsavedChanges = false;

        // Event to notify Form1 that the databank was updated
        public event EventHandler DatabankUpdated;

        public ForamAMBIDatabankManagerForm(string currentDatabankName = "jorissen")
        {
            currentBaseDatabankName = currentDatabankName;
            InitializeComponent();
            LoadDatabank();
        }

        private void InitializeComponent()
        {
            this.Text = "Foram-AMBI Databank Manager (Ecological Groups)";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(700, 500);

            // Set user databank path
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            userDatabankPath = Path.Combine(appDataPath, "foram_ambi_databank_user.csv");

            // Menu strip
            menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;

            var fileMenu = new ToolStripMenuItem("File");
            var importMenuItem = new ToolStripMenuItem("Import from CSV...", null, ImportFromCsv_Click);
            var importExcelMenuItem = new ToolStripMenuItem("Import from Excel...", null, ImportFromExcel_Click);
            var exportMenuItem = new ToolStripMenuItem("Export to CSV...", null, ExportToCsv_Click);
            var exportExcelMenuItem = new ToolStripMenuItem("Export to Excel...", null, ExportToExcel_Click);
            var resetMenuItem = new ToolStripMenuItem("Reset to Base Databank", null, ResetButton_Click);
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
                importMenuItem, importExcelMenuItem,
                new ToolStripSeparator(),
                exportMenuItem, exportExcelMenuItem,
                new ToolStripSeparator(),
                resetMenuItem
            });

            var editMenu = new ToolStripMenuItem("Edit");
            var addMenuItem = new ToolStripMenuItem("Add Species...", null, AddSpeciesButton_Click);
            var removeMenuItem = new ToolStripMenuItem("Remove Selected", null, RemoveSpeciesButton_Click);
            var changeEGMenuItem = new ToolStripMenuItem("Change Ecological Group...", null, ChangeEcoGroup_Click);
            editMenu.DropDownItems.AddRange(new ToolStripItem[] { addMenuItem, removeMenuItem, new ToolStripSeparator(), changeEGMenuItem });

            var helpMenu = new ToolStripMenuItem("Help");
            var aboutMenuItem = new ToolStripMenuItem("About Ecological Groups", null, ShowHelp_Click);
            helpMenu.DropDownItems.Add(aboutMenuItem);

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, helpMenu });
            this.MainMenuStrip = menuStrip;

            // Top panel with search and filter
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(10, 5, 10, 5)
            };

            // First row: base databank selection
            var baseDatabankLabel = new Label { Text = "Base Databank:", Location = new Point(10, 10), AutoSize = true };
            baseDatabankCombo = new ComboBox
            {
                Location = new Point(110, 7),
                Size = new Size(280, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            baseDatabankCombo.Items.AddRange(new string[] {
                "Jorissen et al. (2018) - Mediterranean",
                "Alve et al. (2016) - NE Atlantic/Arctic",
                "Bouchet et al. (2012) - Mediterranean",
                "Bouchet et al. (2012) - Atlantic",
                "Bouchet et al. (2025) - South Atlantic",
                "O'Malley et al. (2021) - Gulf of Mexico"
            });
            baseDatabankCombo.SelectedIndex = 0;
            baseDatabankCombo.SelectedIndexChanged += BaseDatabankCombo_SelectedIndexChanged;

            useCustomCheckbox = new CheckBox
            {
                Text = "Use custom modifications",
                Location = new Point(410, 10),
                AutoSize = true,
                Checked = File.Exists(userDatabankPath)
            };
            useCustomCheckbox.CheckedChanged += UseCustomCheckbox_CheckedChanged;

            // Second row: search and filter
            var searchLabel = new Label { Text = "Search:", Location = new Point(10, 45), AutoSize = true };
            searchBox = new TextBox { Location = new Point(70, 42), Size = new Size(200, 25) };
            searchBox.TextChanged += SearchBox_TextChanged;

            var filterLabel = new Label { Text = "Eco Group:", Location = new Point(290, 45), AutoSize = true };
            filterCombo = new ComboBox
            {
                Location = new Point(370, 42),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            filterCombo.Items.AddRange(new string[] { "All", "EG1", "EG2", "EG3", "EG4", "EG5" });
            filterCombo.SelectedIndex = 0;
            filterCombo.SelectedIndexChanged += FilterCombo_SelectedIndexChanged;

            topPanel.Controls.AddRange(new Control[] {
                baseDatabankLabel, baseDatabankCombo, useCustomCheckbox,
                searchLabel, searchBox, filterLabel, filterCombo
            });

            // Bottom panel with buttons and stats
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                Padding = new Padding(10)
            };

            statsLabel = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(800, 35),
                Font = new Font(Font.FontFamily, 9)
            };

            addSpeciesButton = new Button
            {
                Text = "Add Species",
                Location = new Point(10, 55),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            addSpeciesButton.Click += AddSpeciesButton_Click;

            removeSpeciesButton = new Button
            {
                Text = "Remove",
                Location = new Point(120, 55),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(205, 92, 92),
                ForeColor = Color.White
            };
            removeSpeciesButton.Click += RemoveSpeciesButton_Click;

            saveButton = new Button
            {
                Text = "Save Custom",
                Location = new Point(230, 55),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            saveButton.Click += SaveButton_Click;

            resetButton = new Button
            {
                Text = "Reset",
                Location = new Point(360, 55),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 165, 0),
                ForeColor = Color.White
            };
            resetButton.Click += ResetButton_Click;

            closeButton = new Button
            {
                Text = "Close",
                Location = new Point(470, 55),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat
            };
            closeButton.Click += (s, e) => this.Close();

            bottomPanel.Controls.AddRange(new Control[] {
                statsLabel, addSpeciesButton, removeSpeciesButton, saveButton, resetButton, closeButton
            });

            // DataGridView
            databankGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = DataGridViewEditMode.EditOnEnter
            };
            databankGrid.CellValueChanged += DatabankGrid_CellValueChanged;
            databankGrid.CellFormatting += DatabankGrid_CellFormatting;

            var gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 5, 10, 5)
            };
            gridPanel.Controls.Add(databankGrid);

            this.Controls.Add(gridPanel);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(topPanel);
            this.Controls.Add(menuStrip);

            this.FormClosing += Form_FormClosing;
        }

        private void LoadDatabank()
        {
            databankTable = new DataTable();
            databankTable.Columns.Add("Species", typeof(string));
            databankTable.Columns.Add("Ecogroup", typeof(int));

            // First check for user custom databank
            if (File.Exists(userDatabankPath) && useCustomCheckbox.Checked)
            {
                LoadFromCsv(userDatabankPath);
            }
            else
            {
                // Load from base databank
                string basePath = GetBaseDatabankPath();
                if (File.Exists(basePath))
                {
                    if (basePath.EndsWith(".csv"))
                        LoadFromCsv(basePath);
                    else
                        LoadFromExcel(basePath);
                }
            }

            SetupGridColumns();
            UpdateStats();
            hasUnsavedChanges = false;
        }

        private string GetBaseDatabankPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return baseDatabankCombo.SelectedIndex switch
            {
                0 => Path.Combine(baseDir, "jorissen.xls"),
                1 => Path.Combine(baseDir, "alve.xls"),
                2 => Path.Combine(baseDir, "bouchetmed.xls"),
                3 => Path.Combine(baseDir, "bouchetatl.xls"),
                4 => Path.Combine(baseDir, "bouchetsouthatl.xls"),
                5 => Path.Combine(baseDir, "OMalley2021.csv"),
                _ => Path.Combine(baseDir, "jorissen.xls")
            };
        }

        private void LoadFromCsv(string path)
        {
            try
            {
                databankTable.Clear();
                using var reader = new StreamReader(path, Encoding.UTF8);
                string line;
                bool isFirst = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirst) { isFirst = false; continue; }
                    var parts = line.Split(';');
                    if (parts.Length >= 2)
                    {
                        string species = parts[0].Trim();
                        if (int.TryParse(parts[1].Trim(), out int eg) && eg >= 1 && eg <= 5)
                        {
                            databankTable.Rows.Add(species, eg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading CSV: {ex.Message}", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadFromExcel(string path)
        {
            try
            {
                databankTable.Clear();

                // Use ExcelDataReader for .xls files (Excel 97-2003), ClosedXML for .xlsx
                if (path.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    LoadFromXls(path);
                }
                else
                {
                    LoadFromXlsx(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Excel: {ex.Message}", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadFromXls(string path)
        {
            // Use ExcelDataReader for .xls files (Excel 97-2003 format)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var result = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            if (result.Tables.Count == 0) return;

            DataTable table = result.Tables[0];
            foreach (DataRow sourceRow in table.Rows)
            {
                if (table.Columns.Count < 2) continue;

                string species = sourceRow[0]?.ToString()?.Trim() ?? "";
                string egStr = sourceRow[1]?.ToString()?.Trim() ?? "";

                if (!string.IsNullOrEmpty(species) && int.TryParse(egStr, out int eg) && eg >= 1 && eg <= 5)
                {
                    databankTable.Rows.Add(species, eg);
                }
            }
        }

        private void LoadFromXlsx(string path)
        {
            // Use ClosedXML for .xlsx files (Excel 2007+ format)
            using var workbook = new XLWorkbook(path);
            var worksheet = workbook.Worksheet(1);

            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            int startRow = 2; // Assume header in row 1

            for (int row = startRow; row <= lastRow; row++)
            {
                string species = worksheet.Cell(row, 1).GetString().Trim();
                string egStr = worksheet.Cell(row, 2).GetString().Trim();

                if (!string.IsNullOrEmpty(species) && int.TryParse(egStr, out int eg) && eg >= 1 && eg <= 5)
                {
                    databankTable.Rows.Add(species, eg);
                }
            }
        }

        private void SetupGridColumns()
        {
            databankGrid.Columns.Clear();
            databankGrid.AutoGenerateColumns = false;

            databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Species",
                HeaderText = "Species",
                DataPropertyName = "Species",
                Width = 400,
                ReadOnly = false
            });

            var egCol = new DataGridViewComboBoxColumn
            {
                Name = "Ecogroup",
                HeaderText = "Ecological Group",
                DataPropertyName = "Ecogroup",
                Width = 150,
                FlatStyle = FlatStyle.Flat
            };
            egCol.Items.AddRange(1, 2, 3, 4, 5);
            databankGrid.Columns.Add(egCol);

            databankGrid.DataSource = databankTable;
        }

        private void UpdateStats()
        {
            var counts = new int[5];
            foreach (DataRow row in databankTable.Rows)
            {
                if (int.TryParse(row["Ecogroup"]?.ToString(), out int eg) && eg >= 1 && eg <= 5)
                {
                    counts[eg - 1]++;
                }
            }

            int total = databankTable.Rows.Count;
            string source = File.Exists(userDatabankPath) && useCustomCheckbox.Checked ? "Custom" : baseDatabankCombo.SelectedItem?.ToString() ?? "Base";

            statsLabel.Text = $"Total: {total} species | " +
                $"EG1 (Sensitive): {counts[0]} | EG2 (Indifferent): {counts[1]} | " +
                $"EG3 (Tolerant): {counts[2]} | EG4 (Opportunistic I): {counts[3]} | EG5 (Opportunistic II): {counts[4]}\n" +
                $"Source: {source}";
        }

        private void BaseDatabankCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Save before switching databank?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                    SaveUserDatabank();
                else if (result == DialogResult.Cancel)
                    return;
            }

            LoadDatabank();
        }

        private void SearchBox_TextChanged(object sender, EventArgs e) => ApplyFilter();
        private void FilterCombo_SelectedIndexChanged(object sender, EventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            string search = searchBox.Text.ToLower().Trim();
            string filter = filterCombo.SelectedItem?.ToString() ?? "All";

            var dv = databankTable.DefaultView;
            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(search))
            {
                conditions.Add($"Species LIKE '%{search.Replace("'", "''")}%'");
            }

            if (filter != "All" && filter.StartsWith("EG"))
            {
                int eg = int.Parse(filter.Substring(2));
                conditions.Add($"Ecogroup = {eg}");
            }

            dv.RowFilter = conditions.Count > 0 ? string.Join(" AND ", conditions) : "";
        }

        private void DatabankGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                hasUnsavedChanges = true;
                UpdateStats();
            }
        }

        private void DatabankGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (databankGrid.Columns.Count > 1 && e.ColumnIndex == 1 && e.Value != null)
            {
                if (int.TryParse(e.Value.ToString(), out int eg))
                {
                    e.CellStyle.BackColor = eg switch
                    {
                        1 => Color.FromArgb(144, 238, 144), // Light green - Sensitive
                        2 => Color.FromArgb(173, 216, 230), // Light blue - Indifferent
                        3 => Color.FromArgb(255, 255, 150), // Yellow - Tolerant
                        4 => Color.FromArgb(255, 200, 150), // Orange - Opportunistic I
                        5 => Color.FromArgb(255, 150, 150), // Red - Opportunistic II
                        _ => Color.White
                    };
                }
            }
        }

        private void AddSpeciesButton_Click(object sender, EventArgs e)
        {
            using var addForm = new Form
            {
                Text = "Add Species with Ecological Group",
                Size = new Size(450, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = this.Icon
            };

            var speciesLabel = new Label { Text = "Species name:", Location = new Point(20, 25), AutoSize = true };
            var speciesBox = new TextBox { Location = new Point(150, 22), Size = new Size(260, 25) };

            var egLabel = new Label { Text = "Ecological Group:", Location = new Point(20, 60), AutoSize = true };
            var egCombo = new ComboBox
            {
                Location = new Point(150, 57),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            egCombo.Items.AddRange(new string[] {
                "EG1 - Sensitive",
                "EG2 - Indifferent",
                "EG3 - Tolerant",
                "EG4 - 1st Order Opportunistic",
                "EG5 - 2nd Order Opportunistic"
            });
            egCombo.SelectedIndex = 0;

            var addBtn = new Button
            {
                Text = "Add",
                Location = new Point(150, 105),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };

            var cancelBtn = new Button
            {
                Text = "Cancel",
                Location = new Point(240, 105),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            addForm.Controls.AddRange(new Control[] { speciesLabel, speciesBox, egLabel, egCombo, addBtn, cancelBtn });
            addForm.AcceptButton = addBtn;
            addForm.CancelButton = cancelBtn;

            if (addForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(speciesBox.Text))
            {
                string species = speciesBox.Text.Trim();
                int eg = egCombo.SelectedIndex + 1;

                // Check if species already exists
                var existing = databankTable.AsEnumerable()
                    .FirstOrDefault(r => r["Species"].ToString().Equals(species, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    var result = MessageBox.Show(
                        $"'{species}' already exists with EG{existing["Ecogroup"]}.\n\nUpdate to EG{eg}?",
                        "Species Exists",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        existing["Ecogroup"] = eg;
                        hasUnsavedChanges = true;
                        UpdateStats();
                    }
                }
                else
                {
                    databankTable.Rows.Add(species, eg);
                    hasUnsavedChanges = true;
                    UpdateStats();
                }
            }
        }

        private void RemoveSpeciesButton_Click(object sender, EventArgs e)
        {
            if (databankGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select one or more species to remove.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Remove {databankGrid.SelectedRows.Count} selected species?",
                "Confirm Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var rowsToRemove = new List<DataRow>();
                foreach (DataGridViewRow row in databankGrid.SelectedRows)
                {
                    if (row.DataBoundItem is DataRowView drv)
                    {
                        rowsToRemove.Add(drv.Row);
                    }
                }

                foreach (var row in rowsToRemove)
                {
                    databankTable.Rows.Remove(row);
                }

                hasUnsavedChanges = true;
                UpdateStats();
            }
        }

        private void ChangeEcoGroup_Click(object sender, EventArgs e)
        {
            if (databankGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select species to change.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var changeForm = new Form
            {
                Text = "Change Ecological Group",
                Size = new Size(350, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = this.Icon
            };

            var label = new Label { Text = $"New EG for {databankGrid.SelectedRows.Count} species:", Location = new Point(20, 20), AutoSize = true };
            var egCombo = new ComboBox
            {
                Location = new Point(20, 50),
                Size = new Size(280, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            egCombo.Items.AddRange(new string[] {
                "EG1 - Sensitive",
                "EG2 - Indifferent",
                "EG3 - Tolerant",
                "EG4 - 1st Order Opportunistic",
                "EG5 - 2nd Order Opportunistic"
            });
            egCombo.SelectedIndex = 0;

            var okBtn = new Button { Text = "Apply", Location = new Point(100, 85), Size = new Size(80, 30), DialogResult = DialogResult.OK };
            var cancelBtn = new Button { Text = "Cancel", Location = new Point(190, 85), Size = new Size(80, 30), DialogResult = DialogResult.Cancel };

            changeForm.Controls.AddRange(new Control[] { label, egCombo, okBtn, cancelBtn });
            changeForm.AcceptButton = okBtn;
            changeForm.CancelButton = cancelBtn;

            if (changeForm.ShowDialog() == DialogResult.OK)
            {
                int newEg = egCombo.SelectedIndex + 1;
                foreach (DataGridViewRow row in databankGrid.SelectedRows)
                {
                    if (row.DataBoundItem is DataRowView drv)
                    {
                        drv["Ecogroup"] = newEg;
                    }
                }
                hasUnsavedChanges = true;
                UpdateStats();
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveUserDatabank();
        }

        private void SaveUserDatabank()
        {
            try
            {
                using var writer = new StreamWriter(userDatabankPath, false, Encoding.UTF8);
                writer.WriteLine("Species;Ecogroup");

                foreach (DataRow row in databankTable.Rows)
                {
                    writer.WriteLine($"{row["Species"]};{row["Ecogroup"]}");
                }

                hasUnsavedChanges = false;
                useCustomCheckbox.Checked = true;
                UpdateStats();

                DatabankUpdated?.Invoke(this, EventArgs.Empty);

                MessageBox.Show($"Custom databank saved with {databankTable.Rows.Count} species.\n\n" +
                    "This custom databank will be available for Foram-AMBI calculations.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving databank: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Reset to the base databank?\n\nThis will discard all custom modifications.",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                useCustomCheckbox.Checked = false;
                LoadDatabank();
                DatabankUpdated?.Invoke(this, EventArgs.Empty);
                MessageBox.Show("Reset to base databank.", "Reset Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UseCustomCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            LoadDatabank();
        }

        private void ImportFromCsv_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "CSV Files|*.csv|All Files|*.*",
                Title = "Import Foram-AMBI Databank"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ImportCsv(dialog.FileName);
            }
        }

        private void ImportCsv(string filePath)
        {
            try
            {
                var newData = new DataTable();
                newData.Columns.Add("Species", typeof(string));
                newData.Columns.Add("Ecogroup", typeof(int));

                using var reader = new StreamReader(filePath, Encoding.UTF8);
                string line;
                bool isFirst = true;
                int imported = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirst) { isFirst = false; continue; }
                    var parts = line.Contains(';') ? line.Split(';') : line.Split(',');
                    if (parts.Length >= 2)
                    {
                        string species = parts[0].Trim();
                        if (int.TryParse(parts[1].Trim(), out int eg) && eg >= 1 && eg <= 5)
                        {
                            newData.Rows.Add(species, eg);
                            imported++;
                        }
                    }
                }

                if (imported > 0)
                {
                    var result = MessageBox.Show(
                        $"Imported {imported} species.\n\nReplace or Merge with current databank?",
                        "Import Options",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes) // Replace
                    {
                        databankTable.Clear();
                        foreach (DataRow row in newData.Rows)
                            databankTable.Rows.Add(row["Species"], row["Ecogroup"]);
                    }
                    else if (result == DialogResult.No) // Merge
                    {
                        var existing = databankTable.AsEnumerable()
                            .ToDictionary(r => r["Species"].ToString().ToLower(), r => r);
                        foreach (DataRow newRow in newData.Rows)
                        {
                            string key = newRow["Species"].ToString().ToLower();
                            if (existing.TryGetValue(key, out DataRow existingRow))
                                existingRow["Ecogroup"] = newRow["Ecogroup"];
                            else
                                databankTable.Rows.Add(newRow["Species"], newRow["Ecogroup"]);
                        }
                    }
                    else return;

                    hasUnsavedChanges = true;
                    UpdateStats();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportFromExcel_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls|All Files|*.*",
                Title = "Import Foram-AMBI Databank from Excel"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    int imported = 0;
                    string filePath = dialog.FileName;

                    // Use ExcelDataReader for .xls, ClosedXML for .xlsx
                    if (filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) &&
                        !filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        imported = ImportFromXlsFile(filePath);
                    }
                    else
                    {
                        imported = ImportFromXlsxFile(filePath);
                    }

                    if (imported > 0)
                    {
                        hasUnsavedChanges = true;
                        UpdateStats();
                        MessageBox.Show($"Imported {imported} species.", "Import Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing Excel: {ex.Message}", "Import Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private int ImportFromXlsFile(string filePath)
        {
            int imported = 0;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var result = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            if (result.Tables.Count == 0) return 0;

            DataTable table = result.Tables[0];
            foreach (DataRow sourceRow in table.Rows)
            {
                if (table.Columns.Count < 2) continue;

                string species = sourceRow[0]?.ToString()?.Trim() ?? "";
                string egStr = sourceRow[1]?.ToString()?.Trim() ?? "";

                if (!string.IsNullOrEmpty(species) && int.TryParse(egStr, out int eg) && eg >= 1 && eg <= 5)
                {
                    databankTable.Rows.Add(species, eg);
                    imported++;
                }
            }

            return imported;
        }

        private int ImportFromXlsxFile(string filePath)
        {
            int imported = 0;

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);

            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int row = 2; row <= lastRow; row++)
            {
                string species = worksheet.Cell(row, 1).GetString().Trim();
                string egStr = worksheet.Cell(row, 2).GetString().Trim();

                if (!string.IsNullOrEmpty(species) && int.TryParse(egStr, out int eg) && eg >= 1 && eg <= 5)
                {
                    databankTable.Rows.Add(species, eg);
                    imported++;
                }
            }

            return imported;
        }

        private void ExportToCsv_Click(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                Title = "Export Foram-AMBI Databank",
                FileName = "foram_ambi_databank_export.csv"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);
                    writer.WriteLine("Species;Ecogroup");
                    foreach (DataRow row in databankTable.Rows)
                        writer.WriteLine($"{row["Species"]};{row["Ecogroup"]}");

                    MessageBox.Show($"Exported {databankTable.Rows.Count} species.",
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting: {ex.Message}", "Export Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportToExcel_Click(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Export Foram-AMBI Databank to Excel",
                FileName = "foram_ambi_databank_export.xlsx"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Foram-AMBI Databank");

                    worksheet.Cell(1, 1).Value = "Species";
                    worksheet.Cell(1, 2).Value = "Ecogroup";
                    worksheet.Row(1).Style.Font.Bold = true;

                    int row = 2;
                    foreach (DataRow dataRow in databankTable.Rows)
                    {
                        worksheet.Cell(row, 1).Value = dataRow["Species"].ToString();
                        int eg = (int)dataRow["Ecogroup"];
                        worksheet.Cell(row, 2).Value = eg;

                        // Color code
                        worksheet.Cell(row, 2).Style.Fill.BackgroundColor = eg switch
                        {
                            1 => XLColor.LightGreen,
                            2 => XLColor.LightBlue,
                            3 => XLColor.LightYellow,
                            4 => XLColor.LightSalmon,
                            5 => XLColor.LightPink,
                            _ => XLColor.White
                        };
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(dialog.FileName);

                    MessageBox.Show($"Exported {databankTable.Rows.Count} species to Excel.",
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowHelp_Click(object sender, EventArgs e)
        {
            string helpText = @"Foram-AMBI Ecological Groups Classification Guide

The Foram-AMBI index uses 5 ecological groups based on species sensitivity:

EG1 - SENSITIVE SPECIES
Species sensitive to organic enrichment. Predominant in pristine conditions.
Examples: Cibicides spp., Planulina spp., most epifaunal species

EG2 - INDIFFERENT SPECIES
Species indifferent to organic enrichment. Always present in low densities.
Examples: Quinqueloculina spp., Textularia spp.

EG3 - TOLERANT SPECIES
Species tolerant to excess organic matter enrichment.
Examples: Nonionella spp., Cassidulina spp., Melonis spp.

EG4 - 2nd ORDER OPPORTUNISTIC
Second-order opportunistic species (slight to moderate enrichment).
Examples: Bolivina spp., Bulimina spp., Uvigerina spp.

EG5 - 1st ORDER OPPORTUNISTIC
First-order opportunistic species (marked enrichment).
Examples: Ammonia spp., Elphidium excavatum, Stainforthia spp.

FORAM-AMBI FORMULA:
Foram-AMBI = (0×EG1 + 1.5×EG2 + 3×EG3 + 4.5×EG4 + 6×EG5) / 100

Values range from 0 (pristine) to 6 (highly degraded).

References:
- Jorissen et al. (2018) - doi:10.1016/j.marmicro.2017.12.006
- Alve et al. (2016) - doi:10.1016/j.marmicro.2015.11.001
- Borja et al. (2000) - Original AMBI methodology";

            MessageBox.Show(helpText, "About Ecological Groups",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Save before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                    SaveUserDatabank();
                else if (result == DialogResult.Cancel)
                    e.Cancel = true;
            }
        }

        /// <summary>
        /// Gets the current user Foram-AMBI databank for calculations
        /// </summary>
        public static Dictionary<string, int> GetUserForamAMBIDatabank()
        {
            string userPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "foram_ambi_databank_user.csv");

            if (!File.Exists(userPath))
                return null;

            var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var reader = new StreamReader(userPath, Encoding.UTF8);
                string line;
                bool isFirst = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirst) { isFirst = false; continue; }
                    var parts = line.Split(';');
                    if (parts.Length >= 2)
                    {
                        string species = parts[0].Trim();
                        if (int.TryParse(parts[1].Trim(), out int eg) && eg >= 1 && eg <= 5)
                        {
                            lookup[species] = eg;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            return lookup.Count > 0 ? lookup : null;
        }
    }
}
