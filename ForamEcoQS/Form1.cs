//MIT License

using ClosedXML.Excel;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ForamEcoQS
{
    public partial class ForamEcoQS : Form
    {
        private int datasetLoaded = 0;
        private ExcelReader a;
        private Stack<DeletedColumnInfo> deletedColumns = new Stack<DeletedColumnInfo>();
        private DataTable databank;
        private Dictionary<string, double> extractedMudPercentages = null;
        private EcologicalGroupOverrideManager overrideManager;

        private readonly string[] databankOptions = {
            "Jorissen (Mediterranean, Foram-AMBI)",
            "Alve (NE Atlantic/Arctic, Foram-AMBI)",
            "Bouchet (Mediterranean, Foram-AMBI)",
            "Bouchet (Atlantic, Foram-AMBI)",
            "Bouchet (South Atlantic, Foram-AMBI)",
            "O'Malley (Gulf of Mexico, Foram-AMBI)"
        };

        private string PromptForDatabankSelection(string title)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 360;
                prompt.Height = 170;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = title;
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;
                prompt.Icon = this.Icon;

                Label textLabel = new Label() { Left = 10, Top = 20, Width = 330, Text = "Select a databank (Foram-AMBI):" };
                ComboBox selectionBox = new ComboBox() { Left = 10, Top = 50, Width = 330, DropDownStyle = ComboBoxStyle.DropDownList };
                selectionBox.Items.AddRange(databankOptions);
                selectionBox.SelectedItem = comboBox1.SelectedItem ?? databankOptions.First();

                Button confirmation = new Button() { Text = "OK", Left = 135, Width = 90, Top = 85, DialogResult = DialogResult.OK };
                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(selectionBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog(this) == DialogResult.OK ? selectionBox.SelectedItem?.ToString() : null;
            }
        }

        private char? PromptForCsvSeparator()
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 320;
                prompt.Height = 170;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "CSV Separator";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;
                prompt.Icon = this.Icon;

                Label textLabel = new Label() { Left = 10, Top = 20, Width = 290, Text = "Select the column separator used in your CSV file:" };
                ComboBox separatorBox = new ComboBox() { Left = 10, Top = 50, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
                separatorBox.Items.Add("Semicolon (;)");
                separatorBox.Items.Add("Comma (,)");
                separatorBox.Items.Add("Tab");
                separatorBox.Items.Add("Pipe (|)");
                separatorBox.SelectedIndex = 0; // Default to semicolon

                Button okButton = new Button() { Text = "OK", Left = 70, Width = 80, Top = 90, DialogResult = DialogResult.OK };
                Button cancelButton = new Button() { Text = "Cancel", Left = 160, Width = 80, Top = 90, DialogResult = DialogResult.Cancel };

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(separatorBox);
                prompt.Controls.Add(okButton);
                prompt.Controls.Add(cancelButton);
                prompt.AcceptButton = okButton;
                prompt.CancelButton = cancelButton;

                if (prompt.ShowDialog(this) == DialogResult.OK)
                {
                    return separatorBox.SelectedIndex switch
                    {
                        0 => ';',
                        1 => ',',
                        2 => '\t',
                        3 => '|',
                        _ => ';'
                    };
                }
                return null;
            }
        }

        private DataTable LoadDatabankByName(string selectedOption)
        {
            if (string.IsNullOrWhiteSpace(selectedOption))
            {
                return null;
            }

            LoadDataBank loader = new LoadDataBank();

            // Match by prefix to handle descriptive names
            if (selectedOption.StartsWith("Jorissen"))
                return loader.LoadDataSet("jorissen");
            if (selectedOption.StartsWith("Alve"))
                return loader.LoadDataSet("alve");
            if (selectedOption.StartsWith("Bouchet") && selectedOption.Contains("Mediterranean"))
                return loader.LoadDataSet("bouchetmed");
            if (selectedOption.StartsWith("Bouchet") && selectedOption.Contains("South Atlantic"))
                return loader.LoadDataSet("bouchetsouthatl");
            if (selectedOption.StartsWith("Bouchet") && selectedOption.Contains("Atlantic"))
                return loader.LoadDataSet("bouchetatl");
            if (selectedOption.StartsWith("O'Malley"))
                return loader.LoadDataSet("OMalley2021");

            MessageBox.Show("Unknown dataset selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }

        private void UpdateIndicesButtonState()
        {
            bool hasData = datasetLoaded == 1 && dataGridView1.Columns.Count > 1;
            advancedIndicesToolStripMenuItem.Enabled = hasData;
            advancedIndicesButton.Enabled = hasData;
            plotIndicesButton.Enabled = hasData;
            compositePlotButton.Enabled = hasData;
        }

        private void InheritParentIcon(Form childForm)
        {
            if (this.Icon != null && childForm != null)
            {
                childForm.Icon = this.Icon;
            }
        }

        private void PrepareNewWorkspace()
        {
            dataGridView1.DataSource = null;
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullColumnSelect;

            datasetLoaded = 0;
            editDataToolStripMenuItem.Enabled = false;
            exportStatsButton.Enabled = false;
            listBox1.Items.Clear();
            label1.Text = "None";
            cleanNormalizeButton.Enabled = false;
            advancedIndicesToolStripMenuItem.Enabled = false;
            advancedIndicesButton.Enabled = false;
            plotIndicesButton.Enabled = false;
            compositePlotButton.Enabled = false;
            compareDatabankButton.Enabled = false;
            saveToolStripMenuItem.Enabled = false;
            listBox1.Visible = false;
        }

        private void PopulateTemplateFromDatabank(DataTable selectedDatabank)
        {
            if (selectedDatabank == null || selectedDatabank.Rows.Count == 0)
            {
                MessageBox.Show("The selected databank is empty or could not be loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            PrepareNewWorkspace();

            var speciesColumn = new DataGridViewTextBoxColumn
            {
                Name = "Species",
                HeaderText = "Species",
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var sampleColumn = new DataGridViewTextBoxColumn
            {
                Name = "Sample",
                HeaderText = "Sample",
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            dataGridView1.Columns.Add(speciesColumn);
            dataGridView1.Columns.Add(sampleColumn);

            foreach (DataRow row in selectedDatabank.Rows)
            {
                string speciesName = row[0]?.ToString();
                dataGridView1.Rows.Add(speciesName, string.Empty);
            }

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            datasetLoaded = 1;
            databank = selectedDatabank;
            editDataToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = dataGridView1.Rows.Count > 0;
            compareDatabankButton.Enabled = dataGridView1.Rows.Count > 0;

            UpdateIndicesButtonState();
        }

        private static DataTable BuildTemplateDataTable(IEnumerable<string> speciesNames)
        {
            DataTable template = new DataTable();
            template.Columns.Add("Species");
            template.Columns.Add("Sample");

            foreach (string species in speciesNames)
            {
                if (!string.IsNullOrWhiteSpace(species))
                {
                    template.Rows.Add(species.Trim(), string.Empty);
                }
            }

            return template;
        }
        private void SaveDataGridViewToExcel(string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sheet1");

                // Add the column headers
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = dataGridView1.Columns[i].HeaderText;
                }

                // Add the rows
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    for (int j = 0; j < dataGridView1.Columns.Count; j++)
                    {
                        worksheet.Cell(i + 2, j + 1).Value = dataGridView1.Rows[i].Cells[j].Value?.ToString() ?? string.Empty;
                    }
                }

                // Save the file
                workbook.SaveAs(filePath);
            }
        }

        public class DeletedColumnInfo
        {
            public int Index { get; set; }
            public string HeaderText { get; set; }
            public DataGridViewColumn Column { get; set; }
            public List<object> ColumnData { get; set; }
        }

        public ForamEcoQS()
        {
            InitializeComponent();
            overrideManager = new EcologicalGroupOverrideManager();
            editDataToolStripMenuItem.Enabled = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullColumnSelect;

            // Populate comboBox1 with options in alphabetical order
            string[] options = databankOptions.ToArray();
            Array.Sort(options); // Sort options alphabetically

            comboBox1.Items.AddRange(options);

            // Set the default selected option to "Alve"
            comboBox1.SelectedItem = "Alve";

            LoadDataBank loader = new LoadDataBank();
            databank = loader.LoadDataSet("alve");
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            cleanNormalizeButton.Enabled = false; //Disable per default Clean and Normalize
            advancedIndicesButton.Enabled = false;
            plotIndicesButton.Enabled = false;
            compositePlotButton.Enabled = false;
            compareDatabankButton.Enabled = false;
            
            // Context Menu
            dataGridView1.MouseClick += DataGridView1_MouseClick;

            // Setup tooltip for the override checkbox
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(overrideClassificationCheckBox,
                "When checked, unassigned taxa will be included in normalization\n" +
                "and all index calculations (Foram-AMBI and others) instead of being removed.");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            System.Windows.Forms.Application.Exit();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open a file dialog to choose the file
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xls;*.xlsx|CSV Files|*.csv";
                openFileDialog.Title = "Open Data File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    string fileExtension = Path.GetExtension(filePath).ToLower();

                    // Reset extracted mud percentages
                    extractedMudPercentages = null;

                    if (fileExtension == ".xls" || fileExtension == ".xlsx")
                    {
                        // Handle Excel file loading using the refactored ExcelReader
                        a = new ExcelReader();
                        a.LoadExcelIntoDataGridView(this.dataGridView1, filePath);
                    }
                    else if (fileExtension == ".csv")
                    {
                        // Ask user for CSV separator
                        char? separator = PromptForCsvSeparator();
                        if (separator == null)
                        {
                            return; // User cancelled
                        }
                        // Handle CSV file loading
                        LoadCsvIntoDataGridView(filePath, separator.Value);
                    }

                    // Extract mud row if present (works for both Excel and CSV)
                    if (dataGridView1.DataSource is DataTable loadedDataTable)
                    {
                        extractedMudPercentages = ExtractMudRowFromDataTable(loadedDataTable);
                        if (extractedMudPercentages != null && extractedMudPercentages.Count > 0)
                        {
                            // Mud row was found and extracted - the DataTable is already updated
                            // and the DataGridView will reflect the removal automatically
                        }
                    }

                    //Set variables
                    datasetLoaded = 1;
                    editDataToolStripMenuItem.Enabled = true;
                    saveToolStripMenuItem.Enabled = dataGridView1.Rows.Count > 0;
                    cleanNormalizeButton.Enabled = false;
                    compareDatabankButton.Enabled = dataGridView1.Rows.Count > 0;
                    UpdateIndicesButtonState();
                    listBox1.Items.Clear();
                    label1.Text = "None";
                    listBox1.Visible = false;
                    exportStatsButton.Enabled = false;
                }
            }
        }

        // Method to load CSV into DataGridView
        private void LoadCsvIntoDataGridView(string filePath, char separator)
        {
            try
            {
                // Clear existing data and columns from DataGridView
                dataGridView1.DataSource = null;
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();

                // Temporarily disable SelectionMode
                dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

                using (StreamReader reader = new StreamReader(filePath))
                {
                    bool isFirstRow = true;
                    string line;

                    // Create a DataTable to store the CSV data
                    DataTable dataTable = new DataTable();

                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] values = line.Split(separator);

                        if (isFirstRow)
                        {
                            // Manually create columns based on headers
                            for (int i = 0; i < values.Length; i++)
                            {
                                // Assume the first column is string, others are double
                                if (i == 0)
                                {
                                    dataTable.Columns.Add(values[i], typeof(string));
                                }
                                else
                                {
                                    dataTable.Columns.Add(values[i], typeof(double));
                                }
                            }
                            isFirstRow = false;
                        }
                        else
                        {
                            // Create a new DataRow
                            DataRow dataRow = dataTable.NewRow();

                            // Limit to the number of columns defined in header to avoid "Cannot find column" error
                            int columnCount = Math.Min(values.Length, dataTable.Columns.Count);
                            for (int i = 0; i < columnCount; i++)
                            {
                                if (i == 0)
                                {
                                    // First column is string
                                    dataRow[i] = values[i];
                                }
                                else
                                {
                                    // Try to parse other columns as double
                                    if (double.TryParse(values[i], out double numericValue))
                                    {
                                        dataRow[i] = numericValue;
                                    }
                                    else
                                    {
                                        dataRow[i] = DBNull.Value; // Set to null if parsing fails
                                    }
                                }
                            }

                            // Add the DataRow to the DataTable
                            dataTable.Rows.Add(dataRow);
                        }
                    }

                    // Disable auto-generation of columns to avoid FillWeight overflow
                    // and manually create columns (consistent with Excel loading)
                    dataGridView1.AutoGenerateColumns = false;

                    // Manually create columns with minimal FillWeight
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        var dgvCol = new DataGridViewTextBoxColumn
                        {
                            DataPropertyName = col.ColumnName,
                            Name = col.ColumnName,
                            HeaderText = col.ColumnName,
                            ValueType = col.DataType,
                            FillWeight = 1
                        };
                        dataGridView1.Columns.Add(dgvCol);
                    }

                    // Set the DataTable as the DataSource of the DataGridView
                    dataGridView1.DataSource = dataTable;

                    // Apply formatting for numeric columns
                    foreach (DataGridViewColumn column in dataGridView1.Columns)
                    {
                        if (column.ValueType == typeof(double) || column.ValueType == typeof(float) || column.ValueType == typeof(decimal))
                        {
                            column.DefaultCellStyle.Format = "N2";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading CSV file: {ex.Message}");
            }
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrepareNewWorkspace();
        }

        private void newEmptyDatasetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string selectedOption = PromptForDatabankSelection("Create Empty Dataset");
            if (string.IsNullOrWhiteSpace(selectedOption))
            {
                return;
            }

            DataTable selectedDatabank = LoadDatabankByName(selectedOption);
            PopulateTemplateFromDatabank(selectedDatabank);
        }

        private void createTemplateFromDatabankToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string selectedOption = PromptForDatabankSelection("Create Template from Databank");
            if (string.IsNullOrWhiteSpace(selectedOption))
            {
                return;
            }

            DataTable selectedDatabank = LoadDatabankByName(selectedOption);
            PopulateTemplateFromDatabank(selectedDatabank);
        }

        private void createFSITemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fsiLookup = SpecializedDatabankLoader.LoadFSIDatabank();
            if (fsiLookup == null || fsiLookup.Count == 0)
            {
                MessageBox.Show("The FSI databank is not available. Please ensure fsi_databank.csv is in the application folder.",
                    "FSI Databank Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable template = BuildTemplateDataTable(fsiLookup.Keys);
            PopulateTemplateFromDatabank(template);
        }

        private void createTSIMedTemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tsiMedLookup = SpecializedDatabankLoader.LoadTSIMedDatabank();
            if (tsiMedLookup == null || tsiMedLookup.Count == 0)
            {
                MessageBox.Show("The TSI-Med databank is not available. Please ensure tsimed_databank.csv is in the application folder.",
                    "TSI-Med Databank Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable template = BuildTemplateDataTable(tsiMedLookup);
            PopulateTemplateFromDatabank(template);
        }

        private void editDataToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void newSampleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (NewSample newSampleForm = new NewSample())
            {
                InheritParentIcon(newSampleForm);
                if (newSampleForm.ShowDialog() == DialogResult.OK)
                {
                    // Get the sample name from the form
                    string sampleName = newSampleForm.SampleName;

                    if (sampleName == null)
                    {
                        return;
                    }
                        

                    // Create a new DataGridViewTextBoxColumn
                    DataGridViewTextBoxColumn newColumn = new DataGridViewTextBoxColumn
                    {
                        HeaderText = sampleName,
                        SortMode = DataGridViewColumnSortMode.NotSortable
                    };

                    // Add the new column to the DataGridView
                    dataGridView1.Columns.Add(newColumn);

                    // Disable the save option (optional, depending on your logic)
                    saveToolStripMenuItem.Enabled = false;

                    UpdateIndicesButtonState();
                }
            }
        }

        private string PromptForSampleRename(string currentName)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 360;
                prompt.Height = 170;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Rename Sample";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;
                prompt.Icon = this.Icon;

                Label textLabel = new Label { Left = 10, Top = 20, Width = 330, Text = "New sample name:" };
                TextBox inputBox = new TextBox { Left = 10, Top = 50, Width = 330, Text = currentName };

                Button confirmation = new Button { Text = "OK", Left = 135, Width = 90, Top = 85, DialogResult = DialogResult.OK };
                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(inputBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog(this) == DialogResult.OK ? inputBox.Text : null;
            }
        }

        private void renameSelectedSampleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count == 0)
            {
                MessageBox.Show("No cells or columns selected.", "Rename Sample", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int columnIndex = dataGridView1.SelectedCells[0].ColumnIndex;
            if (columnIndex == 0)
            {
                MessageBox.Show("The species column cannot be renamed.", "Rename Sample", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var column = dataGridView1.Columns[columnIndex];
            string newName = PromptForSampleRename(column.HeaderText);

            if (string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            column.HeaderText = newName;
            column.Name = newName;

            listBox1.Items.Clear();
            for (int i = 1; i < dataGridView1.Columns.Count; i++)
            {
                listBox1.Items.Add(dataGridView1.Columns[i].HeaderText);
            }

            listBox1.SelectedItem = newName;
        }

        private void removeSelectedSampleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check if at least one cell is selected
            if (dataGridView1.SelectedCells.Count > 0)
            {
                // Get the column index of the first selected cell
                int columnIndex = dataGridView1.SelectedCells[0].ColumnIndex;
                var selectedColumn = dataGridView1.Columns[columnIndex];
                if (columnIndex == 0)
                {
                    MessageBox.Show("The species column cannot be removed.", "Remove Columns", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // Exit the function
                }
                // Store column data
                var columnData = new List<object>();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    columnData.Add(row.Cells[columnIndex].Value);
                }

                // Store deleted column information
                deletedColumns.Push(new DeletedColumnInfo
                {
                    Index = columnIndex,
                    HeaderText = selectedColumn.HeaderText,
                    Column = selectedColumn,
                    ColumnData = columnData
                });

                // Remove the column
                dataGridView1.Columns.Remove(selectedColumn);
                UpdateIndicesButtonState();
            }
            else
            {
                MessageBox.Show("No cells or columns selected.", "Remove Columns", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            listBox1.Items.Clear();
            for (int i = 1; i < dataGridView1.Columns.Count; i++)
            {
                listBox1.Items.Add(dataGridView1.Columns[i].HeaderText);
            }
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (deletedColumns.Count > 0)
            {
                // Retrieve the last deleted column information
                var deletedColumnInfo = deletedColumns.Pop();

                // Create a new column with the same properties
                DataGridViewTextBoxColumn restoredColumn = new DataGridViewTextBoxColumn
                {
                    HeaderText = deletedColumnInfo.HeaderText,
                    Name = deletedColumnInfo.Column.Name,
                    SortMode = DataGridViewColumnSortMode.NotSortable // Set SortMode to NotSortable
                };

                // Temporarily change the SelectionMode to avoid the conflict
                var previousSelectionMode = dataGridView1.SelectionMode;
                dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;

                // Insert the column back into the DataGridView at the original index
                dataGridView1.Columns.Insert(deletedColumnInfo.Index, restoredColumn);

                // Restore the data for the column
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    dataGridView1.Rows[i].Cells[deletedColumnInfo.Index].Value = deletedColumnInfo.ColumnData[i];
                }

                // Restore the original SelectionMode
                dataGridView1.SelectionMode = previousSelectionMode;
                UpdateIndicesButtonState();
            }
            else
            {
                MessageBox.Show("No actions to undo.", "Undo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count > 0)
            {
                // Create SaveFileDialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Save as Excel File",
                    FileName = "Data.xlsx"
                };

                // Show the dialog and check if the user clicked "Save"
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;

                    // Save the DataGridView data to an Excel file with colors
                    SaveDataGridViewToExcelWithColors(filePath);
                }
            }

            else
            {
                MessageBox.Show("No data to save.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void SaveDataGridViewToExcelWithColors(string filePath)
        {
            try
            {
                // Create a new Excel workbook using ClosedXML
                using (var workbook = new XLWorkbook())
                {
                    // Add a new worksheet
                    var worksheet = workbook.Worksheets.Add("Data");

                    // Add column headers to the Excel file
                    for (int i = 0; i < dataGridView1.Columns.Count; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = dataGridView1.Columns[i].HeaderText;
                    }

                    // Add rows and set cell values and colors
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        DataGridViewRow row = dataGridView1.Rows[i];

                        for (int j = 0; j < dataGridView1.Columns.Count; j++)
                        {
                            var cell = worksheet.Cell(i + 2, j + 1); // +2 to account for the header row

                            // Set the cell value
                            cell.Value = row.Cells[j].Value?.ToString();

                            // Set the cell background color if it is set in the DataGridView
                            Color cellColor = row.DefaultCellStyle.BackColor;
                            if (cellColor != Color.Empty)
                            {
                                cell.Style.Fill.BackgroundColor = XLColor.FromColor(cellColor);
                            }
                        }
                    }

                    // Save the workbook
                    workbook.SaveAs(filePath);

                    MessageBox.Show("Data successfully exported!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the Excel file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Load the selected dataset based on the comboBox selection
            LoadSelectedDataset(comboBox1.SelectedItem.ToString());
        }
        private void LoadSelectedDataset(string selectedOption)
        {
            databank = LoadDatabankByName(selectedOption);
        }

        private void showDatabankButton_Click(object sender, EventArgs e)
        {
            // Create an instance of the DataBank viewer form, passing the databank DataTable and name
            string databankName = comboBox1.SelectedItem?.ToString() ?? "Foram-AMBI Databank";
            DataBankViewerForm form2 = new DataBankViewerForm(databank, databankName);
            InheritParentIcon(form2);

            // Show the databank viewer
            form2.Show();
        }

        private void compareDatabankButton_Click(object sender, EventArgs e)
        {
            if (datasetLoaded == 0 || dataGridView1.Rows.Count == 0 || dataGridView1.Columns.Count == 0)
            {
                MessageBox.Show("Please load or create a dataset before comparing with a databank.", "No Dataset", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            exportStatsButton.Enabled = true;
            listBox1.Enabled = true;
            if (databank == null || databank.Rows.Count == 0)
            {
                MessageBox.Show("The databank is empty or not loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Get the first column values from databank for intelligent matching
            // This handles species names with author citations like "Ammonia parkinsoniana (d'Orbigny, 1839)"
            var databankValues = new List<string>();
            foreach (DataRow row in databank.Rows)
            {
                string value = row[0]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    databankValues.Add(value);
                }
            }

            // Create intelligent species name matcher
            var speciesMatcher = new SpeciesNameMatcher(databankValues);

            // Iterate through each row in dataGridView1
            foreach (DataGridViewRow gridRow in dataGridView1.Rows)
            {
                if (gridRow.Cells[0].Value != null)
                {
                    string gridValue = gridRow.Cells[0].Value.ToString()?.Trim();

                    // Check for Override FIRST
                    if (!string.IsNullOrEmpty(gridValue) && overrideManager.HasOverride(gridValue))
                    {
                        // Overridden: Set color to Yellow
                        gridRow.DefaultCellStyle.BackColor = Color.Yellow;
                        // Force update if needed?
                    }
                    // Check if the value from dataGridView1's first column matches any entry in databank
                    // Uses intelligent matching that handles author citations and name variations
                    else if (!string.IsNullOrEmpty(gridValue) && !speciesMatcher.IsMatch(gridValue))
                    {
                        // If not found, color the row red
                        gridRow.DefaultCellStyle.BackColor = Color.Red;
                    }
                    else
                    {
                        // Reset the row color if found
                        gridRow.DefaultCellStyle.BackColor = dataGridView1.DefaultCellStyle.BackColor;
                    }
                }
            }

            // Make listBox1 visible
            listBox1.Visible = true;

            // Clear existing items in listBox1
            listBox1.Items.Clear();

            // Populate listBox1 with column headers starting from the second column
            for (int i = 1; i < dataGridView1.Columns.Count; i++)
            {
                listBox1.Items.Add(dataGridView1.Columns[i].HeaderText);
            }

            cleanNormalizeButton.Enabled = true; //Enable Clean and Normalize
            advancedIndicesToolStripMenuItem.Enabled = true;
            advancedIndicesButton.Enabled = true;
            plotIndicesButton.Enabled = true;
            compositePlotButton.Enabled = true;
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1 || listBox1.SelectedItem == null)
                return;

            // Get the selected column name
            string selectedColumn = listBox1.SelectedItem.ToString();

            // Find the column in a case-insensitive way
            DataGridViewColumn column = dataGridView1.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(c => string.Equals(c.HeaderText, selectedColumn, StringComparison.OrdinalIgnoreCase));

            // Check if the column exists
            if (column == null)
            {
                MessageBox.Show($"Column '{selectedColumn}' does not exist in the DataGridView.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int columnIndex = column.Index;

            double assignedSum = 0;
            double unassignedSum = 0;

            // Iterate through the rows of the DataGridView
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[columnIndex].Value != null && double.TryParse(row.Cells[columnIndex].Value.ToString(), out double cellValue))
                {
                    if (row.DefaultCellStyle.BackColor == Color.Red)
                    {
                        // This is an unassigned (mismatched) row
                        unassignedSum += cellValue;
                    }
                    else
                    {
                        // This is an assigned (matched) row
                        assignedSum += cellValue;
                    }
                }
            }

            // Calculate total
            double totalSum = assignedSum + unassignedSum;

            // Calculate the percentage of assigned species
            double assignedPercentage = (totalSum > 0) ? (assignedSum / totalSum) * 100 : 0;

            // Update label1, numbers rounded to 2 decimal places
            label1.Text = $"Assigned: {Math.Round(assignedSum, 2)} ({assignedPercentage:F2}%)\nUnassigned: {Math.Round(unassignedSum, 2)}\nTotal: {Math.Round(totalSum, 2)}";

            // Change the color of the text if the percentage is below 70%
            if (assignedPercentage < 70)
            {
                label1.ForeColor = Color.Red;
            }
            else
            {
                label1.ForeColor = SystemColors.ControlText; // Default text color
            }
        }

        private void plotExistingDataToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void loadFAMBIDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            // Create and configure OpenFileDialog
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xls;*.xlsx";
                openFileDialog.Title = "Load Exported Indices for Graphs";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;

                    // Load data from the selected Excel file (exported from Advanced Indices Form)
                    DataTable dataTable = LoadExcelFileToDataTable(filePath);

                    // Check if data was successfully loaded
                    if (dataTable != null)
                    {
                        // Open AdvancedIndicesForm with loaded data for graphs only (no calculations)
                        var advancedForm = new AdvancedIndicesForm();
                        InheritParentIcon(advancedForm);
                        advancedForm.LoadIndicesFromExcel(dataTable);
                        advancedForm.Show();
                    }
                    else
                    {
                        MessageBox.Show("Failed to load indices data from the selected file.\n\nMake sure you're loading an Excel file exported from the Advanced Indices Form.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private DataTable LoadExcelFileToDataTable(string filePath)
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true // Use first row as header
                        }
                    });

                    // Assume the first table in the dataset is the one we need
                    return result.Tables.Count > 0 ? result.Tables[0] : null;
                }
            }
        }

        private void ForamEcoQS_Load(object sender, EventArgs e)
        {

        }

        private void cleanNormalizeButton_Click(object sender, EventArgs e)
        {
            listBox1.Enabled = false;
            exportStatsButton.Enabled = false;

            // Step 1: Delete rows marked in red (only if override is NOT checked)
            if (!overrideClassificationCheckBox.Checked)
            {
                label1.Text = "Samples Normalized over 100%";
                for (int i = dataGridView1.Rows.Count - 1; i >= 0; i--)
                {
                    DataGridViewRow row = dataGridView1.Rows[i];
                    string speciesName = row.Cells[0].Value?.ToString();

                    // Check if row is red AND not overridden
                    if (row.DefaultCellStyle.BackColor == Color.Red && 
                        (string.IsNullOrEmpty(speciesName) || !overrideManager.HasOverride(speciesName)))
                    {
                        dataGridView1.Rows.RemoveAt(i);
                    }
                }
            }
            else
            {
                label1.Text = "Normalized (unassigned included)";
            }

            // Step 2: Normalize the data in the remaining rows
            for (int colIndex = 1; colIndex < dataGridView1.Columns.Count; colIndex++) // Skip the first column
            {
                double columnSum = 0;

                // Calculate the sum of the column
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue; // Skip the last empty row used for adding new rows

                    if (double.TryParse(row.Cells[colIndex].Value?.ToString(), out double value))
                    {
                        columnSum += value;
                    }
                    else
                    {
                        row.Cells[colIndex].Value = 0; // Consider empty cells as 0
                    }
                }

                // Normalize the column
                if (columnSum > 0) // Avoid division by zero
                {
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.IsNewRow) continue; // Skip the last empty row used for adding new rows

                        if (double.TryParse(row.Cells[colIndex].Value.ToString(), out double value))
                        {
                            row.Cells[colIndex].Value = (value / columnSum) * 100;
                        }
                    }
                }
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create an instance of the AboutBox1 form
            AboutBox1 aboutBox = new AboutBox1();

            // Show the AboutBox1 form as a modal dialog
            aboutBox.ShowDialog();
        }

        private void fsiDatabankManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show FSI Databank Manager form
            using var managerForm = new FSIDatabankManagerForm();
            InheritParentIcon(managerForm);
            managerForm.DatabankUpdated += (s, ev) =>
            {
                // Optionally refresh any FSI-related UI elements
                var (source, count) = SpecializedDatabankLoader.GetFSIDatabankInfo();
                // Could update a status label if desired
            };
            managerForm.ShowDialog(this);
        }

        private void foramAMBIDatabankManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show Foram-AMBI Databank Manager form
            string currentDatabank = comboBox1.SelectedItem?.ToString() ?? "Jorissen";
            using var managerForm = new ForamAMBIDatabankManagerForm(currentDatabank);
            InheritParentIcon(managerForm);
            managerForm.DatabankUpdated += (s, ev) =>
            {
                // Reload databank if user made changes
                MessageBox.Show("Foram-AMBI databank updated. Re-run comparison for changes to take effect.",
                    "Databank Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            managerForm.ShowDialog(this);
        }

        private void geographicAreasDatabankToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show Geographic Areas Databank form
            using var geographicForm = new GeographicAreasDatabankForm();
            InheritParentIcon(geographicForm);
            geographicForm.ShowDialog(this);
        }

        private void userCustomListsManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show User Custom Lists Manager form
            using var customListsForm = new UserCustomListsManagerForm();
            InheritParentIcon(customListsForm);
            customListsForm.ListsUpdated += (s, ev) =>
            {
                // Optionally refresh any UI elements that depend on custom lists
            };
            customListsForm.ShowDialog(this);
        }

        private void indexSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show Index Settings form with current settings
            using var settingsForm = new IndexSettingsForm(
                _currentFAMBIThreshold, _currentTSIReference,
                _currentTSIThreshold, _currentExpHbcThreshold,
                _useJorissenList, _calculateEQR, _fsiRefValue, _expHbcRefValue);
            InheritParentIcon(settingsForm);
            if (settingsForm.ShowDialog(this) == DialogResult.OK)
            {
                // Store settings for later use
                _currentFAMBIThreshold = settingsForm.FAMBIThreshold;
                _currentTSIReference = settingsForm.TSIReference;
                _currentTSIThreshold = settingsForm.TSIThreshold;
                _currentExpHbcThreshold = settingsForm.ExpHbcThreshold;
                _useJorissenList = settingsForm.UseJorissenTolerantList;
                _calculateEQR = settingsForm.CalculateEQR;
                _fsiRefValue = settingsForm.FSIReferenceValue;
                _expHbcRefValue = settingsForm.ExpHbcReferenceValue;

                MessageBox.Show("Index settings updated. New settings will be applied to next calculation.",
                    "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Index settings storage
        private FAMBIThresholdType _currentFAMBIThreshold = FAMBIThresholdType.Borja2003;
        private TSIReferenceType _currentTSIReference = TSIReferenceType.Barras2014_150um;
        private TSIThresholdType _currentTSIThreshold = TSIThresholdType.Parent2021;
        private ExpHbcThresholdType _currentExpHbcThreshold = ExpHbcThresholdType.OBrien2021_Norwegian63um;
        private bool _useJorissenList = false;
        private bool _calculateEQR = false;
        private double _fsiRefValue = 10.0;
        private double _expHbcRefValue = 20.0;

        private void listBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void exportStatsButton_Click(object sender, EventArgs e)
        {
            // Open a SaveFileDialog to specify the path to save the Excel file
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                saveFileDialog.Title = "Save Excel File";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Create a new Excel workbook using ClosedXML
                        using (var workbook = new XLWorkbook())
                        {
                            // Add a new worksheet
                            var worksheet = workbook.Worksheets.Add("Results");

                            // Set the first column header and labels
                            worksheet.Cell(1, 1).Value = "Sample";
                            worksheet.Cell(2, 1).Value = "Assigned";
                            worksheet.Cell(3, 1).Value = "Unassigned";
                            worksheet.Cell(4, 1).Value = "Total";

                            // Initialize column index in Excel for samples (starts at 2 for ClosedXML)
                            int excelColumnIndex = 2;

                            // Iterate through each item in the ListBox
                            foreach (var item in listBox1.Items)
                            {
                                // Get the column name from the ListBox
                                string columnName = item.ToString();

                                // Find the column in a case-insensitive way
                                DataGridViewColumn column = dataGridView1.Columns
                                    .Cast<DataGridViewColumn>()
                                    .FirstOrDefault(c => string.Equals(c.HeaderText, columnName, StringComparison.OrdinalIgnoreCase));

                                if (column == null)
                                    continue; // Skip if column not found

                                int columnIndex = column.Index;

                                double assignedSum = 0;
                                double unassignedSum = 0;

                                // Iterate through the rows of the DataGridView
                                foreach (DataGridViewRow row in dataGridView1.Rows)
                                {
                                    if (row.Cells[columnIndex].Value != null && double.TryParse(row.Cells[columnIndex].Value.ToString(), out double cellValue))
                                    {
                                        if (row.DefaultCellStyle.BackColor == Color.Red)
                                        {
                                            // This is an unassigned (mismatched) row
                                            unassignedSum += cellValue;
                                        }
                                        else
                                        {
                                            // This is an assigned (matched) row
                                            assignedSum += cellValue;
                                        }
                                    }
                                }

                                // Calculate total
                                double totalSum = assignedSum + unassignedSum;

                                // Write the sample name in the header row
                                worksheet.Cell(1, excelColumnIndex).Value = columnName;

                                // Write assigned, unassigned, and total values (rounded to 2 decimals)
                                worksheet.Cell(2, excelColumnIndex).Value = Math.Round(assignedSum, 2);
                                worksheet.Cell(3, excelColumnIndex).Value = Math.Round(unassignedSum, 2);
                                worksheet.Cell(4, excelColumnIndex).Value = Math.Round(totalSum, 2);

                                // Move to the next column
                                excelColumnIndex++;
                            }

                            // Save the workbook
                            workbook.SaveAs(saveFileDialog.FileName);

                            // Inform the user that the file was saved successfully
                            MessageBox.Show("Excel file saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while saving the Excel file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void advancedIndicesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LaunchAdvancedIndices();
        }

        private void advancedIndicesButton_Click(object sender, EventArgs e)
        {
            LaunchAdvancedIndices();
        }

        private void plotIndicesButton_Click(object sender, EventArgs e)
        {
            LaunchAdvancedIndices(form =>
            {
                form.PreselectIndices(new[] { "Foram-AMBI", "FSI", "NQIf", "exp(H'bc)", "FIEI" });
                form.FocusPlotTab("Grouped Bar");
                form.GenerateSelectedPlot("Grouped Bar");
            });
        }

        private void compositePlotButton_Click(object sender, EventArgs e)
        {
            LaunchAdvancedIndices(form => form.ShowCompositePanel());
        }

        private void LaunchAdvancedIndices(Action<AdvancedIndicesForm> configureForm = null)
        {
            // Check if we have data to analyze
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Please load sample data first.", "No Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if we have sample columns (Column 0 is Species, so need > 1)
            if (dataGridView1.Columns.Count <= 1)
            {
                MessageBox.Show("Please add at least one sample column to calculate indices.", "No Samples",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Determine if comparison has been done and databank is valid for F-AMBI
            bool comparisonDone = cleanNormalizeButton.Enabled;
            bool fambiAvailable = comparisonDone && databank != null;
            bool foramIndexAvailable = SpecializedDatabankLoader.CheckFoRAMDatabankAvailability();

            // Show index selection dialog
            List<string> selectedIndices = null;
            using (var selectionForm = new IndexSelectionForm(fambiAvailable, foramIndexAvailable))
            {
                InheritParentIcon(selectionForm);
                if (selectionForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                selectedIndices = selectionForm.SelectedIndices;
            }

            if (selectedIndices == null || selectedIndices.Count == 0)
            {
                MessageBox.Show("No indices selected.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sourceData = BuildSourceDataTable();

            // Check if TSI-Med databank is available and prompt for mud percentages
            // ONLY if TSI-Med is selected
            Dictionary<string, double> mudPercentages = null;
            var (fsiAvail, tsiAvail) = SpecializedDatabankLoader.CheckDatabanksAvailability();

            if (selectedIndices.Contains("TSI-Med") && tsiAvail)
            {
                // Get sample names from columns (skip first column which is species)
                var sampleNames = new List<string>();
                for (int i = 1; i < sourceData.Columns.Count; i++)
                {
                    sampleNames.Add(sourceData.Columns[i].ColumnName);
                }

                // Check if mud percentages were auto-extracted from loaded file
                if (extractedMudPercentages != null && extractedMudPercentages.Count > 0)
                {
                    // Mud values were auto-detected - show form pre-populated for confirmation/editing
                    using var mudForm = new MudPercentageForm(sampleNames, extractedMudPercentages);
                    InheritParentIcon(mudForm);
                    if (mudForm.ShowDialog() == DialogResult.OK)
                    {
                        mudPercentages = mudForm.MudPercentages;
                    }
                }
                else
                {
                    // No auto-detected values - ask user if they want to provide mud percentages
                    var result = MessageBox.Show(
                        "TSI-Med index requires sediment grain-size data (% mud <63 µm) for accurate calculation.\n\n" +
                        "Do you want to provide mud percentages for each sample?\n\n" +
                        "Click 'Yes' to enter values, 'No' to use default (50%).",
                        "TSI-Med: Sediment Data",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        using var mudForm = new MudPercentageForm(sampleNames);
                        InheritParentIcon(mudForm);
                        if (mudForm.ShowDialog() == DialogResult.OK)
                        {
                            mudPercentages = mudForm.MudPercentages;
                        }
                    }
                }
            }

            var advancedForm = new AdvancedIndicesForm();
            InheritParentIcon(advancedForm);
            // Apply user-selected index settings (thresholds, reference curves)
            advancedForm.ConfigureIndexSettings(
                _currentFAMBIThreshold, _currentTSIReference,
                _currentTSIThreshold, _currentExpHbcThreshold);
            // Pass databank only if comparison was performed, otherwise pass null to skip F-AMBI
            // Pass overrides for robust calculation
            advancedForm.LoadResults(sourceData, comparisonDone ? databank : null, mudPercentages, selectedIndices, overrideManager.GetAllOverrides());
            configureForm?.Invoke(advancedForm);
            advancedForm.Show();
        }

        private DataTable BuildSourceDataTable()
        {
            // Create DataTable from dataGridView1
            DataTable sourceData = new DataTable();

            // Add columns
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                sourceData.Columns.Add(col.HeaderText, typeof(string));
            }

            // Add rows using the same inclusion logic as F-AMBI calculations
            foreach (DataGridViewRow row in GetRowsForCalculations())
            {
                DataRow newRow = sourceData.NewRow();
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    newRow[i] = row.Cells[i].Value?.ToString() ?? "";
                }
                sourceData.Rows.Add(newRow);
            }

            return sourceData;
        }

        /// <summary>
        /// Returns the rows to use for any index calculation, honoring the override checkbox.
        /// </summary>
        private IEnumerable<DataGridViewRow> GetRowsForCalculations()
        {
            return dataGridView1.Rows
                .Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow &&
                              (overrideClassificationCheckBox.Checked ||
                               row.DefaultCellStyle.BackColor != Color.Red));
        }

        /// <summary>
        /// Extracts mud percentage row from a DataTable if present.
        /// Looks for rows with first column matching "mud", "Mud", "MUD", "mud (%)", etc. (case insensitive).
        /// If found, extracts values for each sample column and removes the row from the DataTable.
        /// </summary>
        /// <param name="dataTable">The DataTable to process</param>
        /// <returns>Dictionary of sample name to mud percentage, or null if no mud row found</returns>
        private Dictionary<string, double> ExtractMudRowFromDataTable(DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0 || dataTable.Columns.Count <= 1)
                return null;

            // Patterns to match for mud row (case insensitive)
            string[] mudPatterns = { "mud", "mud (%)", "mud(%)", "% mud", "%mud", "fango", "fango (%)" };

            // Find the mud row
            DataRow mudRow = null;
            int mudRowIndex = -1;

            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var row = dataTable.Rows[i];
                string firstCellValue = row[0]?.ToString()?.Trim() ?? "";

                // Check if this row matches any mud pattern (case insensitive)
                foreach (var pattern in mudPatterns)
                {
                    if (string.Equals(firstCellValue, pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        mudRow = row;
                        mudRowIndex = i;
                        break;
                    }
                }

                if (mudRow != null)
                    break;
            }

            if (mudRow == null)
                return null;

            // Extract mud percentages for each sample column (skip first column which is species/row names)
            var mudPercentages = new Dictionary<string, double>();

            for (int colIndex = 1; colIndex < dataTable.Columns.Count; colIndex++)
            {
                string sampleName = dataTable.Columns[colIndex].ColumnName;
                double mudValue = 50.0; // Default value

                var cellValue = mudRow[colIndex];
                if (cellValue != null && cellValue != DBNull.Value)
                {
                    string valueStr = cellValue.ToString().Trim();
                    // Remove % sign if present
                    valueStr = valueStr.Replace("%", "").Trim();

                    if (double.TryParse(valueStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double parsedValue))
                    {
                        // Clamp to valid range 0-100
                        mudValue = Math.Max(0, Math.Min(100, parsedValue));
                    }
                    else if (double.TryParse(valueStr, out parsedValue))
                    {
                        mudValue = Math.Max(0, Math.Min(100, parsedValue));
                    }
                }

                mudPercentages[sampleName] = mudValue;
            }

            // Remove the mud row from the DataTable
            dataTable.Rows.RemoveAt(mudRowIndex);
            dataTable.AcceptChanges();

            return mudPercentages;
        }
        private void DataGridView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hitTest = dataGridView1.HitTest(e.X, e.Y);
                if (hitTest.Type == DataGridViewHitTestType.Cell && hitTest.RowIndex >= 0)
                {
                    dataGridView1.ClearSelection();
                    dataGridView1.Rows[hitTest.RowIndex].Selected = true;
                    
                    var contextMenu = new ContextMenuStrip();
                    var overrideItem = new ToolStripMenuItem("Override Ecological Group...");
                    overrideItem.Click += (s, ev) => ShowOverrideDialog(hitTest.RowIndex);
                    contextMenu.Items.Add(overrideItem);
                    
                    // Add "Clear Override" option if applicable
                    string speciesName = dataGridView1.Rows[hitTest.RowIndex].Cells[0].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(speciesName) && overrideManager.HasOverride(speciesName))
                    {
                        var clearItem = new ToolStripMenuItem("Remove Override");
                        clearItem.Click += (s, ev) => 
                        {
                            overrideManager.RemoveOverride(speciesName);
                            // Refresh color (re-run logic logic simplified)
                            compareDatabankButton.PerformClick();
                        };
                        contextMenu.Items.Add(clearItem);
                    }

                    contextMenu.Items.Add(new ToolStripSeparator());

                    var clearAllItem = new ToolStripMenuItem("Reset All Overrides");
                    clearAllItem.Click += (s, ev) =>
                    {
                        if (MessageBox.Show("Are you sure you want to remove ALL manual overrides?\nThis action cannot be undone.", 
                            "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            overrideManager.ClearAll();
                            compareDatabankButton.PerformClick();
                        }
                    };
                    contextMenu.Items.Add(clearAllItem);

                    contextMenu.Show(dataGridView1, e.Location);
                }
            }
        }

        private void ShowOverrideDialog(int rowIndex)
        {
            string speciesName = dataGridView1.Rows[rowIndex].Cells[0].Value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(speciesName)) return;

            int currentGroup = overrideManager.GetOverride(speciesName) ?? 0;
            using (var dialog = new OverrideForm(speciesName, currentGroup))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    overrideManager.AddOverride(speciesName, dialog.SelectedGroup);
                    // Refresh colors
                    compareDatabankButton.PerformClick();
                }
            }
        }
    }
}