//MIT License
// Form2.cs - Enhanced DataBank Viewer with search, filter, and statistics (read-only)

using ClosedXML.Excel;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ForamEcoQS
{
    public partial class DataBankViewerForm : Form
    {
        private DataTable sourceDatabank;
        private DataTable viewTable;
        private DataGridView databankGrid;
        private TextBox searchBox;
        private ComboBox filterCombo;
        private Label statsLabel;
        private MenuStrip menuStrip;
        private string databankName;

        public DataBankViewerForm(DataTable databank, string name = "Foram-AMBI Databank")
        {
            sourceDatabank = databank;
            databankName = name;
            InitializeComponent();
            LoadDatabank();
        }

        // Keep original constructor for compatibility
        public DataBankViewerForm(DataTable databank) : this(databank, "Foram-AMBI Databank")
        {
        }

        private void InitializeComponent()
        {
            this.Text = $"DataBank Viewer - {databankName}";
            this.Size = new Size(750, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(500, 400);

            // Menu strip
            menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;

            var fileMenu = new ToolStripMenuItem("File");
            var exportCsvMenuItem = new ToolStripMenuItem("Export to CSV...", null, ExportCsv_Click);
            var exportExcelMenuItem = new ToolStripMenuItem("Export to Excel...", null, ExportExcel_Click);
            var closeMenuItem = new ToolStripMenuItem("Close", null, (s, e) => this.Close());
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { 
                exportCsvMenuItem, exportExcelMenuItem, 
                new ToolStripSeparator(), 
                closeMenuItem 
            });

            var helpMenu = new ToolStripMenuItem("Help");
            var aboutMenuItem = new ToolStripMenuItem("About Ecological Groups", null, ShowEcoGroupsHelp_Click);
            helpMenu.DropDownItems.Add(aboutMenuItem);

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, helpMenu });
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
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            filterCombo.Items.AddRange(new string[] { "All", "EG1 - Sensitive", "EG2 - Indifferent", "EG3 - Tolerant", "EG4 - 2nd Order Opportunistic", "EG5 - 1st Order Opportunistic" });
            filterCombo.SelectedIndex = 0;
            filterCombo.SelectedIndexChanged += FilterCombo_SelectedIndexChanged;

            topPanel.Controls.AddRange(new Control[] { searchLabel, searchBox, filterLabel, filterCombo });

            // Bottom panel with statistics and buttons
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                Padding = new Padding(10)
            };

            statsLabel = new Label
            {
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 9)
            };

            var exportCsvButton = new Button
            {
                Text = "Export CSV",
                Location = new Point(10, 40),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White
            };
            exportCsvButton.Click += ExportCsv_Click;

            var exportExcelButton = new Button
            {
                Text = "Export Excel",
                Location = new Point(120, 40),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(95, 158, 160),
                ForeColor = Color.White
            };
            exportExcelButton.Click += ExportExcel_Click;

            var closeButton = new Button
            {
                Text = "Close",
                Location = new Point(610, 40),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat
            };
            closeButton.Click += (s, e) => this.Close();

            bottomPanel.Controls.AddRange(new Control[] { statsLabel, exportCsvButton, exportExcelButton, closeButton });

            // DataGridView
            databankGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersWidth = 50
            };
            databankGrid.CellFormatting += DatabankGrid_CellFormatting;

            var gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 5, 10, 5)
            };
            gridPanel.Controls.Add(databankGrid);

            // Add controls in correct order for docking
            this.Controls.Add(gridPanel);      // Fill - added first but docks last
            this.Controls.Add(bottomPanel);    // Bottom
            this.Controls.Add(topPanel);       // Top (below menu)
            this.Controls.Add(menuStrip);      // Menu at very top
        }

        private void LoadDatabank()
        {
            if (sourceDatabank == null || sourceDatabank.Rows.Count == 0)
            {
                statsLabel.Text = "No data available.";
                return;
            }

            // Create a copy for viewing with standardized column names
            viewTable = new DataTable();
            viewTable.Columns.Add("Species", typeof(string));
            viewTable.Columns.Add("Ecogroup", typeof(string));

            // Copy data from source
            foreach (DataRow row in sourceDatabank.Rows)
            {
                string species = row[0]?.ToString() ?? "";
                string ecogroup = sourceDatabank.Columns.Count >= 2 ? (row[1]?.ToString() ?? "") : "";
                viewTable.Rows.Add(species, ecogroup);
            }

            // Set up columns manually before binding
            databankGrid.AutoGenerateColumns = false;
            databankGrid.Columns.Clear();

            var speciesCol = new DataGridViewTextBoxColumn
            {
                Name = "Species",
                HeaderText = "Species",
                DataPropertyName = "Species",
                Width = 450,
                ReadOnly = true
            };

            var ecogroupCol = new DataGridViewTextBoxColumn
            {
                Name = "Ecogroup",
                HeaderText = "Ecogroup",
                DataPropertyName = "Ecogroup",
                Width = 100,
                ReadOnly = true
            };

            databankGrid.Columns.Add(speciesCol);
            databankGrid.Columns.Add(ecogroupCol);

            // Now bind the data
            databankGrid.DataSource = viewTable;

            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            if (viewTable == null) return;

            int total = viewTable.Rows.Count;
            int[] egCounts = new int[5];

            foreach (DataRow row in viewTable.Rows)
            {
                string eg = row["Ecogroup"]?.ToString()?.Trim() ?? "";
                if (int.TryParse(eg, out int egNum) && egNum >= 1 && egNum <= 5)
                {
                    egCounts[egNum - 1]++;
                }
            }

            statsLabel.Text = $"Total: {total} species  |  " +
                             $"EG1: {egCounts[0]}  |  EG2: {egCounts[1]}  |  EG3: {egCounts[2]}  |  " +
                             $"EG4: {egCounts[3]}  |  EG5: {egCounts[4]}";
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
            if (sourceDatabank == null) return;

            string searchText = searchBox.Text.Trim().ToLower();
            string filterEcogroup = "";
            
            switch (filterCombo.SelectedIndex)
            {
                case 1: filterEcogroup = "1"; break;
                case 2: filterEcogroup = "2"; break;
                case 3: filterEcogroup = "3"; break;
                case 4: filterEcogroup = "4"; break;
                case 5: filterEcogroup = "5"; break;
            }

            // Build filtered table with standardized column names
            viewTable = new DataTable();
            viewTable.Columns.Add("Species", typeof(string));
            viewTable.Columns.Add("Ecogroup", typeof(string));

            foreach (DataRow row in sourceDatabank.Rows)
            {
                string species = row[0]?.ToString() ?? "";
                string ecogroup = sourceDatabank.Columns.Count >= 2 ? (row[1]?.ToString()?.Trim() ?? "") : "";

                bool matchesSearch = string.IsNullOrEmpty(searchText) || species.ToLower().Contains(searchText);
                bool matchesFilter = string.IsNullOrEmpty(filterEcogroup) || ecogroup == filterEcogroup;

                if (matchesSearch && matchesFilter)
                {
                    viewTable.Rows.Add(species, ecogroup);
                }
            }

            databankGrid.DataSource = viewTable;
            UpdateStatistics();
        }

        private void DatabankGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || databankGrid.Rows[e.RowIndex].IsNewRow) return;
            if (databankGrid.Columns.Count < 2) return;

            try
            {
                var row = databankGrid.Rows[e.RowIndex];
                // Use column index 1 (Ecogroup) instead of name
                string ecogroup = row.Cells[1]?.Value?.ToString()?.Trim() ?? "";

                // Color code by ecogroup
                Color backColor = ecogroup switch
                {
                    "1" => Color.FromArgb(144, 238, 144),  // Light green - Sensitive
                    "2" => Color.FromArgb(173, 216, 230),  // Light blue - Indifferent
                    "3" => Color.FromArgb(255, 255, 150),  // Light yellow - Tolerant
                    "4" => Color.FromArgb(255, 200, 100),  // Light orange - 2nd Order Opp.
                    "5" => Color.FromArgb(255, 150, 150),  // Light red - 1st Order Opp.
                    _ => Color.White
                };

                row.DefaultCellStyle.BackColor = backColor;
            }
            catch
            {
                // Ignore formatting errors
            }
        }

        private void ExportCsv_Click(object sender, EventArgs e)
        {
            if (viewTable == null || viewTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                Title = "Export Databank to CSV",
                FileName = $"{databankName.Replace(" ", "_")}_export.csv"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var writer = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8);
                    writer.WriteLine("Species;Ecogroup");
                    foreach (DataRow row in viewTable.Rows)
                    {
                        writer.WriteLine($"{row["Species"]};{row["Ecogroup"]}");
                    }
                    MessageBox.Show($"Exported {viewTable.Rows.Count} species to CSV.", "Export Complete", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting: {ex.Message}", "Export Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportExcel_Click(object sender, EventArgs e)
        {
            if (viewTable == null || viewTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Export Databank to Excel",
                FileName = $"{databankName.Replace(" ", "_")}_export.xlsx"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Databank");

                    // Headers
                    worksheet.Cell(1, 1).Value = "Species";
                    worksheet.Cell(1, 2).Value = "Ecogroup";
                    worksheet.Row(1).Style.Font.Bold = true;

                    // Data with color coding
                    int row = 2;
                    foreach (DataRow dataRow in viewTable.Rows)
                    {
                        string species = dataRow["Species"]?.ToString() ?? "";
                        string ecogroup = dataRow["Ecogroup"]?.ToString()?.Trim() ?? "";

                        worksheet.Cell(row, 1).Value = species;
                        worksheet.Cell(row, 2).Value = ecogroup;

                        // Color code row
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
                    workbook.SaveAs(saveDialog.FileName);

                    MessageBox.Show($"Exported {viewTable.Rows.Count} species to Excel.", "Export Complete", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting: {ex.Message}", "Export Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowEcoGroupsHelp_Click(object sender, EventArgs e)
        {
            string helpText = @"FORAM-AMBI ECOLOGICAL GROUPS

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

            var helpForm = new Form
            {
                Text = "About Ecological Groups",
                Size = new Size(550, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = this.Icon
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Text = helpText,
                Font = new Font("Segoe UI", 10)
            };

            helpForm.Controls.Add(textBox);
            helpForm.ShowDialog(this);
        }

        private void DataBankViewerForm_Load(object sender, EventArgs e)
        {
            // Already handled in constructor
        }
    }
}