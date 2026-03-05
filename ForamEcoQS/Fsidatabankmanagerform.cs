//MIT License
// FSIDatabankManagerForm.cs - Utility for managing FSI databank classifications

using ClosedXML.Excel;
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
    public partial class FSIDatabankManagerForm : Form
    {
        private DataGridView databankGrid;
        private TextBox searchBox;
        private Label statsLabel;
        private Button importButton;
        private Button exportButton;
        private Button addSpeciesButton;
        private Button removeSpeciesButton;
        private Button saveButton;
        private Button resetButton;
        private Button closeButton;
        private ComboBox filterCombo;
        private CheckBox useCustomCheckbox;
        private MenuStrip menuStrip;

        private DataTable databankTable;
        private string userDatabankPath;
        private bool hasUnsavedChanges = false;

        // Event to notify Form1 that the databank was updated
        public event EventHandler DatabankUpdated;

        public FSIDatabankManagerForm()
        {
            InitializeComponent();
            
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            userDatabankPath = Path.Combine(appDataPath, "fsi_databank_user.csv");
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            LoadDatabank();
        }

        private void InitializeComponent()
        {
            this.Text = "FSI Databank Manager";
            this.Size = new Size(800, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(600, 500);

            // Set user databank path
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            userDatabankPath = Path.Combine(appDataPath, "fsi_databank_user.csv");

            // Menu strip
            menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;
            var fileMenu = new ToolStripMenuItem("File");
            var importMenuItem = new ToolStripMenuItem("Import from CSV...", null, ImportButton_Click);
            var importExcelMenuItem = new ToolStripMenuItem("Import from Excel...", null, ImportExcel_Click);
            var exportMenuItem = new ToolStripMenuItem("Export to CSV...", null, ExportButton_Click);
            var exportExcelMenuItem = new ToolStripMenuItem("Export to Excel...", null, ExportExcel_Click);
            var resetMenuItem = new ToolStripMenuItem("Reset to Original", null, ResetButton_Click);
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
            var toggleMenuItem = new ToolStripMenuItem("Toggle Selected Classification", null, ToggleClassification_Click);
            editMenu.DropDownItems.AddRange(new ToolStripItem[] { addMenuItem, removeMenuItem, new ToolStripSeparator(), toggleMenuItem });

            var helpMenu = new ToolStripMenuItem("Help");
            var aboutMenuItem = new ToolStripMenuItem("About FSI Classifications", null, ShowHelp_Click);
            helpMenu.DropDownItems.Add(aboutMenuItem);

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, helpMenu });
            this.MainMenuStrip = menuStrip;

            // Top panel with search and filter
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10, 5, 10, 5)
            };

            var searchLabel = new Label { Text = "Search:", Location = new Point(10, 15), AutoSize = true };
            searchBox = new TextBox { Location = new Point(70, 12), Size = new Size(200, 25) };
            searchBox.TextChanged += SearchBox_TextChanged;

            var filterLabel = new Label { Text = "Filter:", Location = new Point(290, 15), AutoSize = true };
            filterCombo = new ComboBox
            {
                Location = new Point(340, 12),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            filterCombo.Items.AddRange(new string[] { "All", "Sensitive (S)", "Tolerant (T)" });
            filterCombo.SelectedIndex = 0;
            filterCombo.SelectedIndexChanged += FilterCombo_SelectedIndexChanged;

            useCustomCheckbox = new CheckBox
            {
                Text = "Use custom databank for calculations",
                Location = new Point(490, 13),
                AutoSize = true,
                Checked = File.Exists(userDatabankPath)
            };
            useCustomCheckbox.CheckedChanged += UseCustomCheckbox_CheckedChanged;

            topPanel.Controls.AddRange(new Control[] { searchLabel, searchBox, filterLabel, filterCombo, useCustomCheckbox });

            // Bottom panel with buttons and stats
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Padding = new Padding(10)
            };

            statsLabel = new Label
            {
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 9)
            };

            importButton = new Button
            {
                Text = "Import CSV",
                Location = new Point(10, 50),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White
            };
            importButton.Click += ImportButton_Click;

            exportButton = new Button
            {
                Text = "Export CSV",
                Location = new Point(120, 50),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(95, 158, 160),
                ForeColor = Color.White
            };
            exportButton.Click += ExportButton_Click;

            addSpeciesButton = new Button
            {
                Text = "Add Species",
                Location = new Point(230, 50),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            addSpeciesButton.Click += AddSpeciesButton_Click;

            removeSpeciesButton = new Button
            {
                Text = "Remove",
                Location = new Point(340, 50),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(205, 92, 92),
                ForeColor = Color.White
            };
            removeSpeciesButton.Click += RemoveSpeciesButton_Click;

            saveButton = new Button
            {
                Text = "Save Changes",
                Location = new Point(520, 50),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            saveButton.Click += SaveButton_Click;

            closeButton = new Button
            {
                Text = "Close",
                Location = new Point(650, 50),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat
            };
            closeButton.Click += (s, e) => this.Close();

            bottomPanel.Controls.AddRange(new Control[] { 
                statsLabel, importButton, exportButton, addSpeciesButton, 
                removeSpeciesButton, saveButton, closeButton 
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

            // ADD CONTROLS IN CORRECT ORDER:
            // 1. Bottom panel first (Dock.Bottom)
            // 2. Top panel (Dock.Top) 
            // 3. Menu strip (Dock.Top, but special handling)
            // 4. Fill panel last (Dock.Fill)
            this.Controls.Add(gridPanel);      // Fill - added first but docks last
            this.Controls.Add(bottomPanel);    // Bottom
            this.Controls.Add(topPanel);       // Top (below menu)
            this.Controls.Add(menuStrip);      // Menu at very top

            // Handle form closing
            this.FormClosing += FSIDatabankManagerForm_FormClosing;
        }

        private void LoadDatabank()
        {
            databankTable = new DataTable();
            databankTable.Columns.Add("Species", typeof(string));
            databankTable.Columns.Add("Classification", typeof(string));

            // Try to load user databank first, then fall back to original
            string pathToLoad = File.Exists(userDatabankPath) ? userDatabankPath : 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fsi_databank.csv");

            if (File.Exists(pathToLoad))
            {
                try
                {
                    using var reader = new StreamReader(pathToLoad, Encoding.UTF8);
                    string line;
                    bool isFirst = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (isFirst) { isFirst = false; continue; }
                        var parts = line.Split(';');
                        if (parts.Length >= 2)
                        {
                            string species = parts[0].Trim();
                            string cat = parts[1].Trim().ToUpper();
                            if (!string.IsNullOrEmpty(species) && (cat == "S" || cat == "T"))
                            {
                                databankTable.Rows.Add(species, cat);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading databank: {ex.Message}", "Load Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            // Clear any existing columns first
            databankGrid.Columns.Clear();
            databankGrid.AutoGenerateColumns = false;
            
            // Create columns manually for better control
            var speciesCol = new DataGridViewTextBoxColumn
            {
                Name = "Species",
                HeaderText = "Species",
                DataPropertyName = "Species",
                Width = 350,
                ReadOnly = false
            };
            databankGrid.Columns.Add(speciesCol);
            
            // Make Classification a combo box column
            var classCol = new DataGridViewComboBoxColumn
            {
                Name = "Classification",
                HeaderText = "Classification",
                DataPropertyName = "Classification",
                Width = 120,
                FlatStyle = FlatStyle.Flat
            };
            classCol.Items.AddRange("S", "T");
            databankGrid.Columns.Add(classCol);
            
            // Now bind the data
            databankGrid.DataSource = databankTable;

            UpdateStats();
            hasUnsavedChanges = false;
        }

        private void UpdateStats()
        {
            int total = databankTable.Rows.Count;
            int sensitive = databankTable.AsEnumerable().Count(r => r["Classification"]?.ToString() == "S");
            int tolerant = databankTable.AsEnumerable().Count(r => r["Classification"]?.ToString() == "T");

            string source = File.Exists(userDatabankPath) ? "Custom" : "Original";
            statsLabel.Text = $"Total: {total} species | Sensitive (S): {sensitive} | Tolerant (T): {tolerant} | Source: {source}";
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void FilterCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

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

            if (filter == "Sensitive (S)")
            {
                conditions.Add("Classification = 'S'");
            }
            else if (filter == "Tolerant (T)")
            {
                conditions.Add("Classification = 'T'");
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
                string val = e.Value.ToString();
                if (val == "S")
                {
                    e.CellStyle.BackColor = Color.FromArgb(200, 255, 200);
                }
                else if (val == "T")
                {
                    e.CellStyle.BackColor = Color.FromArgb(255, 220, 200);
                }
            }
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "CSV Files|*.csv|All Files|*.*",
                Title = "Import FSI Databank"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ImportFromCsv(dialog.FileName);
            }
        }

        private void ImportFromCsv(string filePath)
        {
            try
            {
                var newData = new DataTable();
                newData.Columns.Add("Species", typeof(string));
                newData.Columns.Add("Classification", typeof(string));

                using var reader = new StreamReader(filePath, Encoding.UTF8);
                string line;
                bool isFirst = true;
                int imported = 0;
                int skipped = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirst) { isFirst = false; continue; }
                    
                    // Support both ; and , delimiters
                    var parts = line.Contains(';') ? line.Split(';') : line.Split(',');
                    
                    if (parts.Length >= 2)
                    {
                        string species = parts[0].Trim().Replace("_", " ");
                        string cat = parts[1].Trim().ToUpper();

                        // Handle different category formats
                        if (cat == "SEN" || cat == "SENSITIVE") cat = "S";
                        if (cat == "STR" || cat == "TOLERANT" || cat == "TOL") cat = "T";

                        if (!string.IsNullOrEmpty(species) && (cat == "S" || cat == "T"))
                        {
                            newData.Rows.Add(species, cat);
                            imported++;
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                }

                if (imported > 0)
                {
                    // Ask whether to replace or merge
                    var result = MessageBox.Show(
                        $"Imported {imported} species ({skipped} skipped).\n\n" +
                        "Replace: Clear current databank and use imported data only.\n" +
                        "Merge: Add new species and update existing classifications.\n\n" +
                        "Click Yes to Replace, No to Merge, Cancel to abort.",
                        "Import Options",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        databankTable.Clear();
                        foreach (DataRow row in newData.Rows)
                        {
                            databankTable.Rows.Add(row["Species"], row["Classification"]);
                        }
                    }
                    else if (result == DialogResult.No)
                    {
                        // Merge: update existing, add new
                        var existingSpecies = databankTable.AsEnumerable()
                            .ToDictionary(r => r["Species"].ToString().ToLower(), r => r);

                        foreach (DataRow newRow in newData.Rows)
                        {
                            string speciesKey = newRow["Species"].ToString().ToLower();
                            if (existingSpecies.TryGetValue(speciesKey, out DataRow existing))
                            {
                                existing["Classification"] = newRow["Classification"];
                            }
                            else
                            {
                                databankTable.Rows.Add(newRow["Species"], newRow["Classification"]);
                            }
                        }
                    }
                    else
                    {
                        return;
                    }

                    hasUnsavedChanges = true;
                    UpdateStats();
                    MessageBox.Show($"Import complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No valid species classifications found in the file.\n\n" +
                        "Expected format: Species;Classification (S or T)",
                        "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing file: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportExcel_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls|All Files|*.*",
                Title = "Import FSI Databank from Excel"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ImportFromExcel(dialog.FileName);
            }
        }

        private void ImportFromExcel(string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);

                var newData = new DataTable();
                newData.Columns.Add("Species", typeof(string));
                newData.Columns.Add("Classification", typeof(string));

                int imported = 0;
                int skipped = 0;

                // Find header row (look for "Species" or similar)
                int startRow = 1;
                for (int row = 1; row <= Math.Min(10, worksheet.LastRowUsed()?.RowNumber() ?? 1); row++)
                {
                    string cell1 = worksheet.Cell(row, 1).GetString().ToLower();
                    if (cell1.Contains("species") || cell1.Contains("taxon"))
                    {
                        startRow = row + 1;
                        break;
                    }
                }

                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                for (int row = startRow; row <= lastRow; row++)
                {
                    string species = worksheet.Cell(row, 1).GetString().Trim().Replace("_", " ");
                    string cat = worksheet.Cell(row, 2).GetString().Trim().ToUpper();

                    // Handle different category formats
                    if (cat == "SEN" || cat == "SENSITIVE") cat = "S";
                    if (cat == "STR" || cat == "TOLERANT" || cat == "TOL") cat = "T";

                    if (!string.IsNullOrEmpty(species) && (cat == "S" || cat == "T"))
                    {
                        newData.Rows.Add(species, cat);
                        imported++;
                    }
                    else if (!string.IsNullOrEmpty(species))
                    {
                        skipped++;
                    }
                }

                if (imported > 0)
                {
                    var result = MessageBox.Show(
                        $"Found {imported} species ({skipped} skipped).\n\n" +
                        "Replace: Clear current databank and use imported data only.\n" +
                        "Merge: Add new species and update existing classifications.\n\n" +
                        "Click Yes to Replace, No to Merge, Cancel to abort.",
                        "Import Options",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        databankTable.Clear();
                        foreach (DataRow row in newData.Rows)
                        {
                            databankTable.Rows.Add(row["Species"], row["Classification"]);
                        }
                    }
                    else if (result == DialogResult.No)
                    {
                        var existingSpecies = databankTable.AsEnumerable()
                            .ToDictionary(r => r["Species"].ToString().ToLower(), r => r);

                        foreach (DataRow newRow in newData.Rows)
                        {
                            string speciesKey = newRow["Species"].ToString().ToLower();
                            if (existingSpecies.TryGetValue(speciesKey, out DataRow existing))
                            {
                                existing["Classification"] = newRow["Classification"];
                            }
                            else
                            {
                                databankTable.Rows.Add(newRow["Species"], newRow["Classification"]);
                            }
                        }
                    }
                    else
                    {
                        return;
                    }

                    hasUnsavedChanges = true;
                    UpdateStats();
                    MessageBox.Show($"Import complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No valid species classifications found.\n\n" +
                        "Expected format: Column 1 = Species name, Column 2 = Classification (S/T or Sen/Str)",
                        "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing Excel file: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                Title = "Export FSI Databank",
                FileName = "fsi_databank_export.csv"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ExportToCsv(dialog.FileName);
            }
        }

        private void ExportToCsv(string filePath)
        {
            try
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                writer.WriteLine("Species;FSI_Category");

                foreach (DataRow row in databankTable.Rows)
                {
                    writer.WriteLine($"{row["Species"]};{row["Classification"]}");
                }

                MessageBox.Show($"Exported {databankTable.Rows.Count} species to:\n{filePath}",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting file: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportExcel_Click(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Export FSI Databank to Excel",
                FileName = "fsi_databank_export.xlsx"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("FSI Databank");

                    worksheet.Cell(1, 1).Value = "Species";
                    worksheet.Cell(1, 2).Value = "FSI_Category";
                    worksheet.Row(1).Style.Font.Bold = true;

                    int row = 2;
                    foreach (DataRow dataRow in databankTable.Rows)
                    {
                        worksheet.Cell(row, 1).Value = dataRow["Species"].ToString();
                        worksheet.Cell(row, 2).Value = dataRow["Classification"].ToString();

                        // Color code
                        if (dataRow["Classification"].ToString() == "S")
                            worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightGreen;
                        else
                            worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightSalmon;

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

        private void AddSpeciesButton_Click(object sender, EventArgs e)
        {
            using var addForm = new Form
            {
                Text = "Add Species",
                Size = new Size(400, 180),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = this.Icon
            };

            var speciesLabel = new Label { Text = "Species name:", Location = new Point(20, 20), AutoSize = true };
            var speciesBox = new TextBox { Location = new Point(120, 17), Size = new Size(240, 25) };

            var classLabel = new Label { Text = "Classification:", Location = new Point(20, 55), AutoSize = true };
            var classCombo = new ComboBox
            {
                Location = new Point(120, 52),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            classCombo.Items.AddRange(new string[] { "S - Sensitive", "T - Tolerant" });
            classCombo.SelectedIndex = 0;

            var addBtn = new Button
            {
                Text = "Add",
                Location = new Point(120, 95),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };

            var cancelBtn = new Button
            {
                Text = "Cancel",
                Location = new Point(210, 95),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            addForm.Controls.AddRange(new Control[] { speciesLabel, speciesBox, classLabel, classCombo, addBtn, cancelBtn });
            addForm.AcceptButton = addBtn;
            addForm.CancelButton = cancelBtn;

            if (addForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(speciesBox.Text))
            {
                string species = speciesBox.Text.Trim();
                string classification = classCombo.SelectedIndex == 0 ? "S" : "T";

                // Check if species already exists
                var existing = databankTable.AsEnumerable()
                    .FirstOrDefault(r => r["Species"].ToString().Equals(species, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    var result = MessageBox.Show(
                        $"'{species}' already exists with classification '{existing["Classification"]}'.\n\n" +
                        $"Update to '{classification}'?",
                        "Species Exists",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        existing["Classification"] = classification;
                        hasUnsavedChanges = true;
                        UpdateStats();
                    }
                }
                else
                {
                    databankTable.Rows.Add(species, classification);
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

        private void ToggleClassification_Click(object sender, EventArgs e)
        {
            if (databankGrid.SelectedRows.Count == 0) return;

            foreach (DataGridViewRow row in databankGrid.SelectedRows)
            {
                if (row.DataBoundItem is DataRowView drv)
                {
                    string current = drv["Classification"].ToString();
                    drv["Classification"] = current == "S" ? "T" : "S";
                }
            }

            hasUnsavedChanges = true;
            UpdateStats();
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
                writer.WriteLine("Species;FSI_Category");

                foreach (DataRow row in databankTable.Rows)
                {
                    writer.WriteLine($"{row["Species"]};{row["Classification"]}");
                }

                hasUnsavedChanges = false;
                useCustomCheckbox.Checked = true;
                UpdateStats();

                // Notify Form1 that databank was updated
                DatabankUpdated?.Invoke(this, EventArgs.Empty);

                MessageBox.Show($"Custom databank saved with {databankTable.Rows.Count} species.\n\n" +
                    "This databank will be used for FSI calculations.",
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
                "Reset to the original FSI databank?\n\n" +
                "This will discard all custom modifications.",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // Delete user databank
                if (File.Exists(userDatabankPath))
                {
                    try
                    {
                        File.Delete(userDatabankPath);
                    }
                    catch { }
                }

                // Reload original
                LoadDatabank();
                useCustomCheckbox.Checked = false;

                DatabankUpdated?.Invoke(this, EventArgs.Empty);
                MessageBox.Show("Reset to original databank.", "Reset Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UseCustomCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!useCustomCheckbox.Checked && File.Exists(userDatabankPath))
            {
                var result = MessageBox.Show(
                    "Unchecking this will use the original databank for calculations.\n\n" +
                    "Your custom databank will still be saved. Continue?",
                    "Use Original Databank",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    useCustomCheckbox.Checked = true;
                }
            }
        }

        private void ShowHelp_Click(object sender, EventArgs e)
        {
            string helpText = @"FSI (Foram Stress Index) Classification Guide

SENSITIVE (S) species:
- Indicator of good environmental quality
- Typically found in undisturbed, well-oxygenated environments
- Examples: Adelosina spp., Quinqueloculina spp., Rosalina spp.

TOLERANT (T) species:
- Indicator of stressed environmental conditions
- Tolerant to organic enrichment, low oxygen, pollution
- Examples: Ammonia spp., Bolivina spp., Bulimina spp.

FSI Formula:
FSI = (10 × %Sensitive + %Tolerant) / (%Sensitive + %Tolerant)

Values range from 1 (highly stressed) to 10 (pristine).

Reference: Dimiza et al. (2016); summary in O’Brien et al. (2021) Water 13, 1898";

            MessageBox.Show(helpText, "About FSI Classifications", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FSIDatabankManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Save before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveUserDatabank();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// Gets the current user FSI databank for calculations
        /// Returns null if no custom databank exists or user disabled it
        /// </summary>
        public static Dictionary<string, string> GetUserFSIDatabank()
        {
            string userPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fsi_databank_user.csv");
            
            if (!File.Exists(userPath))
                return null;

            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
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
                        string cat = parts[1].Trim().ToUpper();
                        if (!string.IsNullOrEmpty(species) && (cat == "S" || cat == "T"))
                        {
                            lookup[species] = cat;
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
