//MIT License
// MainWindow.cs - The ForamEcoQS main window: sample grid, databank comparison and index launch.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Eto.Drawing;
using Eto.Forms;
using ForamEcoQS.Compat;
using DataGridView = ForamEcoQS.Compat.DataGridView;
using MessageBox = ForamEcoQS.Compat.MessageBox;

namespace ForamEcoQS
{
    public class MainWindow : Form
    {
        private int _datasetLoaded;
        private DataTable _databank;
        private Dictionary<string, double> _extractedMudPercentages;
        private readonly EcologicalGroupOverrideManager _overrideManager;
        private readonly Stack<DeletedColumnInfo> _deletedColumns = new Stack<DeletedColumnInfo>();

        private readonly DataGridView _dataGridView;
        private readonly DropDown _databankCombo;
        private readonly ListBox _sampleListBox;
        private readonly Label _statsLabel;
        private readonly Label _statsHintLabel;
        private readonly CheckBox _overrideClassificationCheckBox;

        private readonly AccentButton _showDatabankButton;
        private readonly AccentButton _compareDatabankButton;
        private readonly AccentButton _advancedIndicesButton;
        private readonly AccentButton _plotIndicesButton;
        private readonly AccentButton _compositePlotButton;
        private readonly AccentButton _cleanNormalizeButton;
        private readonly AccentButton _exportStatsButton;

        private readonly Command _editDataCommand;
        private readonly Command _saveCommand;
        private readonly Command _advancedIndicesCommand;

        // Index settings storage
        private FAMBIThresholdType _currentFAMBIThreshold = FAMBIThresholdType.Borja2003;
        private TSIReferenceType _currentTSIReference = TSIReferenceType.Barras2014_150um;
        private TSIThresholdType _currentTSIThreshold = TSIThresholdType.Parent2021;
        private ExpHbcThresholdType _currentExpHbcThreshold = ExpHbcThresholdType.OBrien2021_Norwegian63um;
        private bool _useJorissenList;
        private bool _calculateEQR;
        private double _fsiRefValue = 10.0;
        private double _expHbcRefValue = 20.0;
        private bool _useWormsVerification;

        /// <summary>Decimals kept when a sample column is normalized to 100%.</summary>
        private const int NormalizedDecimals = 2;

        private readonly string[] _databankOptions =
        {
            "Jorissen (Mediterranean, Foram-AMBI)",
            "Alve (NE Atlantic/Arctic, Foram-AMBI)",
            "Bouchet (Mediterranean, Foram-AMBI)",
            "Bouchet (Atlantic, Foram-AMBI)",
            "Bouchet (South Atlantic, Foram-AMBI)",
            "O'Malley (Gulf of Mexico, Foram-AMBI)"
        };

        public MainWindow()
        {
            Title = "ForamEcoQS";
            ClientSize = new Size(1447, 780);
            this.Prepare();

            _overrideManager = new EcologicalGroupOverrideManager();

            // ---------- Grid ----------
            _dataGridView = new DataGridView
            {
                SelectionMode = Compat.DataGridViewSelectionMode.FullColumnSelect,
                AutoSizeColumnsMode = Compat.DataGridViewAutoSizeColumnsMode.DisplayedCells
            };
            _dataGridView.CellRightClick += DataGridView_CellRightClick;

            // ---------- Databank group ----------
            _databankCombo = new DropDown();
            var sortedOptions = _databankOptions.ToArray();
            Array.Sort(sortedOptions, StringComparer.Ordinal);
            foreach (var option in sortedOptions)
            {
                _databankCombo.Items.Add(option);
            }

            // Select the full descriptive entry rather than the old short label, so the first
            // databank comparison always has a valid selection.
            string alveOption = _databankOptions.First(o => o.StartsWith("Alve"));
            _databankCombo.SelectedIndex = sortedOptions.ToList().IndexOf(alveOption);

            _showDatabankButton = UiHelpers.ActionButton("Show DataBank", AppColors.Teal, (s, e) => ShowDatabank());

            var databankGroup = new GroupBox
            {
                Text = "Databank (Foram-AMBI)",
                Content = new TableLayout
                {
                    Padding = new Padding(8),
                    Spacing = new Size(0, 8),
                    Rows = { new TableRow(_databankCombo), new TableRow(_showDatabankButton) }
                }
            };

            _compareDatabankButton = UiHelpers.ActionButton("Compare", AppColors.SteelBlue);
            _compareDatabankButton.Click += async (s, e) => await CompareDatabankAsync();
            _compareDatabankButton.Enabled = false;

            _overrideClassificationCheckBox = new CheckBox
            {
                Text = "Include unassigned taxa",
                ToolTip = "When checked, unassigned taxa will be included in normalization\n" +
                          "and all index calculations (Foram-AMBI and others) instead of being removed."
            };

            // ---------- Indices group ----------
            _advancedIndicesButton = UiHelpers.ActionButton("Calculate Indices", AppColors.SeaGreen,
                (s, e) => LaunchAdvancedIndices());
            _advancedIndicesButton.Enabled = false;

            _plotIndicesButton = UiHelpers.ActionButton("Open Plot Options", AppColors.SteelBlue, (s, e) =>
                LaunchAdvancedIndices(form =>
                {
                    form.PreselectIndices(new[] { "Foram-AMBI", "FSI", "NQIf", "exp(H'bc)", "FIEI" });
                    form.FocusPlotTab("Grouped Bar");
                    form.GenerateSelectedPlot("Grouped Bar");
                }));
            _plotIndicesButton.Enabled = false;

            _compositePlotButton = UiHelpers.ActionButton("Open Composite Dashboard", AppColors.CornflowerBlue,
                (s, e) => LaunchAdvancedIndices(form => form.ShowCompositePanel()));
            _compositePlotButton.Enabled = false;

            var indicesGroup = new GroupBox
            {
                Text = "Indices & Plots",
                Content = new TableLayout
                {
                    Padding = new Padding(8),
                    Spacing = new Size(0, 8),
                    Rows =
                    {
                        new TableRow(_advancedIndicesButton),
                        new TableRow(_plotIndicesButton),
                        new TableRow(_compositePlotButton)
                    }
                }
            };

            // ---------- Statistics group ----------
            _statsHintLabel = new Label { Text = "Click on a sample to see its stats" };
            _sampleListBox = new ListBox { Height = 110, Visible = false };
            _sampleListBox.SelectedIndexChanged += SampleListBox_SelectedIndexChanged;
            _statsLabel = new Label { Text = "None" };

            var statsGroup = new GroupBox
            {
                Text = "Statistics",
                Content = new TableLayout
                {
                    Padding = new Padding(8),
                    Spacing = new Size(0, 6),
                    Rows =
                    {
                        new TableRow(_statsHintLabel),
                        new TableRow(_sampleListBox),
                        new TableRow(_statsLabel),
                        null
                    }
                }
            };

            _exportStatsButton = UiHelpers.ActionButton("Export Stats", AppColors.CornflowerBlue, (s, e) => ExportStats());
            _exportStatsButton.Enabled = false;

            _cleanNormalizeButton = UiHelpers.ActionButton("Clean and normalize", AppColors.Orange,
                (s, e) => CleanAndNormalize());
            _cleanNormalizeButton.Enabled = false;

            var sidePanel = new TableLayout
            {
                Padding = new Padding(8, 0, 0, 0),
                Spacing = new Size(0, 10),
                Width = 285,
                Rows =
                {
                    new TableRow(databankGroup),
                    new TableRow(_compareDatabankButton),
                    new TableRow(_overrideClassificationCheckBox),
                    new TableRow(indicesGroup),
                    new TableRow(statsGroup) { ScaleHeight = true },
                    new TableRow(_exportStatsButton),
                    new TableRow(_cleanNormalizeButton)
                }
            };

            Content = new TableLayout
            {
                Padding = new Padding(10),
                Spacing = new Size(10, 0),
                Rows =
                {
                    new TableRow(new TableCell(_dataGridView, true), new TableCell(sidePanel))
                    {
                        ScaleHeight = true
                    }
                }
            };

            // ---------- Menu ----------
            _editDataCommand = new Command { MenuText = "Edit Data", Enabled = false };
            _saveCommand = new Command((s, e) => SaveSamples()) { MenuText = "Save Samples", Enabled = false };
            _advancedIndicesCommand = new Command((s, e) => LaunchAdvancedIndices())
            {
                MenuText = "Advanced Indices",
                Enabled = false
            };

            Menu = BuildMenu();

            // The default databank is loaded up front so "Compare" works without touching the combo.
            _databank = LoadDatabankByName(alveOption);
            _databankCombo.SelectedIndexChanged += (s, e) => LoadSelectedDataset(SelectedDatabankName());
        }

        private MenuBar BuildMenu()
        {
            var newEmptyDataset = new Command((s, e) => NewEmptyDataset()) { MenuText = "Empty Dataset" };
            var clearWorkspace = new Command((s, e) => PrepareNewWorkspace()) { MenuText = "Clear Workspace" };

            var newSubMenu = new SubMenuItem
            {
                Text = "New",
                Items = { newEmptyDataset, clearWorkspace }
            };

            var fileMenu = new SubMenuItem
            {
                Text = "&File",
                Items =
                {
                    newSubMenu,
                    new Command((s, e) => CreateTemplateFromDatabank()) { MenuText = "Create Template from Databank" },
                    new Command((s, e) => CreateFSITemplate()) { MenuText = "Create FSI Template" },
                    new Command((s, e) => CreateTSIMedTemplate()) { MenuText = "Create TSI-Med Template" },
                    new Command((s, e) => OpenDataFile()) { MenuText = "Open" },
                    new Command((s, e) => LoadIndicesForGraphs()) { MenuText = "Load Indices for Graphs..." },
                    _saveCommand,
                    new Command((s, e) => Close()) { MenuText = "Exit" }
                }
            };

            var editMenu = new SubMenuItem
            {
                Text = "&Edit Data",
                Items =
                {
                    new Command((s, e) => NewSample()) { MenuText = "New Sample" },
                    new Command((s, e) => RemoveSelectedSample()) { MenuText = "Remove Selected Sample" },
                    new Command((s, e) => RenameSelectedSample()) { MenuText = "Rename Selected Sample" },
                    new SeparatorMenuItem(),
                    new Command((s, e) => TransposeLoadedData()) { MenuText = "Transpose Data (species \u2194 samples)" }
                }
            };

            var toolsMenu = new SubMenuItem
            {
                Text = "&Tools",
                Items =
                {
                    new Command((s, e) => ShowFsiDatabankManager()) { MenuText = "FSI Databank Manager..." },
                    new Command((s, e) => ShowForamAmbiDatabankManager()) { MenuText = "Foram-AMBI Databank Manager..." },
                    new Command((s, e) => ShowGeographicAreasDatabank()) { MenuText = "Geographic Areas Database..." },
                    new Command((s, e) => ShowUserCustomListsManager()) { MenuText = "User Custom Lists Manager..." },
                    new SeparatorMenuItem(),
                    new Command((s, e) => ExportOverrides()) { MenuText = "Export Ecological-Group Overrides..." },
                    new Command((s, e) => ImportOverrides()) { MenuText = "Import Ecological-Group Overrides..." },
                    new SeparatorMenuItem(),
                    new Command((s, e) => ShowIndexSettings()) { MenuText = "Index Calculation Settings..." }
                }
            };

            return new MenuBar
            {
                Items =
                {
                    fileMenu,
                    editMenu,
                    new SubMenuItem { Text = "&Undo", Items = { new Command((s, e) => UndoColumnRemoval()) { MenuText = "Undo Remove Sample" } } },
                    new SubMenuItem { Text = "&Advanced", Items = { _advancedIndicesCommand } },
                    toolsMenu,
                    new SubMenuItem { Text = "&About", Items = { new Command((s, e) => ShowAbout()) { MenuText = "About ForamEcoQS" } } }
                }
            };
        }

        private string SelectedDatabankName() =>
            _databankCombo.SelectedIndex >= 0 ? _databankCombo.Items[_databankCombo.SelectedIndex].Text : null;

        #region Databank handling

        private string PromptForDatabankSelection(string title)
        {
            using var dialog = new ChoiceDialog(
                title, "Select a databank (Foram-AMBI):", _databankOptions, SelectedDatabankName());
            return dialog.ShowModal(this) == true ? dialog.SelectedValue : null;
        }

        private char? PromptForCsvSeparator()
        {
            using var dialog = new ChoiceDialog(
                "CSV Separator",
                "Select the column separator used in your CSV file:",
                new[] { "Semicolon (;)", "Comma (,)", "Tab", "Pipe (|)" });

            if (dialog.ShowModal(this) != true)
            {
                return null;
            }

            return dialog.SelectedIndex switch
            {
                0 => ';',
                1 => ',',
                2 => '\t',
                3 => '|',
                _ => ';'
            };
        }

        private DataTable LoadDatabankByName(string selectedOption)
        {
            if (string.IsNullOrWhiteSpace(selectedOption))
            {
                return null;
            }

            var loader = new LoadDataBank();

            // Match by prefix to handle the descriptive names.
            if (selectedOption.StartsWith("Jorissen")) return loader.LoadDataSet("jorissen");
            if (selectedOption.StartsWith("Alve")) return loader.LoadDataSet("alve");
            if (selectedOption.StartsWith("Bouchet") && selectedOption.Contains("Mediterranean")) return loader.LoadDataSet("bouchetmed");
            if (selectedOption.StartsWith("Bouchet") && selectedOption.Contains("South Atlantic")) return loader.LoadDataSet("bouchetsouthatl");
            if (selectedOption.StartsWith("Bouchet") && selectedOption.Contains("Atlantic")) return loader.LoadDataSet("bouchetatl");
            if (selectedOption.StartsWith("O'Malley")) return loader.LoadDataSet("OMalley2021");

            MessageBox.Show("Unknown dataset selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }

        private void LoadSelectedDataset(string selectedOption)
        {
            try
            {
                _databank = LoadDatabankByName(selectedOption);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"Could not load the selected databank:\n{ex.Message}", "Databank Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _databank = null;
            }
        }

        private void ShowDatabank()
        {
            if (_databank == null)
            {
                MessageBox.Show("The databank is empty or not loaded.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string databankName = SelectedDatabankName() ?? "Foram-AMBI Databank";
            new DataBankViewerForm(_databank, databankName).Show();
        }

        #endregion

        #region Workspace

        private void UpdateIndicesButtonState()
        {
            bool hasData = _datasetLoaded == 1 && _dataGridView.Columns.Count > 1;
            _advancedIndicesCommand.Enabled = hasData;
            _advancedIndicesButton.Enabled = hasData;
            _plotIndicesButton.Enabled = hasData;
            _compositePlotButton.Enabled = hasData;
        }

        private void PrepareNewWorkspace()
        {
            _dataGridView.DataSource = null;
            _dataGridView.Rows.Clear();
            _dataGridView.Columns.Clear();
            _dataGridView.SelectionMode = Compat.DataGridViewSelectionMode.FullColumnSelect;

            _datasetLoaded = 0;
            _editDataCommand.Enabled = false;
            _exportStatsButton.Enabled = false;
            _sampleListBox.Items.Clear();
            _statsLabel.Text = "None";
            _cleanNormalizeButton.Enabled = false;
            _advancedIndicesCommand.Enabled = false;
            _advancedIndicesButton.Enabled = false;
            _plotIndicesButton.Enabled = false;
            _compositePlotButton.Enabled = false;
            _compareDatabankButton.Enabled = false;
            _saveCommand.Enabled = false;
            _sampleListBox.Visible = false;
        }

        private void NewEmptyDataset()
        {
            string selectedOption = PromptForDatabankSelection("Create Empty Dataset");
            if (string.IsNullOrWhiteSpace(selectedOption))
            {
                return;
            }

            PopulateTemplateFromDatabank(LoadDatabankByName(selectedOption));
        }

        private void CreateTemplateFromDatabank()
        {
            string selectedOption = PromptForDatabankSelection("Create Template from Databank");
            if (string.IsNullOrWhiteSpace(selectedOption))
            {
                return;
            }

            PopulateTemplateFromDatabank(LoadDatabankByName(selectedOption));
        }

        private void CreateFSITemplate()
        {
            var fsiLookup = SpecializedDatabankLoader.LoadFSIDatabank();
            if (fsiLookup == null || fsiLookup.Count == 0)
            {
                MessageBox.Show(
                    "The FSI databank is not available. Please ensure fsi_databank.csv is in the application folder.",
                    "FSI Databank Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PopulateTemplateFromDatabank(BuildTemplateDataTable(fsiLookup.Keys));
        }

        private void CreateTSIMedTemplate()
        {
            var tsiMedLookup = SpecializedDatabankLoader.LoadTSIMedDatabank();
            if (tsiMedLookup == null || tsiMedLookup.Count == 0)
            {
                MessageBox.Show(
                    "The TSI-Med databank is not available. Please ensure tsimed_databank.csv is in the application folder.",
                    "TSI-Med Databank Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PopulateTemplateFromDatabank(BuildTemplateDataTable(tsiMedLookup));
        }

        private void PopulateTemplateFromDatabank(DataTable selectedDatabank)
        {
            if (selectedDatabank == null || selectedDatabank.Rows.Count == 0)
            {
                MessageBox.Show("The selected databank is empty or could not be loaded.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            PrepareNewWorkspace();

            _dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "Species", HeaderText = "Species" });
            _dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sample", HeaderText = "Sample" });

            foreach (DataRow row in selectedDatabank.Rows)
            {
                _dataGridView.Rows.Add(row[0]?.ToString(), string.Empty);
            }

            _datasetLoaded = 1;
            _databank = selectedDatabank;
            _editDataCommand.Enabled = true;
            _saveCommand.Enabled = _dataGridView.Rows.Count > 0;
            _compareDatabankButton.Enabled = _dataGridView.Rows.Count > 0;

            UpdateIndicesButtonState();
        }

        private static DataTable BuildTemplateDataTable(IEnumerable<string> speciesNames)
        {
            var template = new DataTable();
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

        #endregion

        #region File open / save

        private void OpenDataFile()
        {
            using var openDialog = new OpenFileDialog
            {
                Title = "Open Data File",
                Filters = { Filters.Excel, Filters.Csv }
            };

            if (openDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            string filePath = openDialog.FileName;
            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

            _extractedMudPercentages = null;

            try
            {
                if (fileExtension == ".xls" || fileExtension == ".xlsx")
                {
                    LoadExcelIntoGrid(filePath);
                }
                else if (fileExtension == ".csv")
                {
                    char? separator = PromptForCsvSeparator();
                    if (separator == null)
                    {
                        return;
                    }
                    LoadCsvIntoGrid(filePath, separator.Value);
                }
                else
                {
                    MessageBox.Show("Unsupported file type.", "Open", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Open Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Put the matrix the right way round before anything reads it, then extract the mud
            // row if present (works for both Excel and CSV).
            if (_dataGridView.DataSource is DataTable loadedDataTable)
            {
                loadedDataTable = ApplyMatrixOrientation(loadedDataTable);
                _extractedMudPercentages = SpecializedDatabankLoader.ExtractMudRowFromDataTable(loadedDataTable);
            }

            _datasetLoaded = 1;
            _editDataCommand.Enabled = true;
            _saveCommand.Enabled = _dataGridView.Rows.Count > 0;
            _cleanNormalizeButton.Enabled = false;
            _compareDatabankButton.Enabled = _dataGridView.Rows.Count > 0;
            UpdateIndicesButtonState();
            _sampleListBox.Items.Clear();
            _statsLabel.Text = "None";
            _sampleListBox.Visible = false;
            _exportStatsButton.Enabled = false;
        }

        /// <summary>
        /// ForamEcoQS expects species down the first column, but spreadsheets are just as often
        /// written the other way round. A clear-cut case is flipped straight away; a borderline
        /// one asks, because transposing a file that was already correct is just as wrong.
        /// </summary>
        private DataTable ApplyMatrixOrientation(DataTable dataTable)
        {
            var layout = SampleMatrixLayout.Detect(dataTable);

            if (!layout.NeedsTranspose)
            {
                return dataTable;
            }

            if (layout.Confidence < SampleMatrixLayout.AmbiguousConfidence)
            {
                return dataTable;
            }

            if (layout.Confidence < SampleMatrixLayout.HighConfidence)
            {
                var answer = MessageBox.Show(this,
                    "This file looks like it has species in the columns and samples in the rows, " +
                    "but the evidence is not conclusive.\n\n" + layout.Reason +
                    "\n\nTranspose it so species are in rows?",
                    "Transpose Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                {
                    return dataTable;
                }
            }

            DataTable transposed = SampleMatrixLayout.Transpose(dataTable);
            BindDataTable(transposed);

            MessageBox.Show(this,
                "The file was transposed so that species are in rows and samples in columns.\n\n" +
                layout.Reason + "\n\nUse Edit Data \u25B8 Transpose Data to flip it back.",
                "Transpose Data", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return transposed;
        }

        /// <summary>Flips the loaded matrix on demand, for files the detector did not catch.</summary>
        private void TransposeLoadedData()
        {
            if (_dataGridView.DataSource is not DataTable dataTable || dataTable.Columns.Count == 0)
            {
                MessageBox.Show("Load a data file first.", "Transpose Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BindDataTable(SampleMatrixLayout.Transpose(dataTable));

            _extractedMudPercentages = SpecializedDatabankLoader.ExtractMudRowFromDataTable(
                (DataTable)_dataGridView.DataSource);

            _saveCommand.Enabled = _dataGridView.Rows.Count > 0;
            _compareDatabankButton.Enabled = _dataGridView.Rows.Count > 0;
            UpdateIndicesButtonState();
        }

        private void LoadExcelIntoGrid(string filePath)
        {
            var loadingDialog = new LoadingDialog();
            loadingDialog.Show();

            try
            {
                loadingDialog.SetStatus("Reading Excel file...");

                _dataGridView.DataSource = null;
                _dataGridView.Columns.Clear();
                _dataGridView.SelectionMode = Compat.DataGridViewSelectionMode.CellSelect;

                DataTable dataTable = ExcelDataLoader.LoadTrimmed(filePath, loadingDialog.Report);
                if (dataTable == null)
                {
                    MessageBox.Show("The workbook contains no readable sheet.", "Open Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                loadingDialog.Report($"Loading {dataTable.Columns.Count} columns into grid...", 70);
                BindDataTable(dataTable);
                loadingDialog.Report("Complete!", 100);
            }
            finally
            {
                loadingDialog.Close();
            }
        }

        private void LoadCsvIntoGrid(string filePath, char separator)
        {
            _dataGridView.DataSource = null;
            _dataGridView.Rows.Clear();
            _dataGridView.Columns.Clear();
            _dataGridView.SelectionMode = Compat.DataGridViewSelectionMode.CellSelect;

            BindDataTable(ExcelDataLoader.ReadCsvFile(filePath, separator));
        }

        /// <summary>
        /// Creates the grid columns explicitly (rather than auto-generating them) so numeric
        /// columns keep their two-decimal formatting.
        /// </summary>
        private void BindDataTable(DataTable dataTable)
        {
            _dataGridView.AutoGenerateColumns = false;
            _dataGridView.Columns.Clear();

            foreach (DataColumn col in dataTable.Columns)
            {
                var gridColumn = new DataGridViewTextBoxColumn
                {
                    DataPropertyName = col.ColumnName,
                    Name = col.ColumnName,
                    HeaderText = col.ColumnName,
                    ValueType = col.DataType,
                    SortMode = Compat.DataGridViewColumnSortMode.NotSortable
                };

                if (col.DataType == typeof(double) || col.DataType == typeof(float) || col.DataType == typeof(decimal))
                {
                    gridColumn.DefaultCellStyle.Format = "N2";
                }

                _dataGridView.Columns.Add(gridColumn);
            }

            _dataGridView.DataSource = dataTable;
        }

        private void LoadIndicesForGraphs()
        {
            ExcelDataLoader.EnsureEncodingProvider();

            using var openDialog = new OpenFileDialog
            {
                Title = "Load Exported Indices for Graphs",
                Filters = { Filters.Excel }
            };

            if (openDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                DataTable dataTable = ExcelDataLoader.ReadExcelFile(openDialog.FileName);

                if (dataTable != null)
                {
                    var advancedForm = new AdvancedIndicesForm();
                    advancedForm.LoadIndicesFromExcel(dataTable);
                    advancedForm.Show();
                }
                else
                {
                    MessageBox.Show(
                        "Failed to load indices data from the selected file.\n\n" +
                        "Make sure you're loading an Excel file exported from the Advanced Indices Form.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load indices data: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveSamples()
        {
            if (_dataGridView.Rows.Count == 0)
            {
                MessageBox.Show("No data to save.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Title = "Save as Excel File",
                FileName = "Data.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (saveDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            SaveGridToExcelWithColors(UiHelpers.EnsureExtension(saveDialog.FileName, ".xlsx"));
        }

        private void SaveGridToExcelWithColors(string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Data");

                for (int i = 0; i < _dataGridView.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = _dataGridView.Columns[i].HeaderText;
                }

                for (int i = 0; i < _dataGridView.Rows.Count; i++)
                {
                    var row = _dataGridView.Rows[i];

                    for (int j = 0; j < _dataGridView.Columns.Count; j++)
                    {
                        var cell = worksheet.Cell(i + 2, j + 1); // +2 to account for the header row
                        cell.Value = row.Cells[j].Value?.ToString();

                        // Carry the row highlight (unmatched / overridden species) into the workbook.
                        if (row.DefaultCellStyle.HasBackColor)
                        {
                            cell.Style.Fill.BackgroundColor = row.DefaultCellStyle.BackColor.ToXLColor();
                        }
                    }
                }

                workbook.SaveAs(filePath);

                MessageBox.Show("Data successfully exported!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the Excel file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Sample editing

        private class DeletedColumnInfo
        {
            public int Index { get; set; }
            public string HeaderText { get; set; }
            public string Name { get; set; }
            public List<object> ColumnData { get; set; }
        }

        private void NewSample()
        {
            using var dialog = new NewSampleDialog();
            if (dialog.ShowModal(this) != true || dialog.SampleName == null)
            {
                return;
            }

            _dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = dialog.SampleName,
                HeaderText = dialog.SampleName
            });

            _saveCommand.Enabled = false;
            UpdateIndicesButtonState();
        }

        private void RenameSelectedSample()
        {
            var selectedCells = _dataGridView.SelectedCells;
            if (selectedCells.Count == 0)
            {
                MessageBox.Show("No cells or columns selected.", "Rename Sample",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int columnIndex = selectedCells[0].ColumnIndex;
            if (columnIndex == 0)
            {
                MessageBox.Show("The species column cannot be renamed.", "Rename Sample",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var column = _dataGridView.Columns[columnIndex];

            using var dialog = new TextPromptDialog("Rename Sample", "New sample name:", column.HeaderText);
            if (dialog.ShowModal(this) != true || string.IsNullOrWhiteSpace(dialog.Value))
            {
                return;
            }

            column.HeaderText = dialog.Value;
            column.Name = dialog.Value;
            _dataGridView.Columns.Remove(column);
            _dataGridView.Columns.Insert(columnIndex, column);

            RefreshSampleList();
            _sampleListBox.SelectedIndex = _sampleListBox.Items.ToList().FindIndex(i => i.Text == dialog.Value);
        }

        private void RemoveSelectedSample()
        {
            var selectedCells = _dataGridView.SelectedCells;
            if (selectedCells.Count == 0)
            {
                MessageBox.Show("No cells or columns selected.", "Remove Columns",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int columnIndex = selectedCells[0].ColumnIndex;
            if (columnIndex == 0)
            {
                MessageBox.Show("The species column cannot be removed.", "Remove Columns",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedColumn = _dataGridView.Columns[columnIndex];

            var columnData = _dataGridView.Rows.Select(row => row.Cells[columnIndex].Value).ToList();

            _deletedColumns.Push(new DeletedColumnInfo
            {
                Index = columnIndex,
                HeaderText = selectedColumn.HeaderText,
                Name = selectedColumn.Name,
                ColumnData = columnData
            });

            _dataGridView.Columns.Remove(selectedColumn);
            UpdateIndicesButtonState();
            RefreshSampleList();
        }

        private void UndoColumnRemoval()
        {
            if (_deletedColumns.Count == 0)
            {
                MessageBox.Show("No actions to undo.", "Undo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var deletedColumnInfo = _deletedColumns.Pop();

            _dataGridView.Columns.Insert(deletedColumnInfo.Index, new DataGridViewTextBoxColumn
            {
                Name = deletedColumnInfo.Name,
                HeaderText = deletedColumnInfo.HeaderText
            });

            for (int i = 0; i < _dataGridView.Rows.Count && i < deletedColumnInfo.ColumnData.Count; i++)
            {
                _dataGridView.Rows[i].Cells[deletedColumnInfo.Index].Value = deletedColumnInfo.ColumnData[i];
            }

            _dataGridView.Refresh();
            UpdateIndicesButtonState();
        }

        private void RefreshSampleList()
        {
            _sampleListBox.Items.Clear();
            for (int i = 1; i < _dataGridView.Columns.Count; i++)
            {
                _sampleListBox.Items.Add(_dataGridView.Columns[i].HeaderText);
            }
        }

        #endregion

        #region Databank comparison

        private async Task CompareDatabankAsync()
        {
            if (_datasetLoaded == 0 || _dataGridView.Rows.Count == 0 || _dataGridView.Columns.Count == 0)
            {
                MessageBox.Show("Please load or create a dataset before comparing with a databank.", "No Dataset",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_databank == null || _databank.Rows.Count == 0)
            {
                MessageBox.Show("The databank is empty or not loaded.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _exportStatsButton.Enabled = true;
            _sampleListBox.Enabled = true;

            // Intelligent matching handles species names carrying author citations, such as
            // "Ammonia parkinsoniana (d'Orbigny, 1839)".
            var databankValues = new List<string>();
            foreach (DataRow row in _databank.Rows)
            {
                string value = row[0]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    databankValues.Add(value);
                }
            }

            var speciesMatcher = new SpeciesNameMatcher(databankValues);

            // Rows whose species name matched neither an override nor the databank;
            // candidates for the optional WoRMS online verification pass below.
            var unmatchedRows = new List<DataGridViewRow>();

            foreach (var gridRow in _dataGridView.Rows)
            {
                if (gridRow.Cells[0].Value == null)
                {
                    continue;
                }

                string gridValue = gridRow.Cells[0].Value.ToString()?.Trim();
                gridRow.Cells[0].ToolTipText = string.Empty;

                // Check for an override first.
                if (!string.IsNullOrEmpty(gridValue) && _overrideManager.HasOverride(gridValue))
                {
                    gridRow.DefaultCellStyle.BackColor = Colors.Yellow;
                }
                else if (!string.IsNullOrEmpty(gridValue) && !speciesMatcher.IsMatch(gridValue))
                {
                    gridRow.DefaultCellStyle.BackColor = Colors.Red;
                    unmatchedRows.Add(gridRow);
                }
                else
                {
                    gridRow.DefaultCellStyle.BackColor = _dataGridView.DefaultCellStyle.BackColor;
                }
            }

            _dataGridView.Refresh();

            _sampleListBox.Visible = true;
            RefreshSampleList();

            _cleanNormalizeButton.Enabled = true;
            _advancedIndicesCommand.Enabled = true;
            _advancedIndicesButton.Enabled = true;
            _plotIndicesButton.Enabled = true;
            _compositePlotButton.Enabled = true;

            if (_useWormsVerification && unmatchedRows.Count > 0)
            {
                await VerifyUnmatchedSpeciesWithWormsAsync(unmatchedRows);
            }
        }

        /// <summary>
        /// Looks up species names that were not found in the local databank against the
        /// WoRMS (World Register of Marine Species) online database. Names recognised as
        /// valid marine taxa are re-coloured orange (instead of red) so they are not
        /// silently deleted by "Clean and Normalize", and a tooltip shows the current
        /// accepted name when the entered name is an outdated synonym.
        /// </summary>
        private async Task VerifyUnmatchedSpeciesWithWormsAsync(List<DataGridViewRow> unmatchedRows)
        {
            var namesToCheck = unmatchedRows
                .Select(r => r.Cells[0].Value?.ToString()?.Trim())
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (namesToCheck.Count == 0)
            {
                return;
            }

            _compareDatabankButton.Enabled = false;

            try
            {
                Dictionary<string, WormsRecord> matches;
                try
                {
                    matches = await WormsService.MatchNamesAsync(namesToCheck);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Could not reach the WoRMS (World Register of Marine Species) database:\n{ex.Message}\n\n" +
                        "Species not found in the local databank remain marked in red.",
                        "WoRMS Lookup Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int recognizedCount = 0;
                var suggestions = new List<string>();

                foreach (var row in unmatchedRows)
                {
                    string gridValue = row.Cells[0].Value?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(gridValue))
                    {
                        continue;
                    }

                    if (!matches.TryGetValue(gridValue, out var record) || record == null)
                    {
                        continue;
                    }

                    recognizedCount++;
                    row.DefaultCellStyle.BackColor = Colors.Orange;

                    string acceptedName = record.IsAccepted ? record.Scientificname : record.Valid_name;
                    row.Cells[0].ToolTipText =
                        $"WoRMS: {record.Rank} - {record.Status}\n" +
                        $"AphiaID: {record.AphiaID}\n" +
                        (record.IsAccepted ? string.Empty : $"Accepted name: {acceptedName}\n") +
                        $"Classification: {record.Kingdom} > {record.Phylum} > {record.TaxonomicClass} > " +
                        $"{record.Order} > {record.Family} > {record.Genus}";

                    if (!record.IsAccepted && !string.IsNullOrEmpty(acceptedName) &&
                        !string.Equals(acceptedName, gridValue, StringComparison.OrdinalIgnoreCase))
                    {
                        suggestions.Add($"{gridValue} -> {acceptedName}");
                    }
                }

                _dataGridView.Refresh();

                string summary = "WoRMS verification complete.\n\n" +
                    $"{recognizedCount} of {namesToCheck.Count} unmatched name(s) were recognized as valid marine taxa " +
                    "(highlighted in orange) even though they are not present in the loaded ecological databank.";

                if (suggestions.Count > 0)
                {
                    summary += "\n\nPossible name updates (current name is a synonym of the accepted name):\n" +
                        string.Join("\n", suggestions.Take(20));
                }

                MessageBox.Show(summary, "WoRMS Species Verification",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                _compareDatabankButton.Enabled = true;
            }
        }

        #endregion

        #region Statistics

        private void SampleListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_sampleListBox.SelectedIndex < 0)
            {
                return;
            }

            string selectedColumn = _sampleListBox.Items[_sampleListBox.SelectedIndex].Text;

            var column = _dataGridView.Columns
                .FirstOrDefault(c => string.Equals(c.HeaderText, selectedColumn, StringComparison.OrdinalIgnoreCase));

            if (column == null)
            {
                MessageBox.Show($"Column '{selectedColumn}' does not exist in the grid.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var (assignedSum, unassignedSum) = SumColumn(column.Index);
            double totalSum = assignedSum + unassignedSum;
            double assignedPercentage = totalSum > 0 ? assignedSum / totalSum * 100 : 0;

            _statsLabel.Text =
                $"Assigned: {Math.Round(assignedSum, 2)} ({assignedPercentage:F2}%)\n" +
                $"Unassigned: {Math.Round(unassignedSum, 2)}\n" +
                $"Total: {Math.Round(totalSum, 2)}";

            _statsLabel.TextColor = assignedPercentage < 70 ? Colors.Red : Eto.Drawing.SystemColors.ControlText;
        }

        /// <summary>
        /// Splits a sample column's total into the part contributed by matched species and the
        /// part contributed by rows the comparison marked red (unassigned).
        /// </summary>
        private (double assigned, double unassigned) SumColumn(int columnIndex)
        {
            double assignedSum = 0;
            double unassignedSum = 0;

            foreach (var row in _dataGridView.Rows)
            {
                if (row.Cells[columnIndex].Value == null ||
                    !double.TryParse(row.Cells[columnIndex].Value.ToString(), out double cellValue))
                {
                    continue;
                }

                if (IsUnassignedRow(row))
                {
                    unassignedSum += cellValue;
                }
                else
                {
                    assignedSum += cellValue;
                }
            }

            return (assignedSum, unassignedSum);
        }

        private static bool IsUnassignedRow(DataGridViewRow row) =>
            row.DefaultCellStyleOrNull != null
            && row.DefaultCellStyleOrNull.HasBackColor
            && row.DefaultCellStyleOrNull.BackColor == Colors.Red;

        private void ExportStats()
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Save Excel File",
                FileName = "Stats.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (saveDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Results");

                worksheet.Cell(1, 1).Value = "Sample";
                worksheet.Cell(2, 1).Value = "Assigned";
                worksheet.Cell(3, 1).Value = "Unassigned";
                worksheet.Cell(4, 1).Value = "Total";

                int excelColumnIndex = 2;

                foreach (var item in _sampleListBox.Items)
                {
                    string columnName = item.Text;

                    var column = _dataGridView.Columns
                        .FirstOrDefault(c => string.Equals(c.HeaderText, columnName, StringComparison.OrdinalIgnoreCase));

                    if (column == null)
                    {
                        continue;
                    }

                    var (assignedSum, unassignedSum) = SumColumn(column.Index);

                    worksheet.Cell(1, excelColumnIndex).Value = columnName;
                    worksheet.Cell(2, excelColumnIndex).Value = Math.Round(assignedSum, 2);
                    worksheet.Cell(3, excelColumnIndex).Value = Math.Round(unassignedSum, 2);
                    worksheet.Cell(4, excelColumnIndex).Value = Math.Round(assignedSum + unassignedSum, 2);

                    excelColumnIndex++;
                }

                workbook.SaveAs(UiHelpers.EnsureExtension(saveDialog.FileName, ".xlsx"));

                MessageBox.Show("Excel file saved successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the Excel file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Clean and normalize

        private void CleanAndNormalize()
        {
            _sampleListBox.Enabled = false;
            _exportStatsButton.Enabled = false;

            // Normalising writes every cell of the sheet. Without batching, each write would
            // rebuild the whole bound row list, which turns the pass quadratic on large datasets.
            _dataGridView.BeginUpdate();
            try
            {
                NormalizeSamples();
            }
            finally
            {
                _dataGridView.EndUpdate();
            }
        }

        private void NormalizeSamples()
        {
            // Step 1: delete rows marked red, unless the override checkbox keeps them.
            if (_overrideClassificationCheckBox.Checked != true)
            {
                _statsLabel.Text = "Samples Normalized over 100%";
                for (int i = _dataGridView.Rows.Count - 1; i >= 0; i--)
                {
                    var row = _dataGridView.Rows[i];
                    string speciesName = row.Cells[0].Value?.ToString();

                    if (IsUnassignedRow(row) &&
                        (string.IsNullOrEmpty(speciesName) || !_overrideManager.HasOverride(speciesName)))
                    {
                        _dataGridView.Rows.RemoveAt(i);
                    }
                }
            }
            else
            {
                _statsLabel.Text = "Normalized (unassigned included)";
            }

            // Step 2: normalize each sample column to 100%.
            for (int colIndex = 1; colIndex < _dataGridView.Columns.Count; colIndex++)
            {
                double columnSum = 0;

                foreach (var row in _dataGridView.Rows)
                {
                    if (double.TryParse(row.Cells[colIndex].Value?.ToString(), out double value))
                    {
                        columnSum += value;
                    }
                    else
                    {
                        row.Cells[colIndex].Value = 0; // Treat empty cells as 0
                    }
                }

                if (columnSum > 0)
                {
                    NormalizeColumn(colIndex, columnSum);
                }
            }
        }

        /// <summary>
        /// Rewrites one sample column as percentages rounded to two decimals. Rounding each value
        /// on its own would leave the column summing to something like 99.99, so the rounding
        /// error is put back on the largest value: the column the user reads adds up to 100.
        /// </summary>
        private void NormalizeColumn(int columnIndex, double columnSum)
        {
            var values = new List<(DataGridViewRow Row, double Percent)>();

            foreach (var row in _dataGridView.Rows)
            {
                if (double.TryParse(row.Cells[columnIndex].Value?.ToString(), out double value))
                {
                    values.Add((row, Math.Round(value / columnSum * 100, NormalizedDecimals)));
                }
            }

            if (values.Count == 0)
            {
                return;
            }

            int largest = 0;
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i].Percent > values[largest].Percent)
                {
                    largest = i;
                }
            }

            double residual = 100 - values.Sum(v => v.Percent);
            values[largest] = (values[largest].Row,
                Math.Round(values[largest].Percent + residual, NormalizedDecimals));

            foreach (var (row, percent) in values)
            {
                row.Cells[columnIndex].Value = percent.ToString("F" + NormalizedDecimals,
                    CultureInfo.CurrentCulture);
            }
        }

        #endregion

        #region Indices launch

        private void LaunchAdvancedIndices(Action<AdvancedIndicesForm> configureForm = null)
        {
            if (_dataGridView.Rows.Count == 0)
            {
                MessageBox.Show("Please load sample data first.", "No Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Column 0 is Species, so at least one sample column is needed.
            if (_dataGridView.Columns.Count <= 1)
            {
                MessageBox.Show("Please add at least one sample column to calculate indices.", "No Samples",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool comparisonDone = _cleanNormalizeButton.Enabled;
            bool fambiAvailable = comparisonDone && _databank != null;
            bool foramIndexAvailable = SpecializedDatabankLoader.CheckFoRAMDatabankAvailability();

            List<string> selectedIndices;
            using (var selectionDialog = new IndexSelectionDialog(fambiAvailable, foramIndexAvailable))
            {
                if (selectionDialog.ShowModal(this) != true)
                {
                    return;
                }
                selectedIndices = selectionDialog.SelectedIndices;
            }

            if (selectedIndices == null || selectedIndices.Count == 0)
            {
                MessageBox.Show("No indices selected.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sourceData = BuildSourceDataTable();

            // Ask for mud percentages only when TSI-Med was selected and its databank exists.
            Dictionary<string, double> mudPercentages = null;
            var (_, tsiAvail) = SpecializedDatabankLoader.CheckDatabanksAvailability();

            if (selectedIndices.Contains("TSI-Med") && tsiAvail)
            {
                var sampleNames = new List<string>();
                for (int i = 1; i < sourceData.Columns.Count; i++)
                {
                    sampleNames.Add(sourceData.Columns[i].ColumnName);
                }

                if (_extractedMudPercentages != null && _extractedMudPercentages.Count > 0)
                {
                    // Mud values were auto-detected: show the form pre-populated for confirmation.
                    using var mudDialog = new MudPercentageDialog(sampleNames, _extractedMudPercentages);
                    if (mudDialog.ShowModal(this) == true)
                    {
                        mudPercentages = mudDialog.MudPercentages;
                    }
                }
                else
                {
                    var result = MessageBox.Show(
                        "TSI-Med index requires sediment grain-size data (% mud <63 µm) for accurate calculation.\n\n" +
                        "Do you want to provide mud percentages for each sample?\n\n" +
                        "Click 'Yes' to enter values, 'No' to use default (50%).",
                        "TSI-Med: Sediment Data",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        using var mudDialog = new MudPercentageDialog(sampleNames);
                        if (mudDialog.ShowModal(this) == true)
                        {
                            mudPercentages = mudDialog.MudPercentages;
                        }
                    }
                }
            }

            var advancedForm = new AdvancedIndicesForm();
            advancedForm.ConfigureIndexSettings(
                _currentFAMBIThreshold, _currentTSIReference,
                _currentTSIThreshold, _currentExpHbcThreshold);

            // Pass the databank only when a comparison was performed, otherwise F-AMBI is skipped.
            advancedForm.LoadResults(
                sourceData,
                comparisonDone ? _databank : null,
                mudPercentages,
                selectedIndices,
                _overrideManager.GetAllOverrides());

            configureForm?.Invoke(advancedForm);
            advancedForm.Show();
        }

        private DataTable BuildSourceDataTable()
        {
            var sourceData = new DataTable();

            foreach (var col in _dataGridView.Columns)
            {
                sourceData.Columns.Add(col.HeaderText, typeof(string));
            }

            foreach (var row in GetRowsForCalculations())
            {
                var newRow = sourceData.NewRow();
                for (int i = 0; i < _dataGridView.Columns.Count; i++)
                {
                    newRow[i] = row.Cells[i].Value?.ToString() ?? string.Empty;
                }
                sourceData.Rows.Add(newRow);
            }

            return sourceData;
        }

        /// <summary>
        /// Returns the rows to use for any index calculation, honouring the override checkbox.
        /// </summary>
        private IEnumerable<DataGridViewRow> GetRowsForCalculations()
        {
            return _dataGridView.Rows
                .Where(row => _overrideClassificationCheckBox.Checked == true || !IsUnassignedRow(row));
        }

        #endregion

        #region Overrides context menu

        private void DataGridView_CellRightClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            var contextMenu = new ContextMenu();

            var overrideItem = new ButtonMenuItem { Text = "Override Ecological Group..." };
            overrideItem.Click += (s, ev) => ShowOverrideDialog(e.RowIndex);
            contextMenu.Items.Add(overrideItem);

            string speciesName = _dataGridView.Rows[e.RowIndex].Cells[0].Value?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(speciesName) && _overrideManager.HasOverride(speciesName))
            {
                var clearItem = new ButtonMenuItem { Text = "Remove Override" };
                clearItem.Click += async (s, ev) =>
                {
                    _overrideManager.RemoveOverride(speciesName);
                    await CompareDatabankAsync();
                };
                contextMenu.Items.Add(clearItem);
            }

            contextMenu.Items.Add(new SeparatorMenuItem());

            var clearAllItem = new ButtonMenuItem { Text = "Reset All Overrides" };
            clearAllItem.Click += async (s, ev) =>
            {
                if (MessageBox.Show(
                        "Are you sure you want to remove ALL manual overrides?\nThis action cannot be undone.",
                        "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _overrideManager.ClearAll();
                    await CompareDatabankAsync();
                }
            };
            contextMenu.Items.Add(clearAllItem);

            contextMenu.Show(_dataGridView);
        }

        private async void ShowOverrideDialog(int rowIndex)
        {
            string speciesName = _dataGridView.Rows[rowIndex].Cells[0].Value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(speciesName))
            {
                return;
            }

            int currentGroup = _overrideManager.GetOverride(speciesName) ?? 0;
            using var dialog = new OverrideDialog(speciesName, currentGroup);
            if (dialog.ShowModal(this) == true)
            {
                _overrideManager.AddOverride(speciesName, dialog.SelectedGroup);
                await CompareDatabankAsync();
            }
        }

        private void ExportOverrides()
        {
            var allOverrides = _overrideManager.GetAllOverrides();
            if (allOverrides.Count == 0)
            {
                MessageBox.Show("There are no manual ecological-group overrides to export.",
                    "No Overrides", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Ecological-Group Overrides",
                FileName = "eco_overrides.json",
                Filters = { Filters.Json }
            };

            if (saveDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                string path = UiHelpers.EnsureExtension(saveDialog.FileName, ".json");
                _overrideManager.ExportToFile(path);
                MessageBox.Show($"Exported {allOverrides.Count} override(s) to:\n{path}\n\n" +
                    "This file can be shared with collaborators or committed to version control.",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export overrides: {ex.Message}", "Export Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ImportOverrides()
        {
            using var openDialog = new OpenFileDialog
            {
                Title = "Import Ecological-Group Overrides",
                Filters = { Filters.Json }
            };

            if (openDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            bool merge = true;
            if (_overrideManager.GetAllOverrides().Count > 0)
            {
                var result = MessageBox.Show(
                    "Merge imported overrides with the existing ones (Yes),\n" +
                    "or replace all existing overrides entirely (No)?",
                    "Import Overrides", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    return;
                }

                merge = result == DialogResult.Yes;
            }

            try
            {
                int count = _overrideManager.ImportFromFile(openDialog.FileName, merge);
                MessageBox.Show($"Imported {count} override(s) from:\n{openDialog.FileName}\n\n" +
                    "Re-run Compare to apply them to the current dataset.",
                    "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (_compareDatabankButton.Enabled)
                {
                    await CompareDatabankAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import overrides: {ex.Message}", "Import Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Tools

        private void ShowFsiDatabankManager()
        {
            using var managerForm = new FSIDatabankManagerForm();
            managerForm.DatabankUpdated += (s, ev) =>
            {
                // Refresh whatever depends on the FSI databank; currently only its status line.
                SpecializedDatabankLoader.GetFSIDatabankInfo();
            };
            managerForm.ShowModal(this);
        }

        private void ShowForamAmbiDatabankManager()
        {
            string currentDatabank = SelectedDatabankName() ?? "Jorissen";
            using var managerForm = new ForamAMBIDatabankManagerForm(currentDatabank);
            managerForm.DatabankUpdated += (s, ev) =>
                MessageBox.Show("Foram-AMBI databank updated. Re-run comparison for changes to take effect.",
                    "Databank Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            managerForm.ShowModal(this);
        }

        private void ShowGeographicAreasDatabank()
        {
            using var geographicForm = new GeographicAreasDatabankForm();
            geographicForm.ShowModal(this);
        }

        private void ShowUserCustomListsManager()
        {
            using var customListsForm = new UserCustomListsManagerForm();
            customListsForm.ShowModal(this);
        }

        private void ShowIndexSettings()
        {
            using var settingsDialog = new IndexSettingsDialog(
                _currentFAMBIThreshold, _currentTSIReference,
                _currentTSIThreshold, _currentExpHbcThreshold,
                _useJorissenList, _calculateEQR, _fsiRefValue, _expHbcRefValue,
                _useWormsVerification);

            if (settingsDialog.ShowModal(this) != true)
            {
                return;
            }

            _currentFAMBIThreshold = settingsDialog.FAMBIThreshold;
            _currentTSIReference = settingsDialog.TSIReference;
            _currentTSIThreshold = settingsDialog.TSIThreshold;
            _currentExpHbcThreshold = settingsDialog.ExpHbcThreshold;
            _useJorissenList = settingsDialog.UseJorissenTolerantList;
            _calculateEQR = settingsDialog.CalculateEQR;
            _fsiRefValue = settingsDialog.FSIReferenceValue;
            _expHbcRefValue = settingsDialog.ExpHbcReferenceValue;
            _useWormsVerification = settingsDialog.UseWormsVerification;

            MessageBox.Show("Index settings updated. New settings will be applied to next calculation.",
                "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAbout()
        {
            using var aboutDialog = new AboutBox();
            aboutDialog.ShowModal(this);
        }

        #endregion
    }
}
