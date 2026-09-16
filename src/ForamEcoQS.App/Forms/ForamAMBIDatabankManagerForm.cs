//MIT License
// ForamAMBIDatabankManagerForm.cs - Utility for managing the Foram-AMBI ecological group databank.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using Eto.Drawing;
using Eto.Forms;
using ForamEcoQS.Compat;
using DataGridView = ForamEcoQS.Compat.DataGridView;
using MessageBox = ForamEcoQS.Compat.MessageBox;

namespace ForamEcoQS
{
    /// <summary>
    /// Manages a custom Foram-AMBI ecological group (EG1-EG5) databank, derived from one of the
    /// published base databanks.
    /// </summary>
    public class ForamAMBIDatabankManagerForm : Dialog
    {
        private readonly DataGridView _databankGrid;
        private readonly TextBox _searchBox;
        private readonly Label _statsLabel;
        private readonly DropDown _filterCombo;
        private readonly DropDown _baseDatabankCombo;
        private readonly CheckBox _useCustomCheckbox;

        private DataTable _databankTable;
        private readonly string _userDatabankPath;
        private bool _hasUnsavedChanges;
        private bool _loading;

        public event EventHandler DatabankUpdated;

        public ForamAMBIDatabankManagerForm(string currentDatabankName = "jorissen")
        {
            _userDatabankPath = AppData.File("foram_ambi_databank_user.csv");

            Title = "Foram-AMBI Databank Manager (Ecological Groups)";
            ClientSize = new Size(920, 700);
            MinimumSize = new Size(700, 500);
            this.Prepare();

            Menu = new MenuBar
            {
                Items =
                {
                    new SubMenuItem
                    {
                        Text = "&File",
                        Items =
                        {
                            new Command((s, e) => ImportFromCsvDialog()) { MenuText = "Import from CSV..." },
                            new Command((s, e) => ImportFromExcelDialog()) { MenuText = "Import from Excel..." },
                            new SeparatorMenuItem(),
                            new Command((s, e) => ExportToCsvDialog()) { MenuText = "Export to CSV..." },
                            new Command((s, e) => ExportToExcelDialog()) { MenuText = "Export to Excel..." },
                            new SeparatorMenuItem(),
                            new Command((s, e) => ResetToBase()) { MenuText = "Reset to Base Databank" }
                        }
                    },
                    new SubMenuItem
                    {
                        Text = "&Edit",
                        Items =
                        {
                            new Command((s, e) => AddSpecies()) { MenuText = "Add Species..." },
                            new Command((s, e) => RemoveSelectedSpecies()) { MenuText = "Remove Selected" },
                            new SeparatorMenuItem(),
                            new Command((s, e) => ChangeEcoGroup()) { MenuText = "Change Ecological Group..." }
                        }
                    },
                    new SubMenuItem
                    {
                        Text = "&Help",
                        Items = { new Command((s, e) => ShowHelp()) { MenuText = "About Ecological Groups" } }
                    }
                }
            };

            _baseDatabankCombo = new DropDown { Width = 300 };
            _baseDatabankCombo.Items.Add("Jorissen et al. (2018) - Mediterranean");
            _baseDatabankCombo.Items.Add("Alve et al. (2016) - NE Atlantic/Arctic");
            _baseDatabankCombo.Items.Add("Bouchet et al. (2012) - Mediterranean");
            _baseDatabankCombo.Items.Add("Bouchet et al. (2012) - Atlantic");
            _baseDatabankCombo.Items.Add("Bouchet et al. (2025) - South Atlantic");
            _baseDatabankCombo.Items.Add("O'Malley et al. (2021) - Gulf of Mexico");
            _baseDatabankCombo.SelectedIndex = IndexForDatabankName(currentDatabankName);
            _baseDatabankCombo.SelectedIndexChanged += BaseDatabankCombo_SelectedIndexChanged;

            _useCustomCheckbox = new CheckBox
            {
                Text = "Use custom modifications",
                Checked = File.Exists(_userDatabankPath)
            };
            _useCustomCheckbox.CheckedChanged += (s, e) =>
            {
                if (!_loading)
                {
                    LoadDatabank();
                }
            };

            _searchBox = new TextBox { Width = 200 };
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _filterCombo = new DropDown { Width = 120 };
            _filterCombo.Items.Add("All");
            for (int i = 1; i <= 5; i++)
            {
                _filterCombo.Items.Add($"EG{i}");
            }
            _filterCombo.SelectedIndex = 0;
            _filterCombo.SelectedIndexChanged += (s, e) => ApplyFilter();

            _databankGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = Compat.DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = Compat.DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = Compat.DataGridViewEditMode.EditOnEnter
            };
            _databankGrid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    _hasUnsavedChanges = true;
                    UpdateStats();
                }
            };
            _databankGrid.CellFormatting += DatabankGrid_CellFormatting;

            _statsLabel = new Label { Font = SystemFonts.Default(9) };

            var addSpeciesButton = UiHelpers.ActionButton("Add Species", AppColors.SeaGreen, (s, e) => AddSpecies(), 110);
            var removeSpeciesButton = UiHelpers.ActionButton("Remove", AppColors.IndianRed, (s, e) => RemoveSelectedSpecies(), 110);
            var saveButton = UiHelpers.ActionButton("Save Custom", AppColors.SeaGreen, (s, e) => SaveUserDatabank(), 130);
            var resetButton = UiHelpers.ActionButton("Reset", AppColors.Orange, (s, e) => ResetToBase(), 110);
            var closeButton = new Button { Text = "Close", Width = 100 };
            closeButton.Click += (s, e) => Close();

            Content = new TableLayout
            {
                Padding = new Padding(10),
                Spacing = new Size(6, 8),
                Rows =
                {
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {
                            new Label { Text = "Base Databank:" }, _baseDatabankCombo, _useCustomCheckbox
                        }
                    }, true)),
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {
                            new Label { Text = "Search:" }, _searchBox,
                            new Label { Text = "Eco Group:" }, _filterCombo
                        }
                    }, true)),
                    new TableRow(_databankGrid) { ScaleHeight = true },
                    new TableRow(_statsLabel),
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Items =
                        {
                            addSpeciesButton, removeSpeciesButton, saveButton, resetButton,
                            new StackLayoutItem(null, true),
                            closeButton
                        }
                    }, true))
                }
            };

            Closing += Form_Closing;

            LoadDatabank();
        }

        private static int IndexForDatabankName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }
            if (name.StartsWith("Alve", StringComparison.OrdinalIgnoreCase)) return 1;
            if (name.StartsWith("Bouchet", StringComparison.OrdinalIgnoreCase) && name.Contains("South")) return 4;
            if (name.StartsWith("Bouchet", StringComparison.OrdinalIgnoreCase) && name.Contains("Mediterranean")) return 2;
            if (name.StartsWith("Bouchet", StringComparison.OrdinalIgnoreCase)) return 3;
            if (name.StartsWith("O'Malley", StringComparison.OrdinalIgnoreCase)) return 5;
            return 0;
        }

        private void LoadDatabank()
        {
            _loading = true;
            try
            {
                _databankTable = new DataTable();
                _databankTable.Columns.Add("Species", typeof(string));
                _databankTable.Columns.Add("Ecogroup", typeof(int));

                if (File.Exists(_userDatabankPath) && _useCustomCheckbox.Checked == true)
                {
                    LoadFromCsv(_userDatabankPath);
                }
                else
                {
                    string basePath = GetBaseDatabankPath();
                    if (File.Exists(basePath))
                    {
                        if (basePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            LoadFromCsv(basePath);
                        }
                        else
                        {
                            LoadFromExcel(basePath);
                        }
                    }
                }

                SetupGridColumns();
                UpdateStats();
                _hasUnsavedChanges = false;
            }
            finally
            {
                _loading = false;
            }
        }

        private string GetBaseDatabankPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return _baseDatabankCombo.SelectedIndex switch
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
                _databankTable.Clear();
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
                            _databankTable.Rows.Add(species, eg);
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
                _databankTable.Clear();

                // ExcelDataReader handles the legacy .xls files, ClosedXML the modern .xlsx ones.
                if (path.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
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
            DataTable table = ExcelDataLoader.ReadExcelFile(path);
            if (table == null || table.Columns.Count < 2)
            {
                return;
            }

            foreach (DataRow sourceRow in table.Rows)
            {
                string species = sourceRow[0]?.ToString()?.Trim() ?? string.Empty;
                string egStr = sourceRow[1]?.ToString()?.Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(species) && int.TryParse(egStr, out int eg) && eg >= 1 && eg <= 5)
                {
                    _databankTable.Rows.Add(species, eg);
                }
            }
        }

        private void LoadFromXlsx(string path)
        {
            using var workbook = new XLWorkbook(path);
            var worksheet = workbook.Worksheet(1);

            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int row = 2; row <= lastRow; row++)
            {
                string species = worksheet.Cell(row, 1).GetString().Trim();
                string egStr = worksheet.Cell(row, 2).GetString().Trim();

                if (!string.IsNullOrEmpty(species) && int.TryParse(egStr, out int eg) && eg >= 1 && eg <= 5)
                {
                    _databankTable.Rows.Add(species, eg);
                }
            }
        }

        private void SetupGridColumns()
        {
            _databankGrid.Columns.Clear();
            _databankGrid.AutoGenerateColumns = false;

            _databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Species",
                HeaderText = "Species",
                DataPropertyName = "Species",
                Width = 400
            });

            var egCol = new DataGridViewComboBoxColumn
            {
                Name = "Ecogroup",
                HeaderText = "Ecological Group",
                DataPropertyName = "Ecogroup",
                ValueType = typeof(int),
                Width = 150
            };
            egCol.Items.AddRange(1, 2, 3, 4, 5);
            _databankGrid.Columns.Add(egCol);

            _databankGrid.DataSource = _databankTable;
        }

        private void UpdateStats()
        {
            if (_databankTable == null)
            {
                return;
            }

            var counts = new int[5];
            foreach (DataRow row in _databankTable.Rows)
            {
                if (int.TryParse(row["Ecogroup"]?.ToString(), out int eg) && eg >= 1 && eg <= 5)
                {
                    counts[eg - 1]++;
                }
            }

            int total = _databankTable.Rows.Count;
            string source = File.Exists(_userDatabankPath) && _useCustomCheckbox.Checked == true
                ? "Custom"
                : (_baseDatabankCombo.SelectedIndex >= 0
                    ? _baseDatabankCombo.Items[_baseDatabankCombo.SelectedIndex].Text
                    : "Base");

            _statsLabel.Text = $"Total: {total} species | " +
                $"EG1 (Sensitive): {counts[0]} | EG2 (Indifferent): {counts[1]} | " +
                $"EG3 (Tolerant): {counts[2]} | EG4 (Opportunistic I): {counts[3]} | EG5 (Opportunistic II): {counts[4]}\n" +
                $"Source: {source}";
        }

        private void BaseDatabankCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loading)
            {
                return;
            }

            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Save before switching databank?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveUserDatabank();
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            LoadDatabank();
        }

        private void ApplyFilter()
        {
            if (_databankTable == null)
            {
                return;
            }

            string search = _searchBox.Text.ToLowerInvariant().Trim();
            string filter = _filterCombo.SelectedIndex >= 0
                ? _filterCombo.Items[_filterCombo.SelectedIndex].Text
                : "All";

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

            _databankTable.DefaultView.RowFilter = conditions.Count > 0 ? string.Join(" AND ", conditions) : string.Empty;
        }

        private void DatabankGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_databankGrid.Columns.Count > 1 && e.ColumnIndex == 1 && e.Value != null)
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
                        _ => Colors.White
                    };
                }
            }
        }

        #region Editing

        private static readonly string[] EcoGroupChoices =
        {
            "EG1 - Sensitive",
            "EG2 - Indifferent",
            "EG3 - Tolerant",
            "EG4 - 1st Order Opportunistic",
            "EG5 - 2nd Order Opportunistic"
        };

        private void AddSpecies()
        {
            using var addDialog = new AddSpeciesDialog(
                "Add Species with Ecological Group", "Ecological Group:", EcoGroupChoices);

            if (addDialog.ShowModal(this) != true || string.IsNullOrWhiteSpace(addDialog.SpeciesName))
            {
                return;
            }

            string species = addDialog.SpeciesName.Trim();
            int eg = addDialog.SelectedIndex + 1;

            var existing = _databankTable.AsEnumerable()
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
                    _hasUnsavedChanges = true;
                    UpdateStats();
                }
            }
            else
            {
                _databankTable.Rows.Add(species, eg);
                _hasUnsavedChanges = true;
                UpdateStats();
            }
        }

        private void RemoveSelectedSpecies()
        {
            var selected = _databankGrid.SelectedRows;
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select one or more species to remove.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Remove {selected.Count} selected species?",
                "Confirm Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            var rowsToRemove = selected
                .Select(r => r.DataBoundItem as DataRowView)
                .Where(v => v != null)
                .Select(v => v.Row)
                .ToList();

            foreach (var row in rowsToRemove)
            {
                _databankTable.Rows.Remove(row);
            }

            _hasUnsavedChanges = true;
            UpdateStats();
        }

        private void ChangeEcoGroup()
        {
            var selected = _databankGrid.SelectedRows;
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select species to change.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var changeDialog = new ChoiceDialog(
                "Change Ecological Group",
                $"New EG for {selected.Count} species:",
                EcoGroupChoices);

            if (changeDialog.ShowModal(this) != true)
            {
                return;
            }

            int newEg = changeDialog.SelectedIndex + 1;
            foreach (var row in selected)
            {
                if (row.DataBoundItem is DataRowView drv)
                {
                    drv["Ecogroup"] = newEg;
                }
            }

            _hasUnsavedChanges = true;
            UpdateStats();
        }

        #endregion

        #region Import / export

        private void ImportFromCsvDialog()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import Foram-AMBI Databank",
                Filters = { Filters.Csv, Filters.All }
            };

            if (dialog.ShowDialog(this) == DialogResult.Ok)
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

                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    string line;
                    bool isFirst = true;

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
                            }
                        }
                    }
                }

                if (newData.Rows.Count == 0)
                {
                    MessageBox.Show("No valid species found in the file.\n\nExpected format: Species;Ecogroup (1-5)",
                        "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Imported {newData.Rows.Count} species.\n\nReplace or Merge with current databank?",
                    "Import Options",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes) // Replace
                {
                    _databankTable.Clear();
                    foreach (DataRow row in newData.Rows)
                    {
                        _databankTable.Rows.Add(row["Species"], row["Ecogroup"]);
                    }
                }
                else if (result == DialogResult.No) // Merge
                {
                    var existing = _databankTable.AsEnumerable()
                        .GroupBy(r => r["Species"].ToString().ToLowerInvariant())
                        .ToDictionary(g => g.Key, g => g.First());

                    foreach (DataRow newRow in newData.Rows)
                    {
                        string key = newRow["Species"].ToString().ToLowerInvariant();
                        if (existing.TryGetValue(key, out DataRow existingRow))
                        {
                            existingRow["Ecogroup"] = newRow["Ecogroup"];
                        }
                        else
                        {
                            _databankTable.Rows.Add(newRow["Species"], newRow["Ecogroup"]);
                        }
                    }
                }
                else
                {
                    return;
                }

                _hasUnsavedChanges = true;
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportFromExcelDialog()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import Foram-AMBI Databank from Excel",
                Filters = { Filters.Excel, Filters.All }
            };

            if (dialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                string filePath = dialog.FileName;
                int imported = filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)
                    ? ImportFromXlsFile(filePath)
                    : ImportFromXlsxFile(filePath);

                if (imported > 0)
                {
                    _hasUnsavedChanges = true;
                    UpdateStats();
                    MessageBox.Show($"Imported {imported} species.", "Import Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No valid species found in the file.", "Import Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing Excel: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int ImportFromXlsFile(string filePath)
        {
            int imported = 0;
            DataTable table = ExcelDataLoader.ReadExcelFile(filePath);
            if (table == null || table.Columns.Count < 2)
            {
                return 0;
            }

            foreach (DataRow sourceRow in table.Rows)
            {
                string species = sourceRow[0]?.ToString()?.Trim() ?? string.Empty;
                string egStr = sourceRow[1]?.ToString()?.Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(species) && int.TryParse(egStr, out int eg) && eg >= 1 && eg <= 5)
                {
                    _databankTable.Rows.Add(species, eg);
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
                    _databankTable.Rows.Add(species, eg);
                    imported++;
                }
            }

            return imported;
        }

        private void ExportToCsvDialog()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export Foram-AMBI Databank",
                FileName = "foram_ambi_databank_export.csv",
                Filters = { Filters.Csv }
            };

            if (dialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                string path = UiHelpers.EnsureExtension(dialog.FileName, ".csv");
                using (var writer = new StreamWriter(path, false, Encoding.UTF8))
                {
                    writer.WriteLine("Species;Ecogroup");
                    foreach (DataRow row in _databankTable.Rows)
                    {
                        writer.WriteLine($"{row["Species"]};{row["Ecogroup"]}");
                    }
                }

                MessageBox.Show($"Exported {_databankTable.Rows.Count} species.",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToExcelDialog()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export Foram-AMBI Databank to Excel",
                FileName = "foram_ambi_databank_export.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (dialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Foram-AMBI Databank");

                worksheet.Cell(1, 1).Value = "Species";
                worksheet.Cell(1, 2).Value = "Ecogroup";
                worksheet.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (DataRow dataRow in _databankTable.Rows)
                {
                    worksheet.Cell(row, 1).Value = dataRow["Species"].ToString();
                    int eg = Convert.ToInt32(dataRow["Ecogroup"]);
                    worksheet.Cell(row, 2).Value = eg;

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
                workbook.SaveAs(UiHelpers.EnsureExtension(dialog.FileName, ".xlsx"));

                MessageBox.Show($"Exported {_databankTable.Rows.Count} species to Excel.",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Persistence

        private void SaveUserDatabank()
        {
            try
            {
                using (var writer = new StreamWriter(_userDatabankPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("Species;Ecogroup");
                    foreach (DataRow row in _databankTable.Rows)
                    {
                        writer.WriteLine($"{row["Species"]};{row["Ecogroup"]}");
                    }
                }

                _hasUnsavedChanges = false;
                _loading = true;
                _useCustomCheckbox.Checked = true;
                _loading = false;
                UpdateStats();

                DatabankUpdated?.Invoke(this, EventArgs.Empty);

                MessageBox.Show($"Custom databank saved with {_databankTable.Rows.Count} species.\n\n" +
                    "This custom databank will be available for Foram-AMBI calculations.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving databank: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetToBase()
        {
            var result = MessageBox.Show(
                "Reset to the base databank?\n\nThis will discard all custom modifications.",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            _loading = true;
            _useCustomCheckbox.Checked = false;
            _loading = false;

            LoadDatabank();
            DatabankUpdated?.Invoke(this, EventArgs.Empty);
            MessageBox.Show("Reset to base databank.", "Reset Complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_hasUnsavedChanges)
            {
                return;
            }

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

        #endregion

        private static void ShowHelp()
        {
            const string helpText = @"Foram-AMBI Ecological Groups Classification Guide

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

            TextViewerDialog.Show("About Ecological Groups", helpText, new Size(600, 560));
        }

        /// <summary>
        /// Reads the user Foram-AMBI databank for calculations; null when there is none.
        /// </summary>
        public static Dictionary<string, int> GetUserForamAMBIDatabank()
        {
            string userPath = AppData.File("foram_ambi_databank_user.csv");

            if (!File.Exists(userPath))
            {
                return null;
            }

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
            catch (Exception)
            {
                return null;
            }

            return lookup.Count > 0 ? lookup : null;
        }
    }
}
