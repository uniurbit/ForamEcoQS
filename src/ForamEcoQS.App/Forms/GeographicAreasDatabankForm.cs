//MIT License
// GeographicAreasDatabankForm.cs - Geographic areas and environmental references database.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
    /// Manages the geographic areas / environments database with bibliographic references.
    /// </summary>
    public class GeographicAreasDatabankForm : Dialog
    {
        private readonly DataGridView _databankGrid;
        private readonly TextBox _searchBox;
        private readonly Label _statsLabel;
        private readonly DropDown _filterCombo;

        private DataTable _databankTable;
        private readonly string _databankPath;
        private bool _hasUnsavedChanges;

        public event EventHandler DatabankUpdated;

        private static readonly string[] EnvironmentTypes =
        {
            "Marine Shelf", "Transitional Waters", "Fjord", "Coral Reef", "Deep-Sea", "Estuarine", "Lagoon"
        };

        private static readonly string[] Regions =
        {
            "Mediterranean", "Atlantic", "Pacific", "Indian", "Arctic", "Antarctic", "Tropical", "Global"
        };

        public GeographicAreasDatabankForm()
        {
            Title = "Geographic Areas Database";
            ClientSize = new Size(1000, 620);
            MinimumSize = new Size(800, 500);
            this.Prepare();

            // The editable copy lives in the user folder; seed it from the shipped one.
            string userPath = AppData.File("geographic_areas_databank.csv");
            string originalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "geographic_areas_databank.csv");

            if (!File.Exists(userPath) && File.Exists(originalPath))
            {
                try
                {
                    File.Copy(originalPath, userPath);
                }
                catch (Exception)
                {
                    // Fall through: an empty database is still usable.
                }
            }

            _databankPath = userPath;

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
                            new Command((s, e) => ExportToExcelDialog()) { MenuText = "Export to Excel..." }
                        }
                    },
                    new SubMenuItem
                    {
                        Text = "&Edit",
                        Items =
                        {
                            new Command((s, e) => AddArea()) { MenuText = "Add New Area..." },
                            new Command((s, e) => RemoveSelectedAreas()) { MenuText = "Remove Selected" }
                        }
                    },
                    new SubMenuItem
                    {
                        Text = "&Help",
                        Items = { new Command((s, e) => ShowHelp()) { MenuText = "About Geographic Areas" } }
                    }
                }
            };

            _searchBox = new TextBox { Width = 200 };
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _filterCombo = new DropDown { Width = 160 };
            _filterCombo.Items.Add("All");
            _filterCombo.Items.Add("Marine Shelf");
            _filterCombo.Items.Add("Transitional Waters");
            _filterCombo.Items.Add("Fjord");
            _filterCombo.Items.Add("Coral Reef");
            _filterCombo.Items.Add("Deep-Sea");
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
            _databankGrid.CellContentClick += DatabankGrid_CellContentClick;

            _statsLabel = new Label { Font = SystemFonts.Default(9) };

            var addAreaButton = UiHelpers.ActionButton("Add Area", AppColors.SeaGreen, (s, e) => AddArea(), 110);
            var removeAreaButton = UiHelpers.ActionButton("Remove", AppColors.IndianRed, (s, e) => RemoveSelectedAreas(), 110);
            var saveButton = UiHelpers.ActionButton("Save Changes", AppColors.SeaGreen, (s, e) => SaveDatabank(), 130);
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
                            new Label { Text = "Environment:" }, _filterCombo
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
                            addAreaButton, removeAreaButton, saveButton,
                            new StackLayoutItem(null, true),
                            closeButton
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
            _databankTable.Columns.Add("AreaID", typeof(int));
            _databankTable.Columns.Add("AreaName", typeof(string));
            _databankTable.Columns.Add("EnvironmentType", typeof(string));
            _databankTable.Columns.Add("Region", typeof(string));
            _databankTable.Columns.Add("Description", typeof(string));
            _databankTable.Columns.Add("Reference", typeof(string));
            _databankTable.Columns.Add("DOI", typeof(string));
            _databankTable.Columns.Add("UsedForIndices", typeof(string));

            if (File.Exists(_databankPath))
            {
                try
                {
                    using var reader = new StreamReader(_databankPath, Encoding.UTF8);
                    string line;
                    bool isFirst = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (isFirst) { isFirst = false; continue; }
                        var parts = line.Split(';');
                        if (parts.Length >= 7 && int.TryParse(parts[0].Trim(), out int areaId))
                        {
                            _databankTable.Rows.Add(
                                areaId,
                                parts[1].Trim(),
                                parts[2].Trim(),
                                parts[3].Trim(),
                                parts[4].Trim(),
                                parts[5].Trim(),
                                parts[6].Trim(),
                                parts.Length >= 8 ? parts[7].Trim() : string.Empty);
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
                Name = "AreaID",
                HeaderText = "ID",
                DataPropertyName = "AreaID",
                ValueType = typeof(int),
                Width = 50,
                ReadOnly = true
            });

            _databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AreaName",
                HeaderText = "Area Name",
                DataPropertyName = "AreaName",
                Width = 150
            });

            var envTypeCol = new DataGridViewComboBoxColumn
            {
                Name = "EnvironmentType",
                HeaderText = "Environment Type",
                DataPropertyName = "EnvironmentType",
                Width = 140
            };
            envTypeCol.Items.AddRange(EnvironmentTypes.Cast<object>().ToArray());
            _databankGrid.Columns.Add(envTypeCol);

            var regionCol = new DataGridViewComboBoxColumn
            {
                Name = "Region",
                HeaderText = "Region",
                DataPropertyName = "Region",
                Width = 110
            };
            regionCol.Items.AddRange(Regions.Cast<object>().ToArray());
            _databankGrid.Columns.Add(regionCol);

            _databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Description",
                DataPropertyName = "Description",
                Width = 250
            });

            _databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Reference",
                HeaderText = "Reference",
                DataPropertyName = "Reference",
                Width = 180
            });

            _databankGrid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = "DOI",
                HeaderText = "DOI (click to open)",
                DataPropertyName = "DOI",
                Width = 150
            });

            _databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "UsedForIndices",
                HeaderText = "Used For Indices",
                DataPropertyName = "UsedForIndices",
                Width = 150
            });

            _databankGrid.DataSource = _databankTable;

            UpdateStats();
            _hasUnsavedChanges = false;
        }

        private void DatabankGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Open the DOI in the default browser when its cell is clicked.
            int doiIndex = _databankGrid.Columns.IndexOf("DOI");
            if (e.ColumnIndex != doiIndex || e.RowIndex < 0)
            {
                return;
            }

            var doi = _databankGrid.Rows[e.RowIndex].Cells["DOI"].Value?.ToString();
            if (string.IsNullOrEmpty(doi))
            {
                return;
            }

            try
            {
                string url = doi.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? doi : $"https://doi.org/{doi}";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open DOI link: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateStats()
        {
            if (_databankTable == null)
            {
                return;
            }

            int total = _databankTable.Rows.Count;
            var envTypes = _databankTable.AsEnumerable()
                .GroupBy(r => r["EnvironmentType"]?.ToString() ?? "Unknown")
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();

            _statsLabel.Text = $"Total: {total} geographic areas | {string.Join(" | ", envTypes)}";
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
                string escaped = search.Replace("'", "''");
                conditions.Add($"(AreaName LIKE '%{escaped}%' OR Reference LIKE '%{escaped}%' OR Description LIKE '%{escaped}%')");
            }

            if (filter != "All")
            {
                conditions.Add($"EnvironmentType = '{filter.Replace("'", "''")}'");
            }

            _databankTable.DefaultView.RowFilter = conditions.Count > 0 ? string.Join(" AND ", conditions) : string.Empty;
        }

        private void DatabankGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_databankGrid.Columns.Count > 2 && e.ColumnIndex == 2 && e.Value != null)
            {
                string val = e.Value.ToString();
                e.CellStyle.BackColor = val switch
                {
                    "Marine Shelf" => Color.FromArgb(200, 230, 255),
                    "Transitional Waters" => Color.FromArgb(200, 255, 230),
                    "Fjord" => Color.FromArgb(180, 200, 255),
                    "Coral Reef" => Color.FromArgb(255, 230, 200),
                    "Deep-Sea" => Color.FromArgb(220, 220, 240),
                    _ => Colors.White
                };
            }
        }

        private int NextAreaId(int offset = 0)
        {
            return _databankTable.Rows.Count > 0
                ? _databankTable.AsEnumerable().Max(r => r.Field<int>("AreaID")) + 1 + offset
                : 1 + offset;
        }

        private void AddArea()
        {
            using var addDialog = new AddGeographicAreaDialog(EnvironmentTypes, Regions);
            if (addDialog.ShowModal(this) != true || string.IsNullOrWhiteSpace(addDialog.AreaName))
            {
                return;
            }

            _databankTable.Rows.Add(
                NextAreaId(),
                addDialog.AreaName.Trim(),
                addDialog.EnvironmentType,
                addDialog.Region,
                addDialog.Description.Trim(),
                addDialog.Reference.Trim(),
                addDialog.Doi.Trim(),
                string.Empty);

            _hasUnsavedChanges = true;
            UpdateStats();
        }

        private void RemoveSelectedAreas()
        {
            var selected = _databankGrid.SelectedRows;
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select one or more areas to remove.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Remove {selected.Count} selected area(s)?",
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

        private void SaveDatabank()
        {
            try
            {
                using (var writer = new StreamWriter(_databankPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("AreaID;AreaName;EnvironmentType;Region;Description;Reference;DOI;UsedForIndices");
                    foreach (DataRow row in _databankTable.Rows)
                    {
                        writer.WriteLine(
                            $"{row["AreaID"]};{row["AreaName"]};{row["EnvironmentType"]};{row["Region"]};" +
                            $"{row["Description"]};{row["Reference"]};{row["DOI"]};{row["UsedForIndices"]}");
                    }
                }

                _hasUnsavedChanges = false;
                DatabankUpdated?.Invoke(this, EventArgs.Empty);

                MessageBox.Show($"Database saved with {_databankTable.Rows.Count} geographic areas.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving database: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportFromCsvDialog()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import Geographic Areas",
                Filters = { Filters.Csv, Filters.All }
            };

            if (dialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                int imported = 0;
                using (var reader = new StreamReader(dialog.FileName, Encoding.UTF8))
                {
                    string line;
                    bool isFirst = true;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (isFirst) { isFirst = false; continue; }
                        var parts = line.Contains(';') ? line.Split(';') : line.Split(',');
                        if (parts.Length >= 6)
                        {
                            _databankTable.Rows.Add(
                                NextAreaId(imported),
                                parts[0].Trim(),
                                parts[1].Trim(),
                                parts[2].Trim(),
                                parts[3].Trim(),
                                parts[4].Trim(),
                                parts.Length > 5 ? parts[5].Trim() : string.Empty,
                                string.Empty);
                            imported++;
                        }
                    }
                }

                if (imported > 0)
                {
                    _hasUnsavedChanges = true;
                    UpdateStats();
                    MessageBox.Show($"Imported {imported} geographic areas.", "Import Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No valid rows found in the file.", "Import Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing file: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportFromExcelDialog()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import Geographic Areas from Excel",
                Filters = { Filters.Excel, Filters.All }
            };

            if (dialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook(dialog.FileName);
                var worksheet = workbook.Worksheet(1);
                int imported = 0;

                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                for (int row = 2; row <= lastRow; row++)
                {
                    string areaName = worksheet.Cell(row, 1).GetString().Trim();
                    if (string.IsNullOrEmpty(areaName))
                    {
                        continue;
                    }

                    _databankTable.Rows.Add(
                        NextAreaId(imported),
                        areaName,
                        worksheet.Cell(row, 2).GetString().Trim(),
                        worksheet.Cell(row, 3).GetString().Trim(),
                        worksheet.Cell(row, 4).GetString().Trim(),
                        worksheet.Cell(row, 5).GetString().Trim(),
                        worksheet.Cell(row, 6).GetString().Trim(),
                        string.Empty);
                    imported++;
                }

                if (imported > 0)
                {
                    _hasUnsavedChanges = true;
                    UpdateStats();
                    MessageBox.Show($"Imported {imported} geographic areas.", "Import Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No valid rows found in the file.", "Import Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing Excel file: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToCsvDialog()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export Geographic Areas",
                FileName = "geographic_areas_export.csv",
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
                    writer.WriteLine("AreaName;EnvironmentType;Region;Description;Reference;DOI;UsedForIndices");
                    foreach (DataRow row in _databankTable.Rows)
                    {
                        writer.WriteLine(
                            $"{row["AreaName"]};{row["EnvironmentType"]};{row["Region"]};{row["Description"]};" +
                            $"{row["Reference"]};{row["DOI"]};{row["UsedForIndices"]}");
                    }
                }

                MessageBox.Show($"Exported {_databankTable.Rows.Count} areas.", "Export Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting file: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToExcelDialog()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export Geographic Areas to Excel",
                FileName = "geographic_areas_export.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (dialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Geographic Areas");

                worksheet.Cell(1, 1).Value = "Area Name";
                worksheet.Cell(1, 2).Value = "Environment Type";
                worksheet.Cell(1, 3).Value = "Region";
                worksheet.Cell(1, 4).Value = "Description";
                worksheet.Cell(1, 5).Value = "Reference";
                worksheet.Cell(1, 6).Value = "DOI";
                worksheet.Cell(1, 7).Value = "Used For Indices";
                worksheet.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (DataRow dataRow in _databankTable.Rows)
                {
                    worksheet.Cell(row, 1).Value = dataRow["AreaName"].ToString();
                    worksheet.Cell(row, 2).Value = dataRow["EnvironmentType"].ToString();
                    worksheet.Cell(row, 3).Value = dataRow["Region"].ToString();
                    worksheet.Cell(row, 4).Value = dataRow["Description"].ToString();
                    worksheet.Cell(row, 5).Value = dataRow["Reference"].ToString();

                    string doi = dataRow["DOI"].ToString();
                    if (!string.IsNullOrEmpty(doi))
                    {
                        string url = doi.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? doi : $"https://doi.org/{doi}";
                        worksheet.Cell(row, 6).Value = doi;
                        worksheet.Cell(row, 6).SetHyperlink(new XLHyperlink(url));
                    }
                    worksheet.Cell(row, 7).Value = dataRow["UsedForIndices"].ToString();
                    row++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(UiHelpers.EnsureExtension(dialog.FileName, ".xlsx"));

                MessageBox.Show($"Exported {_databankTable.Rows.Count} areas to Excel.",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ShowHelp()
        {
            const string helpText = @"Geographic Areas & Environmental References Database

This database stores information about geographic areas and marine environments
used in foraminiferal ecological assessments, with bibliographic references.

ENVIRONMENT TYPES:
• Marine Shelf - Coastal and continental shelf environments
• Transitional Waters - Lagoons, estuaries, and coastal transition zones
• Fjord - Norwegian and similar fjord systems
• Coral Reef - Tropical and subtropical coral reef environments
• Deep-Sea - Bathyal and abyssal environments

FIELDS:
• Area Name - Name of the geographic area
• Environment Type - Type of marine environment
• Region - Geographic region (Mediterranean, Atlantic, etc.)
• Description - Brief description of the environment
• Reference - Bibliographic reference (Author et al., Year)
• DOI - Digital Object Identifier for the reference

USAGE:
This database helps associate Foram-AMBI databanks and EcoQS thresholds
with specific geographic regions and environments.

Click on DOI links to open the reference in your browser.";

            TextViewerDialog.Show("About Geographic Areas Database", helpText, new Size(600, 520));
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
                SaveDatabank();
            }
            else if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        /// <summary>Reads every geographic area from the user database.</summary>
        public static List<GeographicArea> GetAllAreas()
        {
            var areas = new List<GeographicArea>();

            string path = AppData.File("geographic_areas_databank.csv");
            if (!File.Exists(path))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "geographic_areas_databank.csv");
            }

            if (!File.Exists(path))
            {
                return areas;
            }

            try
            {
                using var reader = new StreamReader(path, Encoding.UTF8);
                string line;
                bool isFirst = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirst) { isFirst = false; continue; }
                    var parts = line.Split(';');
                    if (parts.Length >= 7 && int.TryParse(parts[0].Trim(), out int id))
                    {
                        areas.Add(new GeographicArea
                        {
                            AreaID = id,
                            AreaName = parts[1].Trim(),
                            EnvironmentType = parts[2].Trim(),
                            Region = parts[3].Trim(),
                            Description = parts[4].Trim(),
                            Reference = parts[5].Trim(),
                            DOI = parts[6].Trim()
                        });
                    }
                }
            }
            catch (Exception)
            {
                // A missing or malformed database simply yields no areas.
            }

            return areas;
        }
    }

    /// <summary>Represents a geographic area with environmental information.</summary>
    public class GeographicArea
    {
        public int AreaID { get; set; }
        public string AreaName { get; set; }
        public string EnvironmentType { get; set; }
        public string Region { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public string DOI { get; set; }

        public override string ToString() => $"{AreaName} ({Region}) - {Reference}";
    }

    /// <summary>Entry form for a new geographic area.</summary>
    public class AddGeographicAreaDialog : Dialog<bool>
    {
        private readonly TextBox _areaNameBox;
        private readonly DropDown _envCombo;
        private readonly DropDown _regionCombo;
        private readonly TextArea _descriptionBox;
        private readonly TextBox _referenceBox;
        private readonly TextBox _doiBox;

        public string AreaName => _areaNameBox.Text;
        public string EnvironmentType => _envCombo.SelectedIndex >= 0 ? _envCombo.Items[_envCombo.SelectedIndex].Text : "Marine Shelf";
        public string Region => _regionCombo.SelectedIndex >= 0 ? _regionCombo.Items[_regionCombo.SelectedIndex].Text : "Mediterranean";
        public string Description => _descriptionBox.Text;
        public string Reference => _referenceBox.Text;
        public string Doi => _doiBox.Text;

        public AddGeographicAreaDialog(string[] environmentTypes, string[] regions)
        {
            Title = "Add Geographic Area";
            ClientSize = new Size(520, 400);
            Resizable = false;
            this.Prepare();

            _areaNameBox = new TextBox();
            _envCombo = new DropDown();
            foreach (var item in environmentTypes)
            {
                _envCombo.Items.Add(item);
            }
            _envCombo.SelectedIndex = 0;

            _regionCombo = new DropDown();
            foreach (var item in regions)
            {
                _regionCombo.Items.Add(item);
            }
            _regionCombo.SelectedIndex = 0;

            _descriptionBox = new TextArea { Height = 60 };
            _referenceBox = new TextBox();
            _doiBox = new TextBox();

            var addButton = new Button { Text = "Add", Width = 90 };
            addButton.Click += (s, e) => Close(true);

            var cancelButton = new Button { Text = "Cancel", Width = 90 };
            cancelButton.Click += (s, e) => Close(false);

            DefaultButton = addButton;
            AbortButton = cancelButton;

            Content = new TableLayout
            {
                Padding = new Padding(20),
                Spacing = new Size(8, 8),
                Rows =
                {
                    Row("Area Name:", _areaNameBox),
                    Row("Environment Type:", _envCombo),
                    Row("Region:", _regionCombo),
                    Row("Description:", _descriptionBox),
                    Row("Reference:", _referenceBox),
                    Row("DOI:", _doiBox),
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

        private static TableRow Row(string label, Control control) =>
            new TableRow(
                new Label { Text = label, VerticalAlignment = VerticalAlignment.Center },
                new TableCell(control, true));
    }
}
