//MIT License
// UserCustomListsManagerForm.cs - Manager for user-defined custom species lists,
// each tagged with the index type it is meant for.

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
    public class UserCustomListsManagerForm : Dialog
    {
        private readonly DataGridView _listsGrid;
        private readonly DataGridView _speciesGrid;
        private readonly Label _statsLabel;
        private readonly DropDown _indexFilterCombo;
        private readonly Splitter _splitter;

        private DataTable _listsTable;
        private DataTable _currentSpeciesTable;
        private readonly string _userListsDirectory;
        private readonly string _manifestPath;
        private bool _hasUnsavedChanges;

        public event EventHandler ListsUpdated;

        /// <summary>Index types a custom list can be assigned to.</summary>
        public static readonly string[] IndexTypes =
        {
            "Foram-AMBI",
            "FSI",
            "TSI-Med",
            "FoRAM Index",
            "BQI",
            "BENTIX",
            "NQIf",
            "Custom"
        };

        public UserCustomListsManagerForm()
        {
            _userListsDirectory = Path.Combine(AppData.Directory, "user_lists");
            Directory.CreateDirectory(_userListsDirectory);
            _manifestPath = Path.Combine(_userListsDirectory, "manifest.csv");

            Title = "User Custom Lists Manager";
            ClientSize = new Size(1000, 700);
            MinimumSize = new Size(800, 600);
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
                            new Command((s, e) => ImportListDialog(false)) { MenuText = "Import List from CSV..." },
                            new Command((s, e) => ImportListDialog(true)) { MenuText = "Import List from Excel..." },
                            new SeparatorMenuItem(),
                            new Command((s, e) => ExportListCsv()) { MenuText = "Export Selected List to CSV..." },
                            new Command((s, e) => ExportListExcel()) { MenuText = "Export Selected List to Excel..." }
                        }
                    },
                    new SubMenuItem
                    {
                        Text = "&Help",
                        Items = { new Command((s, e) => ShowHelp()) { MenuText = "About Custom Lists" } }
                    }
                }
            };

            _indexFilterCombo = new DropDown { Width = 180 };
            _indexFilterCombo.Items.Add("All");
            foreach (var type in IndexTypes)
            {
                _indexFilterCombo.Items.Add(type);
            }
            _indexFilterCombo.SelectedIndex = 0;
            _indexFilterCombo.SelectedIndexChanged += IndexFilterCombo_SelectedIndexChanged;

            // ----- Lists panel -----
            _listsGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = Compat.DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = Compat.DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true
            };
            _listsGrid.SelectionChanged += ListsGrid_SelectionChanged;
            _listsGrid.CellFormatting += ListsGrid_CellFormatting;

            var addListButton = UiHelpers.ActionButton("Add List", AppColors.SeaGreen, (s, e) => AddList(), 95);
            var editListButton = UiHelpers.ActionButton("Edit", AppColors.SteelBlue, (s, e) => EditList(), 75);
            var removeListButton = UiHelpers.ActionButton("Remove", AppColors.IndianRed, (s, e) => RemoveList(), 85);

            var listsPanel = new TableLayout
            {
                Padding = new Padding(10),
                Spacing = new Size(6, 6),
                Rows =
                {
                    new TableRow(new Label { Text = "Custom Lists:", Font = SystemFonts.Bold(10) }),
                    new TableRow(_listsGrid) { ScaleHeight = true },
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        Items = { addListButton, editListButton, removeListButton }
                    }, true))
                }
            };

            // ----- Species panel -----
            _speciesGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = Compat.DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = Compat.DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = Compat.DataGridViewEditMode.EditOnEnter
            };
            _speciesGrid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    _hasUnsavedChanges = true;
                }
            };

            var addSpeciesButton = UiHelpers.ActionButton("Add Species", AppColors.SeaGreen, (s, e) => AddSpecies(), 110);
            var removeSpeciesButton = UiHelpers.ActionButton("Remove", AppColors.IndianRed, (s, e) => RemoveSpecies(), 85);
            var saveSpeciesButton = UiHelpers.ActionButton("Save Changes", AppColors.SteelBlue, (s, e) => SaveSpecies(), 120);

            var speciesPanel = new TableLayout
            {
                Padding = new Padding(10),
                Spacing = new Size(6, 6),
                Rows =
                {
                    new TableRow(new Label { Text = "Species in Selected List:", Font = SystemFonts.Bold(10) }),
                    new TableRow(_speciesGrid) { ScaleHeight = true },
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        Items = { addSpeciesButton, removeSpeciesButton, saveSpeciesButton }
                    }, true))
                }
            };

            _splitter = new Splitter
            {
                Orientation = Orientation.Horizontal,
                FixedPanel = SplitterFixedPanel.Panel1,
                Position = 330,
                Panel1 = listsPanel,
                Panel2 = speciesPanel
            };

            _statsLabel = new Label { Font = SystemFonts.Default(9) };

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
                        Items = { new Label { Text = "Filter by Index Type:" }, _indexFilterCombo }
                    }, true)),
                    new TableRow(_splitter) { ScaleHeight = true },
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items = { _statsLabel, new StackLayoutItem(null, true), closeButton }
                    }, true))
                }
            };

            Closing += Form_Closing;

            LoadManifest();
        }

        #region Manifest and lists

        private void LoadManifest()
        {
            _listsTable = new DataTable();
            _listsTable.Columns.Add("Name", typeof(string));
            _listsTable.Columns.Add("IndexType", typeof(string));
            _listsTable.Columns.Add("SpeciesCount", typeof(int));
            _listsTable.Columns.Add("FileName", typeof(string));
            _listsTable.Columns.Add("Description", typeof(string));

            if (File.Exists(_manifestPath))
            {
                try
                {
                    using var reader = new StreamReader(_manifestPath, Encoding.UTF8);
                    string line;
                    bool isFirst = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (isFirst) { isFirst = false; continue; }
                        var parts = line.Split(';');
                        if (parts.Length >= 4)
                        {
                            string name = parts[0].Trim();
                            string indexType = parts[1].Trim();
                            int count = int.TryParse(parts[2].Trim(), out int c) ? c : 0;
                            string fileName = parts[3].Trim();
                            string desc = parts.Length > 4 ? parts[4].Trim() : string.Empty;
                            _listsTable.Rows.Add(name, indexType, count, fileName, desc);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading manifest: {ex.Message}", "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            SetupListsGrid();
            UpdateStats();
        }

        private void SetupListsGrid()
        {
            _listsGrid.Columns.Clear();
            _listsGrid.AutoGenerateColumns = false;

            _listsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "List Name",
                DataPropertyName = "Name",
                Width = 150
            });

            _listsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IndexType",
                HeaderText = "Index Type",
                DataPropertyName = "IndexType",
                Width = 100
            });

            _listsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SpeciesCount",
                HeaderText = "Species",
                DataPropertyName = "SpeciesCount",
                ValueType = typeof(int),
                Width = 70
            });

            _listsGrid.DataSource = _listsTable;
        }

        private void UpdateStats()
        {
            int totalLists = _listsTable.Rows.Count;
            var indexCounts = _listsTable.AsEnumerable()
                .GroupBy(r => r["IndexType"].ToString())
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();

            _statsLabel.Text = $"Total Lists: {totalLists}" +
                (indexCounts.Count > 0 ? $" | {string.Join(" | ", indexCounts)}" : string.Empty);
        }

        private DataRowView SelectedListRow()
        {
            var selected = _listsGrid.SelectedRows;
            return selected.Count > 0 ? selected[0].DataBoundItem as DataRowView : null;
        }

        private void ListsGrid_SelectionChanged(object sender, EventArgs e)
        {
            var drv = SelectedListRow();
            if (drv != null)
            {
                LoadSpeciesList(drv["FileName"].ToString());
            }
            else
            {
                _speciesGrid.DataSource = null;
            }
        }

        private void ListsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 1 && e.Value != null) // IndexType column
            {
                string indexType = e.Value.ToString();
                e.CellStyle.BackColor = indexType switch
                {
                    "Foram-AMBI" => Color.FromArgb(173, 216, 230),
                    "FSI" => Color.FromArgb(144, 238, 144),
                    "TSI-Med" => Color.FromArgb(255, 255, 150),
                    "FoRAM Index" => Color.FromArgb(255, 200, 150),
                    "BQI" => Color.FromArgb(221, 160, 221),
                    "BENTIX" => Color.FromArgb(176, 224, 230),
                    "NQIf" => Color.FromArgb(240, 230, 140),
                    _ => Color.FromArgb(220, 220, 220)
                };
            }
        }

        private void IndexFilterCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            string filter = _indexFilterCombo.SelectedIndex >= 0
                ? _indexFilterCombo.Items[_indexFilterCombo.SelectedIndex].Text
                : "All";

            _listsTable.DefaultView.RowFilter = filter == "All"
                ? string.Empty
                : $"IndexType = '{filter.Replace("'", "''")}'";
        }

        private void LoadSpeciesList(string fileName)
        {
            _currentSpeciesTable = new DataTable();
            _currentSpeciesTable.Columns.Add("Species", typeof(string));
            _currentSpeciesTable.Columns.Add("Value", typeof(string));

            string filePath = Path.Combine(_userListsDirectory, fileName);
            if (File.Exists(filePath))
            {
                try
                {
                    using var reader = new StreamReader(filePath, Encoding.UTF8);
                    string line;
                    bool isFirst = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (isFirst) { isFirst = false; continue; }
                        var parts = line.Split(';');
                        if (parts.Length >= 2)
                        {
                            _currentSpeciesTable.Rows.Add(parts[0].Trim(), parts[1].Trim());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading species list: {ex.Message}", "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            _speciesGrid.Columns.Clear();
            _speciesGrid.AutoGenerateColumns = false;

            _speciesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Species",
                HeaderText = "Species",
                DataPropertyName = "Species",
                Width = 300
            });

            _speciesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Value",
                HeaderText = "Value/Category",
                DataPropertyName = "Value",
                Width = 150
            });

            _speciesGrid.DataSource = _currentSpeciesTable;
        }

        private void AddList()
        {
            using var dialog = new ListPropertiesDialog("Add New Custom List", IndexTypes);
            if (dialog.ShowModal(this) != true || string.IsNullOrWhiteSpace(dialog.ListName))
            {
                return;
            }

            string name = dialog.ListName.Trim();
            string indexType = dialog.IndexType;
            string desc = dialog.Description.Trim();

            var existing = _listsTable.AsEnumerable()
                .FirstOrDefault(r => r["Name"].ToString().Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                MessageBox.Show($"A list named '{name}' already exists.", "Duplicate Name",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fileName = MakeFileName(name, indexType);
            File.WriteAllText(Path.Combine(_userListsDirectory, fileName), "Species;Value\n", Encoding.UTF8);

            _listsTable.Rows.Add(name, indexType, 0, fileName, desc);
            SaveManifest();

            UpdateStats();
            _hasUnsavedChanges = false;

            MessageBox.Show($"List '{name}' created for {indexType}.\n\nYou can now add species to this list.",
                "List Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string MakeFileName(string name, string indexType) =>
            $"{name.Replace(" ", "_").ToLowerInvariant()}_{indexType.Replace(" ", "_").ToLowerInvariant()}.csv";

        private void EditList()
        {
            var drv = SelectedListRow();
            if (drv == null)
            {
                MessageBox.Show("Please select a list to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new ListPropertiesDialog(
                "Edit List Properties", IndexTypes,
                drv["Name"].ToString(), drv["IndexType"].ToString(), drv["Description"].ToString(), "Save");

            if (dialog.ShowModal(this) != true)
            {
                return;
            }

            drv["Name"] = dialog.ListName.Trim();
            drv["IndexType"] = dialog.IndexType;
            drv["Description"] = dialog.Description.Trim();
            SaveManifest();
            UpdateStats();
        }

        private void RemoveList()
        {
            var drv = SelectedListRow();
            if (drv == null)
            {
                MessageBox.Show("Please select a list to remove.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = drv["Name"].ToString();
            string fileName = drv["FileName"].ToString();

            var result = MessageBox.Show(
                $"Remove list '{name}'?\n\nThis will permanently delete the list and all its species.",
                "Confirm Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string filePath = Path.Combine(_userListsDirectory, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _listsTable.Rows.Remove(drv.Row);
            SaveManifest();
            UpdateStats();

            _speciesGrid.DataSource = null;
        }

        private void SaveManifest()
        {
            try
            {
                using var writer = new StreamWriter(_manifestPath, false, Encoding.UTF8);
                writer.WriteLine("Name;IndexType;SpeciesCount;FileName;Description");
                foreach (DataRow row in _listsTable.Rows)
                {
                    writer.WriteLine(
                        $"{row["Name"]};{row["IndexType"]};{row["SpeciesCount"]};{row["FileName"]};{row["Description"]}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving manifest: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Species editing

        private void AddSpecies()
        {
            if (SelectedListRow() == null || _currentSpeciesTable == null)
            {
                MessageBox.Show("Please select a list first.", "No List Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new AddSpeciesValueDialog();
            if (dialog.ShowModal(this) != true || string.IsNullOrWhiteSpace(dialog.SpeciesName))
            {
                return;
            }

            _currentSpeciesTable.Rows.Add(dialog.SpeciesName.Trim(), dialog.Value.Trim());
            _hasUnsavedChanges = true;
        }

        private void RemoveSpecies()
        {
            var selected = _speciesGrid.SelectedRows;
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select species to remove.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rowsToRemove = selected
                .Select(r => r.DataBoundItem as DataRowView)
                .Where(v => v != null)
                .Select(v => v.Row)
                .ToList();

            foreach (var row in rowsToRemove)
            {
                _currentSpeciesTable.Rows.Remove(row);
            }

            _hasUnsavedChanges = true;
        }

        private void SaveSpecies()
        {
            var drv = SelectedListRow();
            if (drv == null || _currentSpeciesTable == null)
            {
                MessageBox.Show("No list selected.", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fileName = drv["FileName"].ToString();
            string filePath = Path.Combine(_userListsDirectory, fileName);

            try
            {
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("Species;Value");
                    foreach (DataRow row in _currentSpeciesTable.Rows)
                    {
                        writer.WriteLine($"{row["Species"]};{row["Value"]}");
                    }
                }

                drv["SpeciesCount"] = _currentSpeciesTable.Rows.Count;
                SaveManifest();

                _hasUnsavedChanges = false;
                ListsUpdated?.Invoke(this, EventArgs.Empty);

                MessageBox.Show($"List saved with {_currentSpeciesTable.Rows.Count} species.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving list: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Import / export

        private void ImportListDialog(bool isExcel)
        {
            using var dialog = new OpenFileDialog
            {
                Title = isExcel ? "Import Custom List from Excel" : "Import Custom List from CSV",
                Filters = { isExcel ? Filters.Excel : Filters.Csv, Filters.All }
            };

            if (dialog.ShowDialog(this) == DialogResult.Ok)
            {
                ImportListFromFile(dialog.FileName, isExcel);
            }
        }

        private void ImportListFromFile(string filePath, bool isExcel)
        {
            using var metaDialog = new ListPropertiesDialog(
                "Import List - Set Properties", IndexTypes,
                Path.GetFileNameWithoutExtension(filePath), IndexTypes[0], string.Empty, "Import", showDescription: false);

            if (metaDialog.ShowModal(this) != true)
            {
                return;
            }

            string name = metaDialog.ListName.Trim();
            string indexType = metaDialog.IndexType;

            var existing = _listsTable.AsEnumerable()
                .FirstOrDefault(r => r["Name"].ToString().Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                MessageBox.Show($"A list named '{name}' already exists.", "Duplicate Name",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var speciesData = new DataTable();
                speciesData.Columns.Add("Species", typeof(string));
                speciesData.Columns.Add("Value", typeof(string));

                if (isExcel)
                {
                    using var workbook = new XLWorkbook(filePath);
                    var worksheet = workbook.Worksheet(1);
                    int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                    for (int row = 2; row <= lastRow; row++)
                    {
                        string species = worksheet.Cell(row, 1).GetString().Trim();
                        string value = worksheet.Cell(row, 2).GetString().Trim();
                        if (!string.IsNullOrEmpty(species))
                        {
                            speciesData.Rows.Add(species, value);
                        }
                    }
                }
                else
                {
                    using var reader = new StreamReader(filePath, Encoding.UTF8);
                    string line;
                    bool isFirst = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (isFirst) { isFirst = false; continue; }
                        var parts = line.Contains(';') ? line.Split(';') : line.Split(',');
                        if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]))
                        {
                            speciesData.Rows.Add(parts[0].Trim(), parts[1].Trim());
                        }
                    }
                }

                string fileName = MakeFileName(name, indexType);
                string destPath = Path.Combine(_userListsDirectory, fileName);

                // Close the destination file before touching the bound list grid: adding the
                // manifest row raises SelectionChanged, which immediately reloads this file.
                using (var writer = new StreamWriter(destPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("Species;Value");
                    foreach (DataRow row in speciesData.Rows)
                    {
                        writer.WriteLine($"{row["Species"]};{row["Value"]}");
                    }
                }

                _listsTable.Rows.Add(name, indexType, speciesData.Rows.Count, fileName, string.Empty);
                SaveManifest();
                UpdateStats();

                MessageBox.Show($"Imported '{name}' with {speciesData.Rows.Count} species for {indexType}.",
                    "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportListCsv()
        {
            var drv = SelectedListRow();
            if (drv == null || _currentSpeciesTable == null)
            {
                MessageBox.Show("Please select a list to export.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = drv["Name"].ToString();

            using var dialog = new SaveFileDialog
            {
                Title = "Export List to CSV",
                FileName = $"{name.Replace(" ", "_")}_export.csv",
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
                    writer.WriteLine("Species;Value");
                    foreach (DataRow row in _currentSpeciesTable.Rows)
                    {
                        writer.WriteLine($"{row["Species"]};{row["Value"]}");
                    }
                }

                MessageBox.Show($"Exported {_currentSpeciesTable.Rows.Count} species.",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportListExcel()
        {
            var drv = SelectedListRow();
            if (drv == null || _currentSpeciesTable == null)
            {
                MessageBox.Show("Please select a list to export.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = drv["Name"].ToString();
            string indexType = drv["IndexType"].ToString();

            using var dialog = new SaveFileDialog
            {
                Title = "Export List to Excel",
                FileName = $"{name.Replace(" ", "_")}_export.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (dialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(name.Length > 31 ? name.Substring(0, 31) : name);

                worksheet.Cell(1, 1).Value = "Species";
                worksheet.Cell(1, 2).Value = "Value";
                worksheet.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (DataRow dataRow in _currentSpeciesTable.Rows)
                {
                    worksheet.Cell(row, 1).Value = dataRow["Species"].ToString();
                    worksheet.Cell(row, 2).Value = dataRow["Value"].ToString();
                    row++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(UiHelpers.EnsureExtension(dialog.FileName, ".xlsx"));

                MessageBox.Show($"Exported {_currentSpeciesTable.Rows.Count} species to Excel.\nIndex Type: {indexType}",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        private static void ShowHelp()
        {
            const string helpText = @"User Custom Lists Manager
========================

This tool allows you to create and manage your own custom species lists for use with various ecological indices.

KEY FEATURES:

1. CREATE CUSTOM LISTS
   - Click 'Add List' to create a new list
   - Give it a name and select the index type it's designed for
   - Add a description if needed

2. INDEX TYPES
   Each list can be assigned to one of these index types:
   - Foram-AMBI: Species with ecological groups (EG1-EG5)
   - FSI: Species with Sensitive (S) or Tolerant (T) classification
   - TSI-Med: Tolerant species list for Mediterranean
   - FoRAM Index: Species with functional groups (SB, ST, SH)
   - BQI: Species with sensitivity values
   - BENTIX: Species with ecological groups
   - NQIf: Norwegian Quality Index species
   - Custom: User-defined classifications

3. ADD SPECIES
   - Select a list and click 'Add Species'
   - Enter the species name and its value/category
   - Values depend on the index type (e.g., '1-5' for Foram-AMBI, 'S/T' for FSI)

4. IMPORT/EXPORT
   - Import existing lists from CSV or Excel files
   - Export your lists for backup or sharing

5. FILE STORAGE
   Lists are stored in the 'user_lists' folder inside the ForamEcoQS user data directory.

NOTE: Custom lists can be used alongside the built-in databanks for index calculations.";

            TextViewerDialog.Show("About Custom Lists", helpText, new Size(620, 560));
        }

        private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_hasUnsavedChanges)
            {
                return;
            }

            var result = MessageBox.Show(
                "You have unsaved changes to the current species list. Save before closing?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SaveSpecies();
            }
            else if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        #region Static accessors used by the calculation code

        /// <summary>Gets all user custom lists for a specific index type ("All" for every list).</summary>
        public static List<UserCustomList> GetUserListsForIndex(string indexType)
        {
            var lists = new List<UserCustomList>();
            string manifestPath = Path.Combine(AppData.Directory, "user_lists", "manifest.csv");

            if (!File.Exists(manifestPath))
            {
                return lists;
            }

            try
            {
                using var reader = new StreamReader(manifestPath, Encoding.UTF8);
                string line;
                bool isFirst = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirst) { isFirst = false; continue; }
                    var parts = line.Split(';');
                    if (parts.Length >= 4)
                    {
                        string listIndexType = parts[1].Trim();
                        if (indexType == "All" || listIndexType.Equals(indexType, StringComparison.OrdinalIgnoreCase))
                        {
                            lists.Add(new UserCustomList
                            {
                                Name = parts[0].Trim(),
                                IndexType = listIndexType,
                                FileName = parts[3].Trim(),
                                SpeciesCount = int.TryParse(parts[2].Trim(), out int c) ? c : 0
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                // A missing or malformed manifest simply yields no lists.
            }

            return lists;
        }

        /// <summary>Loads the species data of one user custom list.</summary>
        public static Dictionary<string, string> LoadUserList(string fileName)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string filePath = Path.Combine(AppData.Directory, "user_lists", fileName);

            if (!File.Exists(filePath))
            {
                return data;
            }

            try
            {
                using var reader = new StreamReader(filePath, Encoding.UTF8);
                string line;
                bool isFirst = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirst) { isFirst = false; continue; }
                    var parts = line.Split(';');
                    if (parts.Length >= 2)
                    {
                        data[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
            catch (Exception)
            {
                // Ignore unreadable lists.
            }

            return data;
        }

        #endregion
    }

    /// <summary>Represents a user-defined custom list.</summary>
    public class UserCustomList
    {
        public string Name { get; set; }
        public string IndexType { get; set; }
        public string FileName { get; set; }
        public int SpeciesCount { get; set; }
    }

    /// <summary>Create/edit dialog for a custom list's metadata.</summary>
    public class ListPropertiesDialog : Dialog<bool>
    {
        private readonly TextBox _nameBox;
        private readonly DropDown _indexCombo;
        private readonly TextArea _descBox;

        public string ListName => _nameBox.Text;
        public string IndexType => _indexCombo.SelectedIndex >= 0 ? _indexCombo.Items[_indexCombo.SelectedIndex].Text : string.Empty;
        public string Description => _descBox.Text;

        public ListPropertiesDialog(
            string title,
            string[] indexTypes,
            string initialName = "",
            string initialIndexType = null,
            string initialDescription = "",
            string confirmText = "Create",
            bool showDescription = true)
        {
            Title = title;
            ClientSize = new Size(460, showDescription ? 260 : 180);
            Resizable = false;
            this.Prepare();

            _nameBox = new TextBox { Text = initialName ?? string.Empty };

            _indexCombo = new DropDown();
            foreach (var type in indexTypes)
            {
                _indexCombo.Items.Add(type);
            }
            int initialIndex = 0;
            if (!string.IsNullOrEmpty(initialIndexType))
            {
                int found = _indexCombo.Items.ToList().FindIndex(i => i.Text == initialIndexType);
                if (found >= 0)
                {
                    initialIndex = found;
                }
            }
            _indexCombo.SelectedIndex = initialIndex;

            _descBox = new TextArea { Text = initialDescription ?? string.Empty, Height = 60 };

            var confirmButton = new Button { Text = confirmText, Width = 90 };
            confirmButton.Click += (s, e) => Close(true);

            var cancelButton = new Button { Text = "Cancel", Width = 90 };
            cancelButton.Click += (s, e) => Close(false);

            DefaultButton = confirmButton;
            AbortButton = cancelButton;

            var layout = new TableLayout
            {
                Padding = new Padding(20),
                Spacing = new Size(8, 10),
                Rows =
                {
                    new TableRow(new Label { Text = "List Name:", VerticalAlignment = VerticalAlignment.Center },
                                 new TableCell(_nameBox, true)),
                    new TableRow(new Label { Text = "Index Type:", VerticalAlignment = VerticalAlignment.Center },
                                 new TableCell(_indexCombo, true))
                }
            };

            if (showDescription)
            {
                layout.Rows.Add(new TableRow(
                    new Label { Text = "Description:", VerticalAlignment = VerticalAlignment.Top },
                    new TableCell(_descBox, true)));
            }

            layout.Rows.Add(new TableRow(null, new TableCell(new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Items = { confirmButton, cancelButton }
            }, true)));
            layout.Rows.Add(null);

            Content = layout;
        }
    }

    /// <summary>"Species name + free-form value" prompt for custom lists.</summary>
    public class AddSpeciesValueDialog : Dialog<bool>
    {
        private readonly TextBox _speciesBox;
        private readonly TextBox _valueBox;

        public string SpeciesName => _speciesBox.Text;
        public string Value => _valueBox.Text;

        public AddSpeciesValueDialog()
        {
            Title = "Add Species";
            ClientSize = new Size(400, 170);
            Resizable = false;
            this.Prepare();

            _speciesBox = new TextBox();
            _valueBox = new TextBox();

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
                    new TableRow(new Label { Text = "Value/Category:", VerticalAlignment = VerticalAlignment.Center },
                                 new TableCell(_valueBox, true)),
                    new TableRow(null, new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { addButton, cancelButton }
                    }, true)),
                    null
                }
            };
        }
    }
}
