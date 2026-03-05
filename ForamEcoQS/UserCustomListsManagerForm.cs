//MIT License
// UserCustomListsManagerForm.cs - Manager for user-defined custom species lists with index type specification

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
    /// Form for managing user-defined custom species lists.
    /// Each list can be assigned to a specific index type (Foram-AMBI, FSI, TSI-Med, FoRAM Index, etc.)
    /// </summary>
    public partial class UserCustomListsManagerForm : Form
    {
        private DataGridView listsGrid;
        private DataGridView speciesGrid;
        private Label statsLabel;
        private Button addListButton;
        private Button removeListButton;
        private Button editListButton;
        private Button addSpeciesButton;
        private Button removeSpeciesButton;
        private Button closeButton;
        private ComboBox indexFilterCombo;
        private SplitContainer splitContainer;
        private MenuStrip menuStrip;

        private DataTable listsTable;
        private DataTable currentSpeciesTable;
        private string userListsDirectory;
        private string manifestPath;
        private bool hasUnsavedChanges = false;

        public event EventHandler ListsUpdated;

        // Available index types for custom lists
        public static readonly string[] IndexTypes = new string[]
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
            InitializeComponent();
            
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            userListsDirectory = Path.Combine(appDataPath, "user_lists");
            
            if (!Directory.Exists(userListsDirectory))
            {
                Directory.CreateDirectory(userListsDirectory);
            }

            SetupListsGrid();
            LoadManifest();
        }

        private void InitializeComponent()
        {
            this.Text = "User Custom Lists Manager";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 600);

            // Set paths
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            userListsDirectory = Path.Combine(appDataPath, "user_lists");
            manifestPath = Path.Combine(userListsDirectory, "manifest.csv");

            // Ensure directory exists
            if (!Directory.Exists(userListsDirectory))
            {
                Directory.CreateDirectory(userListsDirectory);
            }

            // Menu strip
            menuStrip = new MenuStrip { Dock = DockStyle.Top };

            var fileMenu = new ToolStripMenuItem("File");
            var importMenuItem = new ToolStripMenuItem("Import List from CSV...", null, ImportList_Click);
            var importExcelMenuItem = new ToolStripMenuItem("Import List from Excel...", null, ImportListExcel_Click);
            var exportMenuItem = new ToolStripMenuItem("Export Selected List to CSV...", null, ExportList_Click);
            var exportExcelMenuItem = new ToolStripMenuItem("Export Selected List to Excel...", null, ExportListExcel_Click);
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
                importMenuItem, importExcelMenuItem,
                new ToolStripSeparator(),
                exportMenuItem, exportExcelMenuItem
            });

            var helpMenu = new ToolStripMenuItem("Help");
            var aboutMenuItem = new ToolStripMenuItem("About Custom Lists", null, ShowHelp_Click);
            helpMenu.DropDownItems.Add(aboutMenuItem);

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, helpMenu });
            this.MainMenuStrip = menuStrip;

            // Top panel with filter
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10, 5, 10, 5)
            };

            var filterLabel = new Label { Text = "Filter by Index Type:", Location = new Point(10, 15), AutoSize = true };
            indexFilterCombo = new ComboBox
            {
                Location = new Point(140, 12),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            indexFilterCombo.Items.Add("All");
            indexFilterCombo.Items.AddRange(IndexTypes);
            indexFilterCombo.SelectedIndex = 0;
            indexFilterCombo.SelectedIndexChanged += IndexFilterCombo_SelectedIndexChanged;

            topPanel.Controls.AddRange(new Control[] { filterLabel, indexFilterCombo });

            // Split container for lists and species
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            // SplitterDistance will be set in Load event to avoid validation errors

            // Left panel - Lists
            var listsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var listsLabel = new Label { Text = "Custom Lists:", Dock = DockStyle.Top, Font = new Font(Font.FontFamily, 10, FontStyle.Bold), Height = 25 };

            listsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true
            };
            listsGrid.SelectionChanged += ListsGrid_SelectionChanged;
            listsGrid.CellFormatting += ListsGrid_CellFormatting;

            var listsButtonPanel = new Panel { Dock = DockStyle.Bottom, Height = 45 };
            addListButton = new Button
            {
                Text = "Add List",
                Location = new Point(5, 8),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            addListButton.Click += AddListButton_Click;

            editListButton = new Button
            {
                Text = "Edit",
                Location = new Point(105, 8),
                Size = new Size(70, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White
            };
            editListButton.Click += EditListButton_Click;

            removeListButton = new Button
            {
                Text = "Remove",
                Location = new Point(185, 8),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(205, 92, 92),
                ForeColor = Color.White
            };
            removeListButton.Click += RemoveListButton_Click;

            listsButtonPanel.Controls.AddRange(new Control[] { addListButton, editListButton, removeListButton });

            listsPanel.Controls.Add(listsGrid);
            listsPanel.Controls.Add(listsButtonPanel);
            listsPanel.Controls.Add(listsLabel);

            // Right panel - Species in selected list
            var speciesPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var speciesLabel = new Label { Text = "Species in Selected List:", Dock = DockStyle.Top, Font = new Font(Font.FontFamily, 10, FontStyle.Bold), Height = 25 };

            speciesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = DataGridViewEditMode.EditOnEnter
            };
            speciesGrid.CellValueChanged += SpeciesGrid_CellValueChanged;

            var speciesButtonPanel = new Panel { Dock = DockStyle.Bottom, Height = 45 };
            addSpeciesButton = new Button
            {
                Text = "Add Species",
                Location = new Point(5, 8),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            addSpeciesButton.Click += AddSpeciesButton_Click;

            removeSpeciesButton = new Button
            {
                Text = "Remove",
                Location = new Point(115, 8),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(205, 92, 92),
                ForeColor = Color.White
            };
            removeSpeciesButton.Click += RemoveSpeciesButton_Click;

            var saveSpeciesButton = new Button
            {
                Text = "Save Changes",
                Location = new Point(205, 8),
                Size = new Size(110, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White
            };
            saveSpeciesButton.Click += SaveSpecies_Click;

            speciesButtonPanel.Controls.AddRange(new Control[] { addSpeciesButton, removeSpeciesButton, saveSpeciesButton });

            speciesPanel.Controls.Add(speciesGrid);
            speciesPanel.Controls.Add(speciesButtonPanel);
            speciesPanel.Controls.Add(speciesLabel);

            splitContainer.Panel1.Controls.Add(listsPanel);
            splitContainer.Panel2.Controls.Add(speciesPanel);

            // Bottom panel
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10)
            };

            statsLabel = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(700, 20),
                Font = new Font(Font.FontFamily, 9)
            };

            closeButton = new Button
            {
                Text = "Close",
                Location = new Point(870, 15),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            closeButton.Click += (s, e) => this.Close();

            bottomPanel.Controls.AddRange(new Control[] { statsLabel, closeButton });

            this.Controls.Add(splitContainer);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(topPanel);
            this.Controls.Add(menuStrip);

            this.FormClosing += Form_FormClosing;
            this.Load += Form_Load;
        }

        private void Form_Load(object sender, EventArgs e)
        {
            // Set SplitterDistance after form is loaded to avoid validation errors
            // The form must be visible and properly sized before these values can be set
            try
            {
                if (splitContainer.Width > 400)
                {
                    splitContainer.SplitterDistance = splitContainer.Width / 3;
                }
            }
            catch
            {
                // Ignore if still fails - use default
            }
        }

        private void LoadManifest()
        {
            listsTable = new DataTable();
            listsTable.Columns.Add("Name", typeof(string));
            listsTable.Columns.Add("IndexType", typeof(string));
            listsTable.Columns.Add("SpeciesCount", typeof(int));
            listsTable.Columns.Add("FileName", typeof(string));
            listsTable.Columns.Add("Description", typeof(string));

            if (File.Exists(manifestPath))
            {
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
                            string name = parts[0].Trim();
                            string indexType = parts[1].Trim();
                            int count = int.TryParse(parts[2].Trim(), out int c) ? c : 0;
                            string fileName = parts[3].Trim();
                            string desc = parts.Length > 4 ? parts[4].Trim() : "";
                            listsTable.Rows.Add(name, indexType, count, fileName, desc);
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
            listsGrid.Columns.Clear();
            listsGrid.AutoGenerateColumns = false;

            listsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "List Name",
                DataPropertyName = "Name",
                Width = 150
            });

            listsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IndexType",
                HeaderText = "Index Type",
                DataPropertyName = "IndexType",
                Width = 100
            });

            listsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SpeciesCount",
                HeaderText = "Species",
                DataPropertyName = "SpeciesCount",
                Width = 60
            });

            listsGrid.DataSource = listsTable;
        }

        private void UpdateStats()
        {
            int totalLists = listsTable.Rows.Count;
            var indexCounts = listsTable.AsEnumerable()
                .GroupBy(r => r["IndexType"].ToString())
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();

            statsLabel.Text = $"Total Lists: {totalLists}" +
                (indexCounts.Count > 0 ? $" | {string.Join(" | ", indexCounts)}" : "");
        }

        private void ListsGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (listsGrid.SelectedRows.Count > 0 && listsGrid.SelectedRows[0].DataBoundItem is DataRowView drv)
            {
                string fileName = drv["FileName"].ToString();
                LoadSpeciesList(fileName);
            }
            else
            {
                speciesGrid.DataSource = null;
            }
        }

        private void LoadSpeciesList(string fileName)
        {
            currentSpeciesTable = new DataTable();
            currentSpeciesTable.Columns.Add("Species", typeof(string));
            currentSpeciesTable.Columns.Add("Value", typeof(string));

            string filePath = Path.Combine(userListsDirectory, fileName);
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
                            currentSpeciesTable.Rows.Add(parts[0].Trim(), parts[1].Trim());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading species list: {ex.Message}", "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            speciesGrid.Columns.Clear();
            speciesGrid.AutoGenerateColumns = false;

            speciesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Species",
                HeaderText = "Species",
                DataPropertyName = "Species",
                Width = 300
            });

            speciesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Value",
                HeaderText = "Value/Category",
                DataPropertyName = "Value",
                Width = 150
            });

            speciesGrid.DataSource = currentSpeciesTable;
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
            string filter = indexFilterCombo.SelectedItem?.ToString() ?? "All";
            var dv = listsTable.DefaultView;
            dv.RowFilter = filter == "All" ? "" : $"IndexType = '{filter}'";
        }

        private void AddListButton_Click(object sender, EventArgs e)
        {
            using var addForm = new Form
            {
                Text = "Add New Custom List",
                Size = new Size(450, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = this.Icon
            };

            var nameLabel = new Label { Text = "List Name:", Location = new Point(20, 25), AutoSize = true };
            var nameBox = new TextBox { Location = new Point(140, 22), Size = new Size(270, 25) };

            var indexLabel = new Label { Text = "Index Type:", Location = new Point(20, 60), AutoSize = true };
            var indexCombo = new ComboBox
            {
                Location = new Point(140, 57),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            indexCombo.Items.AddRange(IndexTypes);
            indexCombo.SelectedIndex = 0;

            var descLabel = new Label { Text = "Description:", Location = new Point(20, 95), AutoSize = true };
            var descBox = new TextBox { Location = new Point(140, 92), Size = new Size(270, 60), Multiline = true };

            var addBtn = new Button
            {
                Text = "Create",
                Location = new Point(140, 170),
                Size = new Size(80, 35),
                DialogResult = DialogResult.OK
            };

            var cancelBtn = new Button
            {
                Text = "Cancel",
                Location = new Point(230, 170),
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel
            };

            addForm.Controls.AddRange(new Control[] { nameLabel, nameBox, indexLabel, indexCombo, descLabel, descBox, addBtn, cancelBtn });
            addForm.AcceptButton = addBtn;
            addForm.CancelButton = cancelBtn;

            if (addForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(nameBox.Text))
            {
                string name = nameBox.Text.Trim();
                string indexType = indexCombo.SelectedItem.ToString();
                string desc = descBox.Text.Trim();

                // Check if name already exists
                var existing = listsTable.AsEnumerable()
                    .FirstOrDefault(r => r["Name"].ToString().Equals(name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    MessageBox.Show($"A list named '{name}' already exists.", "Duplicate Name",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create file name
                string fileName = $"{name.Replace(" ", "_").ToLower()}_{indexType.Replace(" ", "_").ToLower()}.csv";

                // Create empty list file
                string filePath = Path.Combine(userListsDirectory, fileName);
                File.WriteAllText(filePath, "Species;Value\n", Encoding.UTF8);

                // Add to manifest
                listsTable.Rows.Add(name, indexType, 0, fileName, desc);
                SaveManifest();

                UpdateStats();
                hasUnsavedChanges = false;

                MessageBox.Show($"List '{name}' created for {indexType}.\n\nYou can now add species to this list.",
                    "List Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void EditListButton_Click(object sender, EventArgs e)
        {
            if (listsGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a list to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var drv = listsGrid.SelectedRows[0].DataBoundItem as DataRowView;
            if (drv == null) return;

            using var editForm = new Form
            {
                Text = "Edit List Properties",
                Size = new Size(450, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = this.Icon
            };

            var nameLabel = new Label { Text = "List Name:", Location = new Point(20, 25), AutoSize = true };
            var nameBox = new TextBox { Location = new Point(140, 22), Size = new Size(270, 25), Text = drv["Name"].ToString() };

            var indexLabel = new Label { Text = "Index Type:", Location = new Point(20, 60), AutoSize = true };
            var indexCombo = new ComboBox
            {
                Location = new Point(140, 57),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            indexCombo.Items.AddRange(IndexTypes);
            indexCombo.SelectedItem = drv["IndexType"].ToString();

            var descLabel = new Label { Text = "Description:", Location = new Point(20, 95), AutoSize = true };
            var descBox = new TextBox { Location = new Point(140, 92), Size = new Size(270, 60), Multiline = true, Text = drv["Description"].ToString() };

            var saveBtn = new Button
            {
                Text = "Save",
                Location = new Point(140, 170),
                Size = new Size(80, 35),
                DialogResult = DialogResult.OK
            };

            var cancelBtn = new Button
            {
                Text = "Cancel",
                Location = new Point(230, 170),
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel
            };

            editForm.Controls.AddRange(new Control[] { nameLabel, nameBox, indexLabel, indexCombo, descLabel, descBox, saveBtn, cancelBtn });
            editForm.AcceptButton = saveBtn;
            editForm.CancelButton = cancelBtn;

            if (editForm.ShowDialog() == DialogResult.OK)
            {
                drv["Name"] = nameBox.Text.Trim();
                drv["IndexType"] = indexCombo.SelectedItem.ToString();
                drv["Description"] = descBox.Text.Trim();
                SaveManifest();
                UpdateStats();
            }
        }

        private void RemoveListButton_Click(object sender, EventArgs e)
        {
            if (listsGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a list to remove.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var drv = listsGrid.SelectedRows[0].DataBoundItem as DataRowView;
            if (drv == null) return;

            string name = drv["Name"].ToString();
            string fileName = drv["FileName"].ToString();

            var result = MessageBox.Show(
                $"Remove list '{name}'?\n\nThis will permanently delete the list and all its species.",
                "Confirm Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // Delete file
                string filePath = Path.Combine(userListsDirectory, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Remove from table
                listsTable.Rows.Remove(drv.Row);
                SaveManifest();
                UpdateStats();

                speciesGrid.DataSource = null;
            }
        }

        private void AddSpeciesButton_Click(object sender, EventArgs e)
        {
            if (listsGrid.SelectedRows.Count == 0 || currentSpeciesTable == null)
            {
                MessageBox.Show("Please select a list first.", "No List Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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

            var speciesLabel = new Label { Text = "Species name:", Location = new Point(20, 25), AutoSize = true };
            var speciesBox = new TextBox { Location = new Point(130, 22), Size = new Size(230, 25) };

            var valueLabel = new Label { Text = "Value/Category:", Location = new Point(20, 60), AutoSize = true };
            var valueBox = new TextBox { Location = new Point(130, 57), Size = new Size(150, 25) };

            var addBtn = new Button { Text = "Add", Location = new Point(130, 100), Size = new Size(80, 30), DialogResult = DialogResult.OK };
            var cancelBtn = new Button { Text = "Cancel", Location = new Point(220, 100), Size = new Size(80, 30), DialogResult = DialogResult.Cancel };

            addForm.Controls.AddRange(new Control[] { speciesLabel, speciesBox, valueLabel, valueBox, addBtn, cancelBtn });
            addForm.AcceptButton = addBtn;
            addForm.CancelButton = cancelBtn;

            if (addForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(speciesBox.Text))
            {
                currentSpeciesTable.Rows.Add(speciesBox.Text.Trim(), valueBox.Text.Trim());
                hasUnsavedChanges = true;
            }
        }

        private void RemoveSpeciesButton_Click(object sender, EventArgs e)
        {
            if (speciesGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select species to remove.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rowsToRemove = new List<DataRow>();
            foreach (DataGridViewRow row in speciesGrid.SelectedRows)
            {
                if (row.DataBoundItem is DataRowView drv)
                {
                    rowsToRemove.Add(drv.Row);
                }
            }

            foreach (var row in rowsToRemove)
            {
                currentSpeciesTable.Rows.Remove(row);
            }

            hasUnsavedChanges = true;
        }

        private void SpeciesGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                hasUnsavedChanges = true;
            }
        }

        private void SaveSpecies_Click(object sender, EventArgs e)
        {
            if (listsGrid.SelectedRows.Count == 0 || currentSpeciesTable == null)
            {
                MessageBox.Show("No list selected.", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var drv = listsGrid.SelectedRows[0].DataBoundItem as DataRowView;
            if (drv == null) return;

            string fileName = drv["FileName"].ToString();
            string filePath = Path.Combine(userListsDirectory, fileName);

            try
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                writer.WriteLine("Species;Value");
                foreach (DataRow row in currentSpeciesTable.Rows)
                {
                    writer.WriteLine($"{row["Species"]};{row["Value"]}");
                }

                // Update species count in manifest
                drv["SpeciesCount"] = currentSpeciesTable.Rows.Count;
                SaveManifest();

                hasUnsavedChanges = false;
                ListsUpdated?.Invoke(this, EventArgs.Empty);

                MessageBox.Show($"List saved with {currentSpeciesTable.Rows.Count} species.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving list: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveManifest()
        {
            try
            {
                using var writer = new StreamWriter(manifestPath, false, Encoding.UTF8);
                writer.WriteLine("Name;IndexType;SpeciesCount;FileName;Description");
                foreach (DataRow row in listsTable.Rows)
                {
                    writer.WriteLine($"{row["Name"]};{row["IndexType"]};{row["SpeciesCount"]};{row["FileName"]};{row["Description"]}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving manifest: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportList_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "CSV Files|*.csv|All Files|*.*",
                Title = "Import Custom List from CSV"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ImportListFromFile(dialog.FileName, false);
            }
        }

        private void ImportListExcel_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls|All Files|*.*",
                Title = "Import Custom List from Excel"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ImportListFromFile(dialog.FileName, true);
            }
        }

        private void ImportListFromFile(string filePath, bool isExcel)
        {
            // First, ask for list name and index type
            using var metaForm = new Form
            {
                Text = "Import List - Set Properties",
                Size = new Size(450, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = this.Icon
            };

            var nameLabel = new Label { Text = "List Name:", Location = new Point(20, 25), AutoSize = true };
            var nameBox = new TextBox
            {
                Location = new Point(140, 22),
                Size = new Size(270, 25),
                Text = Path.GetFileNameWithoutExtension(filePath)
            };

            var indexLabel = new Label { Text = "Index Type:", Location = new Point(20, 60), AutoSize = true };
            var indexCombo = new ComboBox
            {
                Location = new Point(140, 57),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            indexCombo.Items.AddRange(IndexTypes);
            indexCombo.SelectedIndex = 0;

            var importBtn = new Button { Text = "Import", Location = new Point(140, 110), Size = new Size(80, 35), DialogResult = DialogResult.OK };
            var cancelBtn = new Button { Text = "Cancel", Location = new Point(230, 110), Size = new Size(80, 35), DialogResult = DialogResult.Cancel };

            metaForm.Controls.AddRange(new Control[] { nameLabel, nameBox, indexLabel, indexCombo, importBtn, cancelBtn });
            metaForm.AcceptButton = importBtn;
            metaForm.CancelButton = cancelBtn;

            if (metaForm.ShowDialog() != DialogResult.OK) return;

            string name = nameBox.Text.Trim();
            string indexType = indexCombo.SelectedItem.ToString();

            // Check for duplicate name
            var existing = listsTable.AsEnumerable()
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

                // Create file name and save
                string fileName = $"{name.Replace(" ", "_").ToLower()}_{indexType.Replace(" ", "_").ToLower()}.csv";
                string destPath = Path.Combine(userListsDirectory, fileName);

                using var writer = new StreamWriter(destPath, false, Encoding.UTF8);
                writer.WriteLine("Species;Value");
                foreach (DataRow row in speciesData.Rows)
                {
                    writer.WriteLine($"{row["Species"]};{row["Value"]}");
                }

                // Add to manifest
                listsTable.Rows.Add(name, indexType, speciesData.Rows.Count, fileName, "");
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

        private void ExportList_Click(object sender, EventArgs e)
        {
            if (listsGrid.SelectedRows.Count == 0 || currentSpeciesTable == null)
            {
                MessageBox.Show("Please select a list to export.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var drv = listsGrid.SelectedRows[0].DataBoundItem as DataRowView;
            string name = drv?["Name"].ToString() ?? "export";

            using var dialog = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                Title = "Export List to CSV",
                FileName = $"{name.Replace(" ", "_")}_export.csv"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);
                    writer.WriteLine("Species;Value");
                    foreach (DataRow row in currentSpeciesTable.Rows)
                    {
                        writer.WriteLine($"{row["Species"]};{row["Value"]}");
                    }
                    MessageBox.Show($"Exported {currentSpeciesTable.Rows.Count} species.",
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting: {ex.Message}", "Export Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportListExcel_Click(object sender, EventArgs e)
        {
            if (listsGrid.SelectedRows.Count == 0 || currentSpeciesTable == null)
            {
                MessageBox.Show("Please select a list to export.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var drv = listsGrid.SelectedRows[0].DataBoundItem as DataRowView;
            string name = drv?["Name"].ToString() ?? "export";
            string indexType = drv?["IndexType"].ToString() ?? "";

            using var dialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Export List to Excel",
                FileName = $"{name.Replace(" ", "_")}_export.xlsx"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add(name.Length > 31 ? name.Substring(0, 31) : name);

                    worksheet.Cell(1, 1).Value = "Species";
                    worksheet.Cell(1, 2).Value = "Value";
                    worksheet.Row(1).Style.Font.Bold = true;

                    int row = 2;
                    foreach (DataRow dataRow in currentSpeciesTable.Rows)
                    {
                        worksheet.Cell(row, 1).Value = dataRow["Species"].ToString();
                        worksheet.Cell(row, 2).Value = dataRow["Value"].ToString();
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(dialog.FileName);

                    MessageBox.Show($"Exported {currentSpeciesTable.Rows.Count} species to Excel.\nIndex Type: {indexType}",
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
            string helpText = @"User Custom Lists Manager
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
   Lists are stored in the 'user_lists' folder within the application directory.

NOTE: Custom lists can be used alongside the built-in databanks for index calculations.";

            MessageBox.Show(helpText, "About Custom Lists",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes to the current species list. Save before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                    SaveSpecies_Click(this, EventArgs.Empty);
                else if (result == DialogResult.Cancel)
                    e.Cancel = true;
            }
        }

        /// <summary>
        /// Gets all user custom lists for a specific index type
        /// </summary>
        public static List<UserCustomList> GetUserListsForIndex(string indexType)
        {
            var lists = new List<UserCustomList>();
            string userListsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_lists");
            string manifestPath = Path.Combine(userListsDirectory, "manifest.csv");

            if (!File.Exists(manifestPath)) return lists;

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
            catch { }

            return lists;
        }

        /// <summary>
        /// Loads species data from a user custom list
        /// </summary>
        public static Dictionary<string, string> LoadUserList(string fileName)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_lists", fileName);

            if (!File.Exists(filePath)) return data;

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
            catch { }

            return data;
        }
    }

    /// <summary>
    /// Represents a user-defined custom list
    /// </summary>
    public class UserCustomList
    {
        public string Name { get; set; }
        public string IndexType { get; set; }
        public string FileName { get; set; }
        public int SpeciesCount { get; set; }
    }
}
