//MIT License
// FSIDatabankManagerForm.cs - Utility for managing FSI databank classifications.

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
    public class FSIDatabankManagerForm : Dialog
    {
        private readonly DataGridView _databankGrid;
        private readonly TextBox _searchBox;
        private readonly Label _statsLabel;
        private readonly DropDown _filterCombo;
        private readonly CheckBox _useCustomCheckbox;

        private DataTable _databankTable;
        private readonly string _userDatabankPath;
        private bool _hasUnsavedChanges;

        /// <summary>Raised after the custom databank is written or reset.</summary>
        public event EventHandler DatabankUpdated;

        public FSIDatabankManagerForm()
        {
            string appDataPath = AppData.Directory;
            _userDatabankPath = Path.Combine(appDataPath, "fsi_databank_user.csv");

            Title = "FSI Databank Manager";
            ClientSize = new Size(820, 650);
            MinimumSize = new Size(600, 500);
            this.Prepare();

            // ----- Menu -----
            Menu = new MenuBar
            {
                Items =
                {
                    new SubMenuItem
                    {
                        Text = "&File",
                        Items =
                        {
                            new Command((s, e) => ImportCsvDialog()) { MenuText = "Import from CSV..." },
                            new Command((s, e) => ImportExcelDialog()) { MenuText = "Import from Excel..." },
                            new SeparatorMenuItem(),
                            new Command((s, e) => ExportCsvDialog()) { MenuText = "Export to CSV..." },
                            new Command((s, e) => ExportExcelDialog()) { MenuText = "Export to Excel..." },
                            new SeparatorMenuItem(),
                            new Command((s, e) => ResetToOriginal()) { MenuText = "Reset to Original" }
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
                            new Command((s, e) => ToggleClassification()) { MenuText = "Toggle Selected Classification" }
                        }
                    },
                    new SubMenuItem
                    {
                        Text = "&Help",
                        Items = { new Command((s, e) => ShowHelp()) { MenuText = "About FSI Classifications" } }
                    }
                }
            };

            // ----- Search / filter -----
            _searchBox = new TextBox { Width = 200 };
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _filterCombo = new DropDown { Width = 140 };
            _filterCombo.Items.Add("All");
            _filterCombo.Items.Add("Sensitive (S)");
            _filterCombo.Items.Add("Tolerant (T)");
            _filterCombo.SelectedIndex = 0;
            _filterCombo.SelectedIndexChanged += (s, e) => ApplyFilter();

            _useCustomCheckbox = new CheckBox
            {
                Text = "Use custom databank for calculations",
                Checked = File.Exists(_userDatabankPath)
            };
            _useCustomCheckbox.CheckedChanged += UseCustomCheckbox_CheckedChanged;

            // ----- Grid -----
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

            // ----- Buttons -----
            _statsLabel = new Label { Font = SystemFonts.Default(9) };

            var importButton = UiHelpers.ActionButton("Import CSV", AppColors.SteelBlue, (s, e) => ImportCsvDialog(), 110);
            var exportButton = UiHelpers.ActionButton("Export CSV", AppColors.Teal, (s, e) => ExportCsvDialog(), 110);
            var addSpeciesButton = UiHelpers.ActionButton("Add Species", AppColors.SeaGreen, (s, e) => AddSpecies(), 110);
            var removeSpeciesButton = UiHelpers.ActionButton("Remove", AppColors.IndianRed, (s, e) => RemoveSelectedSpecies(), 110);
            var saveButton = UiHelpers.ActionButton("Save Changes", AppColors.SeaGreen, (s, e) => SaveUserDatabank(), 130);
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
                            new Label { Text = "Search:" }, _searchBox,
                            new Label { Text = "Filter:" }, _filterCombo,
                            _useCustomCheckbox
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
                            importButton, exportButton, addSpeciesButton, removeSpeciesButton,
                            new StackLayoutItem(null, true),
                            saveButton, closeButton
                        }
                    }, true))
                }
            };

            Closing += Form_Closing;

            LoadDatabank();
        }

        private void LoadDatabank()
        {
            _databankTable = new DataTable();
            _databankTable.Columns.Add("Species", typeof(string));
            _databankTable.Columns.Add("Classification", typeof(string));

            // Prefer the user's custom databank, otherwise fall back to the shipped one.
            string pathToLoad = File.Exists(_userDatabankPath)
                ? _userDatabankPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fsi_databank.csv");

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
                            string cat = parts[1].Trim().ToUpperInvariant();
                            if (!string.IsNullOrEmpty(species) && (cat == "S" || cat == "T"))
                            {
                                _databankTable.Rows.Add(species, cat);
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

            _databankGrid.Columns.Clear();
            _databankGrid.AutoGenerateColumns = false;

            _databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Species",
                HeaderText = "Species",
                DataPropertyName = "Species",
                Width = 350
            });

            var classCol = new DataGridViewComboBoxColumn
            {
                Name = "Classification",
                HeaderText = "Classification",
                DataPropertyName = "Classification",
                Width = 120
            };
            classCol.Items.AddRange("S", "T");
            _databankGrid.Columns.Add(classCol);

            _databankGrid.DataSource = _databankTable;

            UpdateStats();
            _hasUnsavedChanges = false;
        }

        private void UpdateStats()
        {
            if (_databankTable == null)
            {
                return;
            }

            int total = _databankTable.Rows.Count;
            int sensitive = _databankTable.AsEnumerable().Count(r => r["Classification"]?.ToString() == "S");
            int tolerant = _databankTable.AsEnumerable().Count(r => r["Classification"]?.ToString() == "T");

            string source = File.Exists(_userDatabankPath) ? "Custom" : "Original";
            _statsLabel.Text = $"Total: {total} species | Sensitive (S): {sensitive} | Tolerant (T): {tolerant} | Source: {source}";
        }

        private void ApplyFilter()
        {
            string search = _searchBox.Text.ToLowerInvariant().Trim();
            string filter = _filterCombo.SelectedIndex >= 0
                ? _filterCombo.Items[_filterCombo.SelectedIndex].Text
                : "All";

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

            _databankTable.DefaultView.RowFilter = conditions.Count > 0 ? string.Join(" AND ", conditions) : string.Empty;
        }

        private void DatabankGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_databankGrid.Columns.Count > 1 && e.ColumnIndex == 1 && e.Value != null)
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

        #region Import / export

        private void ImportCsvDialog()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import FSI Databank",
                Filters = { Filters.Csv, Filters.All }
            };

            if (dialog.ShowDialog(this) == DialogResult.Ok)
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

                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    string line;
                    bool isFirst = true;
                    int skippedLocal = 0;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (isFirst) { isFirst = false; continue; }

                        // Support both ; and , delimiters
                        var parts = line.Contains(';') ? line.Split(';') : line.Split(',');

                        if (parts.Length >= 2)
                        {
                            string species = parts[0].Trim().Replace("_", " ");
                            string cat = NormalizeCategory(parts[1]);

                            if (!string.IsNullOrEmpty(species) && (cat == "S" || cat == "T"))
                            {
                                newData.Rows.Add(species, cat);
                            }
                            else
                            {
                                skippedLocal++;
                            }
                        }
                    }

                    ApplyImport(newData, newData.Rows.Count, skippedLocal);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing file: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportExcelDialog()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import FSI Databank from Excel",
                Filters = { Filters.Excel, Filters.All }
            };

            if (dialog.ShowDialog(this) == DialogResult.Ok)
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

                int skipped = 0;

                // Find the header row (look for "Species" or similar)
                int startRow = 1;
                int lastUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                for (int row = 1; row <= Math.Min(10, lastUsed); row++)
                {
                    string cell1 = worksheet.Cell(row, 1).GetString().ToLowerInvariant();
                    if (cell1.Contains("species") || cell1.Contains("taxon"))
                    {
                        startRow = row + 1;
                        break;
                    }
                }

                for (int row = startRow; row <= lastUsed; row++)
                {
                    string species = worksheet.Cell(row, 1).GetString().Trim().Replace("_", " ");
                    string cat = NormalizeCategory(worksheet.Cell(row, 2).GetString());

                    if (!string.IsNullOrEmpty(species) && (cat == "S" || cat == "T"))
                    {
                        newData.Rows.Add(species, cat);
                    }
                    else if (!string.IsNullOrEmpty(species))
                    {
                        skipped++;
                    }
                }

                ApplyImport(newData, newData.Rows.Count, skipped);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing Excel file: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string NormalizeCategory(string raw)
        {
            string cat = raw?.Trim().ToUpperInvariant() ?? string.Empty;
            if (cat == "SEN" || cat == "SENSITIVE") cat = "S";
            if (cat == "STR" || cat == "TOLERANT" || cat == "TOL") cat = "T";
            return cat;
        }

        private void ApplyImport(DataTable newData, int imported, int skipped)
        {
            if (imported == 0)
            {
                MessageBox.Show("No valid species classifications found in the file.\n\n" +
                    "Expected format: Species;Classification (S or T)",
                    "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                _databankTable.Clear();
                foreach (DataRow row in newData.Rows)
                {
                    _databankTable.Rows.Add(row["Species"], row["Classification"]);
                }
            }
            else if (result == DialogResult.No)
            {
                // Merge: update existing, add new
                var existingSpecies = _databankTable.AsEnumerable()
                    .GroupBy(r => r["Species"].ToString().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (DataRow newRow in newData.Rows)
                {
                    string speciesKey = newRow["Species"].ToString().ToLowerInvariant();
                    if (existingSpecies.TryGetValue(speciesKey, out DataRow existing))
                    {
                        existing["Classification"] = newRow["Classification"];
                    }
                    else
                    {
                        _databankTable.Rows.Add(newRow["Species"], newRow["Classification"]);
                    }
                }
            }
            else
            {
                return;
            }

            _hasUnsavedChanges = true;
            UpdateStats();
            MessageBox.Show("Import complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportCsvDialog()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export FSI Databank",
                FileName = "fsi_databank_export.csv",
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
                    writer.WriteLine("Species;FSI_Category");
                    foreach (DataRow row in _databankTable.Rows)
                    {
                        writer.WriteLine($"{row["Species"]};{row["Classification"]}");
                    }
                }

                MessageBox.Show($"Exported {_databankTable.Rows.Count} species to:\n{path}",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting file: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportExcelDialog()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export FSI Databank to Excel",
                FileName = "fsi_databank_export.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (dialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("FSI Databank");

                worksheet.Cell(1, 1).Value = "Species";
                worksheet.Cell(1, 2).Value = "FSI_Category";
                worksheet.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (DataRow dataRow in _databankTable.Rows)
                {
                    worksheet.Cell(row, 1).Value = dataRow["Species"].ToString();
                    worksheet.Cell(row, 2).Value = dataRow["Classification"].ToString();

                    worksheet.Cell(row, 2).Style.Fill.BackgroundColor =
                        dataRow["Classification"].ToString() == "S" ? XLColor.LightGreen : XLColor.LightSalmon;

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

        #region Editing

        private void AddSpecies()
        {
            using var addDialog = new AddSpeciesDialog(
                "Add Species",
                "Classification:",
                new[] { "S - Sensitive", "T - Tolerant" });

            if (addDialog.ShowModal(this) != true || string.IsNullOrWhiteSpace(addDialog.SpeciesName))
            {
                return;
            }

            string species = addDialog.SpeciesName.Trim();
            string classification = addDialog.SelectedIndex == 0 ? "S" : "T";

            var existing = _databankTable.AsEnumerable()
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
                    _hasUnsavedChanges = true;
                    UpdateStats();
                }
            }
            else
            {
                _databankTable.Rows.Add(species, classification);
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

        private void ToggleClassification()
        {
            var selected = _databankGrid.SelectedRows;
            if (selected.Count == 0)
            {
                return;
            }

            foreach (var row in selected)
            {
                if (row.DataBoundItem is DataRowView drv)
                {
                    string current = drv["Classification"].ToString();
                    drv["Classification"] = current == "S" ? "T" : "S";
                }
            }

            _hasUnsavedChanges = true;
            UpdateStats();
        }

        #endregion

        #region Persistence

        private void SaveUserDatabank()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_userDatabankPath));
                using (var writer = new StreamWriter(_userDatabankPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("Species;FSI_Category");
                    foreach (DataRow row in _databankTable.Rows)
                    {
                        writer.WriteLine($"{row["Species"]};{row["Classification"]}");
                    }
                }

                _hasUnsavedChanges = false;
                _useCustomCheckbox.Checked = true;
                UpdateStats();

                DatabankUpdated?.Invoke(this, EventArgs.Empty);

                MessageBox.Show($"Custom databank saved with {_databankTable.Rows.Count} species.\n\n" +
                    "This databank will be used for FSI calculations.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving databank: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetToOriginal()
        {
            var result = MessageBox.Show(
                "Reset to the original FSI databank?\n\n" +
                "This will discard all custom modifications.",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            if (File.Exists(_userDatabankPath))
            {
                try
                {
                    File.Delete(_userDatabankPath);
                }
                catch (Exception)
                {
                    // Best effort: reloading below still shows the shipped databank.
                }
            }

            LoadDatabank();
            _useCustomCheckbox.Checked = false;

            DatabankUpdated?.Invoke(this, EventArgs.Empty);
            MessageBox.Show("Reset to original databank.", "Reset Complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UseCustomCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (_useCustomCheckbox.Checked != true && File.Exists(_userDatabankPath))
            {
                var result = MessageBox.Show(
                    "Unchecking this will use the original databank for calculations.\n\n" +
                    "Your custom databank will still be saved. Continue?",
                    "Use Original Databank",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    _useCustomCheckbox.Checked = true;
                }
            }
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
            const string helpText = @"FSI (Foram Stress Index) Classification Guide

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

Reference: Dimiza et al. (2016); summary in O'Brien et al. (2021) Water 13, 1898";

            TextViewerDialog.Show("About FSI Classifications", helpText, new Size(560, 480));
        }

        /// <summary>
        /// Reads the user FSI databank for calculations; null when there is no custom databank.
        /// </summary>
        public static Dictionary<string, string> GetUserFSIDatabank()
        {
            string userPath = Path.Combine(AppData.Directory, "fsi_databank_user.csv");

            if (!File.Exists(userPath))
            {
                return null;
            }

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
                        string cat = parts[1].Trim().ToUpperInvariant();
                        if (!string.IsNullOrEmpty(species) && (cat == "S" || cat == "T"))
                        {
                            lookup[species] = cat;
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

    /// <summary>Shared "species name + category" prompt used by the databank managers.</summary>
    public class AddSpeciesDialog : Dialog<bool>
    {
        private readonly TextBox _speciesBox;
        private readonly DropDown _categoryCombo;

        public string SpeciesName => _speciesBox.Text;

        public int SelectedIndex => _categoryCombo.SelectedIndex;

        public AddSpeciesDialog(string title, string categoryLabel, IEnumerable<string> categories, string initialSpecies = "")
        {
            Title = title;
            Resizable = false;
            this.Prepare();

            _speciesBox = new TextBox { Width = 260, Text = initialSpecies ?? string.Empty };
            _categoryCombo = new DropDown { Width = 260 };
            foreach (var category in categories)
            {
                _categoryCombo.Items.Add(category);
            }
            if (_categoryCombo.Items.Count > 0)
            {
                _categoryCombo.SelectedIndex = 0;
            }

            var addButton = new Button { Text = "Add", Width = 90 };
            addButton.Click += (s, e) => Close(true);

            var cancelButton = new Button { Text = "Cancel", Width = 90 };
            cancelButton.Click += (s, e) => Close(false);

            DefaultButton = addButton;
            AbortButton = cancelButton;

            Content = new TableLayout
            {
                Padding = new Padding(20),
                Spacing = new Size(8, 10),
                Rows =
                {
                    new TableRow(new Label { Text = "Species name:", VerticalAlignment = VerticalAlignment.Center },
                                 new TableCell(_speciesBox, true)),
                    new TableRow(new Label { Text = categoryLabel, VerticalAlignment = VerticalAlignment.Center },
                                 new TableCell(_categoryCombo, true)),
                    new TableRow(null, new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { addButton, cancelButton }
                    }, true))
                }
            };
        }
    }
}
