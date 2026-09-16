//MIT License
// DataBankViewerForm.cs - Read-only Foram-AMBI databank viewer with search, filter and export.

using System;
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
    public class DataBankViewerForm : Form
    {
        private readonly DataTable _sourceDatabank;
        private readonly string _databankName;
        private DataTable _viewTable;

        private readonly DataGridView _databankGrid;
        private readonly TextBox _searchBox;
        private readonly DropDown _filterCombo;
        private readonly Label _statsLabel;

        public DataBankViewerForm(DataTable databank, string name = "Foram-AMBI Databank")
        {
            _sourceDatabank = databank;
            _databankName = name;

            Title = $"DataBank Viewer - {_databankName}";
            ClientSize = new Size(750, 600);
            MinimumSize = new Size(500, 400);
            this.Prepare();

            // ----- Menu -----
            var exportCsvCommand = new Command((s, e) => ExportCsv()) { MenuText = "Export to CSV..." };
            var exportExcelCommand = new Command((s, e) => ExportExcel()) { MenuText = "Export to Excel..." };
            var closeCommand = new Command((s, e) => Close()) { MenuText = "Close" };
            var aboutCommand = new Command((s, e) => ShowEcoGroupsHelp()) { MenuText = "About Ecological Groups" };

            Menu = new MenuBar
            {
                Items =
                {
                    new SubMenuItem
                    {
                        Text = "&File",
                        Items = { exportCsvCommand, exportExcelCommand, new SeparatorMenuItem(), closeCommand }
                    },
                    new SubMenuItem { Text = "&Help", Items = { aboutCommand } }
                }
            };

            // ----- Search / filter -----
            _searchBox = new TextBox { Width = 200 };
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _filterCombo = new DropDown { Width = 220 };
            _filterCombo.Items.Add("All");
            _filterCombo.Items.Add("EG1 - Sensitive");
            _filterCombo.Items.Add("EG2 - Indifferent");
            _filterCombo.Items.Add("EG3 - Tolerant");
            _filterCombo.Items.Add("EG4 - 2nd Order Opportunistic");
            _filterCombo.Items.Add("EG5 - 1st Order Opportunistic");
            _filterCombo.SelectedIndex = 0;
            _filterCombo.SelectedIndexChanged += (s, e) => ApplyFilter();

            // ----- Grid -----
            _databankGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = Compat.DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = Compat.DataGridViewAutoSizeColumnsMode.Fill
            };
            _databankGrid.CellFormatting += DatabankGrid_CellFormatting;

            // ----- Stats and buttons -----
            _statsLabel = new Label { Font = SystemFonts.Default(9) };

            var exportCsvButton = UiHelpers.ActionButton("Export CSV", AppColors.SteelBlue, (s, e) => ExportCsv(), 110);
            var exportExcelButton = UiHelpers.ActionButton("Export Excel", AppColors.Teal, (s, e) => ExportExcel(), 110);
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
                            new Label { Text = "Filter:" }, _filterCombo
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
                            exportCsvButton,
                            exportExcelButton,
                            new StackLayoutItem(null, true),
                            closeButton
                        }
                    }, true))
                }
            };

            LoadDatabank();
        }

        private void LoadDatabank()
        {
            if (_sourceDatabank == null || _sourceDatabank.Rows.Count == 0)
            {
                _statsLabel.Text = "No data available.";
                return;
            }

            _viewTable = BuildViewTable(null, null);

            _databankGrid.AutoGenerateColumns = false;
            _databankGrid.Columns.Clear();
            _databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Species",
                HeaderText = "Species",
                DataPropertyName = "Species",
                Width = 450,
                ReadOnly = true
            });
            _databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Ecogroup",
                HeaderText = "Ecogroup",
                DataPropertyName = "Ecogroup",
                Width = 100,
                ReadOnly = true
            });

            _databankGrid.DataSource = _viewTable;
            UpdateStatistics();
        }

        private DataTable BuildViewTable(string searchText, string filterEcogroup)
        {
            var table = new DataTable();
            table.Columns.Add("Species", typeof(string));
            table.Columns.Add("Ecogroup", typeof(string));

            foreach (DataRow row in _sourceDatabank.Rows)
            {
                string species = row[0]?.ToString() ?? string.Empty;
                string ecogroup = _sourceDatabank.Columns.Count >= 2 ? (row[1]?.ToString()?.Trim() ?? string.Empty) : string.Empty;

                bool matchesSearch = string.IsNullOrEmpty(searchText) || species.ToLowerInvariant().Contains(searchText);
                bool matchesFilter = string.IsNullOrEmpty(filterEcogroup) || ecogroup == filterEcogroup;

                if (matchesSearch && matchesFilter)
                {
                    table.Rows.Add(species, ecogroup);
                }
            }

            return table;
        }

        private void UpdateStatistics()
        {
            if (_viewTable == null)
            {
                return;
            }

            int total = _viewTable.Rows.Count;
            int[] egCounts = new int[5];

            foreach (DataRow row in _viewTable.Rows)
            {
                string eg = row["Ecogroup"]?.ToString()?.Trim() ?? string.Empty;
                if (int.TryParse(eg, out int egNum) && egNum >= 1 && egNum <= 5)
                {
                    egCounts[egNum - 1]++;
                }
            }

            _statsLabel.Text = $"Total: {total} species  |  " +
                               $"EG1: {egCounts[0]}  |  EG2: {egCounts[1]}  |  EG3: {egCounts[2]}  |  " +
                               $"EG4: {egCounts[3]}  |  EG5: {egCounts[4]}";
        }

        private void ApplyFilter()
        {
            if (_sourceDatabank == null)
            {
                return;
            }

            string searchText = _searchBox.Text.Trim().ToLowerInvariant();
            string filterEcogroup = _filterCombo.SelectedIndex switch
            {
                1 => "1",
                2 => "2",
                3 => "3",
                4 => "4",
                5 => "5",
                _ => string.Empty
            };

            _viewTable = BuildViewTable(searchText, filterEcogroup);
            _databankGrid.DataSource = _viewTable;
            UpdateStatistics();
        }

        private void DatabankGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _databankGrid.Columns.Count < 2)
            {
                return;
            }

            try
            {
                string ecogroup = _databankGrid.Rows[e.RowIndex].Cells[1]?.Value?.ToString()?.Trim() ?? string.Empty;

                e.CellStyle.BackColor = ecogroup switch
                {
                    "1" => Color.FromArgb(144, 238, 144),  // Light green - Sensitive
                    "2" => Color.FromArgb(173, 216, 230),  // Light blue - Indifferent
                    "3" => Color.FromArgb(255, 255, 150),  // Light yellow - Tolerant
                    "4" => Color.FromArgb(255, 200, 100),  // Light orange - 2nd Order Opp.
                    "5" => Color.FromArgb(255, 150, 150),  // Light red - 1st Order Opp.
                    _ => Colors.White
                };
            }
            catch (Exception)
            {
                // Ignore formatting errors
            }
        }

        private void ExportCsv()
        {
            if (_viewTable == null || _viewTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Databank to CSV",
                FileName = $"{_databankName.Replace(" ", "_")}_export.csv",
                Filters = { Filters.Csv }
            };

            if (saveDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                string path = UiHelpers.EnsureExtension(saveDialog.FileName, ".csv");
                using (var writer = new StreamWriter(path, false, Encoding.UTF8))
                {
                    writer.WriteLine("Species;Ecogroup");
                    foreach (DataRow row in _viewTable.Rows)
                    {
                        writer.WriteLine($"{row["Species"]};{row["Ecogroup"]}");
                    }
                }

                MessageBox.Show($"Exported {_viewTable.Rows.Count} species to CSV.", "Export Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportExcel()
        {
            if (_viewTable == null || _viewTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Databank to Excel",
                FileName = $"{_databankName.Replace(" ", "_")}_export.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (saveDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Databank");

                worksheet.Cell(1, 1).Value = "Species";
                worksheet.Cell(1, 2).Value = "Ecogroup";
                worksheet.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (DataRow dataRow in _viewTable.Rows)
                {
                    string species = dataRow["Species"]?.ToString() ?? string.Empty;
                    string ecogroup = dataRow["Ecogroup"]?.ToString()?.Trim() ?? string.Empty;

                    worksheet.Cell(row, 1).Value = species;
                    worksheet.Cell(row, 2).Value = ecogroup;

                    var xlColor = ecogroup switch
                    {
                        "1" => XLColor.FromArgb(144, 238, 144),
                        "2" => XLColor.FromArgb(173, 216, 230),
                        "3" => XLColor.FromArgb(255, 255, 150),
                        "4" => XLColor.FromArgb(255, 200, 100),
                        "5" => XLColor.FromArgb(255, 150, 150),
                        _ => XLColor.White
                    };
                    worksheet.Row(row).Style.Fill.BackgroundColor = xlColor;
                    row++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(UiHelpers.EnsureExtension(saveDialog.FileName, ".xlsx"));

                MessageBox.Show($"Exported {_viewTable.Rows.Count} species to Excel.", "Export Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowEcoGroupsHelp()
        {
            const string helpText = @"FORAM-AMBI ECOLOGICAL GROUPS

The Foram-AMBI index uses five ecological groups based on species sensitivity
to organic matter enrichment and oxygen depletion:

EG1 - SENSITIVE (Green)
Species very sensitive to organic enrichment. Present under unpolluted
conditions, absent or rare when organic matter increases.

EG2 - INDIFFERENT (Blue)
Species indifferent to enrichment. Always present in low densities
with non-significant variations over time.

EG3 - TOLERANT (Yellow)
Species tolerant to excess organic matter enrichment. May occur under
normal conditions but stimulated by organic enrichment.

EG4 - SECOND-ORDER OPPORTUNISTIC (Orange)
Opportunistic species occurring mainly in slight to pronounced
unbalanced conditions.

EG5 - FIRST-ORDER OPPORTUNISTIC (Red)
Highly opportunistic species, pioneers colonizing highly disturbed
and polluted sediments.

REFERENCES:
- Borja et al. (2000) Marine Pollution Bulletin 40:1100-1114
- Alve et al. (2016) Marine Environmental Research 122:1-12
- Jorissen et al. (2018) Marine Micropaleontology 140:33-45";

            TextViewerDialog.Show("About Ecological Groups", helpText, new Size(560, 520));
        }
    }
}
