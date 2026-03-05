//MIT License
// AdvancedIndicesForm.cs - Form for calculating and visualizing advanced biotic indices

using ClosedXML.Excel;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
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
    public partial class AdvancedIndicesForm : Form
    {
        private DataTable resultsTable;
        private List<IndicesResult> allResults;
        // Specialized databanks for FSI, TSI-Med, and FoRAM Index
        private Dictionary<string, string> fsiDatabank;
        private HashSet<string> tsiMedDatabank;
        private Dictionary<string, string> foramDatabank;
        private Dictionary<string, double> mudPercentages;
        private bool fsiDatabankAvailable;
        private bool tsiMedDatabankAvailable;
        private bool foramDatabankAvailable;
        private bool fambiAvailable;
        private bool calculateFoRAMIndex = false;
        private FAMBIThresholdType selectedFAMBIThreshold = FAMBIThresholdType.Borja2003;
        private TSIReferenceType selectedTSIReference = TSIReferenceType.Barras2014_150um;
        private TSIThresholdType selectedTSIThreshold = TSIThresholdType.Parent2021;
        private ExpHbcThresholdType selectedExpHbcThreshold = ExpHbcThresholdType.OBrien2021_Norwegian63um;
        private DataGridView dataGridIndices;
        private MenuStrip menuStrip;
        private TabControl tabControl;
        private GroupBox plotOptionsGroup;
        private CheckedListBox indexSelectionList;
        private CheckedListBox sampleSelectionList;
        private ComboBox plotTypeCombo;
        private Button generatePlotButton;
        private Button exportAllButton;
        private Label infoLabel;
        private ComboBox dpiCombo;
        private ComboBox fontCombo;
        private ComboBox colorSchemeCombo;
        private CheckBox gridCheckbox;
        private Panel plotHostPanel;

        // Available indices for plotting
        private readonly string[] availableIndices = new string[]
        {
            "exp(H'bc)", "H'log2", "H'ln", "FSI", "TSI-Med", "NQIf", "FIEI", "Foram-AMBI", "Foram-M-AMBI", "BENTIX", "BQI", "FoRAM Index",
            "Species Richness (S)", "Total Abundance (N)", "Simpson (1-D)", "Pielou's J", "ES100",
            "Eco1 %", "Eco2 %", "Eco3 %", "Eco4 %", "Eco5 %",
            "FoRAM Symbiont %", "FoRAM Stress-Tolerant %", "FoRAM Heterotrophic %"
        };

        public AdvancedIndicesForm()
        {
            InitializeComponent();
            allResults = new List<IndicesResult>();
        }

        public void ConfigureIndexSettings(
            FAMBIThresholdType fambiThreshold,
            TSIReferenceType tsiReference,
            TSIThresholdType tsiThreshold,
            ExpHbcThresholdType expHbcThreshold)
        {
            selectedFAMBIThreshold = fambiThreshold;
            selectedTSIReference = tsiReference;
            selectedTSIThreshold = tsiThreshold;
            selectedExpHbcThreshold = expHbcThreshold;
        }

        private void InitializeComponent()
        {
            this.Text = "Advanced Biotic Indices Calculator";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            // Create menu strip
            menuStrip = new MenuStrip();
            menuStrip.RenderMode = ToolStripRenderMode.System;

            var fileMenu = new ToolStripMenuItem("File");
            var saveDataItem = new ToolStripMenuItem("Save Results to Excel", null, SaveResults_Click);
            var exportPlotsItem = new ToolStripMenuItem("Export All Plots", null, ExportAllPlots_Click);
            var closeItem = new ToolStripMenuItem("Close", null, (s, e) => this.Close());
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { saveDataItem, exportPlotsItem, new ToolStripSeparator(), closeItem });

            var plotMenu = new ToolStripMenuItem("Plots");
            var barPlotItem = new ToolStripMenuItem("Bar Chart", null, (s, e) => GeneratePlot("Bar"));
            var linePlotItem = new ToolStripMenuItem("Line Plot", null, (s, e) => GeneratePlot("Line"));
            var boxPlotItem = new ToolStripMenuItem("Box Plot", null, (s, e) => GeneratePlot("Box Plot"));
            var scatterPlotItem = new ToolStripMenuItem("Scatter Plot", null, (s, e) => GeneratePlot("Scatter Plot"));
            var heatmapPlotItem = new ToolStripMenuItem("Heatmap", null, (s, e) => GeneratePlot("Heatmap"));
            var ecoGroupsPlotItem = new ToolStripMenuItem("Eco Groups Distribution", null, (s, e) => GenerateEcoGroupsPlot());
            var compositePlotItem = new ToolStripMenuItem("Composite Panel", null, (s, e) => GenerateCompositePlot());
            var eqsPlotItem = new ToolStripMenuItem("EQS Classification", null, (s, e) => GenerateEQSPlot());
            var eqsAgreementItem = new ToolStripMenuItem("EQS Agreement Analysis...", null, (s, e) => ShowEQSAgreementAnalysis());
            plotMenu.DropDownItems.AddRange(new ToolStripItem[] { barPlotItem, linePlotItem, boxPlotItem, scatterPlotItem, heatmapPlotItem,
                new ToolStripSeparator(), ecoGroupsPlotItem, compositePlotItem, eqsPlotItem, eqsAgreementItem });

            // var helpMenu = new ToolStripMenuItem("Help");
            // // var aboutIndicesItem = new ToolStripMenuItem("About Indices", null, ShowIndicesInfo); // Disabled - not in use
            // var referencesItem = new ToolStripMenuItem("References", null, ShowReferences);
            // helpMenu.DropDownItems.AddRange(new ToolStripItem[] { referencesItem });

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, plotMenu });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Create tab control
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;

            // Tab 1: Data Grid
            var dataTab = new TabPage("Results Data");
            dataGridIndices = new DataGridView();
            dataGridIndices.Dock = DockStyle.Fill;
            dataGridIndices.AllowUserToAddRows = false;
            dataGridIndices.AllowUserToDeleteRows = false;
            dataGridIndices.ReadOnly = true;
            dataGridIndices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataTab.Controls.Add(dataGridIndices);

            // Tab 2: Plot Options
            var plotTab = new TabPage("Plot Options");
            SetupPlotOptionsTab(plotTab);

            // Tab 3: EQS Summary
            var eqsTab = new TabPage("EQS Summary");
            SetupEQSSummaryTab(eqsTab);

            tabControl.TabPages.AddRange(new TabPage[] { dataTab, plotTab, eqsTab });

            // Add controls to form
            var mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(0, menuStrip.Height, 0, 0);
            mainPanel.Controls.Add(tabControl);

            this.Controls.Add(mainPanel);
        }

        private void SetupPlotOptionsTab(TabPage tab)
        {
            var mainSplit = new SplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.Orientation = Orientation.Vertical;  // Vertical orientation = left/right split
            mainSplit.FixedPanel = FixedPanel.Panel1;  // Keep left panel fixed width
            mainSplit.IsSplitterFixed = false;  // Allow user to adjust splitter

            // Configure Panel1 (left side) directly
            mainSplit.Panel1.AutoScroll = true;

            int yPos = 10;

            // Index selection
            var indexLabel = new Label { Text = "Select Indices to Plot:", AutoSize = true, Location = new Point(10, yPos) };
            yPos += 25;

            indexSelectionList = new CheckedListBox();
            indexSelectionList.Location = new Point(10, yPos);
            indexSelectionList.Size = new Size(270, 300);
            indexSelectionList.Items.AddRange(availableIndices);
            indexSelectionList.CheckOnClick = true;
            yPos += 310;

            // Select all / none buttons
            var selectAllBtn = new Button { Text = "Select All", Location = new Point(10, yPos), Size = new Size(80, 25) };
            selectAllBtn.Click += (s, e) => { for (int i = 0; i < indexSelectionList.Items.Count; i++) indexSelectionList.SetItemChecked(i, true); };
            var selectNoneBtn = new Button { Text = "Clear All", Location = new Point(100, yPos), Size = new Size(80, 25) };
            selectNoneBtn.Click += (s, e) => { for (int i = 0; i < indexSelectionList.Items.Count; i++) indexSelectionList.SetItemChecked(i, false); };
            yPos += 35;

            // Sample selection
            var sampleLabel = new Label { Text = "Samples to plot:", AutoSize = true, Location = new Point(10, yPos) };
            yPos += 20;

            sampleSelectionList = new CheckedListBox();
            sampleSelectionList.Location = new Point(10, yPos);
            sampleSelectionList.Size = new Size(270, 120);
            sampleSelectionList.CheckOnClick = true;
            yPos += 125;

            var selectAllSamplesBtn = new Button { Text = "Select All Samples", Location = new Point(10, yPos), Size = new Size(120, 25) };
            selectAllSamplesBtn.Click += (s, e) => { for (int i = 0; i < sampleSelectionList.Items.Count; i++) sampleSelectionList.SetItemChecked(i, true); };
            var clearSamplesBtn = new Button { Text = "Clear Samples", Location = new Point(140, yPos), Size = new Size(120, 25) };
            clearSamplesBtn.Click += (s, e) => { for (int i = 0; i < sampleSelectionList.Items.Count; i++) sampleSelectionList.SetItemChecked(i, false); };
            yPos += 35;

            // Plot type selection
            var plotTypeLabel = new Label { Text = "Plot Type:", AutoSize = true, Location = new Point(10, yPos) };
            yPos += 20;

            plotTypeCombo = new ComboBox();
            plotTypeCombo.Location = new Point(10, yPos);
            plotTypeCombo.Size = new Size(270, 25);
            plotTypeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            plotTypeCombo.Items.AddRange(new string[] { "Bar Chart", "Line Plot", "Box Plot", "Scatter Plot", "Heatmap", "Grouped Bar", "Eco Groups", "Composite Panel" });
            plotTypeCombo.SelectedIndex = 0;
            yPos += 35;

            // Generate button
            generatePlotButton = new Button();
            generatePlotButton.Text = "Generate Plot";
            generatePlotButton.Location = new Point(10, yPos);
            generatePlotButton.Size = new Size(120, 35);
            generatePlotButton.BackColor = Color.FromArgb(46, 139, 87);
            generatePlotButton.ForeColor = Color.White;
            generatePlotButton.FlatStyle = FlatStyle.Flat;
            generatePlotButton.Click += GeneratePlotButton_Click;

            // Export button
            exportAllButton = new Button();
            exportAllButton.Text = "Export Plot";
            exportAllButton.Location = new Point(140, yPos);
            exportAllButton.Size = new Size(120, 35);
            exportAllButton.BackColor = Color.FromArgb(70, 130, 180);
            exportAllButton.ForeColor = Color.White;
            exportAllButton.FlatStyle = FlatStyle.Flat;
            exportAllButton.Click += ExportCurrentPlot_Click;
            yPos += 45;

            // Plot settings group
            var settingsGroup = new GroupBox();
            settingsGroup.Text = "Plot Settings (Publication Quality)";
            settingsGroup.Location = new Point(10, yPos);
            settingsGroup.Size = new Size(270, 150);

            var dpiLabel = new Label { Text = "Export DPI:", AutoSize = true, Location = new Point(10, 25) };
            dpiCombo = new ComboBox { Location = new Point(100, 22), Size = new Size(80, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            dpiCombo.Items.AddRange(new string[] { "150", "300", "600", "1200" });
            dpiCombo.SelectedIndex = 1;
            dpiCombo.Tag = "dpi";

            var fontLabel = new Label { Text = "Font Size:", AutoSize = true, Location = new Point(10, 55) };
            fontCombo = new ComboBox { Location = new Point(100, 52), Size = new Size(80, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            fontCombo.Items.AddRange(new string[] { "8", "10", "12", "14", "16" });
            fontCombo.SelectedIndex = 2;
            fontCombo.Tag = "font";

            var colorSchemeLabel = new Label { Text = "Color Scheme:", AutoSize = true, Location = new Point(10, 85) };
            colorSchemeCombo = new ComboBox { Location = new Point(100, 82), Size = new Size(155, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            colorSchemeCombo.Items.AddRange(new string[] { "Scientific (Blue-Red)", "Grayscale", "Colorblind Safe", "Nature Style", "Custom Gradient" });
            colorSchemeCombo.SelectedIndex = 0;
            colorSchemeCombo.Tag = "colorscheme";

            gridCheckbox = new CheckBox { Text = "Show Grid Lines", Location = new Point(10, 115), AutoSize = true, Checked = true };
            gridCheckbox.Tag = "grid";

            settingsGroup.Controls.AddRange(new Control[] { dpiLabel, dpiCombo, fontLabel, fontCombo, colorSchemeLabel, colorSchemeCombo, gridCheckbox });

            // Add controls directly to Panel1
            mainSplit.Panel1.Controls.AddRange(new Control[] { indexLabel, indexSelectionList, selectAllBtn, selectNoneBtn,
                sampleLabel, sampleSelectionList, selectAllSamplesBtn, clearSamplesBtn,
                plotTypeLabel, plotTypeCombo, generatePlotButton, exportAllButton, settingsGroup });

            // Configure Panel2 (right side) - Plot preview
            mainSplit.Panel2.BackColor = Color.White;
            plotHostPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Name = "plotHost" };
            mainSplit.Panel2.Controls.Add(plotHostPanel);

            tab.Controls.Add(mainSplit);

            // Set splitter distance AFTER adding to parent for proper initialization
            tab.Layout += (s, e) =>
            {
                if (mainSplit.SplitterDistance < 300)
                {
                    mainSplit.SplitterDistance = 300;
                }
            };
        }

        private void SetupEQSSummaryTab(TabPage tab)
        {
            var summaryGrid = new DataGridView();
            summaryGrid.Name = "eqsSummaryGrid";
            summaryGrid.Dock = DockStyle.Fill;
            summaryGrid.AllowUserToAddRows = false;
            summaryGrid.ReadOnly = true;
            summaryGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            tab.Controls.Add(summaryGrid);
        }

        public void LoadResults(DataTable sourceData, DataTable databank)
        {
            LoadResults(sourceData, databank, null, null);
        }

        /// <summary>
        /// Loads pre-calculated indices from an exported Excel file for plotting
        /// </summary>
        public void LoadIndicesFromExcel(DataTable indicesData)
        {
            if (indicesData == null || indicesData.Rows.Count == 0 || indicesData.Columns.Count < 2)
            {
                MessageBox.Show("Invalid or empty data file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            allResults.Clear();
            resultsTable = indicesData.Copy();
            fambiAvailable = true; // Assume indices are available for plotting

            // First column is the index name, other columns are samples
            // Create IndicesResult for each sample column
            for (int col = 1; col < indicesData.Columns.Count; col++)
            {
                var result = new IndicesResult
                {
                    SampleName = indicesData.Columns[col].ColumnName,
                    EcoGroups = new double[5]
                };

                // Parse each index row and populate the result
                foreach (DataRow row in indicesData.Rows)
                {
                    string indexName = row[0]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(indexName)) continue;

                    if (double.TryParse(row[col]?.ToString(), out double value))
                    {
                        switch (indexName)
                        {
                            case "exp(H'bc)": result.Exp_Hbc = value; break;
                            case "H'log2": result.Shannon_Log2 = value; break;
                            case "H'ln": result.Shannon_Ln = value; break;
                            case "FSI": result.FSI = value; break;
                            case "TSI-Med": result.TSI_Med = value; break;
                            case "NQIf": result.NQIf = value; break;
                            case "FIEI": result.FIEI = value; break;
                            case "Foram-AMBI":
                            case "F-AMBI": result.FAMBI = value; break;
                            case "Foram-M-AMBI": result.ForamMAMBI = value; break;
                            case "BENTIX": result.BENTIX = value; break;
                            case "BQI": result.BQI = value; break;
                            case "FoRAM Index": result.FoRAM_Index = value; break;
                            case "Species Richness (S)": result.SpeciesRichness = (int)value; break;
                            case "Total Abundance (N)": result.TotalAbundance = value; break;
                            case "Simpson (1-D)": result.Simpson_1D = value; break;
                            case "Pielou's J": result.Pielou_J = value; break;
                            case "ES100": result.ES100 = value; break;
                            case "Eco1 %": result.EcoGroups[0] = value; break;
                            case "Eco2 %": result.EcoGroups[1] = value; break;
                            case "Eco3 %": result.EcoGroups[2] = value; break;
                            case "Eco4 %": result.EcoGroups[3] = value; break;
                            case "Eco5 %": result.EcoGroups[4] = value; break;
                            case "FoRAM Symbiont %": result.FoRAM_SymbiontPercent = value; break;
                            case "FoRAM Stress-Tolerant %": result.FoRAM_StressTolerantPercent = value; break;
                            case "FoRAM Heterotrophic %": result.FoRAM_HeterotrophicPercent = value; break;
                        }
                    }
                }

                allResults.Add(result);
            }

            // Populate sample selection list
            sampleSelectionList.Items.Clear();
            foreach (var sample in allResults.Select(r => r.SampleName))
            {
                sampleSelectionList.Items.Add(sample, true);
            }

            // Populate index selection list with available indices
            indexSelectionList.Items.Clear();
            foreach (var index in availableIndices)
            {
                indexSelectionList.Items.Add(index);
            }

            // Update the data grid
            dataGridIndices.DataSource = resultsTable;

            // Switch to Plot Options tab
            tabControl.SelectedIndex = 1;

            MessageBox.Show($"Loaded {allResults.Count} samples with indices data.\nYou can now create plots from the Plot Options tab.",
                "Data Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void LoadResults(DataTable sourceData, DataTable databank, Dictionary<string, double> sampleMudPercentages, List<string> selectedIndices = null, Dictionary<string, int> overrides = null)
        {
            allResults.Clear();
            resultsTable = new DataTable();
            mudPercentages = sampleMudPercentages;
            fambiAvailable = (databank != null);

            // Load specialized databanks
            fsiDatabank = SpecializedDatabankLoader.LoadFSIDatabank();
            tsiMedDatabank = SpecializedDatabankLoader.LoadTSIMedDatabank();
            foramDatabank = SpecializedDatabankLoader.LoadFoRAMDatabank();
            (fsiDatabankAvailable, tsiMedDatabankAvailable) = SpecializedDatabankLoader.CheckDatabanksAvailability();
            foramDatabankAvailable = SpecializedDatabankLoader.CheckFoRAMDatabankAvailability();

            // Determine if FoRAM Index should be calculated
            calculateFoRAMIndex = false;
            if (selectedIndices != null)
            {
                // If explicitly selected in the dialog
                calculateFoRAMIndex = selectedIndices.Contains("FoRAM Index") && foramDatabankAvailable;
            }
            else if (foramDatabankAvailable && foramDatabank.Count > 0)
            {
                // Fallback legacy behavior: Ask user
                var result = MessageBox.Show(
                    "Do you want to calculate the FoRAM Index?\n\n" +
                    "Note: The FoRAM Index (Hallock et al. 2003; Prazeres et al. 2020) was designed " +
                    "specifically for tropical and subtropical coral reef environments.\n\n" +
                    "FI > 4: Suitable for coral growth\n" +
                    "FI 2-4: Marginal conditions\n" +
                    "FI < 2: Unsuitable for coral growth\n\n" +
                    "Click YES to calculate the FoRAM Index.\n" +
                    "Click NO to skip (recommended for temperate/Mediterranean settings).",
                    "FoRAM Index Calculation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                calculateFoRAMIndex = (result == DialogResult.Yes);
            }

            // Create columns for results table
            resultsTable.Columns.Add("Index", typeof(string));

            // Get sample names from source data (columns 1 to end)
            for (int col = 1; col < sourceData.Columns.Count; col++)
            {
                string sampleName = sourceData.Columns[col].ColumnName;
                resultsTable.Columns.Add(sampleName, typeof(double));
            }

            // Add statistical parameter columns
            resultsTable.Columns.Add("Mean", typeof(double));
            resultsTable.Columns.Add("StdDev", typeof(double));
            resultsTable.Columns.Add("Min", typeof(double));
            resultsTable.Columns.Add("Max", typeof(double));

            // Create eco-group lookup from databank
            var ecoLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (fambiAvailable)
            {
                foreach (DataRow row in databank.Rows)
                {
                    string species = row["Species"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(species) && int.TryParse(row["Ecogroup"]?.ToString(), out int eg))
                    {
                        ecoLookup[species] = eg;
                    }
                }
            }
            
            // Apply overrides if provided
            if (overrides != null)
            {
                foreach (var kvp in overrides)
                {
                    ecoLookup[kvp.Key] = kvp.Value;
                }
            }

            // Helper to check if an index is selected
            bool IsSelected(string indexName) => selectedIndices == null || selectedIndices.Contains(indexName);

            // Check if any index needs eco-groups: Foram-AMBI, Foram-M-AMBI, NQIf, FIEI, BENTIX, BQI, or FSI/TSI (if fallback)
            bool needsEcoGroups = IsSelected("Foram-AMBI") || IsSelected("Foram-M-AMBI") || IsSelected("NQIf") || IsSelected("FIEI") || IsSelected("BENTIX") || IsSelected("BQI") ||
                                  (IsSelected("FSI") && !fsiDatabankAvailable) ||
                                  (IsSelected("TSI-Med") && !tsiMedDatabankAvailable);

            // Calculate indices for each sample
            for (int col = 1; col < sourceData.Columns.Count; col++)
            {
                var result = new IndicesResult();
                result.SampleName = sourceData.Columns[col].ColumnName;
                result.FAMBI_ThresholdType = selectedFAMBIThreshold;
                result.TSIMed_ReferenceType = selectedTSIReference;
                result.TSIMed_ThresholdType = selectedTSIThreshold;
                result.ExpHbc_ThresholdType = selectedExpHbcThreshold;

                // Extract abundances
                var abundances = new List<double>();
                var speciesAbundances = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow row in sourceData.Rows)
                {
                    string species = row[0]?.ToString()?.Trim();
                    if (double.TryParse(row[col]?.ToString(), out double value) && value > 0)
                    {
                        abundances.Add(value);
                        if (!string.IsNullOrEmpty(species))
                        {
                            speciesAbundances[species] = value;
                        }
                    }
                }

                double[] abundArray = abundances.ToArray();

                // ========== DIVERSITY INDICES ==========
                if (IsSelected("H'ln")) result.Shannon_Ln = BioticIndicesCalculator.CalculateShannonLn(abundArray);
                else result.Shannon_Ln = double.NaN;

                if (IsSelected("H'log2")) result.Shannon_Log2 = BioticIndicesCalculator.CalculateShannonLog2(abundArray);
                else result.Shannon_Log2 = double.NaN;

                // H'bc is part of exp(H'bc) calculation or can be standalone
                if (IsSelected("exp(H'bc)"))
                {
                    result.Shannon_BC = BioticIndicesCalculator.CalculateShannonBiasCorrected(abundArray);
                    result.Exp_Hbc = BioticIndicesCalculator.CalculateExpHbc(abundArray);
                }
                else
                {
                    result.Shannon_BC = double.NaN;
                    result.Exp_Hbc = double.NaN;
                }

                if (IsSelected("Simpson (1-D)"))
                {
                    result.Simpson_D = BioticIndicesCalculator.CalculateSimpsonDominance(abundArray);
                    result.Simpson_1D = BioticIndicesCalculator.CalculateSimpsonDiversity(abundArray);
                }
                else result.Simpson_1D = double.NaN;

                if (IsSelected("Pielou's J")) result.Pielou_J = BioticIndicesCalculator.CalculatePielousEvenness(abundArray);
                else result.Pielou_J = double.NaN;

                // Always compute core diversity metrics used by downstream indices.
                result.ES100 = BioticIndicesCalculator.CalculateES(abundArray, 100);
                result.SpeciesRichness = BioticIndicesCalculator.CalculateSpeciesRichness(abundArray);
                result.TotalAbundance = BioticIndicesCalculator.CalculateTotalAbundance(abundArray);

                // ========== F-AMBI ECO-GROUPS ==========
                if (fambiAvailable && needsEcoGroups)
                {
                    result.EcoGroups = BioticIndicesCalculator.CalculateEcoGroupPercentages(speciesAbundances, ecoLookup);
                    if (IsSelected("Foram-AMBI") || IsSelected("Foram-M-AMBI") || IsSelected("NQIf")) // Calculate Foram-AMBI if specifically selected or needed for Foram-M-AMBI/NQI
                        result.FAMBI = BioticIndicesCalculator.CalculateFAMBI(result.EcoGroups);
                    else
                        result.FAMBI = double.NaN;
                }
                else
                {
                    result.EcoGroups = new double[] { 0, 0, 0, 0, 0 };
                    result.FAMBI = double.NaN;
                }

                // ========== FSI - CORRECTED using specialized Dimiza et al. (2016) databank ==========
                if (IsSelected("FSI"))
                {
                    if (fsiDatabankAvailable && fsiDatabank.Count > 0)
                    {
                        var (sensitive, tolerant, assigned) = SpecializedDatabankLoader.CalculateFSIPercentages(
                            speciesAbundances, fsiDatabank);
                        result.FSI = BioticIndicesCalculator.CalculateFSI(sensitive, tolerant);
                        result.FSI_AssignedPercent = assigned;
                        result.FSI_UsingSpecializedDatabank = true;
                    }
                    else if (fambiAvailable)
                    {
                        // Fallback warning: FSI cannot be calculated accurately without proper databank
                        // Using F-AMBI ecogroups as rough approximation (EG1=sensitive, EG3+4+5=tolerant)
                        double sensitive = result.EcoGroups[0];
                        double tolerant = result.EcoGroups[2] + result.EcoGroups[3] + result.EcoGroups[4];
                        result.FSI = BioticIndicesCalculator.CalculateFSI(sensitive, tolerant);
                        result.FSI_UsingSpecializedDatabank = false;
                        result.FSI_AssignedPercent = 0; // Unknown
                    }
                    else
                    {
                        result.FSI = double.NaN;
                    }
                }
                else
                {
                    result.FSI = double.NaN;
                }

                // ========== TSI-Med - CORRECTED with mud% and specialized Barras et al. (2014) databank ==========
                double mudPct = mudPercentages?.GetValueOrDefault(result.SampleName, 50.0) ?? 50.0;
                result.MudPercent = mudPct;

                if (IsSelected("TSI-Med"))
                {
                    result.TSIMed_ReferenceValue = BioticIndicesCalculator.CalculateTSIReference(mudPct, selectedTSIReference);

                    if (tsiMedDatabankAvailable && tsiMedDatabank.Count > 0)
                    {
                        var (tolerantPct, _) = SpecializedDatabankLoader.CalculateTSIMedPercentages(
                            speciesAbundances, tsiMedDatabank);
                        result.TSI_Med = BioticIndicesCalculator.CalculateTSIMed(tolerantPct, mudPct, selectedTSIReference);
                        result.TSIMed_TolerantPercent = tolerantPct;
                        result.TSIMed_UsingSpecializedDatabank = true;
                    }
                    else if (fambiAvailable)
                    {
                        // Fallback: use F-AMBI tolerant groups as approximation
                        double tolerant = result.EcoGroups[2] + result.EcoGroups[3] + result.EcoGroups[4];
                        result.TSI_Med = BioticIndicesCalculator.CalculateTSIMed(tolerant, mudPct, selectedTSIReference);
                        result.TSIMed_UsingSpecializedDatabank = false;
                    }
                    else
                    {
                        result.TSI_Med = double.NaN;
                    }
                }
                else
                {
                    result.TSI_Med = double.NaN;
                }

                // ========== NQIf - CORRECT (uses F-AMBI) ==========
                if (IsSelected("NQIf"))
                {
                    if (fambiAvailable && !double.IsNaN(result.FAMBI))
                    {
                        result.NQIf = BioticIndicesCalculator.CalculateNQIf(result.FAMBI, result.ES100);
                    }
                    else
                    {
                        result.NQIf = double.NaN;
                    }
                }
                else
                {
                    result.NQIf = double.NaN;
                }

                // ========== Foram-M-AMBI - Multivariate AMBI for Foraminifera ==========
                // Reference: Muxika et al. (2007) adapted for foraminifera
                if (IsSelected("Foram-M-AMBI"))
                {
                    if (fambiAvailable && !double.IsNaN(result.FAMBI))
                    {
                        // Ensure we have Shannon and Richness (calculate if not already done)
                        double shannonLn = !double.IsNaN(result.Shannon_Ln) ? result.Shannon_Ln : BioticIndicesCalculator.CalculateShannonLn(abundArray);
                        int richness = result.SpeciesRichness > 0 ? result.SpeciesRichness : BioticIndicesCalculator.CalculateSpeciesRichness(abundArray);

                        // Calculate Foram-M-AMBI using default reference conditions
                        result.ForamMAMBI = BioticIndicesCalculator.CalculateForamMAMBI(result.FAMBI, shannonLn, richness);
                        
                        // Also calculate Euclidean distance version for comparison
                        result.ForamMAMBI_Euclidean = BioticIndicesCalculator.CalculateForamMAMBI_Euclidean(result.FAMBI, shannonLn, richness, null);
                        
                        // Store normalized component values for analysis
                        var refCond = ForamMAMBIReferenceConditions.GetDefaultCoastal();
                        result.ForamMAMBI_NormAMBI = Math.Max(0, Math.Min(1, 
                            (refCond.Bad_FAMBI - result.FAMBI) / (refCond.Bad_FAMBI - refCond.High_FAMBI)));
                        result.ForamMAMBI_NormH = Math.Max(0, Math.Min(1,
                            (shannonLn - refCond.Bad_Shannon) / (refCond.High_Shannon - refCond.Bad_Shannon)));
                        result.ForamMAMBI_NormS = Math.Max(0, Math.Min(1,
                            (richness - refCond.Bad_Richness) / (refCond.High_Richness - refCond.Bad_Richness)));
                    }
                    else
                    {
                        result.ForamMAMBI = double.NaN;
                        result.ForamMAMBI_Euclidean = double.NaN;
                        result.ForamMAMBI_NormAMBI = double.NaN;
                        result.ForamMAMBI_NormH = double.NaN;
                        result.ForamMAMBI_NormS = double.NaN;
                    }
                }
                else
                {
                    result.ForamMAMBI = double.NaN;
                    result.ForamMAMBI_Euclidean = double.NaN;
                }

                // ========== FIEI - Acceptable approximation using F-AMBI ecogroups ==========
                if (IsSelected("FIEI"))
                {
                    if (fambiAvailable)
                    {
                        double opportunistic = result.EcoGroups[3] + result.EcoGroups[4]; // EG4+EG5
                        double tolerantFIEI = result.EcoGroups[2] + result.EcoGroups[3] + result.EcoGroups[4]; // EG3+4+5
                        result.FIEI = BioticIndicesCalculator.CalculateFIEI(tolerantFIEI, opportunistic, 100);
                    }
                    else
                    {
                        result.FIEI = double.NaN;
                    }
                }
                else
                {
                    result.FIEI = double.NaN;
                }

                // ========== BENTIX - Biotic index using simplified ecological groups ==========
                // Reference: Simboura N. & Zenetos A. (2002) Mediterranean Marine Science, 3/2: 77-111
                // BENTIX = (6 x %GS + 2 x %GT) / 100
                // GS = sensitive group (EG1 + EG2), GT = tolerant group (EG3 + EG4 + EG5)
                if (IsSelected("BENTIX"))
                {
                    if (fambiAvailable)
                    {
                        result.BENTIX = BioticIndicesCalculator.CalculateBENTIX(result.EcoGroups);
                    }
                    else
                    {
                        result.BENTIX = double.NaN;
                    }
                }
                else
                {
                    result.BENTIX = double.NaN;
                }

                // ========== BQI - Benthic Quality Index adapted for foraminifera ==========
                // Reference: Rosenberg R. et al. (2004) Marine Pollution Bulletin 49:728-739
                // BQI = log10(S+1) x mean_sensitivity (using eco-groups as sensitivity proxies)
                if (IsSelected("BQI"))
                {
                    if (fambiAvailable)
                    {
                        result.BQI = BioticIndicesCalculator.CalculateBQI(result.SpeciesRichness, result.EcoGroups, result.TotalAbundance);
                    }
                    else
                    {
                        result.BQI = double.NaN;
                    }
                }
                else
                {
                    result.BQI = double.NaN;
                }

                // ========== FoRAM Index - Calculate if user requested ==========
                // FoRAM Index is designed for tropical coral reef environments
                // Reference: Hallock et al. (2003), Prazeres et al. (2020)
                if (calculateFoRAMIndex && foramDatabankAvailable && foramDatabank.Count > 0)
                {
                    var (symbiont, stressTolerant, heterotrophic, assigned) =
                        SpecializedDatabankLoader.CalculateFoRAMPercentages(speciesAbundances, foramDatabank);
                    result.FoRAM_Index = BioticIndicesCalculator.CalculateFoRAMIndex(symbiont, stressTolerant, heterotrophic);
                    result.FoRAM_ApplicableEnvironment = true;
                    result.FoRAM_SymbiontPercent = symbiont;
                    result.FoRAM_StressTolerantPercent = stressTolerant;
                    result.FoRAM_HeterotrophicPercent = heterotrophic;
                    result.FoRAM_AssignedPercent = assigned;
                }
                else
                {
                    result.FoRAM_Index = double.NaN;
                    result.FoRAM_ApplicableEnvironment = false;
                    result.FoRAM_SymbiontPercent = 0;
                    result.FoRAM_StressTolerantPercent = 0;
                    result.FoRAM_HeterotrophicPercent = 0;
                    result.FoRAM_AssignedPercent = 0;
                }

                allResults.Add(result);
            }

            // Populate sample selection list
            sampleSelectionList.Items.Clear();
            foreach (var sample in allResults.Select(r => r.SampleName))
            {
                sampleSelectionList.Items.Add(sample, true);
            }

            // Populate results table
            PopulateResultsTable(selectedIndices);

            // Display in grid
            dataGridIndices.DataSource = resultsTable;
            FormatDataGrid();

            // Update EQS summary
            UpdateEQSSummary();

            // Update plot selection list based on calculated indices
            UpdatePlotSelectionList(selectedIndices, fambiAvailable && needsEcoGroups, calculateFoRAMIndex && foramDatabankAvailable);

            // Show databank status warning if needed
            ShowDatabankWarnings();
        }

        private void UpdatePlotSelectionList(List<string> selectedIndices, bool ecoGroupsCalculated, bool foramCalculated)
        {
            indexSelectionList.Items.Clear();

            bool IsSelected(string indexName) => selectedIndices == null || selectedIndices.Contains(indexName);

            foreach (var index in availableIndices)
            {
                bool shouldAdd = false;

                if (index.StartsWith("Eco") && index.EndsWith("%"))
                {
                    if (ecoGroupsCalculated) shouldAdd = true;
                }
                else if (index.StartsWith("FoRAM") && index.Contains("%"))
                {
                    if (foramCalculated) shouldAdd = true;
                }
                else if (index == "FoRAM Index")
                {
                    if (foramCalculated && IsSelected(index)) shouldAdd = true;
                }
                else
                {
                    if (IsSelected(index))
                    {
                        if ((index == "Foram-AMBI" || index == "BENTIX" || index == "BQI") && !fambiAvailable) shouldAdd = false;
                        else shouldAdd = true;
                    }
                }

                if (shouldAdd)
                {
                    indexSelectionList.Items.Add(index);
                }
            }
        }

        public void PreselectIndices(IEnumerable<string> indices)
        {
            if (indices == null) return;

            for (int i = 0; i < indexSelectionList.Items.Count; i++)
            {
                indexSelectionList.SetItemChecked(i, false);
            }

            foreach (var index in indices)
            {
                int listIndex = indexSelectionList.Items.IndexOf(index);
                if (listIndex >= 0)
                {
                    indexSelectionList.SetItemChecked(listIndex, true);
                }
            }
        }

        public void FocusPlotTab(string plotType = "Bar Chart")
        {
            if (tabControl.TabPages.Count > 1)
            {
                tabControl.SelectedIndex = 1;
            }

            if (!string.IsNullOrEmpty(plotType))
            {
                int index = plotTypeCombo.Items.IndexOf(plotType);
                if (index >= 0)
                {
                    plotTypeCombo.SelectedIndex = index;
                }
            }
        }

        private List<IndicesResult> GetSelectedResults()
        {
            var selectedSamples = sampleSelectionList.CheckedItems.Cast<string>().ToList();
            if (selectedSamples.Count == 0)
            {
                MessageBox.Show("Please select at least one sample to plot.", "No Samples", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return new List<IndicesResult>();
            }

            return allResults.Where(r => selectedSamples.Contains(r.SampleName)).ToList();
        }

        public void GenerateSelectedPlot(string plotType)
        {
            if (string.IsNullOrEmpty(plotType))
            {
                plotType = plotTypeCombo.SelectedItem?.ToString() ?? "Bar Chart";
            }

            GeneratePlot(plotType);
        }

        public void ShowCompositePanel()
        {
            GenerateCompositePlot();
        }

        private void PopulateResultsTable(List<string> selectedIndices = null)
        {
            resultsTable.Rows.Clear();

            // Helper to check if an index is selected
            bool IsSelected(string indexName) => selectedIndices == null || selectedIndices.Contains(indexName);

            // Add rows for each index ONLY if selected
            if (IsSelected("exp(H'bc)")) AddResultRow("exp(H'bc)", allResults.Select(r => r.Exp_Hbc).ToArray());
            if (IsSelected("H'log2")) AddResultRow("H'log2", allResults.Select(r => r.Shannon_Log2).ToArray());
            if (IsSelected("H'ln")) AddResultRow("H'ln", allResults.Select(r => r.Shannon_Ln).ToArray());
            if (IsSelected("exp(H'bc)")) AddResultRow("H'bc", allResults.Select(r => r.Shannon_BC).ToArray());

            if (IsSelected("FSI"))
            {
                AddResultRow("FSI", allResults.Select(r => r.FSI).ToArray());
                AddResultRow("FSI Assigned %", allResults.Select(r => r.FSI_AssignedPercent).ToArray());
            }

            if (IsSelected("TSI-Med"))
            {
                AddResultRow("TSI-Med", allResults.Select(r => r.TSI_Med).ToArray());
                AddResultRow("Mud %", allResults.Select(r => r.MudPercent).ToArray());
            }

            if (IsSelected("NQIf")) AddResultRow("NQIf", allResults.Select(r => r.NQIf).ToArray());

            if (fambiAvailable)
            {
                if (IsSelected("FIEI")) AddResultRow("FIEI", allResults.Select(r => r.FIEI).ToArray());
                if (IsSelected("Foram-AMBI")) AddResultRow("Foram-AMBI", allResults.Select(r => r.FAMBI).ToArray());

                // Foram-M-AMBI (multivariate index based on Foram-AMBI, Shannon H', and Species Richness)
                if (IsSelected("Foram-M-AMBI"))
                {
                    AddResultRow("Foram-M-AMBI", allResults.Select(r => r.ForamMAMBI).ToArray());
                    AddResultRow("Foram-M-AMBI (Euclidean)", allResults.Select(r => r.ForamMAMBI_Euclidean).ToArray());
                    AddResultRow("M-AMBI Norm. AMBI", allResults.Select(r => r.ForamMAMBI_NormAMBI).ToArray());
                    AddResultRow("M-AMBI Norm. H'", allResults.Select(r => r.ForamMAMBI_NormH).ToArray());
                    AddResultRow("M-AMBI Norm. S", allResults.Select(r => r.ForamMAMBI_NormS).ToArray());
                }

                // BENTIX (biotic index using simplified ecological groups)
                if (IsSelected("BENTIX")) AddResultRow("BENTIX", allResults.Select(r => r.BENTIX).ToArray());

                // BQI (Benthic Quality Index adapted for foraminifera)
                if (IsSelected("BQI")) AddResultRow("BQI", allResults.Select(r => r.BQI).ToArray());
            }

            // FoRAM Index - only show if calculated
            if (calculateFoRAMIndex && foramDatabankAvailable)
            {
                AddResultRow("FoRAM Index", allResults.Select(r => r.FoRAM_Index).ToArray());
                AddResultRow("FoRAM Assigned %", allResults.Select(r => r.FoRAM_AssignedPercent).ToArray());
                AddResultRow("FoRAM Symbiont %", allResults.Select(r => r.FoRAM_SymbiontPercent).ToArray());
                AddResultRow("FoRAM Stress-Tolerant %", allResults.Select(r => r.FoRAM_StressTolerantPercent).ToArray());
                AddResultRow("FoRAM Heterotrophic %", allResults.Select(r => r.FoRAM_HeterotrophicPercent).ToArray());
            }

            if (IsSelected("Species Richness (S)")) AddResultRow("Species Richness (S)", allResults.Select(r => (double)r.SpeciesRichness).ToArray());
            if (IsSelected("Total Abundance (N)")) AddResultRow("Total Abundance (N)", allResults.Select(r => r.TotalAbundance).ToArray());
            if (IsSelected("Simpson (1-D)")) AddResultRow("Simpson (1-D)", allResults.Select(r => r.Simpson_1D).ToArray());
            if (IsSelected("Pielou's J")) AddResultRow("Pielou's J", allResults.Select(r => r.Pielou_J).ToArray());
            if (IsSelected("ES100")) AddResultRow("ES100", allResults.Select(r => r.ES100).ToArray());

            if (fambiAvailable)
            {
                // EcoGroups are always available in calculation if needed, but maybe not shown unless F-AMBI is present?
                // Or maybe we should always show them if F-AMBI is available?
                // The user said "DO NOT ASK FOR CALCULATING ECOGROUPS", implying they are auxiliary.
                // Let's show them if F-AMBI or any eco-based index is selected, or if user didn't specify.
                bool showEcoGroups = IsSelected("Foram-AMBI") || IsSelected("FIEI") || IsSelected("NQIf") || IsSelected("FSI") || IsSelected("TSI-Med") || IsSelected("BENTIX") || IsSelected("BQI");

                if (showEcoGroups)
                {
                    AddResultRow("Eco1 %", allResults.Select(r => r.EcoGroups[0]).ToArray());
                    AddResultRow("Eco2 %", allResults.Select(r => r.EcoGroups[1]).ToArray());
                    AddResultRow("Eco3 %", allResults.Select(r => r.EcoGroups[2]).ToArray());
                    AddResultRow("Eco4 %", allResults.Select(r => r.EcoGroups[3]).ToArray());
                    AddResultRow("Eco5 %", allResults.Select(r => r.EcoGroups[4]).ToArray());
                }
            }
        }

        private void AddResultRow(string indexName, double[] values)
        {
            var row = resultsTable.NewRow();
            row["Index"] = indexName;

            // List to store valid values for stats calculation
            var validValues = new List<double>();

            // The number of sample columns is total columns minus 5 (Index, Mean, StdDev, Min, Max)
            int sampleCount = resultsTable.Columns.Count - 5;

            for (int i = 0; i < values.Length && i < sampleCount; i++)
            {
                if (double.IsNaN(values[i]))
                {
                    row[i + 1] = DBNull.Value; // Handle NaN as null in DataTable
                }
                else
                {
                    row[i + 1] = Math.Round(values[i], 4);
                    validValues.Add(values[i]);
                }
            }

            // Calculate statistics if we have valid values
            if (validValues.Count > 0)
            {
                double mean = validValues.Average();
                double sumOfSquares = validValues.Sum(v => Math.Pow(v - mean, 2));
                double stdDev = Math.Sqrt(sumOfSquares / (validValues.Count > 1 ? validValues.Count - 1 : 1));
                double min = validValues.Min();
                double max = validValues.Max();

                row["Mean"] = Math.Round(mean, 4);
                row["StdDev"] = Math.Round(stdDev, 4);
                row["Min"] = Math.Round(min, 4);
                row["Max"] = Math.Round(max, 4);
            }
            else
            {
                row["Mean"] = DBNull.Value;
                row["StdDev"] = DBNull.Value;
                row["Min"] = DBNull.Value;
                row["Max"] = DBNull.Value;
            }

            resultsTable.Rows.Add(row);
        }

        private void FormatDataGrid()
        {
            foreach (DataGridViewColumn col in dataGridIndices.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (col.Index > 0)
                {
                    col.DefaultCellStyle.Format = "N4";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
        }

        private void UpdateEQSSummary()
        {
            var eqsGrid = tabControl.TabPages[2].Controls.Find("eqsSummaryGrid", true).FirstOrDefault() as DataGridView;
            if (eqsGrid == null || allResults.Count == 0) return;

            var eqsTable = new DataTable();
            eqsTable.Columns.Add("Sample", typeof(string));
            eqsTable.Columns.Add("Foram-AMBI EQS", typeof(string));
            eqsTable.Columns.Add("Foram-M-AMBI EQS", typeof(string));
            eqsTable.Columns.Add("BENTIX EQS", typeof(string));
            eqsTable.Columns.Add("BQI EQS", typeof(string));
            eqsTable.Columns.Add("FSI EQS", typeof(string));
            eqsTable.Columns.Add("NQI EQS", typeof(string));
            eqsTable.Columns.Add("exp(H'bc) EQS", typeof(string));
            eqsTable.Columns.Add("FoRAM Status", typeof(string));
            eqsTable.Columns.Add("FSI Databank", typeof(string));
            eqsTable.Columns.Add("TSI-Med Databank", typeof(string));

            foreach (var result in allResults)
            {
                var row = eqsTable.NewRow();
                row["Sample"] = result.SampleName;
                row["Foram-AMBI EQS"] = result.FAMBI_EQS;
                row["Foram-M-AMBI EQS"] = double.IsNaN(result.ForamMAMBI) ? "N/A" : result.ForamMAMBI_EQS;
                row["BENTIX EQS"] = result.BENTIX_EQS;
                row["BQI EQS"] = result.BQI_EQS;
                row["FSI EQS"] = double.IsNaN(result.FSI) ? "N/A" : result.FSI_EQS;
                row["NQI EQS"] = result.NQI_EQS;
                row["exp(H'bc) EQS"] = result.ExpHbc_EQS;
                row["FoRAM Status"] = result.FoRAM_Status;
                row["FSI Databank"] = result.FSI_UsingSpecializedDatabank ? "Dimiza" : "Approx";
                row["TSI-Med Databank"] = result.TSIMed_UsingSpecializedDatabank ? "Barras" : "Approx";
                eqsTable.Rows.Add(row);
            }

            eqsGrid.DataSource = eqsTable;

            // Color-code EQS cells
            eqsGrid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 1 && e.ColumnIndex <= 8 && e.Value != null)
                {
                    string value = e.Value.ToString();
                    e.CellStyle.BackColor = GetEQSColor(value);
                    e.CellStyle.ForeColor = value == "Bad" || value == "Poor" || value == "Unsuitable for coral growth" ? Color.White : Color.Black;
                }
                // Highlight approximation warnings
                if (e.ColumnIndex >= 9 && e.Value?.ToString() == "Approx")
                {
                    e.CellStyle.BackColor = Color.LightYellow;
                }
            };
        }

        private void ShowDatabankWarnings()
        {
            var warnings = new List<string>();

            if (!fsiDatabankAvailable)
            {
                warnings.Add("FSI: Specialized databank (fsi_databank.csv) not found.\nUsing F-AMBI ecogroups as approximation. Results may not be accurate per Dimiza et al. (2016).");
            }

            if (!tsiMedDatabankAvailable)
            {
                warnings.Add("TSI-Med: Specialized databank (tsimed_databank.csv) not found.\nUsing F-AMBI ecogroups as approximation. Results may not be accurate per Barras et al. (2014).");
            }

            if (mudPercentages == null || mudPercentages.Count == 0)
            {
                warnings.Add("TSI-Med: Mud percentages not provided.\nUsing default value of 50% for all samples. For accurate results, provide sediment grain-size data.");
            }

            if (calculateFoRAMIndex && foramDatabankAvailable)
            {
                warnings.Add("FoRAM Index: CALCULATED.\nThis index is designed for tropical/subtropical coral reef environments (Hallock et al. 2003; Prazeres et al. 2020).\nInterpretation: FI > 4 = suitable for coral growth; FI 2-4 = marginal; FI < 2 = unsuitable.");
            }
            else if (!foramDatabankAvailable)
            {
                warnings.Add("FoRAM Index: Databank (foram_index_databank.csv) not found.\nIndex cannot be calculated.");
            }
            else
            {
                warnings.Add("FoRAM Index: Not calculated (user skipped).\nNote: This index is designed for tropical coral reef environments.");
            }

            if (warnings.Count > 0)
            {
                string message = "DATABANK STATUS:\n\n" + string.Join("\n\n", warnings);
                MessageBox.Show(message, "Index Calculation Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private Color GetEQSColor(string eqs)
        {
            return eqs switch
            {
                "High" or "Suitable for coral growth" => Color.FromArgb(0, 128, 0),
                "Good" => Color.FromArgb(144, 238, 144),
                "Moderate" or "Marginal conditions" => Color.FromArgb(255, 255, 0),
                "Poor" => Color.FromArgb(255, 165, 0),
                "Bad" or "Unsuitable for coral growth" => Color.FromArgb(255, 0, 0),
                _ => Color.White
            };
        }

        private void GeneratePlotButton_Click(object sender, EventArgs e)
        {
            string plotType = plotTypeCombo.SelectedItem?.ToString() ?? "Bar Chart";
            GeneratePlot(plotType);
        }

        private void GeneratePlot(string plotType)
        {
            var selectedIndices = indexSelectionList.CheckedItems.Cast<string>().ToList();
            var filteredResults = GetSelectedResults();

            // Eco Groups and Composite Panel don't require index selection
            if (plotType == "Eco Groups")
            {
                if (!fambiAvailable)
                {
                    MessageBox.Show("Eco Groups data not available. F-AMBI databank is required.", "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Show both line plot and box plot side by side
                ShowPlotModels(new List<PlotModel> { CreateEcoGroupsPlot(), CreateEcoGroupsBoxPlot() });
                return;
            }

            if (plotType == "Composite Panel")
            {
                GenerateCompositePlot();
                return;
            }

            // Other plot types require index selection
            if (selectedIndices.Count == 0)
            {
                MessageBox.Show("Please select at least one index to plot.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (filteredResults.Count == 0) return;

            switch (plotType)
            {
                case "Bar Chart":
                    ShowPlotModels(CreatePerSampleBarPlots(selectedIndices, filteredResults, false));
                    break;
                case "Grouped Bar":
                    ShowPlotModels(CreatePerSampleBarPlots(selectedIndices, filteredResults, true));
                    break;
                case "Line Plot":
                    ShowPlotModels(new List<PlotModel> { CreateLinePlot(selectedIndices, filteredResults) });
                    break;
                case "Box Plot":
                    ShowPlotModels(CreatePerIndexBoxPlots(selectedIndices, filteredResults));
                    break;
                case "Scatter Plot":
                    ShowPlotModels(CreatePerIndexScatterPlots(selectedIndices, filteredResults));
                    break;
                case "Heatmap":
                    ShowPlotModels(new List<PlotModel> { CreateHeatmapPlot(selectedIndices, filteredResults) });
                    break;
            }
        }

        private void ShowPlotModels(List<PlotModel> models)
        {
            plotHostPanel.Controls.Clear();

            if (models == null || models.Count == 0)
                return;

            if (models.Count == 1)
            {
                var plotView = new PlotView { Dock = DockStyle.Fill, Model = models[0], Name = "plotPreview" };
                plotHostPanel.Controls.Add(plotView);
                return;
            }

            int columns = Math.Min(3, (int)Math.Ceiling(Math.Sqrt(models.Count)));
            int rows = (int)Math.Ceiling((double)models.Count / columns);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = columns,
                RowCount = rows,
                Padding = new Padding(10)
            };

            for (int c = 0; c < columns; c++)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
            }
            for (int r = 0; r < rows; r++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
            }

            for (int i = 0; i < models.Count; i++)
            {
                var plotView = new PlotView { Dock = DockStyle.Fill, Model = models[i] };
                int row = i / columns;
                int col = i % columns;
                table.Controls.Add(plotView, col, row);
            }

            plotHostPanel.Controls.Add(table);
        }

        private List<PlotModel> CreatePerSampleBarPlots(List<string> indices, List<IndicesResult> results, bool grouped)
        {
            var models = new List<PlotModel>();
            var colors = GetColorPalette(indices.Count);

            foreach (var result in results)
            {
                var model = new PlotModel { Title = $"{result.SampleName} - {(grouped ? "Grouped" : "Stacked")} Bars" };
                ConfigurePlotStyle(model);

                // For BarSeries (horizontal bars): CategoryAxis on Y (Left), ValueAxis on X (Bottom)
                var categoryAxis = new CategoryAxis { Position = AxisPosition.Left };
                categoryAxis.Labels.AddRange(indices);
                ApplyAxisStyle(categoryAxis);
                model.Axes.Add(categoryAxis);

                var valueAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Value", AbsoluteMinimum = 0 };
                ApplyAxisStyle(valueAxis);
                model.Axes.Add(valueAxis);

                var series = new BarSeries { StrokeThickness = 1, StrokeColor = OxyColors.Black, IsStacked = !grouped, LabelPlacement = LabelPlacement.Inside };

                var statsValues = new List<double>();
                for (int i = 0; i < indices.Count; i++)
                {
                    double value = GetIndexValueForResult(indices[i], result);
                    var item = new BarItem(double.IsNaN(value) ? 0 : value) { Color = colors[i % colors.Length] };
                    series.Items.Add(item);
                    if (!double.IsNaN(value)) statsValues.Add(value);
                }

                model.Series.Add(series);

                // Add statistics annotation to bottom of plot
                AddStatisticsAnnotation(model, statsValues, result.SampleName);

                // Add legend for bar colors
                AddBarLegend(model, indices, colors);

                models.Add(model);
            }

            return models;
        }

        private void AddBarLegend(PlotModel model, List<string> indices, OxyColor[] colors)
        {
            // Create invisible line series for legend entries
            for (int i = 0; i < indices.Count; i++)
            {
                var legendSeries = new LineSeries
                {
                    Title = indices[i],
                    Color = colors[i % colors.Length],
                    StrokeThickness = 0,
                    MarkerType = MarkerType.Square,
                    MarkerSize = 8,
                    MarkerFill = colors[i % colors.Length]
                };
                model.Series.Add(legendSeries);
            }
            AddLegend(model);
        }

        private void AddStatisticsAnnotation(PlotModel model, List<double> values, string seriesName = null)
        {
            if (values == null || values.Count == 0) return;

            double mean = values.Average();
            double stdDev = values.Count > 1 ? Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1)) : 0;
            double min = values.Min();
            double max = values.Max();

            string statsText = seriesName != null
                ? $"Statistics ({seriesName}): n={values.Count}, Mean={mean:F3}, SD={stdDev:F3}, Min={min:F3}, Max={max:F3}"
                : $"Statistics: n={values.Count}, Mean={mean:F3}, SD={stdDev:F3}, Min={min:F3}, Max={max:F3}";

            // Add subtitle with stats
            model.Subtitle = statsText;
            model.SubtitleFontSize = GetSelectedFontSize() - 1;
            model.SubtitleColor = OxyColors.DarkGray;
        }

        private PlotModel CreateLinePlot(List<string> indices, List<IndicesResult> results)
        {
            var model = new PlotModel { Title = "Biotic Indices Comparison" };
            ConfigurePlotStyle(model);

            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Angle = 45 };
            categoryAxis.Labels.AddRange(results.Select(r => r.SampleName));
            ApplyAxisStyle(categoryAxis);
            model.Axes.Add(categoryAxis);

            var valueAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Value" };
            ApplyAxisStyle(valueAxis);
            model.Axes.Add(valueAxis);

            var colors = GetColorPalette(indices.Count);
            var allValues = new List<double>();
            var statsBuilder = new StringBuilder();

            for (int i = 0; i < indices.Count; i++)
            {
                var series = new LineSeries
                {
                    Title = indices[i],
                    Color = colors[i % colors.Length],
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 5,
                    MarkerStroke = colors[i % colors.Length],
                    MarkerFill = colors[i % colors.Length],
                    StrokeThickness = 2
                };

                var values = results.Select(r => GetIndexValueForResult(indices[i], r)).ToList();
                var validValues = values.Where(v => !double.IsNaN(v)).ToList();

                for (int j = 0; j < values.Count; j++)
                {
                    if (!double.IsNaN(values[j]))
                    {
                        series.Points.Add(new DataPoint(j, values[j]));
                        allValues.Add(values[j]);
                    }
                }

                // Add per-index stats to subtitle
                if (validValues.Count > 0)
                {
                    double mean = validValues.Average();
                    double sd = validValues.Count > 1 ? Math.Sqrt(validValues.Sum(v => Math.Pow(v - mean, 2)) / (validValues.Count - 1)) : 0;
                    statsBuilder.Append($"{indices[i]}: Î¼={mean:F2}Â±{sd:F2}  ");
                }

                model.Series.Add(series);
            }

            // Add statistics as subtitle
            if (statsBuilder.Length > 0)
            {
                model.Subtitle = statsBuilder.ToString().TrimEnd();
                model.SubtitleFontSize = GetSelectedFontSize() - 2;
                model.SubtitleColor = OxyColors.DarkGray;
            }

            AddLegend(model);
            return model;
        }

        private List<PlotModel> CreatePerIndexScatterPlots(List<string> indices, List<IndicesResult> results)
        {
            var models = new List<PlotModel>();
            var sampleNames = results.Select(r => r.SampleName).ToList();
            var colors = GetScatterColorPalette(indices.Count);
            var markerTypes = new[] { MarkerType.Circle, MarkerType.Square, MarkerType.Triangle, MarkerType.Diamond, MarkerType.Star, MarkerType.Cross, MarkerType.Plus };

            for (int i = 0; i < indices.Count; i++)
            {
                var model = new PlotModel { Title = $"{indices[i]} (Scatter by Sample)" };
                ConfigurePlotStyle(model);

                var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Angle = 45, Key = "SamplesAxis" };
                categoryAxis.Labels.AddRange(sampleNames);
                ApplyAxisStyle(categoryAxis);
                model.Axes.Add(categoryAxis);

                var valueAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Value" };
                ApplyAxisStyle(valueAxis);
                model.Axes.Add(valueAxis);

                var series = new ScatterSeries
                {
                    Title = indices[i],
                    XAxisKey = "SamplesAxis",
                    MarkerType = markerTypes[i % markerTypes.Length],
                    MarkerSize = 8,
                    MarkerFill = colors[i % colors.Length],
                    MarkerStroke = OxyColor.FromAColor(200, colors[i % colors.Length]),
                    MarkerStrokeThickness = 1.5
                };

                var scatterValues = new List<double>();
                for (int j = 0; j < sampleNames.Count; j++)
                {
                    double value = GetIndexValueForResult(indices[i], results[j]);
                    if (!double.IsNaN(value))
                    {
                        series.Points.Add(new ScatterPoint(j, value));
                        scatterValues.Add(value);
                    }
                }

                model.Series.Add(series);

                // Add statistics annotation
                if (scatterValues.Count > 0)
                {
                    double mean = scatterValues.Average();
                    double sd = scatterValues.Count > 1 ? Math.Sqrt(scatterValues.Sum(v => Math.Pow(v - mean, 2)) / (scatterValues.Count - 1)) : 0;
                    model.Subtitle = $"n={scatterValues.Count}, Mean={mean:F3}, SD={sd:F3}, Min={scatterValues.Min():F3}, Max={scatterValues.Max():F3}";
                    model.SubtitleFontSize = GetSelectedFontSize() - 1;
                    model.SubtitleColor = OxyColors.DarkGray;
                }

                // Add legend
                AddLegend(model);

                models.Add(model);
            }

            return models;
        }

        private void GenerateCompositePlot()
        {
            // Create a form with multiple plots in a grid
            var plotForm = new Form
            {
                Text = "Composite Plot Panel",
                Size = new Size(1200, 900),
                StartPosition = FormStartPosition.CenterScreen,
                Icon = this.Icon
            };

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Name = "compositePlotTable"
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            // Plot 1: Diversity Indices
            var plot1 = CreateSubPlot("Diversity Indices", new[] { "exp(H'bc)", "H'log2", "Species Richness (S)" });
            tableLayout.Controls.Add(plot1, 0, 0);

            // Plot 2: Sensitivity Indices
            var plot2 = CreateSubPlot("Sensitivity-Based Indices", new[] { "FSI", "NQIf", "F-AMBI" });
            tableLayout.Controls.Add(plot2, 1, 0);

            // Plot 3: Eco-Groups
            var plot3 = CreateSubPlotEcoGroups("Ecological Group Distribution");
            tableLayout.Controls.Add(plot3, 0, 1);

            // Plot 4: Additional Indices
            var plot4 = CreateSubPlot("Additional Indices", new[] { "FIEI", "TSI-Med", "FoRAM Index" });
            tableLayout.Controls.Add(plot4, 1, 1);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };

            // Save PNG button
            var saveBtn = new Button
            {
                Text = "Save as PNG",
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveBtn.Click += (s, e) => SaveCompositePlot(tableLayout);

            // Export to Excel button
            var exportExcelBtn = new Button
            {
                Text = "Export Stats to Excel",
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            exportExcelBtn.Click += (s, e) => ExportCompositeStatsToExcel();

            // DPI selector
            var dpiLabel = new Label { Text = "DPI:", AutoSize = true, Padding = new Padding(10, 8, 0, 0) };
            var dpiSelector = new ComboBox { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            dpiSelector.Items.AddRange(new[] { "150", "300", "600", "1200" });
            dpiSelector.SelectedIndex = 1;
            dpiSelector.Tag = "compositeDpi";

            buttonPanel.Controls.AddRange(new Control[] { saveBtn, exportExcelBtn, dpiLabel, dpiSelector });

            plotForm.Controls.Add(tableLayout);
            plotForm.Controls.Add(buttonPanel);
            plotForm.Show();
        }

        private void ExportCompositeStatsToExcel()
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Export Composite Statistics",
                FileName = "CompositeStats.xlsx"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("Statistics");

                // Headers
                sheet.Cell(1, 1).Value = "Index";
                sheet.Cell(1, 2).Value = "n";
                sheet.Cell(1, 3).Value = "Mean";
                sheet.Cell(1, 4).Value = "StdDev";
                sheet.Cell(1, 5).Value = "Min";
                sheet.Cell(1, 6).Value = "Max";

                int row = 2;
                foreach (var indexName in availableIndices)
                {
                    var values = allResults.Select(r => GetIndexValueForResult(indexName, r)).Where(v => !double.IsNaN(v)).ToList();
                    if (values.Count > 0)
                    {
                        double mean = values.Average();
                        double sd = values.Count > 1 ? Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1)) : 0;

                        sheet.Cell(row, 1).Value = indexName;
                        sheet.Cell(row, 2).Value = values.Count;
                        sheet.Cell(row, 3).Value = Math.Round(mean, 4);
                        sheet.Cell(row, 4).Value = Math.Round(sd, 4);
                        sheet.Cell(row, 5).Value = Math.Round(values.Min(), 4);
                        sheet.Cell(row, 6).Value = Math.Round(values.Max(), 4);
                        row++;
                    }
                }

                workbook.SaveAs(saveDialog.FileName);
                MessageBox.Show("Statistics exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private PlotView CreateSubPlot(string title, string[] indices)
        {
            var model = CreateLinePlot(indices.ToList(), allResults);
            model.Title = title;

            var plotView = new PlotView { Dock = DockStyle.Fill, Model = model };
            return plotView;
        }

        private PlotView CreateSubPlotEcoGroups(string title)
        {
            var model = CreateEcoGroupsPlot();
            model.Title = title;

            var plotView = new PlotView { Dock = DockStyle.Fill, Model = model };
            return plotView;
        }

        private void GenerateEQSPlot()
        {
            var plotForm = new Form
            {
                Text = "Ecological Quality Status Overview",
                Size = new Size(1000, 700),
                StartPosition = FormStartPosition.CenterScreen,
                Icon = this.Icon
            };

            var model = new PlotModel { Title = "Ecological Quality Status by Sample" };
            ConfigurePlotStyle(model);

            // Create stacked area or heatmap showing EQS across samples
            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Angle = 45 };
            categoryAxis.Labels.AddRange(allResults.Select(r => r.SampleName));
            model.Axes.Add(categoryAxis);

            var valueAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                Key = "Indices"
            };
            valueAxis.Labels.AddRange(new[] { "F-AMBI", "FSI", "NQI", "exp(H'bc)" });
            model.Axes.Add(valueAxis);

            // Create heatmap-like visualization
            var heatmapSeries = new RectangleBarSeries();

            for (int i = 0; i < allResults.Count; i++)
            {
                var result = allResults[i];
                AddEQSBar(heatmapSeries, i, 0, result.FAMBI_EQS);
                AddEQSBar(heatmapSeries, i, 1, result.FSI_EQS);
                AddEQSBar(heatmapSeries, i, 2, result.NQI_EQS);
                AddEQSBar(heatmapSeries, i, 3, result.ExpHbc_EQS);
            }

            model.Series.Add(heatmapSeries);

            var plotView = new PlotView { Dock = DockStyle.Fill, Model = model };

            var saveBtn = new Button
            {
                Text = "Save as PNG",
                Dock = DockStyle.Bottom,
                Height = 40
            };
            saveBtn.Click += (s, e) => SavePlotAsPng(model, "EQS_Overview.png");

            plotForm.Controls.Add(plotView);
            plotForm.Controls.Add(saveBtn);
            plotForm.Show();
        }

        private void AddEQSBar(RectangleBarSeries series, int x, int y, string eqs)
        {
            var color = eqs switch
            {
                "High" => OxyColor.FromRgb(0, 128, 0),
                "Good" => OxyColor.FromRgb(144, 238, 144),
                "Moderate" => OxyColor.FromRgb(255, 255, 0),
                "Poor" => OxyColor.FromRgb(255, 165, 0),
                "Bad" => OxyColor.FromRgb(255, 0, 0),
                _ => OxyColor.FromRgb(200, 200, 200)
            };

            series.Items.Add(new RectangleBarItem(x - 0.4, y - 0.4, x + 0.4, y + 0.4) { Color = color });
        }

        private void ShowBoxPlotWithStats(List<string> indices, List<IndicesResult> results)
        {
            // Create a new form for the plot (Form3 style)
            Form plotForm = new Form();
            plotForm.Text = "Biotic Indices - Box Plot";
            plotForm.Size = new Size(800, 600);
            plotForm.StartPosition = FormStartPosition.CenterScreen;
            plotForm.Icon = this.Icon;

            // Create the plot model
            var plotModel = new PlotModel { Title = "Biotic Indices Distribution Across Samples" };
            ConfigurePlotStyle(plotModel);

            // X-axis: Index names
            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                Angle = 45
            };
            categoryAxis.Labels.AddRange(indices);
            ApplyAxisStyle(categoryAxis);
            plotModel.Axes.Add(categoryAxis);

            // Y-axis: Values
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Value"
            };
            ApplyAxisStyle(valueAxis);
            plotModel.Axes.Add(valueAxis);

            // Create BoxPlotSeries
            var boxPlotSeries = new BoxPlotSeries
            {
                Title = "Indices",
                BoxWidth = 0.3,
                Stroke = OxyColors.Black,
                Fill = OxyColor.FromRgb(100, 149, 237),
                WhiskerWidth = 0.6,
                MedianThickness = 2
            };

            var statsBuilder = new StringBuilder();

            // Calculate and add BoxPlot items for each index
            for (int i = 0; i < indices.Count; i++)
            {
                List<double> values = new List<double>();

                // Collect all values for this index across all samples
                foreach (var result in results)
                {
                    double val = GetIndexValueForResult(indices[i], result);
                    if (!double.IsNaN(val))
                    {
                        values.Add(val);
                    }
                }

                // Ensure we have enough data points to calculate statistics
                if (values.Count > 0)
                {
                    values.Sort();

                    double min = values.First();
                    double max = values.Last();
                    double median = GetMedian(values);
                    double q1 = GetPercentile(values, 25);
                    double q3 = GetPercentile(values, 75);

                    // Add the BoxPlotItem using the calculated values
                    boxPlotSeries.Items.Add(new BoxPlotItem(i, min, q1, median, q3, max));

                    // Append statistics to the StringBuilder
                    statsBuilder.AppendLine($"{indices[i]}: Min = {min:F4}, Q1 = {q1:F4}, Median = {median:F4}, Q3 = {q3:F4}, Max = {max:F4}");
                }
            }

            // Add the series to the plot model
            plotModel.Series.Add(boxPlotSeries);

            // Create a PlotView control and add it to the form
            var plotView = new PlotView
            {
                Dock = DockStyle.Fill,
                Model = plotModel
            };
            plotForm.Controls.Add(plotView);

            // Create a "Save as PNG" button
            var saveButton = new Button
            {
                Text = "Save as PNG",
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveButton.Click += (s, ev) => SavePlotAsPng(plotModel, "BoxPlot_Indices.png");
            plotForm.Controls.Add(saveButton);

            // Create a label to display the statistics
            var statsLabel = new Label
            {
                Text = statsBuilder.ToString(),
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(10),
                Font = new Font("Consolas", 9)
            };
            plotForm.Controls.Add(statsLabel);

            // Display the form
            plotForm.ShowDialog();
        }

        private PlotModel CreateEcoGroupsPlot()
        {
            var model = new PlotModel { Title = "Ecological Groups (Eco1-5) Across Samples" };
            ConfigurePlotStyle(model);

            // X-axis: Sample names
            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Angle = 45 };
            categoryAxis.Labels.AddRange(allResults.Select(r => r.SampleName));
            ApplyAxisStyle(categoryAxis);
            model.Axes.Add(categoryAxis);

            // Y-axis: Percentage values
            var valueAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Percentage (%)", Minimum = 0, Maximum = 100 };
            ApplyAxisStyle(valueAxis);
            model.Axes.Add(valueAxis);

            // Define eco group names and colors
            var ecoGroups = new[] { "Eco1", "Eco2", "Eco3", "Eco4", "Eco5" };
            var ecoColors = new[]
            {
                OxyColor.FromRgb(0, 114, 178),    // Blue
                OxyColor.FromRgb(0, 158, 115),    // Green
                OxyColor.FromRgb(240, 228, 66),   // Yellow
                OxyColor.FromRgb(230, 159, 0),    // Orange
                OxyColor.FromRgb(213, 94, 0)      // Red
            };

            var statsBuilder = new StringBuilder();

            // Create a line series for each eco group
            for (int i = 0; i < ecoGroups.Length; i++)
            {
                var series = new LineSeries
                {
                    Title = ecoGroups[i],
                    Color = ecoColors[i],
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 6,
                    MarkerStroke = ecoColors[i],
                    MarkerFill = ecoColors[i],
                    StrokeThickness = 2
                };

                var ecoValues = new List<double>();

                // Add data points for each sample
                for (int j = 0; j < allResults.Count; j++)
                {
                    double value = allResults[j].EcoGroups[i];
                    series.Points.Add(new DataPoint(j, value));
                    ecoValues.Add(value);
                }

                // Calculate stats for this eco group
                if (ecoValues.Count > 0)
                {
                    double mean = ecoValues.Average();
                    statsBuilder.Append($"{ecoGroups[i]}: {mean:F1}%  ");
                }

                model.Series.Add(series);
            }

            // Add statistics as subtitle
            if (statsBuilder.Length > 0)
            {
                model.Subtitle = "Mean: " + statsBuilder.ToString().TrimEnd();
                model.SubtitleFontSize = GetSelectedFontSize() - 2;
                model.SubtitleColor = OxyColors.DarkGray;
            }

            AddLegend(model);

            return model;
        }

        private void GenerateEcoGroupsPlot()
        {
            if (!fambiAvailable)
            {
                MessageBox.Show("Eco Groups data not available. F-AMBI databank is required.", "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var plotForm = new Form
            {
                Text = "Ecological Groups Distribution",
                Size = new Size(1200, 700),
                StartPosition = FormStartPosition.CenterScreen,
                Icon = this.Icon
            };

            // Create a split panel for two plots
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Name = "ecoGroupsTable"
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Plot 1: Line plot (samples on X-axis, eco group values)
            var linePlotModel = CreateEcoGroupsPlot();
            linePlotModel.Title = "Eco Groups - Line Plot";
            var linePlotView = new PlotView { Dock = DockStyle.Fill, Model = linePlotModel };
            tableLayout.Controls.Add(linePlotView, 0, 0);

            // Plot 2: Box plot (eco groups on X-axis, distribution across samples)
            var boxPlotModel = CreateEcoGroupsBoxPlot();
            var boxPlotView = new PlotView { Dock = DockStyle.Fill, Model = boxPlotModel };
            tableLayout.Controls.Add(boxPlotView, 1, 0);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };

            var saveBtn = new Button
            {
                Text = "Save as PNG",
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveBtn.Click += (s, e) => SaveEcoGroupsCompositePlot(tableLayout);

            var exportStatsBtn = new Button
            {
                Text = "Export Stats",
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            exportStatsBtn.Click += (s, e) => ExportEcoGroupsStats();

            buttonPanel.Controls.AddRange(new Control[] { saveBtn, exportStatsBtn });

            plotForm.Controls.Add(tableLayout);
            plotForm.Controls.Add(buttonPanel);
            plotForm.Show();
        }

        private PlotModel CreateEcoGroupsBoxPlot()
        {
            var model = new PlotModel { Title = "Eco Groups - Box Plot" };
            ConfigurePlotStyle(model);

            // X-axis: Eco group names
            var ecoGroups = new[] { "Eco1", "Eco2", "Eco3", "Eco4", "Eco5" };
            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom };
            categoryAxis.Labels.AddRange(ecoGroups);
            ApplyAxisStyle(categoryAxis);
            model.Axes.Add(categoryAxis);

            // Y-axis: Percentage values
            var valueAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Percentage (%)", Minimum = 0, Maximum = 100 };
            ApplyAxisStyle(valueAxis);
            model.Axes.Add(valueAxis);

            // Define eco group colors
            var ecoColors = new[]
            {
                OxyColor.FromRgb(0, 114, 178),    // Blue
                OxyColor.FromRgb(0, 158, 115),    // Green
                OxyColor.FromRgb(240, 228, 66),   // Yellow
                OxyColor.FromRgb(230, 159, 0),    // Orange
                OxyColor.FromRgb(213, 94, 0)      // Red
            };

            var statsBuilder = new StringBuilder();

            // Create box plot for each eco group
            for (int i = 0; i < ecoGroups.Length; i++)
            {
                var values = allResults.Select(r => r.EcoGroups[i]).ToList();
                values.Sort();

                if (values.Count > 0)
                {
                    double min = values.Min();
                    double max = values.Max();
                    double median = GetMedian(values);
                    double q1 = GetPercentile(values, 25);
                    double q3 = GetPercentile(values, 75);

                    var boxSeries = new BoxPlotSeries
                    {
                        Title = ecoGroups[i],
                        Fill = ecoColors[i],
                        Stroke = OxyColors.Black,
                        StrokeThickness = 1.5,
                        BoxWidth = 0.5,
                        WhiskerWidth = 0.6,
                        MedianThickness = 2
                    };
                    boxSeries.Items.Add(new BoxPlotItem(i, min, q1, median, q3, max));
                    model.Series.Add(boxSeries);

                    statsBuilder.Append($"{ecoGroups[i]}: Med={median:F1}%  ");
                }
            }

            // Add statistics as subtitle
            model.Subtitle = statsBuilder.ToString().TrimEnd();
            model.SubtitleFontSize = GetSelectedFontSize() - 2;
            model.SubtitleColor = OxyColors.DarkGray;

            AddLegend(model);

            return model;
        }

        private void SaveEcoGroupsCompositePlot(TableLayoutPanel layout)
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Save Eco Groups Plot",
                FileName = "EcoGroups_Composite.png"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                using var bitmap = new Bitmap(layout.Width, layout.Height);
                layout.DrawToBitmap(bitmap, new Rectangle(0, 0, layout.Width, layout.Height));
                bitmap.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                MessageBox.Show("Plot saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportEcoGroupsStats()
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Export Eco Groups Statistics",
                FileName = "EcoGroups_Stats.xlsx"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("EcoGroups");

                // Headers
                sheet.Cell(1, 1).Value = "Sample";
                sheet.Cell(1, 2).Value = "Eco1 %";
                sheet.Cell(1, 3).Value = "Eco2 %";
                sheet.Cell(1, 4).Value = "Eco3 %";
                sheet.Cell(1, 5).Value = "Eco4 %";
                sheet.Cell(1, 6).Value = "Eco5 %";

                for (int i = 0; i < allResults.Count; i++)
                {
                    sheet.Cell(i + 2, 1).Value = allResults[i].SampleName;
                    for (int j = 0; j < 5; j++)
                    {
                        sheet.Cell(i + 2, j + 2).Value = Math.Round(allResults[i].EcoGroups[j], 2);
                    }
                }

                // Add statistics row
                int statsRow = allResults.Count + 3;
                sheet.Cell(statsRow, 1).Value = "Mean";
                for (int j = 0; j < 5; j++)
                {
                    var values = allResults.Select(r => r.EcoGroups[j]).ToList();
                    sheet.Cell(statsRow, j + 2).Value = Math.Round(values.Average(), 2);
                }

                statsRow++;
                sheet.Cell(statsRow, 1).Value = "StdDev";
                for (int j = 0; j < 5; j++)
                {
                    var values = allResults.Select(r => r.EcoGroups[j]).ToList();
                    double mean = values.Average();
                    double sd = Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1));
                    sheet.Cell(statsRow, j + 2).Value = Math.Round(sd, 2);
                }

                workbook.SaveAs(saveDialog.FileName);
                MessageBox.Show("Eco Groups statistics exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportPlotWithDialog(PlotModel model, string defaultName)
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|PDF Document|*.pdf",
                Title = "Export Plot",
                FileName = defaultName
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                string ext = Path.GetExtension(saveDialog.FileName).ToLower();
                int dpi = GetSelectedDpi();

                if (ext == ".png" || ext == ".jpg")
                {
                    SavePlotAsPng(model, saveDialog.FileName, dpi);
                }
                else if (ext == ".pdf")
                {
                    using var stream = File.Create(saveDialog.FileName);
                    var pdfExporter = new OxyPlot.PdfExporter { Width = 800, Height = 600 };
                    pdfExporter.Export(model, stream);
                }

                MessageBox.Show("Plot exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ConfigurePlotStyle(PlotModel model)
        {
            int fontSize = GetSelectedFontSize();
            model.TitleFontSize = fontSize + 2;
            model.TitleFontWeight = FontWeights.Bold;
            model.PlotAreaBorderThickness = new OxyThickness(1);
            model.PlotAreaBorderColor = OxyColors.Black;
            model.DefaultFont = "Arial";
            model.DefaultFontSize = fontSize;
        }

        private void ApplyAxisStyle(Axis axis)
        {
            bool showGrid = gridCheckbox?.Checked ?? true;
            axis.MajorGridlineStyle = showGrid ? LineStyle.Solid : LineStyle.None;
            axis.MinorGridlineStyle = showGrid ? LineStyle.Dot : LineStyle.None;
            axis.MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.Gray);
            axis.MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.Gray);
            axis.TitleFontSize = GetSelectedFontSize();
            axis.FontSize = GetSelectedFontSize();
        }

        private void AddLegend(PlotModel model)
        {
            model.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendBackground = OxyColor.FromAColor(230, OxyColors.White),
                LegendBorder = OxyColor.FromRgb(180, 180, 180),
                LegendBorderThickness = 1,
                LegendPadding = 10,
                LegendMargin = 10,
                LegendItemSpacing = 8,
                LegendLineSpacing = 4,
                LegendSymbolMargin = 10,
                LegendFontSize = 11,
                LegendTitleFontSize = 12,
                LegendTitleFontWeight = FontWeights.Bold,
                LegendMaxWidth = 200,
                LegendMaxHeight = double.NaN
            });
        }

        private List<PlotModel> CreatePerIndexBoxPlots(List<string> indices, List<IndicesResult> results)
        {
            // Create ONE plot with ALL indices as box plots showing distribution across samples
            var model = new PlotModel { Title = "Biotic Indices - Distribution Across Samples" };
            ConfigurePlotStyle(model);

            // X-axis: Index names
            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Angle = 45 };
            categoryAxis.Labels.AddRange(indices);
            ApplyAxisStyle(categoryAxis);
            model.Axes.Add(categoryAxis);

            // Y-axis: Values
            var valueAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Value" };
            ApplyAxisStyle(valueAxis);
            model.Axes.Add(valueAxis);

            var colors = GetColorPalette(indices.Count);
            var statsBuilder = new StringBuilder();
            int validCount = 0;

            // For each index, collect all sample values and create box plot
            for (int i = 0; i < indices.Count; i++)
            {
                var values = new List<double>();

                // Collect all values for this index across all samples
                foreach (var result in results)
                {
                    double val = GetIndexValueForResult(indices[i], result);
                    if (!double.IsNaN(val))
                        values.Add(val);
                }

                if (values.Count > 0)
                {
                    values.Sort();

                    // Calculate box plot statistics
                    double min = values.Min();
                    double max = values.Max();
                    double median = GetMedian(values);
                    double q1 = GetPercentile(values, 25);
                    double q3 = GetPercentile(values, 75);

                    var boxSeries = new BoxPlotSeries
                    {
                        Title = indices[i],
                        Fill = colors[i % colors.Length],
                        Stroke = OxyColors.Black,
                        StrokeThickness = 1.5,
                        BoxWidth = 0.4,
                        WhiskerWidth = 0.6,
                        MedianThickness = 2,
                        MedianPointSize = 3
                    };
                    boxSeries.Items.Add(new BoxPlotItem(i, min, q1, median, q3, max));
                    model.Series.Add(boxSeries);

                    // Add to stats
                    statsBuilder.Append($"{indices[i]}: Med={median:F2}  ");
                    validCount++;
                }
            }

            // Add statistics as subtitle
            if (validCount > 0)
            {
                model.Subtitle = statsBuilder.ToString().TrimEnd();
                model.SubtitleFontSize = GetSelectedFontSize() - 2;
                model.SubtitleColor = OxyColors.DarkGray;
            }

            // Add legend
            AddLegend(model);

            return new List<PlotModel> { model };
        }

        private double GetIndexValueForResult(string indexName, IndicesResult result)
        {
            return indexName switch
            {
                "exp(H'bc)" => result.Exp_Hbc,
                "H'log2" => result.Shannon_Log2,
                "H'ln" => result.Shannon_Ln,
                "FSI" => result.FSI,
                "TSI-Med" => result.TSI_Med,
                "NQIf" => result.NQIf,
                "FIEI" => result.FIEI,
                "FoRAM Index" => result.FoRAM_Index,
                "Foram-AMBI" => result.FAMBI,
                "Foram-M-AMBI" => result.ForamMAMBI,
                "Foram-M-AMBI (Euclidean)" => result.ForamMAMBI_Euclidean,
                "M-AMBI Norm. AMBI" => result.ForamMAMBI_NormAMBI,
                "M-AMBI Norm. H'" => result.ForamMAMBI_NormH,
                "M-AMBI Norm. S" => result.ForamMAMBI_NormS,
                "BENTIX" => result.BENTIX,
                "BQI" => result.BQI,
                "Species Richness (S)" => result.SpeciesRichness,
                "Total Abundance (N)" => result.TotalAbundance,
                "Simpson (1-D)" => result.Simpson_1D,
                "Pielou's J" => result.Pielou_J,
                "ES100" => result.ES100,
                "Eco1 %" => result.EcoGroups[0],
                "Eco2 %" => result.EcoGroups[1],
                "Eco3 %" => result.EcoGroups[2],
                "Eco4 %" => result.EcoGroups[3],
                "Eco5 %" => result.EcoGroups[4],
                "FoRAM Symbiont %" => result.FoRAM_SymbiontPercent,
                "FoRAM Stress-Tolerant %" => result.FoRAM_StressTolerantPercent,
                "FoRAM Heterotrophic %" => result.FoRAM_HeterotrophicPercent,
                _ => double.NaN
            };
        }

        private OxyColor[] GetColorPalette(int count)
        {
            var baseColors = colorSchemeCombo?.SelectedItem?.ToString() switch
            {
                "Grayscale" => new[]
                {
                    OxyColor.FromRgb(50,50,50), OxyColor.FromRgb(100,100,100), OxyColor.FromRgb(150,150,150),
                    OxyColor.FromRgb(200,200,200), OxyColor.FromRgb(80,80,80)
                },
                "Colorblind Safe" => new[]
                {
                    OxyColor.FromRgb(0, 114, 178), OxyColor.FromRgb(213, 94, 0), OxyColor.FromRgb(240, 228, 66),
                    OxyColor.FromRgb(0, 158, 115), OxyColor.FromRgb(204, 121, 167)
                },
                "Nature Style" => new[]
                {
                    OxyColor.FromRgb(52, 101, 36), OxyColor.FromRgb(166, 97, 26), OxyColor.FromRgb(94, 60, 153),
                    OxyColor.FromRgb(17, 138, 178), OxyColor.FromRgb(231, 111, 81)
                },
                "Custom Gradient" => Enumerable.Range(0, Math.Max(count, 5)).Select(i => OxyColor.Interpolate(OxyColors.DarkBlue, OxyColors.OrangeRed, i / (double)Math.Max(1, count - 1))).ToArray(),
                _ => new[]
                {
                    OxyColor.FromRgb(0, 114, 178),
                    OxyColor.FromRgb(230, 159, 0),
                    OxyColor.FromRgb(0, 158, 115),
                    OxyColor.FromRgb(204, 121, 167),
                    OxyColor.FromRgb(86, 180, 233),
                    OxyColor.FromRgb(213, 94, 0),
                    OxyColor.FromRgb(240, 228, 66),
                    OxyColor.FromRgb(100, 100, 100),
                }
            };

            var colors = new OxyColor[count];
            for (int i = 0; i < count; i++)
            {
                colors[i] = baseColors[i % baseColors.Length];
            }
            return colors;
        }

        private int GetSelectedFontSize()
        {
            if (fontCombo != null && int.TryParse(fontCombo.SelectedItem?.ToString(), out int size))
            {
                return size;
            }
            return 12;
        }

        private OxyColor[] GetScatterColorPalette(int count)
        {
            // Enhanced scatter plot palette with better contrast and visibility
            var baseColors = new OxyColor[]
            {
                OxyColor.FromRgb(31, 119, 180),   // Muted Blue
                OxyColor.FromRgb(255, 127, 14),   // Safety Orange
                OxyColor.FromRgb(44, 160, 44),    // Cooked Asparagus Green
                OxyColor.FromRgb(214, 39, 40),    // Brick Red
                OxyColor.FromRgb(148, 103, 189),  // Muted Purple
                OxyColor.FromRgb(140, 86, 75),    // Chestnut Brown
                OxyColor.FromRgb(227, 119, 194),  // Raspberry Yogurt Pink
                OxyColor.FromRgb(127, 127, 127),  // Middle Gray
                OxyColor.FromRgb(188, 189, 34),   // Curry Yellow-Green
                OxyColor.FromRgb(23, 190, 207),   // Blue-Teal
            };

            var colors = new OxyColor[count];
            for (int i = 0; i < count; i++)
            {
                colors[i] = baseColors[i % baseColors.Length];
            }
            return colors;
        }

        private double GetMedian(List<double> values)
        {
            int n = values.Count;
            if (n == 0) return 0;
            return n % 2 == 0 ? (values[n / 2 - 1] + values[n / 2]) / 2 : values[n / 2];
        }

        private double GetPercentile(List<double> values, double percentile)
        {
            if (values.Count == 0) return 0;
            double n = (values.Count - 1) * percentile / 100.0 + 1;
            if (n == 1) return values[0];
            if (n == values.Count) return values.Last();
            int k = (int)n;
            double d = n - k;
            return values[k - 1] + d * (values[k] - values[k - 1]);
        }

        private PlotView FindPlotView()
        {
            return GetCurrentPlotViews().FirstOrDefault();
        }

        private List<PlotView> GetCurrentPlotViews()
        {
            var views = new List<PlotView>();
            views.AddRange(plotHostPanel.Controls.OfType<PlotView>());
            foreach (var table in plotHostPanel.Controls.OfType<TableLayoutPanel>())
            {
                views.AddRange(table.Controls.OfType<PlotView>());
            }
            return views;
        }

        private void SaveResults_Click(object sender, EventArgs e)
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Save Results to Excel",
                FileName = "BiotiIndices_Results.xlsx"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                using var workbook = new XLWorkbook();

                // Results sheet
                var resultsSheet = workbook.Worksheets.Add("Indices Results");
                for (int i = 0; i < resultsTable.Columns.Count; i++)
                {
                    resultsSheet.Cell(1, i + 1).Value = resultsTable.Columns[i].ColumnName;
                }
                for (int i = 0; i < resultsTable.Rows.Count; i++)
                {
                    for (int j = 0; j < resultsTable.Columns.Count; j++)
                    {
                        resultsSheet.Cell(i + 2, j + 1).Value = resultsTable.Rows[i][j]?.ToString() ?? "";
                    }
                }

                // EQS Summary sheet - now includes all indices
                var eqsSheet = workbook.Worksheets.Add("EQS Summary");
                eqsSheet.Cell(1, 1).Value = "Sample";
                eqsSheet.Cell(1, 2).Value = "F-AMBI EQS";
                eqsSheet.Cell(1, 3).Value = "Foram-M-AMBI EQS";
                eqsSheet.Cell(1, 4).Value = "FSI EQS";
                eqsSheet.Cell(1, 5).Value = "NQI EQS";
                eqsSheet.Cell(1, 6).Value = "exp(H'bc) EQS";
                eqsSheet.Cell(1, 7).Value = "TSI-Med EQS";
                eqsSheet.Cell(1, 8).Value = "BENTIX EQS";
                eqsSheet.Cell(1, 9).Value = "BQI EQS";
                eqsSheet.Cell(1, 10).Value = "FoRAM Status";

                for (int i = 0; i < allResults.Count; i++)
                {
                    eqsSheet.Cell(i + 2, 1).Value = allResults[i].SampleName;
                    eqsSheet.Cell(i + 2, 2).Value = allResults[i].FAMBI_EQS;
                    eqsSheet.Cell(i + 2, 3).Value = double.IsNaN(allResults[i].ForamMAMBI) ? "N/A" : allResults[i].ForamMAMBI_EQS;
                    eqsSheet.Cell(i + 2, 4).Value = allResults[i].FSI_EQS;
                    eqsSheet.Cell(i + 2, 5).Value = allResults[i].NQI_EQS;
                    eqsSheet.Cell(i + 2, 6).Value = allResults[i].ExpHbc_EQS;
                    eqsSheet.Cell(i + 2, 7).Value = allResults[i].TSIMed_EQS;
                    eqsSheet.Cell(i + 2, 8).Value = allResults[i].BENTIX_EQS;
                    eqsSheet.Cell(i + 2, 9).Value = allResults[i].BQI_EQS;
                    eqsSheet.Cell(i + 2, 10).Value = allResults[i].FoRAM_Status;
                }

                // Add Cohen's Kappa Matrix sheet
                AddKappaMatrixSheet(workbook);

                workbook.SaveAs(saveDialog.FileName);
                MessageBox.Show("Results saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Adds Cohen's Kappa Matrix as a sheet to the workbook
        /// </summary>
        private void AddKappaMatrixSheet(XLWorkbook workbook)
        {
            // Get available indices with EQS classifications
            var allEqsIndices = new List<string> { "Foram-AMBI", "FSI", "TSI-Med", "NQIf", "exp(H'bc)", "BENTIX", "BQI", "Foram-M-AMBI" };
            var availableIndices = allEqsIndices.Where(HasIndexData).ToList();

            if (availableIndices.Count < 2)
                return;

            var kappaSheet = workbook.Worksheets.Add("Kappa Matrix");

            // Header row
            kappaSheet.Cell(1, 1).Value = "Index";
            for (int i = 0; i < availableIndices.Count; i++)
            {
                kappaSheet.Cell(1, i + 2).Value = availableIndices[i];
            }

            // Build kappa matrix
            for (int i = 0; i < availableIndices.Count; i++)
            {
                kappaSheet.Cell(i + 2, 1).Value = availableIndices[i];
                for (int j = 0; j < availableIndices.Count; j++)
                {
                    if (i == j)
                    {
                        kappaSheet.Cell(i + 2, j + 2).Value = 1.0;
                    }
                    else
                    {
                        double kappa = CalculateCohensKappa(availableIndices[i], availableIndices[j]);
                        kappaSheet.Cell(i + 2, j + 2).Value = kappa;
                    }

                    // Apply color formatting
                    double kappaValue = i == j ? 1.0 : CalculateCohensKappa(availableIndices[i], availableIndices[j]);
                    var color = GetKappaColor(kappaValue);
                    kappaSheet.Cell(i + 2, j + 2).Style.Fill.BackgroundColor = XLColor.FromColor(color);
                    kappaSheet.Cell(i + 2, j + 2).Style.NumberFormat.Format = "0.000";
                }
            }

            // Format header
            kappaSheet.Range(1, 1, 1, availableIndices.Count + 1).Style.Font.Bold = true;
            kappaSheet.Range(1, 1, 1, availableIndices.Count + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            kappaSheet.Column(1).Style.Font.Bold = true;

            // Auto-fit columns
            kappaSheet.Columns().AdjustToContents();

            // Add interpretation guide sheet
            var interpSheet = workbook.Worksheets.Add("Kappa Interpretation");
            interpSheet.Cell(1, 1).Value = "Kappa Range";
            interpSheet.Cell(1, 2).Value = "Interpretation";
            interpSheet.Cell(1, 1).Style.Font.Bold = true;
            interpSheet.Cell(1, 2).Style.Font.Bold = true;

            interpSheet.Cell(2, 1).Value = "0.81 - 1.00";
            interpSheet.Cell(2, 2).Value = "Almost perfect agreement";
            interpSheet.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(0, 128, 0));

            interpSheet.Cell(3, 1).Value = "0.61 - 0.80";
            interpSheet.Cell(3, 2).Value = "Substantial agreement";
            interpSheet.Cell(3, 1).Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(144, 238, 144));

            interpSheet.Cell(4, 1).Value = "0.41 - 0.60";
            interpSheet.Cell(4, 2).Value = "Moderate agreement";
            interpSheet.Cell(4, 1).Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(255, 255, 150));

            interpSheet.Cell(5, 1).Value = "0.21 - 0.40";
            interpSheet.Cell(5, 2).Value = "Fair agreement";
            interpSheet.Cell(5, 1).Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(255, 200, 100));

            interpSheet.Cell(6, 1).Value = "0.00 - 0.20";
            interpSheet.Cell(6, 2).Value = "Slight agreement";
            interpSheet.Cell(6, 1).Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(255, 150, 150));

            interpSheet.Cell(7, 1).Value = "< 0.00";
            interpSheet.Cell(7, 2).Value = "Poor agreement (worse than chance)";
            interpSheet.Cell(7, 1).Style.Fill.BackgroundColor = XLColor.FromColor(Color.FromArgb(255, 100, 100));

            interpSheet.Cell(9, 1).Value = "Note:";
            interpSheet.Cell(9, 2).Value = "Cohen's Kappa measures agreement between indices beyond chance.";
            interpSheet.Cell(10, 2).Value = "'Azoic' is kept separate from 'Bad' as it indicates no specimens found.";

            interpSheet.Columns().AdjustToContents();
        }

        private void ExportAllPlots_Click(object sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select folder to save plots"
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                string folder = folderDialog.SelectedPath;
                var filtered = GetSelectedResults();
                if (filtered.Count == 0) return;

                // Export diversity indices plot
                var model1 = CreateLinePlot(new List<string> { "exp(H'bc)", "H'log2", "Species Richness (S)" }, filtered);
                model1.Title = "Diversity Indices";
                SavePlotAsPng(model1, Path.Combine(folder, "Diversity_Indices.png"), GetSelectedDpi());

                // Export sensitivity indices plot
                var model2 = CreateLinePlot(new List<string> { "FSI", "NQIf", "F-AMBI", "FIEI" }, filtered);
                model2.Title = "Sensitivity-Based Indices";
                SavePlotAsPng(model2, Path.Combine(folder, "Sensitivity_Indices.png"), GetSelectedDpi());

                // Export eco-groups plot
                var model3 = CreateLinePlot(new List<string> { "Eco1 %", "Eco2 %", "Eco3 %", "Eco4 %", "Eco5 %" }, filtered);
                model3.Title = "Ecological Group Distribution";
                SavePlotAsPng(model3, Path.Combine(folder, "EcoGroups.png"), GetSelectedDpi());

                MessageBox.Show($"Plots exported to:\n{folder}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportCurrentPlot_Click(object sender, EventArgs e)
        {
            var plotViews = GetCurrentPlotViews();
            if (plotViews.Count == 0)
            {
                MessageBox.Show("No plot to export. Generate a plot first.", "No Plot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|PDF Document|*.pdf",
                Title = "Export Plot",
                FileName = "BiotiIndices_Plot"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                string ext = Path.GetExtension(saveDialog.FileName).ToLower();
                int dpi = GetSelectedDpi();

                if (plotViews.Count == 1)
                {
                    if (ext == ".png" || ext == ".jpg")
                    {
                        SavePlotAsPng(plotViews[0].Model, saveDialog.FileName, dpi);
                    }
                    else if (ext == ".pdf")
                    {
                        using var stream = File.Create(saveDialog.FileName);
                        var pdfExporter = new OxyPlot.PdfExporter { Width = 800, Height = 600 };
                        pdfExporter.Export(plotViews[0].Model, stream);
                    }
                }
                else
                {
                    // Composite plots are exported as images of the full panel
                    var size = plotHostPanel.DisplayRectangle.Size;
                    if (size.Width == 0 || size.Height == 0)
                    {
                        size = plotHostPanel.Size;
                    }
                    using var bitmap = new Bitmap(size.Width, size.Height);
                    plotHostPanel.DrawToBitmap(bitmap, new Rectangle(Point.Empty, size));

                    if (ext == ".pdf")
                    {
                        string pngPath = Path.ChangeExtension(saveDialog.FileName, ".png");
                        bitmap.Save(pngPath, System.Drawing.Imaging.ImageFormat.Png);
                        MessageBox.Show($"Composite plots exported as PNG at {pngPath} because PDF export is not supported for multi-panels.", "Export Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (ext == ".jpg")
                        bitmap.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    else
                        bitmap.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }

                MessageBox.Show("Plot exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SavePlotAsPng(PlotModel model, string filePath, int dpi = 300)
        {
            int width = 800 * dpi / 96;
            int height = 600 * dpi / 96;

            var pngExporter = new PngExporter { Width = width, Height = height };

            using var stream = File.Create(filePath);
            pngExporter.Export(model, stream);
        }

        private int GetSelectedDpi()
        {
            if (dpiCombo != null && int.TryParse(dpiCombo.SelectedItem?.ToString(), out int dpi))
            {
                return dpi;
            }
            return 300;
        }

        private void SaveCompositePlot(TableLayoutPanel layout)
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Save Composite Plot",
                FileName = "Composite_BiotiIndices.png"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                using var bitmap = new Bitmap(layout.Width, layout.Height);
                layout.DrawToBitmap(bitmap, new Rectangle(0, 0, layout.Width, layout.Height));
                bitmap.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                MessageBox.Show("Composite plot saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Disabled - About Indices window not in use
        // private void ShowIndicesInfo(object sender, EventArgs e)
        // {
        //     string info = @"BIOTIC INDICES OVERVIEW
        //
        // DIVERSITY INDICES:
        // - exp(H'bc) - Effective Number of Species (Chao & Shen 2003, Bouchet et al. 2012)
        //   Bias-corrected Shannon diversity, expressed as equivalent number of equally-common species.
        //
        // - H'log2 - Shannon-Wiener Index (base 2) (Shannon & Weaver 1963)
        //   Classic diversity measure using log base 2.
        //
        // - H'ln - Shannon-Wiener Index (natural log)
        //   Shannon index using natural logarithm.
        //
        // SENSITIVITY-BASED INDICES:
        // - FSI - Foram Stress Index (Dimiza et al. 2016)
        // - TSI-Med - Tolerant Species Index for Mediterranean (Barras et al. 2014)
        // - NQIf - Norwegian Quality Index for foraminifera (Alve et al. 2019)
        // - FIEI - Foraminiferal Index of Environmental Impact (Mojtahid et al. 2006)
        // - FoRAM Index (Hallock et al. 2003, Prazeres et al. 2020)
        // - Foram-AMBI - Foraminifera AMBI (Alve et al. 2016, Jorissen et al. 2018)
        // - BENTIX - Benthic Index (Simboura & Zenetos 2002)
        // - BQI - Benthic Quality Index (Rosenberg et al. 2004, adapted for foraminifera)
        // - Foram-M-AMBI - Multivariate Foram-AMBI (adapted from Muxika et al. 2007)";
        //
        //     MessageBox.Show(info, "About Biotic Indices", MessageBoxButtons.OK, MessageBoxIcon.Information);
        // }

        /*
        private void ShowReferences(object sender, EventArgs e)
        {
            string refs = @"REFERENCES

Alve E. et al. (2019) Intercalibration of benthic foraminiferal and macrofaunal biotic indices. Ecological Indicators 96:107-115.

Alve E. et al. (2016) Foram-AMBI: A sensitivity index based on benthic foraminiferal faunas. Marine Environmental Research 122:1-12.

Barras C. et al. (2014) Live benthic foraminiferal faunas from the French Mediterranean coast: Towards a new biotic index of environmental quality. Ecological Indicators 36:719-743.

Bouchet V.M.P. et al. (2012) Benthic foraminifera provide a promising tool for ecological quality assessment. Ecological Indicators 23:66-75.

Chao A. & Shen T.J. (2003) Nonparametric estimation of Shannon's index of diversity. Environmental and Ecological Statistics 10:429-443.

Dimiza M.D. et al. (2016) The Foram Stress Index: A new tool for environmental assessment of soft-bottom environments using benthic foraminifera. Ecological Indicators 60:611-621.

Hallock P. et al. (2003) Foraminifera as bioindicators in coral reef assessment and monitoring: The FORAM Index. Environmental Monitoring and Assessment 81:221-238.

Jorissen F.J. et al. (2018) Developing Foram-AMBI for biomonitoring in the Mediterranean. Marine Micropaleontology 140:33-45.

Mojtahid M. et al. (2006) Benthic foraminifera as bio-indicators of drill cutting disposal. Marine Micropaleontology 61:58-75.

Muxika I., Borja Á., Bald J. (2007) Using historical data, expert judgement and multivariate analysis in assessing reference conditions and benthic ecological status, according to the European Water Framework Directive. Marine Pollution Bulletin 55(1-6):16-29.
https://doi.org/10.1016/j.marpolbul.2006.05.025

Prazeres M., Martinez-Colon M., Hallock P. (2020) Foraminifera as bioindicators of water quality: The FoRAM Index revisited. Environmental Pollution 257:113612.
https://doi.org/10.1016/j.envpol.2019.113612

O'Malley B.J. et al. (2021) Development of a benthic foraminifera based marine biotic index (Foram-AMBI) for the Gulf of Mexico: A decision support tool. Ecological Indicators 120:106916.
https://doi.org/10.1016/j.ecolind.2020.107049

Rosenberg R., Blomqvist M., Nilsson H.C., Cederwall H., Dimming A. (2004) Marine quality assessment by use of benthic species-abundance distributions: a proposed new protocol within the European Union Water Framework Directive. Marine Pollution Bulletin 49:728-739.
https://doi.org/10.1016/j.marpolbul.2004.05.013

Simboura N. & Zenetos A. (2002) Benthic indicators to use in Ecological Quality classification of Mediterranean soft bottom marine ecosystems, including a new Biotic Index. Mediterranean Marine Science 3/2:77-111.
https://doi.org/10.12681/mms.266";

            var refsForm = new Form
            {
                Text = "References",
                Size = new Size(650, 500),
                StartPosition = FormStartPosition.CenterParent,
                Icon = this.Icon
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Text = refs,
                Font = new Font("Segoe UI", 10)
            };

            refsForm.Controls.Add(textBox);
            refsForm.ShowDialog(this);
        }
        */

        #region Heatmap Plotting

        /// <summary>
        /// Creates a heatmap plot showing indices values across samples
        /// </summary>
        private PlotModel CreateHeatmapPlot(List<string> selectedIndices, List<IndicesResult> results)
        {
            var model = new PlotModel { Title = "Biotic Indices Heatmap" };

            // Normalize data for better visualization
            var data = new double[selectedIndices.Count, results.Count];
            var minMax = new Dictionary<string, (double min, double max)>();

            // Calculate min/max for each index for normalization
            foreach (var index in selectedIndices)
            {
                var values = results.Select(r => GetIndexValueForResult(index, r)).Where(v => !double.IsNaN(v)).ToList();
                if (values.Count > 0)
                {
                    minMax[index] = (values.Min(), values.Max());
                }
                else
                {
                    minMax[index] = (0, 1);
                }
            }

            // Fill data matrix (normalized 0-1)
            for (int i = 0; i < selectedIndices.Count; i++)
            {
                var (min, max) = minMax[selectedIndices[i]];
                double range = max - min;
                if (range == 0) range = 1;

                for (int j = 0; j < results.Count; j++)
                {
                    double val = GetIndexValueForResult(selectedIndices[i], results[j]);
                    data[i, j] = double.IsNaN(val) ? 0 : (val - min) / range;
                }
            }

            // Create heatmap series
            var heatMapSeries = new OxyPlot.Series.HeatMapSeries
            {
                X0 = 0,
                X1 = results.Count - 1,
                Y0 = 0,
                Y1 = selectedIndices.Count - 1,
                Interpolate = false,
                Data = data,
                RenderMethod = OxyPlot.Series.HeatMapRenderMethod.Rectangles
            };

            model.Series.Add(heatMapSeries);

            // Add color axis
            var colorAxis = new OxyPlot.Axes.LinearColorAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Right,
                Palette = OxyPalettes.Jet(100),
                Title = "Normalized Value",
                Minimum = 0,
                Maximum = 1
            };
            model.Axes.Add(colorAxis);

            // X axis (samples)
            var xAxis = new OxyPlot.Axes.CategoryAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Title = "Samples",
                Angle = 45
            };
            foreach (var r in results)
            {
                xAxis.Labels.Add(r.SampleName.Length > 15 ? r.SampleName.Substring(0, 12) + "..." : r.SampleName);
            }
            model.Axes.Add(xAxis);

            // Y axis (indices)
            var yAxis = new OxyPlot.Axes.CategoryAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Indices"
            };
            foreach (var idx in selectedIndices)
            {
                yAxis.Labels.Add(idx);
            }
            model.Axes.Add(yAxis);

            return model;
        }

        #endregion

        #region EQS Agreement Analysis (Cohen's Kappa & Confusion Matrix)

        // EQS class indices for numerical comparison
        private readonly string[] eqsClasses = { "High", "Good", "Moderate", "Poor", "Bad", "Azoic" };

        // Indices that have EQS classification
        private readonly string[] eqsIndices = { "Foram-AMBI", "FSI", "TSI-Med", "NQIf", "exp(H'bc)", "BENTIX", "BQI", "Foram-M-AMBI" };

        /// <summary>
        /// Shows the EQS Agreement Analysis dialog with Cohen's Kappa and Confusion Matrix
        /// </summary>
        private void ShowEQSAgreementAnalysis()
        {
            if (allResults == null || allResults.Count == 0)
            {
                MessageBox.Show("No data available. Please calculate indices first.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var form = new Form
            {
                Text = "EQS Agreement Analysis (Cohen's Kappa & Confusion Matrix)",
                Size = new Size(1200, 800),
                StartPosition = FormStartPosition.CenterParent,
                WindowState = FormWindowState.Maximized,
                Icon = this.Icon
            };

            var tabControl = new TabControl { Dock = DockStyle.Fill };

            // Tab 1: Cohen's Kappa Matrix
            var kappaTab = new TabPage("Cohen's Kappa Matrix");
            SetupKappaTab(kappaTab);
            tabControl.TabPages.Add(kappaTab);

            // Tab 2: Pairwise Confusion Matrices
            var confusionTab = new TabPage("Confusion Matrices");
            SetupConfusionMatrixTab(confusionTab);
            tabControl.TabPages.Add(confusionTab);

            // Tab 3: Agreement Heatmap
            var heatmapTab = new TabPage("Agreement Heatmap");
            SetupAgreementHeatmapTab(heatmapTab);
            tabControl.TabPages.Add(heatmapTab);

            // Tab 4: Summary Statistics
            var summaryTab = new TabPage("Summary Statistics");
            SetupAgreementSummaryTab(summaryTab);
            tabControl.TabPages.Add(summaryTab);

            form.Controls.Add(tabControl);
            form.ShowDialog(this);
        }

        /// <summary>
        /// Sets up the Cohen's Kappa matrix tab
        /// </summary>
        private void SetupKappaTab(TabPage tab)
        {
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400
            };

            // Kappa matrix grid
            var kappaGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
            };

            // Get available indices with EQS classifications
            var availableEqsIndices = eqsIndices.Where(idx => HasIndexData(idx)).ToList();

            if (availableEqsIndices.Count < 2)
            {
                var label = new Label
                {
                    Text = "At least 2 indices with EQS classifications are needed for agreement analysis.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tab.Controls.Add(label);
                return;
            }

            // Create kappa matrix
            var kappaTable = new DataTable();
            kappaTable.Columns.Add("Index", typeof(string));
            foreach (var idx in availableEqsIndices)
            {
                kappaTable.Columns.Add(idx, typeof(string));
            }

            // Calculate pairwise Kappa
            foreach (var idx1 in availableEqsIndices)
            {
                var row = kappaTable.NewRow();
                row["Index"] = idx1;
                foreach (var idx2 in availableEqsIndices)
                {
                    if (idx1 == idx2)
                    {
                        row[idx2] = "1.00";
                    }
                    else
                    {
                        double kappa = CalculateCohensKappa(idx1, idx2);
                        row[idx2] = kappa.ToString("F3");
                    }
                }
                kappaTable.Rows.Add(row);
            }

            kappaGrid.DataSource = kappaTable;
            kappaGrid.DataBindingComplete += (s, e) =>
            {
                // Color cells based on kappa value
                foreach (DataGridViewRow row in kappaGrid.Rows)
                {
                    for (int i = 1; i < kappaGrid.Columns.Count; i++)
                    {
                        if (double.TryParse(row.Cells[i].Value?.ToString(), out double kappa))
                        {
                            row.Cells[i].Style.BackColor = GetKappaColor(kappa);
                        }
                    }
                }
            };

            splitContainer.Panel1.Controls.Add(kappaGrid);

            // Legend and interpretation
            var legendPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var legendText = new Label
            {
                AutoSize = true,
                Location = new Point(10, 10),
                Text = @"Cohen's Kappa Interpretation:

κ < 0.00     Poor agreement (worse than chance)
0.00 - 0.20  Slight agreement
0.21 - 0.40  Fair agreement
0.41 - 0.60  Moderate agreement
0.61 - 0.80  Substantial agreement
0.81 - 1.00  Almost perfect agreement

The kappa statistic measures inter-rater agreement for categorical items,
accounting for agreement occurring by chance. In this context, it compares
how similarly different biotic indices classify samples into EQS classes
(High, Good, Moderate, Poor, Bad).

Higher kappa values indicate better agreement between indices in their
ecological quality assessments."
            };
            legendPanel.Controls.Add(legendText);

            // Add export button
            var exportBtn = new Button
            {
                Text = "Export to Excel",
                Location = new Point(10, 200),
                Size = new Size(120, 30)
            };
            exportBtn.Click += (s, e) => ExportKappaMatrixToExcel(kappaTable, availableEqsIndices);
            legendPanel.Controls.Add(exportBtn);

            splitContainer.Panel2.Controls.Add(legendPanel);
            tab.Controls.Add(splitContainer);
        }

        /// <summary>
        /// Sets up the confusion matrix tab for pairwise comparisons
        /// </summary>
        private void SetupConfusionMatrixTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill };

            // Index selection
            var selectionPanel = new Panel { Dock = DockStyle.Top, Height = 80 };

            var label1 = new Label { Text = "Index 1:", Location = new Point(10, 15), AutoSize = true };
            var combo1 = new ComboBox { Location = new Point(80, 12), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            var label2 = new Label { Text = "Index 2:", Location = new Point(300, 15), AutoSize = true };
            var combo2 = new ComboBox { Location = new Point(370, 12), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            var generateBtn = new Button { Text = "Generate Confusion Matrix", Location = new Point(590, 10), Size = new Size(180, 30) };

            var availableEqsIndices = eqsIndices.Where(idx => HasIndexData(idx)).ToArray();
            combo1.Items.AddRange(availableEqsIndices);
            combo2.Items.AddRange(availableEqsIndices);
            if (combo1.Items.Count > 0) combo1.SelectedIndex = 0;
            if (combo2.Items.Count > 1) combo2.SelectedIndex = 1;

            selectionPanel.Controls.AddRange(new Control[] { label1, combo1, label2, combo2, generateBtn });

            // Results panel
            var resultsSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 500
            };

            var confusionGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                Name = "confusionGrid"
            };

            var statsLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Font = new Font("Consolas", 10),
                Name = "statsLabel"
            };

            resultsSplit.Panel1.Controls.Add(confusionGrid);
            resultsSplit.Panel2.Controls.Add(statsLabel);

            generateBtn.Click += (s, e) =>
            {
                if (combo1.SelectedItem == null || combo2.SelectedItem == null)
                {
                    MessageBox.Show("Please select both indices.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string idx1 = combo1.SelectedItem.ToString();
                string idx2 = combo2.SelectedItem.ToString();

                GenerateConfusionMatrix(idx1, idx2, confusionGrid, statsLabel);
            };

            mainPanel.Controls.Add(resultsSplit);
            mainPanel.Controls.Add(selectionPanel);
            resultsSplit.BringToFront();

            tab.Controls.Add(mainPanel);
        }

        /// <summary>
        /// Generates a confusion matrix between two indices
        /// </summary>
        private void GenerateConfusionMatrix(string index1, string index2, DataGridView grid, Label statsLabel)
        {
            var classifications1 = allResults.Select(r => GetEQSClass(r, index1)).ToList();
            var classifications2 = allResults.Select(r => GetEQSClass(r, index2)).ToList();

            // Create confusion matrix
            var usedClasses = classifications1.Concat(classifications2).Distinct().OrderBy(c => Array.IndexOf(eqsClasses, c)).ToList();

            var confusionTable = new DataTable();
            confusionTable.Columns.Add($"{index1} \\ {index2}", typeof(string));
            foreach (var cls in usedClasses)
            {
                confusionTable.Columns.Add(cls, typeof(int));
            }
            confusionTable.Columns.Add("Total", typeof(int));

            int[,] matrix = new int[usedClasses.Count, usedClasses.Count];
            int[] rowTotals = new int[usedClasses.Count];
            int[] colTotals = new int[usedClasses.Count];

            for (int i = 0; i < allResults.Count; i++)
            {
                int row = usedClasses.IndexOf(classifications1[i]);
                int col = usedClasses.IndexOf(classifications2[i]);
                if (row >= 0 && col >= 0)
                {
                    matrix[row, col]++;
                    rowTotals[row]++;
                    colTotals[col]++;
                }
            }

            // Fill table
            for (int i = 0; i < usedClasses.Count; i++)
            {
                var row = confusionTable.NewRow();
                row[0] = usedClasses[i];
                for (int j = 0; j < usedClasses.Count; j++)
                {
                    row[j + 1] = matrix[i, j];
                }
                row[usedClasses.Count + 1] = rowTotals[i];
                confusionTable.Rows.Add(row);
            }

            // Add totals row
            var totalsRow = confusionTable.NewRow();
            totalsRow[0] = "Total";
            for (int j = 0; j < usedClasses.Count; j++)
            {
                totalsRow[j + 1] = colTotals[j];
            }
            totalsRow[usedClasses.Count + 1] = allResults.Count;
            confusionTable.Rows.Add(totalsRow);

            grid.DataSource = confusionTable;

            // Color diagonal cells
            grid.DataBindingComplete += (s, e) =>
            {
                for (int i = 0; i < usedClasses.Count; i++)
                {
                    if (i < grid.Rows.Count - 1)
                    {
                        grid.Rows[i].Cells[i + 1].Style.BackColor = Color.LightGreen;
                    }
                }
            };

            // Calculate statistics
            double kappa = CalculateCohensKappa(index1, index2);
            int agreement = 0;
            for (int i = 0; i < usedClasses.Count; i++)
            {
                agreement += matrix[i, i];
            }
            double percentAgreement = (double)agreement / allResults.Count * 100;

            statsLabel.Text = $@"Agreement Statistics for {index1} vs {index2}:

Samples Analyzed: {allResults.Count}
Exact Agreement: {agreement} ({percentAgreement:F1}%)
Cohen's Kappa: {kappa:F3}

Kappa Interpretation:
{GetKappaInterpretation(kappa)}

Note: Diagonal cells (highlighted green) show samples
where both indices assigned the same EQS class.";
        }

        // Store current heatmap bitmap for export
        private Bitmap _currentKappaHeatmap;

        /// <summary>
        /// Sets up the agreement heatmap tab
        /// </summary>
        private void SetupAgreementHeatmapTab(TabPage tab)
        {
            var availableEqsIndices = eqsIndices.Where(idx => HasIndexData(idx)).ToList();

            if (availableEqsIndices.Count < 2)
            {
                var label = new Label
                {
                    Text = "At least 2 indices with EQS classifications are needed for heatmap.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tab.Controls.Add(label);
                return;
            }

            // Create main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            // Create scrollable panel for the heatmap
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            // Create PictureBox for the heatmap
            var pictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                BackColor = Color.White
            };

            // Generate heatmap bitmap
            _currentKappaHeatmap = CreateKappaHeatmapBitmap(availableEqsIndices);
            pictureBox.Image = _currentKappaHeatmap;

            scrollPanel.Controls.Add(pictureBox);
            mainPanel.Controls.Add(scrollPanel, 0, 0);

            // Create export button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };

            var exportButton = new Button
            {
                Text = "Export as PNG",
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5)
            };
            exportButton.Click += (s, e) => ExportKappaHeatmapToPng();
            buttonPanel.Controls.Add(exportButton);

            mainPanel.Controls.Add(buttonPanel, 0, 1);
            tab.Controls.Add(mainPanel);
        }

        /// <summary>
        /// Creates a bitmap heatmap of Cohen's Kappa values between all pairs of indices
        /// </summary>
        private Bitmap CreateKappaHeatmapBitmap(List<string> indices)
        {
            int n = indices.Count;
            var data = new double[n, n];

            // Calculate kappa values
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        data[i, j] = 1.0;
                    }
                    else
                    {
                        data[i, j] = CalculateCohensKappa(indices[i], indices[j]);
                    }
                }
            }

            // Sizing constants
            int cellSize = 70;
            int leftMargin = 120;  // Space for Y-axis labels
            int topMargin = 50;   // Space for title
            int bottomMargin = 100; // Space for X-axis labels (rotated)
            int rightMargin = 100;  // Space for color legend
            int legendWidth = 30;
            int legendHeight = n * cellSize;

            int width = leftMargin + n * cellSize + rightMargin;
            int height = topMargin + n * cellSize + bottomMargin;

            var bitmap = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var titleFont = new Font("Segoe UI", 14, FontStyle.Bold);
                var labelFont = new Font("Segoe UI", 9);
                var valueFont = new Font("Segoe UI", 9, FontStyle.Bold);
                var legendFont = new Font("Segoe UI", 8);

                // Draw title
                var title = "Cohen's Kappa Agreement Heatmap";
                var titleSize = g.MeasureString(title, titleFont);
                g.DrawString(title, titleFont, Brushes.Black,
                    (width - rightMargin / 2 - titleSize.Width) / 2, 10);

                // Draw heatmap cells
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        double kappa = data[i, j];
                        var cellColor = GetKappaHeatmapColor(kappa);

                        int x = leftMargin + j * cellSize;
                        int y = topMargin + i * cellSize;

                        using (var brush = new SolidBrush(cellColor))
                        {
                            g.FillRectangle(brush, x, y, cellSize, cellSize);
                        }

                        // Draw cell border
                        g.DrawRectangle(Pens.DarkGray, x, y, cellSize, cellSize);

                        // Draw kappa value text
                        string valueText = kappa.ToString("F2");
                        var textSize = g.MeasureString(valueText, valueFont);

                        // Choose text color based on background brightness
                        var textColor = GetContrastingTextColor(cellColor);
                        using (var textBrush = new SolidBrush(textColor))
                        {
                            g.DrawString(valueText, valueFont, textBrush,
                                x + (cellSize - textSize.Width) / 2,
                                y + (cellSize - textSize.Height) / 2);
                        }
                    }
                }

                // Draw Y-axis labels (index names on left)
                for (int i = 0; i < n; i++)
                {
                    int y = topMargin + i * cellSize + cellSize / 2;
                    var labelSize = g.MeasureString(indices[i], labelFont);
                    g.DrawString(indices[i], labelFont, Brushes.Black,
                        leftMargin - labelSize.Width - 5,
                        y - labelSize.Height / 2);
                }

                // Draw X-axis labels (index names on bottom, rotated)
                for (int j = 0; j < n; j++)
                {
                    int x = leftMargin + j * cellSize + cellSize / 2;
                    int y = topMargin + n * cellSize + 5;

                    var state = g.Save();
                    g.TranslateTransform(x, y);
                    g.RotateTransform(45);
                    g.DrawString(indices[j], labelFont, Brushes.Black, 0, 0);
                    g.Restore(state);
                }

                // Draw color legend
                int legendX = leftMargin + n * cellSize + 20;
                int legendY = topMargin;

                // Draw legend gradient
                for (int i = 0; i < legendHeight; i++)
                {
                    double value = 1.0 - (double)i / legendHeight * 1.2 - 0.2; // Range from 1.0 to -0.2
                    value = Math.Max(-0.2, Math.Min(1.0, value));
                    var color = GetKappaHeatmapColor(value);
                    using (var pen = new Pen(color))
                    {
                        g.DrawLine(pen, legendX, legendY + i, legendX + legendWidth, legendY + i);
                    }
                }

                // Draw legend border
                g.DrawRectangle(Pens.Black, legendX, legendY, legendWidth, legendHeight);

                // Draw legend labels
                var legendLabels = new[] { "1.0", "0.8", "0.6", "0.4", "0.2", "0.0", "-0.2" };
                for (int i = 0; i < legendLabels.Length; i++)
                {
                    double value = 1.0 - i * 0.2;
                    int labelY = legendY + (int)((1.0 - value) / 1.2 * legendHeight);
                    g.DrawLine(Pens.Black, legendX + legendWidth, labelY, legendX + legendWidth + 3, labelY);
                    g.DrawString(legendLabels[i], legendFont, Brushes.Black, legendX + legendWidth + 5, labelY - 6);
                }

                // Draw legend title
                var legendTitle = "Cohen's Kappa";
                var state2 = g.Save();
                g.TranslateTransform(legendX + legendWidth + 35, legendY + legendHeight / 2);
                g.RotateTransform(-90);
                var legendTitleSize = g.MeasureString(legendTitle, labelFont);
                g.DrawString(legendTitle, labelFont, Brushes.Black, -legendTitleSize.Width / 2, 0);
                g.Restore(state2);

                // Dispose fonts
                titleFont.Dispose();
                labelFont.Dispose();
                valueFont.Dispose();
                legendFont.Dispose();
            }

            return bitmap;
        }

        /// <summary>
        /// Gets a color for a kappa value using a blue-white-red gradient
        /// </summary>
        private Color GetKappaHeatmapColor(double kappa)
        {
            // Clamp to valid range
            kappa = Math.Max(-0.2, Math.Min(1.0, kappa));

            // Normalize to 0-1 range where 0 = -0.2 and 1 = 1.0
            double normalized = (kappa + 0.2) / 1.2;

            // Blue (low) -> White (middle) -> Red (high) gradient
            // But for Kappa, high is good, so we use: Red (low/poor) -> White (middle) -> Blue (high/good)
            int r, gr, b;

            if (normalized < 0.5)
            {
                // Red to White
                double t = normalized * 2;
                r = 255;
                gr = (int)(255 * t);
                b = (int)(255 * t);
            }
            else
            {
                // White to Blue
                double t = (normalized - 0.5) * 2;
                r = (int)(255 * (1 - t));
                gr = (int)(255 * (1 - t));
                b = 255;
            }

            return Color.FromArgb(r, gr, b);
        }

        /// <summary>
        /// Gets a contrasting text color (black or white) based on background brightness
        /// </summary>
        private Color GetContrastingTextColor(Color background)
        {
            // Calculate relative luminance
            double luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255;
            return luminance > 0.5 ? Color.Black : Color.White;
        }

        /// <summary>
        /// Exports the current kappa heatmap to a PNG file
        /// </summary>
        private void ExportKappaHeatmapToPng()
        {
            if (_currentKappaHeatmap == null)
            {
                MessageBox.Show("No heatmap available to export.", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Image|*.png";
                saveDialog.Title = "Export Kappa Heatmap";
                saveDialog.FileName = "Kappa_Heatmap.png";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _currentKappaHeatmap.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        MessageBox.Show($"Heatmap exported successfully to:\n{saveDialog.FileName}",
                            "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting heatmap: {ex.Message}",
                            "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Sets up the agreement summary tab
        /// </summary>
        private void SetupAgreementSummaryTab(TabPage tab)
        {
            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                WordWrap = false
            };

            var availableEqsIndices = eqsIndices.Where(idx => HasIndexData(idx)).ToList();
            var sb = new StringBuilder();

            sb.AppendLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║              EQS AGREEMENT ANALYSIS SUMMARY                                  ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Samples analyzed: {allResults.Count}");
            sb.AppendLine($"Indices compared: {string.Join(", ", availableEqsIndices)}");
            sb.AppendLine();

            // EQS distribution per index
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("EQS CLASS DISTRIBUTION BY INDEX:");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            foreach (var idx in availableEqsIndices)
            {
                var classifications = allResults.Select(r => GetEQSClass(r, idx)).ToList();
                var grouped = classifications.GroupBy(c => c).OrderBy(g => Array.IndexOf(eqsClasses, g.Key));

                sb.AppendLine($"{idx}:");
                foreach (var g in grouped)
                {
                    double pct = (double)g.Count() / allResults.Count * 100;
                    string bar = new string('█', (int)(pct / 5));
                    sb.AppendLine($"  {g.Key,-10} {g.Count(),4} ({pct,5:F1}%) {bar}");
                }
                sb.AppendLine();
            }

            // Pairwise Kappa summary
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("PAIRWISE COHEN'S KAPPA VALUES:");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            var kappaPairs = new List<(string pair, double kappa)>();
            for (int i = 0; i < availableEqsIndices.Count; i++)
            {
                for (int j = i + 1; j < availableEqsIndices.Count; j++)
                {
                    double kappa = CalculateCohensKappa(availableEqsIndices[i], availableEqsIndices[j]);
                    kappaPairs.Add(($"{availableEqsIndices[i]} vs {availableEqsIndices[j]}", kappa));
                }
            }

            foreach (var pair in kappaPairs.OrderByDescending(p => p.kappa))
            {
                sb.AppendLine($"{pair.pair,-40} κ = {pair.kappa,6:F3}  ({GetKappaInterpretation(pair.kappa)})");
            }

            if (kappaPairs.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Mean Kappa: {kappaPairs.Average(p => p.kappa):F3}");
                sb.AppendLine($"Best agreement: {kappaPairs.OrderByDescending(p => p.kappa).First().pair}");
                sb.AppendLine($"Worst agreement: {kappaPairs.OrderBy(p => p.kappa).First().pair}");
            }

            // Overall consensus
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("CONSENSUS ANALYSIS:");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            int fullConsensus = 0;
            int majorityConsensus = 0;
            foreach (var result in allResults)
            {
                var classes = availableEqsIndices.Select(idx => GetEQSClass(result, idx)).ToList();
                var majority = classes.GroupBy(c => c).OrderByDescending(g => g.Count()).First();
                if (majority.Count() == classes.Count)
                {
                    fullConsensus++;
                }
                else if (majority.Count() > classes.Count / 2.0)
                {
                    majorityConsensus++;
                }
            }

            sb.AppendLine($"Full consensus (all indices agree):     {fullConsensus,4} ({(double)fullConsensus / allResults.Count * 100:F1}%)");
            sb.AppendLine($"Majority consensus (>50% agree):        {majorityConsensus,4} ({(double)majorityConsensus / allResults.Count * 100:F1}%)");
            sb.AppendLine($"No clear consensus:                     {allResults.Count - fullConsensus - majorityConsensus,4} ({(double)(allResults.Count - fullConsensus - majorityConsensus) / allResults.Count * 100:F1}%)");

            textBox.Text = sb.ToString();
            tab.Controls.Add(textBox);
        }

        /// <summary>
        /// Calculates Cohen's Kappa between two indices
        /// </summary>
        private double CalculateCohensKappa(string index1, string index2)
        {
            var classifications1 = allResults.Select(r => GetEQSClass(r, index1)).ToList();
            var classifications2 = allResults.Select(r => GetEQSClass(r, index2)).ToList();

            int n = allResults.Count;
            if (n == 0) return 0;

            // Get all unique classes
            var allClasses = classifications1.Concat(classifications2).Distinct().ToList();
            int k = allClasses.Count;

            // Create confusion matrix
            int[,] matrix = new int[k, k];
            for (int i = 0; i < n; i++)
            {
                int row = allClasses.IndexOf(classifications1[i]);
                int col = allClasses.IndexOf(classifications2[i]);
                if (row >= 0 && col >= 0)
                {
                    matrix[row, col]++;
                }
            }

            // Calculate observed agreement (Po)
            double po = 0;
            for (int i = 0; i < k; i++)
            {
                po += matrix[i, i];
            }
            po /= n;

            // Calculate expected agreement (Pe)
            double pe = 0;
            for (int i = 0; i < k; i++)
            {
                double rowSum = 0, colSum = 0;
                for (int j = 0; j < k; j++)
                {
                    rowSum += matrix[i, j];
                    colSum += matrix[j, i];
                }
                pe += (rowSum / n) * (colSum / n);
            }

            // Cohen's Kappa
            if (pe >= 1.0) return 1.0;
            return (po - pe) / (1 - pe);
        }

        /// <summary>
        /// Gets the EQS classification for a result based on the specified index
        /// </summary>
        private string GetEQSClass(IndicesResult result, string index)
        {
            return index switch
            {
                "Foram-AMBI" or "F-AMBI" => result.FAMBI_EQS,
                "FSI" => result.FSI_EQS,
                "TSI-Med" => result.TSIMed_EQS,
                "NQIf" => result.NQI_EQS,
                "exp(H'bc)" => result.ExpHbc_EQS,
                "BENTIX" => result.BENTIX_EQS,
                "BQI" => result.BQI_EQS,
                "Foram-M-AMBI" => result.ForamMAMBI_EQS,
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Checks if the specified index has data available
        /// </summary>
        private bool HasIndexData(string index)
        {
            if (allResults == null || allResults.Count == 0) return false;

            return index switch
            {
                "Foram-AMBI" or "F-AMBI" => allResults.Any(r => r.FAMBI > 0),
                "FSI" => allResults.Any(r => r.FSI > 0),
                "TSI-Med" => allResults.Any(r => r.TSI_Med >= 0),
                "NQIf" => allResults.Any(r => r.NQIf > 0),
                "exp(H'bc)" => allResults.Any(r => r.Exp_Hbc > 0),
                "BENTIX" => allResults.Any(r => r.BENTIX > 0),
                "BQI" => allResults.Any(r => r.BQI > 0),
                "Foram-M-AMBI" => allResults.Any(r => r.ForamMAMBI > 0),
                _ => false
            };
        }

        /// <summary>
        /// Gets a color based on kappa value
        /// </summary>
        private Color GetKappaColor(double kappa)
        {
            if (kappa >= 0.81) return Color.FromArgb(0, 128, 0);       // Dark green - Almost perfect
            if (kappa >= 0.61) return Color.FromArgb(144, 238, 144);   // Light green - Substantial
            if (kappa >= 0.41) return Color.FromArgb(255, 255, 150);   // Light yellow - Moderate
            if (kappa >= 0.21) return Color.FromArgb(255, 200, 100);   // Orange - Fair
            if (kappa >= 0.00) return Color.FromArgb(255, 150, 150);   // Light red - Slight
            return Color.FromArgb(255, 100, 100);                       // Red - Poor
        }

        /// <summary>
        /// Gets the interpretation text for a kappa value
        /// </summary>
        private string GetKappaInterpretation(double kappa)
        {
            if (kappa >= 0.81) return "Almost perfect";
            if (kappa >= 0.61) return "Substantial";
            if (kappa >= 0.41) return "Moderate";
            if (kappa >= 0.21) return "Fair";
            if (kappa >= 0.00) return "Slight";
            return "Poor";
        }

        /// <summary>
        /// Exports the Kappa matrix to Excel
        /// </summary>
        private void ExportKappaMatrixToExcel(DataTable kappaTable, List<string> indices)
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Export Kappa Matrix",
                FileName = "EQS_Agreement_Kappa_Matrix.xlsx"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Kappa Matrix");

                // Header
                ws.Cell(1, 1).Value = "Index";
                for (int i = 0; i < indices.Count; i++)
                {
                    ws.Cell(1, i + 2).Value = indices[i];
                }

                // Data
                for (int row = 0; row < kappaTable.Rows.Count; row++)
                {
                    for (int col = 0; col < kappaTable.Columns.Count; col++)
                    {
                        ws.Cell(row + 2, col + 1).Value = kappaTable.Rows[row][col].ToString();
                    }
                }

                // Formatting
                ws.Range(1, 1, 1, indices.Count + 1).Style.Font.Bold = true;
                ws.Range(1, 1, 1, indices.Count + 1).Style.Fill.BackgroundColor = XLColor.LightGray;

                // Color cells based on kappa value
                for (int row = 0; row < indices.Count; row++)
                {
                    for (int col = 0; col < indices.Count; col++)
                    {
                        if (double.TryParse(kappaTable.Rows[row][col + 1]?.ToString(), out double kappa))
                        {
                            var color = GetKappaColor(kappa);
                            ws.Cell(row + 2, col + 2).Style.Fill.BackgroundColor = XLColor.FromColor(color);
                        }
                    }
                }

                // Add interpretation guide
                var ws2 = workbook.Worksheets.Add("Interpretation");
                ws2.Cell(1, 1).Value = "Kappa Range";
                ws2.Cell(1, 2).Value = "Interpretation";
                ws2.Cell(2, 1).Value = "0.81 - 1.00";
                ws2.Cell(2, 2).Value = "Almost perfect agreement";
                ws2.Cell(3, 1).Value = "0.61 - 0.80";
                ws2.Cell(3, 2).Value = "Substantial agreement";
                ws2.Cell(4, 1).Value = "0.41 - 0.60";
                ws2.Cell(4, 2).Value = "Moderate agreement";
                ws2.Cell(5, 1).Value = "0.21 - 0.40";
                ws2.Cell(5, 2).Value = "Fair agreement";
                ws2.Cell(6, 1).Value = "0.00 - 0.20";
                ws2.Cell(6, 2).Value = "Slight agreement";
                ws2.Cell(7, 1).Value = "< 0.00";
                ws2.Cell(7, 2).Value = "Poor agreement (worse than chance)";

                workbook.SaveAs(saveDialog.FileName);
                MessageBox.Show("Kappa matrix exported successfully!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion
    }
}
