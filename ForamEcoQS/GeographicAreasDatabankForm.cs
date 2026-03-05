//MIT License
// GeographicAreasDatabankForm.cs - Utility for managing geographic areas and environmental references

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
    /// <summary>
    /// Form for managing the geographic areas/environments database with bibliographic references
    /// </summary>
    public partial class GeographicAreasDatabankForm : Form
    {
        private DataGridView databankGrid;
        private TextBox searchBox;
        private Label statsLabel;
        private Button addAreaButton;
        private Button removeAreaButton;
        private Button saveButton;
        private Button closeButton;
        private ComboBox filterCombo;
        private MenuStrip menuStrip;

        private DataTable databankTable;
        private string databankPath;
        private bool hasUnsavedChanges = false;

        // Event to notify when databank is updated
        public event EventHandler DatabankUpdated;

        public GeographicAreasDatabankForm()
        {
            InitializeComponent();
            LoadDatabank();
        }

        private void InitializeComponent()
        {
            this.Text = "Geographic Areas Database";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 500);

            // Path handling
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            string userPath = Path.Combine(appDataPath, "geographic_areas_databank.csv");
            string originalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "geographic_areas_databank.csv");
            
            // If user file doesn't exist but original does, copy it
            if (!File.Exists(userPath) && File.Exists(originalPath))
            {
                try { File.Copy(originalPath, userPath); } catch {}
            }
            
            databankPath = userPath;

            // Menu strip
            menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;

            var fileMenu = new ToolStripMenuItem("File");
            var importMenuItem = new ToolStripMenuItem("Import from CSV...", null, ImportFromCsv_Click);
            var importExcelMenuItem = new ToolStripMenuItem("Import from Excel...", null, ImportFromExcel_Click);
            var exportMenuItem = new ToolStripMenuItem("Export to CSV...", null, ExportToCsv_Click);
            var exportExcelMenuItem = new ToolStripMenuItem("Export to Excel...", null, ExportToExcel_Click);
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
                importMenuItem, importExcelMenuItem,
                new ToolStripSeparator(),
                exportMenuItem, exportExcelMenuItem
            });

            var editMenu = new ToolStripMenuItem("Edit");
            var addMenuItem = new ToolStripMenuItem("Add New Area...", null, AddAreaButton_Click);
            var removeMenuItem = new ToolStripMenuItem("Remove Selected", null, RemoveAreaButton_Click);
            editMenu.DropDownItems.AddRange(new ToolStripItem[] { addMenuItem, removeMenuItem });

            var helpMenu = new ToolStripMenuItem("Help");
            var aboutMenuItem = new ToolStripMenuItem("About Geographic Areas", null, ShowHelp_Click);
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

            var filterLabel = new Label { Text = "Environment:", Location = new Point(290, 15), AutoSize = true };
            filterCombo = new ComboBox
            {
                Location = new Point(380, 12),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            filterCombo.Items.AddRange(new string[] { "All", "Marine Shelf", "Transitional Waters", "Fjord", "Coral Reef", "Deep-Sea" });
            filterCombo.SelectedIndex = 0;
            filterCombo.SelectedIndexChanged += FilterCombo_SelectedIndexChanged;

            topPanel.Controls.AddRange(new Control[] { searchLabel, searchBox, filterLabel, filterCombo });

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

            addAreaButton = new Button
            {
                Text = "Add Area",
                Location = new Point(10, 50),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            addAreaButton.Click += AddAreaButton_Click;

            removeAreaButton = new Button
            {
                Text = "Remove",
                Location = new Point(120, 50),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(205, 92, 92),
                ForeColor = Color.White
            };
            removeAreaButton.Click += RemoveAreaButton_Click;

            saveButton = new Button
            {
                Text = "Save Changes",
                Location = new Point(230, 50),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            saveButton.Click += SaveButton_Click;

            closeButton = new Button
            {
                Text = "Close",
                Location = new Point(360, 50),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat
            };
            closeButton.Click += (s, e) => this.Close();

            bottomPanel.Controls.AddRange(new Control[] { statsLabel, addAreaButton, removeAreaButton, saveButton, closeButton });

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

            // Add controls
            this.Controls.Add(gridPanel);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(topPanel);
            this.Controls.Add(menuStrip);

            this.FormClosing += Form_FormClosing;
        }

        private void LoadDatabank()
        {
            databankTable = new DataTable();
            databankTable.Columns.Add("AreaID", typeof(int));
            databankTable.Columns.Add("AreaName", typeof(string));
            databankTable.Columns.Add("EnvironmentType", typeof(string));
            databankTable.Columns.Add("Region", typeof(string));
            databankTable.Columns.Add("Description", typeof(string));
            databankTable.Columns.Add("Reference", typeof(string));
            databankTable.Columns.Add("DOI", typeof(string));
            databankTable.Columns.Add("UsedForIndices", typeof(string));

            if (File.Exists(databankPath))
            {
                try
                {
                    using var reader = new StreamReader(databankPath, Encoding.UTF8);
                    string line;
                    bool isFirst = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (isFirst) { isFirst = false; continue; }
                        var parts = line.Split(';');
                        if (parts.Length >= 7)
                        {
                            if (int.TryParse(parts[0].Trim(), out int areaId))
                            {
                                databankTable.Rows.Add(
                                    areaId,
                                    parts[1].Trim(),
                                    parts[2].Trim(),
                                    parts[3].Trim(),
                                    parts[4].Trim(),
                                    parts[5].Trim(),
                                    parts[6].Trim(),
                                    parts.Length >= 8 ? parts[7].Trim() : ""
                                );
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

            // Configure grid columns
            databankGrid.Columns.Clear();
            databankGrid.AutoGenerateColumns = false;

            databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AreaID",
                HeaderText = "ID",
                DataPropertyName = "AreaID",
                Width = 40,
                ReadOnly = true
            });

            databankGrid.Columns.Add(new DataGridViewTextBoxColumn
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
                Width = 120,
                FlatStyle = FlatStyle.Flat
            };
            envTypeCol.Items.AddRange("Marine Shelf", "Transitional Waters", "Fjord", "Coral Reef", "Deep-Sea", "Estuarine", "Lagoon");
            databankGrid.Columns.Add(envTypeCol);

            var regionCol = new DataGridViewComboBoxColumn
            {
                Name = "Region",
                HeaderText = "Region",
                DataPropertyName = "Region",
                Width = 100,
                FlatStyle = FlatStyle.Flat
            };
            regionCol.Items.AddRange("Mediterranean", "Atlantic", "Pacific", "Indian", "Arctic", "Antarctic", "Tropical", "Global");
            databankGrid.Columns.Add(regionCol);

            databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Description",
                DataPropertyName = "Description",
                Width = 250
            });

            databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Reference",
                HeaderText = "Reference",
                DataPropertyName = "Reference",
                Width = 180
            });

            databankGrid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = "DOI",
                HeaderText = "DOI",
                DataPropertyName = "DOI",
                Width = 150,
                LinkBehavior = LinkBehavior.HoverUnderline
            });

            databankGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "UsedForIndices",
                HeaderText = "Used For Indices",
                DataPropertyName = "UsedForIndices",
                Width = 150
            });

            databankGrid.DataSource = databankTable;
            databankGrid.CellContentClick += DatabankGrid_CellContentClick;

            UpdateStats();
            hasUnsavedChanges = false;
        }

        private void DatabankGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Handle DOI link click
            if (e.ColumnIndex == databankGrid.Columns["DOI"].Index && e.RowIndex >= 0)
            {
                var doi = databankGrid.Rows[e.RowIndex].Cells["DOI"].Value?.ToString();
                if (!string.IsNullOrEmpty(doi))
                {
                    try
                    {
                        string url = doi.StartsWith("http") ? doi : $"https://doi.org/{doi}";
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not open DOI link: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void UpdateStats()
        {
            int total = databankTable.Rows.Count;
            var envTypes = databankTable.AsEnumerable()
                .GroupBy(r => r["EnvironmentType"]?.ToString() ?? "Unknown")
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();

            statsLabel.Text = $"Total: {total} geographic areas | {string.Join(" | ", envTypes)}";
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
                conditions.Add($"(AreaName LIKE '%{search.Replace("'", "''")}%' OR Reference LIKE '%{search.Replace("'", "''")}%' OR Description LIKE '%{search.Replace("'", "''")}%')");
            }

            if (filter != "All")
            {
                conditions.Add($"EnvironmentType = '{filter}'");
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
            if (databankGrid.Columns.Count > 2 && e.ColumnIndex == 2 && e.Value != null)
            {
                string val = e.Value.ToString();
                e.CellStyle.BackColor = val switch
                {
                    "Marine Shelf" => Color.FromArgb(200, 230, 255),
                    "Transitional Waters" => Color.FromArgb(200, 255, 230),
                    "Fjord" => Color.FromArgb(180, 200, 255),
                    "Coral Reef" => Color.FromArgb(255, 230, 200),
                    "Deep-Sea" => Color.FromArgb(220, 220, 240),
                    _ => Color.White
                };
            }
        }

        private void AddAreaButton_Click(object sender, EventArgs e)
        {
            using var addForm = new Form
            {
                Text = "Add Geographic Area",
                Size = new Size(500, 380),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = this.Icon
            };

            int yPos = 20;
            var labels = new[] { "Area Name:", "Environment Type:", "Region:", "Description:", "Reference:", "DOI:" };
            var controls = new Control[6];

            // Area Name
            addForm.Controls.Add(new Label { Text = labels[0], Location = new Point(20, yPos), AutoSize = true });
            controls[0] = new TextBox { Location = new Point(140, yPos - 3), Size = new Size(320, 25) };
            addForm.Controls.Add(controls[0]);
            yPos += 35;

            // Environment Type
            addForm.Controls.Add(new Label { Text = labels[1], Location = new Point(20, yPos), AutoSize = true });
            var envCombo = new ComboBox { Location = new Point(140, yPos - 3), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            envCombo.Items.AddRange(new[] { "Marine Shelf", "Transitional Waters", "Fjord", "Coral Reef", "Deep-Sea", "Estuarine", "Lagoon" });
            envCombo.SelectedIndex = 0;
            controls[1] = envCombo;
            addForm.Controls.Add(controls[1]);
            yPos += 35;

            // Region
            addForm.Controls.Add(new Label { Text = labels[2], Location = new Point(20, yPos), AutoSize = true });
            var regionCombo = new ComboBox { Location = new Point(140, yPos - 3), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            regionCombo.Items.AddRange(new[] { "Mediterranean", "Atlantic", "Pacific", "Indian", "Arctic", "Antarctic", "Tropical", "Global" });
            regionCombo.SelectedIndex = 0;
            controls[2] = regionCombo;
            addForm.Controls.Add(controls[2]);
            yPos += 35;

            // Description
            addForm.Controls.Add(new Label { Text = labels[3], Location = new Point(20, yPos), AutoSize = true });
            controls[3] = new TextBox { Location = new Point(140, yPos - 3), Size = new Size(320, 50), Multiline = true };
            addForm.Controls.Add(controls[3]);
            yPos += 60;

            // Reference
            addForm.Controls.Add(new Label { Text = labels[4], Location = new Point(20, yPos), AutoSize = true });
            controls[4] = new TextBox { Location = new Point(140, yPos - 3), Size = new Size(320, 25) };
            addForm.Controls.Add(controls[4]);
            yPos += 35;

            // DOI
            addForm.Controls.Add(new Label { Text = labels[5], Location = new Point(20, yPos), AutoSize = true });
            controls[5] = new TextBox { Location = new Point(140, yPos - 3), Size = new Size(320, 25) };
            addForm.Controls.Add(controls[5]);
            yPos += 45;

            var addBtn = new Button { Text = "Add", Location = new Point(140, yPos), Size = new Size(80, 30), DialogResult = DialogResult.OK };
            var cancelBtn = new Button { Text = "Cancel", Location = new Point(230, yPos), Size = new Size(80, 30), DialogResult = DialogResult.Cancel };

            addForm.Controls.AddRange(new Control[] { addBtn, cancelBtn });
            addForm.AcceptButton = addBtn;
            addForm.CancelButton = cancelBtn;

            if (addForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(controls[0].Text))
            {
                int nextId = databankTable.Rows.Count > 0
                    ? databankTable.AsEnumerable().Max(r => r.Field<int>("AreaID")) + 1
                    : 1;

                databankTable.Rows.Add(
                    nextId,
                    controls[0].Text.Trim(),
                    (controls[1] as ComboBox)?.SelectedItem?.ToString() ?? "Marine Shelf",
                    (controls[2] as ComboBox)?.SelectedItem?.ToString() ?? "Mediterranean",
                    controls[3].Text.Trim(),
                    controls[4].Text.Trim(),
                    controls[5].Text.Trim()
                );

                hasUnsavedChanges = true;
                UpdateStats();
            }
        }

        private void RemoveAreaButton_Click(object sender, EventArgs e)
        {
            if (databankGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select one or more areas to remove.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Remove {databankGrid.SelectedRows.Count} selected area(s)?",
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

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveDatabank();
        }

        private void SaveDatabank()
        {
            try
            {
                using var writer = new StreamWriter(databankPath, false, Encoding.UTF8);
                writer.WriteLine("AreaID;AreaName;EnvironmentType;Region;Description;Reference;DOI;UsedForIndices");

                foreach (DataRow row in databankTable.Rows)
                {
                    writer.WriteLine($"{row["AreaID"]};{row["AreaName"]};{row["EnvironmentType"]};{row["Region"]};{row["Description"]};{row["Reference"]};{row["DOI"]};{row["UsedForIndices"]}");
                }

                hasUnsavedChanges = false;
                DatabankUpdated?.Invoke(this, EventArgs.Empty);

                MessageBox.Show($"Database saved with {databankTable.Rows.Count} geographic areas.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving database: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportFromCsv_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "CSV Files|*.csv|All Files|*.*",
                Title = "Import Geographic Areas"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var newData = new List<DataRow>();
                    using var reader = new StreamReader(dialog.FileName, Encoding.UTF8);
                    string line;
                    bool isFirst = true;
                    int imported = 0;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (isFirst) { isFirst = false; continue; }
                        var parts = line.Contains(';') ? line.Split(';') : line.Split(',');
                        if (parts.Length >= 6)
                        {
                            int nextId = databankTable.Rows.Count > 0
                                ? databankTable.AsEnumerable().Max(r => r.Field<int>("AreaID")) + 1 + imported
                                : 1 + imported;

                            databankTable.Rows.Add(
                                nextId,
                                parts[0].Trim(),
                                parts[1].Trim(),
                                parts[2].Trim(),
                                parts[3].Trim(),
                                parts[4].Trim(),
                                parts.Length > 5 ? parts[5].Trim() : ""
                            );
                            imported++;
                        }
                    }

                    if (imported > 0)
                    {
                        hasUnsavedChanges = true;
                        UpdateStats();
                        MessageBox.Show($"Imported {imported} geographic areas.", "Import Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing file: {ex.Message}", "Import Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ImportFromExcel_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls|All Files|*.*",
                Title = "Import Geographic Areas from Excel"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var workbook = new XLWorkbook(dialog.FileName);
                    var worksheet = workbook.Worksheet(1);
                    int imported = 0;

                    int startRow = 2; // Assume header in row 1
                    int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                    for (int row = startRow; row <= lastRow; row++)
                    {
                        string areaName = worksheet.Cell(row, 1).GetString().Trim();
                        if (!string.IsNullOrEmpty(areaName))
                        {
                            int nextId = databankTable.Rows.Count > 0
                                ? databankTable.AsEnumerable().Max(r => r.Field<int>("AreaID")) + 1 + imported
                                : 1 + imported;

                            databankTable.Rows.Add(
                                nextId,
                                areaName,
                                worksheet.Cell(row, 2).GetString().Trim(),
                                worksheet.Cell(row, 3).GetString().Trim(),
                                worksheet.Cell(row, 4).GetString().Trim(),
                                worksheet.Cell(row, 5).GetString().Trim(),
                                worksheet.Cell(row, 6).GetString().Trim()
                            );
                            imported++;
                        }
                    }

                    if (imported > 0)
                    {
                        hasUnsavedChanges = true;
                        UpdateStats();
                        MessageBox.Show($"Imported {imported} geographic areas.", "Import Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing Excel file: {ex.Message}", "Import Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportToCsv_Click(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                Title = "Export Geographic Areas",
                FileName = "geographic_areas_export.csv"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);
                    writer.WriteLine("AreaName;EnvironmentType;Region;Description;Reference;DOI;UsedForIndices");

                    foreach (DataRow row in databankTable.Rows)
                    {
                        writer.WriteLine($"{row["AreaName"]};{row["EnvironmentType"]};{row["Region"]};{row["Description"]};{row["Reference"]};{row["DOI"]};{row["UsedForIndices"]}");
                    }

                    MessageBox.Show($"Exported {databankTable.Rows.Count} areas.", "Export Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting file: {ex.Message}", "Export Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportToExcel_Click(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Export Geographic Areas to Excel",
                FileName = "geographic_areas_export.xlsx"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Geographic Areas");

                    // Headers
                    worksheet.Cell(1, 1).Value = "Area Name";
                    worksheet.Cell(1, 2).Value = "Environment Type";
                    worksheet.Cell(1, 3).Value = "Region";
                    worksheet.Cell(1, 4).Value = "Description";
                    worksheet.Cell(1, 5).Value = "Reference";
                    worksheet.Cell(1, 6).Value = "DOI";
                    worksheet.Cell(1, 7).Value = "Used For Indices";
                    worksheet.Row(1).Style.Font.Bold = true;

                    int row = 2;
                    foreach (DataRow dataRow in databankTable.Rows)
                    {
                        worksheet.Cell(row, 1).Value = dataRow["AreaName"].ToString();
                        worksheet.Cell(row, 2).Value = dataRow["EnvironmentType"].ToString();
                        worksheet.Cell(row, 3).Value = dataRow["Region"].ToString();
                        worksheet.Cell(row, 4).Value = dataRow["Description"].ToString();
                        worksheet.Cell(row, 5).Value = dataRow["Reference"].ToString();

                        string doi = dataRow["DOI"].ToString();
                        if (!string.IsNullOrEmpty(doi))
                        {
                            string url = doi.StartsWith("http") ? doi : $"https://doi.org/{doi}";
                            worksheet.Cell(row, 6).Value = doi;
                            worksheet.Cell(row, 6).SetHyperlink(new XLHyperlink(url));
                        }
                        worksheet.Cell(row, 7).Value = dataRow["UsedForIndices"].ToString();
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(dialog.FileName);

                    MessageBox.Show($"Exported {databankTable.Rows.Count} areas to Excel.",
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
            string helpText = @"Geographic Areas & Environmental References Database

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

            MessageBox.Show(helpText, "About Geographic Areas Database",
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
                {
                    SaveDatabank();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// Gets all geographic areas from the database
        /// </summary>
        public static List<GeographicArea> GetAllAreas()
        {
            var areas = new List<GeographicArea>();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "geographic_areas_databank.csv");

            if (!File.Exists(path)) return areas;

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
            catch { }

            return areas;
        }
    }

    /// <summary>
    /// Represents a geographic area with environmental information
    /// </summary>
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
}
