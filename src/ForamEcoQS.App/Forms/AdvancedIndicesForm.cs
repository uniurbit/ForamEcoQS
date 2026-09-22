//MIT License
// AdvancedIndicesForm.cs - Calculates and visualises the advanced biotic indices.

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
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Eto;
using OxyPlot.Legends;
using OxyPlot.Series;
using DataGridView = ForamEcoQS.Compat.DataGridView;
using MessageBox = ForamEcoQS.Compat.MessageBox;
using VerticalAlignment = Eto.Forms.VerticalAlignment;
using HorizontalAlignment = Eto.Forms.HorizontalAlignment;

namespace ForamEcoQS
{
    public class AdvancedIndicesForm : Form
    {
        private DataTable _resultsTable;
        private readonly List<IndicesResult> _allResults = new List<IndicesResult>();

        // Specialized databanks for FSI, TSI-Med and FoRAM Index
        private Dictionary<string, string> _fsiDatabank;
        private HashSet<string> _tsiMedDatabank;
        private Dictionary<string, string> _foramDatabank;
        private Dictionary<string, double> _mudPercentages;
        private bool _fsiDatabankAvailable;
        private bool _tsiMedDatabankAvailable;
        private bool _foramDatabankAvailable;
        private bool _fambiAvailable;
        private bool _calculateFoRAMIndex;

        private FAMBIThresholdType _selectedFAMBIThreshold = FAMBIThresholdType.Borja2003;
        private TSIReferenceType _selectedTSIReference = TSIReferenceType.Barras2014_150um;
        private TSIThresholdType _selectedTSIThreshold = TSIThresholdType.Parent2021;
        private ExpHbcThresholdType _selectedExpHbcThreshold = ExpHbcThresholdType.OBrien2021_Norwegian63um;

        private readonly DataGridView _dataGridIndices;
        private readonly DataGridView _eqsSummaryGrid;
        private readonly TabControl _tabControl;
        private readonly CheckedListBox _indexSelectionList;
        private readonly CheckedListBox _sampleSelectionList;
        private readonly DropDown _plotTypeCombo;
        private readonly DropDown _dpiCombo;
        private readonly DropDown _fontCombo;
        private readonly DropDown _colorSchemeCombo;
        private readonly CheckBox _gridCheckbox;
        private readonly Panel _plotHostPanel;

        /// <summary>Plot models currently shown in the preview pane, in layout order.</summary>
        private readonly List<PlotModel> _currentPlotModels = new List<PlotModel>();
        private int _currentPlotColumns = 1;

        // Available indices for plotting
        private readonly string[] _availableIndices =
        {
            "exp(H'bc)", "H'log2", "H'ln", "FSI", "TSI-Med", "NQIf", "FIEI", "Foram-AMBI", "Foram-M-AMBI",
            "BENTIX", "BQI", "FoRAM Index",
            "Species Richness (S)", "Total Abundance (N)", "Simpson (1-D)", "Pielou's J", "ES100",
            "Eco1 %", "Eco2 %", "Eco3 %", "Eco4 %", "Eco5 %",
            "FoRAM Symbiont %", "FoRAM Stress-Tolerant %", "FoRAM Heterotrophic %"
        };

        public AdvancedIndicesForm()
        {
            Title = "Advanced Biotic Indices Calculator";
            ClientSize = new Size(1400, 900);
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
                            new Command((s, e) => SaveResults()) { MenuText = "Save Results to Excel" },
                            new Command((s, e) => ExportAllPlots()) { MenuText = "Export All Plots" },
                            new SeparatorMenuItem(),
                            new Command((s, e) => Close()) { MenuText = "Close" }
                        }
                    },
                    new SubMenuItem
                    {
                        Text = "&Plots",
                        Items =
                        {
                            new Command((s, e) => GeneratePlot("Bar Chart")) { MenuText = "Bar Chart" },
                            new Command((s, e) => GeneratePlot("Line Plot")) { MenuText = "Line Plot" },
                            new Command((s, e) => GeneratePlot("Box Plot")) { MenuText = "Box Plot" },
                            new Command((s, e) => GeneratePlot("Scatter Plot")) { MenuText = "Scatter Plot" },
                            new Command((s, e) => GeneratePlot("Heatmap")) { MenuText = "Heatmap" },
                            new SeparatorMenuItem(),
                            new Command((s, e) => GenerateEcoGroupsPlot()) { MenuText = "Eco Groups Distribution" },
                            new Command((s, e) => GenerateCompositePlot()) { MenuText = "Composite Panel" },
                            new Command((s, e) => GenerateEQSPlot()) { MenuText = "EQS Classification" },
                            new Command((s, e) => ShowEQSAgreementAnalysis()) { MenuText = "EQS Agreement Analysis..." }
                        }
                    }
                }
            };

            // ---- Tab 1: results data ----
            _dataGridIndices = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = Compat.DataGridViewAutoSizeColumnsMode.AllCells
            };

            // ---- Tab 2: plot options ----
            _indexSelectionList = new CheckedListBox { Height = 300 };
            _indexSelectionList.Items.AddRange(_availableIndices);

            _sampleSelectionList = new CheckedListBox { Height = 120 };

            _plotTypeCombo = new DropDown();
            foreach (var type in new[]
                     {
                         "Bar Chart", "Line Plot", "Box Plot", "Scatter Plot", "Heatmap",
                         "Grouped Bar", "Eco Groups", "Composite Panel"
                     })
            {
                _plotTypeCombo.Items.Add(type);
            }
            _plotTypeCombo.SelectedIndex = 0;

            _dpiCombo = new DropDown { Width = 90 };
            foreach (var dpi in new[] { "150", "300", "600", "1200" })
            {
                _dpiCombo.Items.Add(dpi);
            }
            _dpiCombo.SelectedIndex = 1;

            _fontCombo = new DropDown { Width = 90 };
            foreach (var size in new[] { "8", "10", "12", "14", "16" })
            {
                _fontCombo.Items.Add(size);
            }
            _fontCombo.SelectedIndex = 2;

            _colorSchemeCombo = new DropDown { Width = 170 };
            foreach (var scheme in new[]
                     {
                         "Scientific (Blue-Red)", "Grayscale", "Colorblind Safe", "Nature Style", "Custom Gradient"
                     })
            {
                _colorSchemeCombo.Items.Add(scheme);
            }
            _colorSchemeCombo.SelectedIndex = 0;

            _gridCheckbox = new CheckBox { Text = "Show Grid Lines", Checked = true };

            _plotHostPanel = new Panel { BackgroundColor = Colors.White };

            var plotOptionsTab = new TabPage { Text = "Plot Options", Content = BuildPlotOptionsTab() };

            // ---- Tab 3: EQS summary ----
            _eqsSummaryGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = Compat.DataGridViewAutoSizeColumnsMode.AllCells
            };

            _tabControl = new TabControl
            {
                Style = "advanced-results-tabs",
                Pages =
                {
                    new TabPage { Text = "Results Data", Content = _dataGridIndices },
                    plotOptionsTab,
                    new TabPage { Text = "EQS Summary", Content = _eqsSummaryGrid }
                }
            };

            Content = _tabControl;
        }

        private Control BuildPlotOptionsTab()
        {
            var selectAllButton = new Button { Text = "Select All", Width = 90 };
            selectAllButton.Click += (s, e) => SetAllIndices(true);

            var selectNoneButton = new Button { Text = "Clear All", Width = 90 };
            selectNoneButton.Click += (s, e) => SetAllIndices(false);

            var selectAllSamplesButton = new Button { Text = "Select All Samples", Width = 130 };
            selectAllSamplesButton.Click += (s, e) => SetAllSamples(true);

            var clearSamplesButton = new Button { Text = "Clear Samples", Width = 130 };
            clearSamplesButton.Click += (s, e) => SetAllSamples(false);

            var generatePlotButton = UiHelpers.ActionButton("Generate Plot", AppColors.SeaGreen, null, 130);
            generatePlotButton.Click += (s, e) => GeneratePlot(SelectedPlotType());

            var exportPlotButton = UiHelpers.ActionButton("Export Plot", AppColors.SteelBlue, null, 130);
            exportPlotButton.Click += (s, e) => ExportCurrentPlot();

            var settingsGroup = new GroupBox
            {
                Text = "Plot Settings (Publication Quality)",
                Content = new TableLayout
                {
                    Padding = new Padding(10),
                    Spacing = new Size(6, 6),
                    Rows =
                    {
                        new TableRow(new Label { Text = "Export DPI:", VerticalAlignment = VerticalAlignment.Center },
                                     new TableCell(_dpiCombo, true)),
                        new TableRow(new Label { Text = "Font Size:", VerticalAlignment = VerticalAlignment.Center },
                                     new TableCell(_fontCombo, true)),
                        new TableRow(new Label { Text = "Color Scheme:", VerticalAlignment = VerticalAlignment.Center },
                                     new TableCell(_colorSchemeCombo, true)),
                        new TableRow(new TableCell(_gridCheckbox, true), null)
                    }
                }
            };

            var optionsPanel = new Scrollable
            {
                Border = BorderType.None,
                Content = new TableLayout
                {
                    Padding = new Padding(10),
                    Spacing = new Size(6, 6),
                    Rows =
                    {
                        new TableRow(new Label { Text = "Select Indices to Plot:" }),
                        new TableRow(_indexSelectionList) { ScaleHeight = true },
                        new TableRow(new TableCell(new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 6,
                            Items = { selectAllButton, selectNoneButton }
                        }, true)),
                        new TableRow(new Label { Text = "Samples to plot:" }),
                        new TableRow(_sampleSelectionList),
                        new TableRow(new TableCell(new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 6,
                            Items = { selectAllSamplesButton, clearSamplesButton }
                        }, true)),
                        new TableRow(new Label { Text = "Plot Type:" }),
                        new TableRow(_plotTypeCombo),
                        new TableRow(new TableCell(new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 6,
                            Items = { generatePlotButton, exportPlotButton }
                        }, true)),
                        new TableRow(settingsGroup),
                        null
                    }
                }
            };

            return new Splitter
            {
                Orientation = Orientation.Horizontal,
                FixedPanel = SplitterFixedPanel.Panel1,
                Position = 310,
                Panel1 = optionsPanel,
                Panel2 = _plotHostPanel
            };
        }

        private string SelectedPlotType() =>
            _plotTypeCombo.SelectedIndex >= 0 ? _plotTypeCombo.Items[_plotTypeCombo.SelectedIndex].Text : "Bar Chart";

        private void SetAllIndices(bool state)
        {
            for (int i = 0; i < _indexSelectionList.Items.Count; i++)
            {
                _indexSelectionList.SetItemChecked(i, state);
            }
        }

        private void SetAllSamples(bool state)
        {
            for (int i = 0; i < _sampleSelectionList.Items.Count; i++)
            {
                _sampleSelectionList.SetItemChecked(i, state);
            }
        }

        public void ConfigureIndexSettings(
            FAMBIThresholdType fambiThreshold,
            TSIReferenceType tsiReference,
            TSIThresholdType tsiThreshold,
            ExpHbcThresholdType expHbcThreshold)
        {
            _selectedFAMBIThreshold = fambiThreshold;
            _selectedTSIReference = tsiReference;
            _selectedTSIThreshold = tsiThreshold;
            _selectedExpHbcThreshold = expHbcThreshold;
        }

        #region Loading results

        public void LoadResults(DataTable sourceData, DataTable databank)
        {
            LoadResults(sourceData, databank, null, null);
        }

        /// <summary>
        /// Loads pre-calculated indices from an exported Excel file, for plotting only.
        /// </summary>
        public void LoadIndicesFromExcel(DataTable indicesData)
        {
            if (indicesData == null || indicesData.Rows.Count == 0 || indicesData.Columns.Count < 2)
            {
                MessageBox.Show("Invalid or empty data file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _allResults.Clear();
            _resultsTable = indicesData.Copy();
            _fambiAvailable = true; // Assume indices are available for plotting

            // First column is the index name, other columns are samples.
            for (int col = 1; col < indicesData.Columns.Count; col++)
            {
                var result = new IndicesResult
                {
                    SampleName = indicesData.Columns[col].ColumnName,
                    EcoGroups = new double[5]
                };

                foreach (DataRow row in indicesData.Rows)
                {
                    string indexName = row[0]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(indexName))
                    {
                        continue;
                    }

                    if (!double.TryParse(row[col]?.ToString(), out double value))
                    {
                        continue;
                    }

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

                _allResults.Add(result);
            }

            _sampleSelectionList.Items.Clear();
            foreach (var sample in _allResults.Select(r => r.SampleName))
            {
                _sampleSelectionList.Items.Add(sample, true);
            }

            _indexSelectionList.Items.Clear();
            _indexSelectionList.Items.AddRange(_availableIndices);

            _dataGridIndices.DataSource = _resultsTable;

            _tabControl.SelectedIndex = 1;

            MessageBox.Show($"Loaded {_allResults.Count} samples with indices data.\n" +
                "You can now create plots from the Plot Options tab.",
                "Data Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void LoadResults(
            DataTable sourceData,
            DataTable databank,
            Dictionary<string, double> sampleMudPercentages,
            List<string> selectedIndices = null,
            Dictionary<string, int> overrides = null)
        {
            _allResults.Clear();
            _resultsTable = new DataTable();
            _mudPercentages = sampleMudPercentages;
            _fambiAvailable = databank != null;

            // Load specialized databanks
            _fsiDatabank = SpecializedDatabankLoader.LoadFSIDatabank();
            _tsiMedDatabank = SpecializedDatabankLoader.LoadTSIMedDatabank();
            _foramDatabank = SpecializedDatabankLoader.LoadFoRAMDatabank();
            (_fsiDatabankAvailable, _tsiMedDatabankAvailable) = SpecializedDatabankLoader.CheckDatabanksAvailability();
            _foramDatabankAvailable = SpecializedDatabankLoader.CheckFoRAMDatabankAvailability();

            // Determine whether the FoRAM Index should be calculated
            _calculateFoRAMIndex = false;
            if (selectedIndices != null)
            {
                _calculateFoRAMIndex = selectedIndices.Contains("FoRAM Index") && _foramDatabankAvailable;
            }
            else if (_foramDatabankAvailable && _foramDatabank.Count > 0)
            {
                // Legacy fallback: ask the user.
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
                _calculateFoRAMIndex = result == DialogResult.Yes;
            }

            // Create columns for the results table
            _resultsTable.Columns.Add("Index", typeof(string));

            for (int col = 1; col < sourceData.Columns.Count; col++)
            {
                _resultsTable.Columns.Add(sourceData.Columns[col].ColumnName, typeof(double));
            }

            _resultsTable.Columns.Add("Mean", typeof(double));
            _resultsTable.Columns.Add("StdDev", typeof(double));
            _resultsTable.Columns.Add("Min", typeof(double));
            _resultsTable.Columns.Add("Max", typeof(double));

            // Create the eco-group lookup from the databank
            var ecoLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (_fambiAvailable)
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

            if (overrides != null)
            {
                foreach (var kvp in overrides)
                {
                    ecoLookup[kvp.Key] = kvp.Value;
                }
            }

            bool IsSelected(string indexName) => selectedIndices == null || selectedIndices.Contains(indexName);

            bool needsEcoGroups = IsSelected("Foram-AMBI") || IsSelected("Foram-M-AMBI") || IsSelected("NQIf")
                                  || IsSelected("FIEI") || IsSelected("BENTIX") || IsSelected("BQI")
                                  || (IsSelected("FSI") && !_fsiDatabankAvailable)
                                  || (IsSelected("TSI-Med") && !_tsiMedDatabankAvailable);

            for (int col = 1; col < sourceData.Columns.Count; col++)
            {
                var result = new IndicesResult
                {
                    SampleName = sourceData.Columns[col].ColumnName,
                    FAMBI_ThresholdType = _selectedFAMBIThreshold,
                    TSIMed_ReferenceType = _selectedTSIReference,
                    TSIMed_ThresholdType = _selectedTSIThreshold,
                    ExpHbc_ThresholdType = _selectedExpHbcThreshold
                };

                // Extract abundances, aggregating rows that share the same species name within
                // this sample (summing their abundances) so a taxon entered on multiple rows
                // (e.g. duplicate entries, split size fractions) is counted once, not as several
                // distinct "species" - this matters for richness/diversity as well as eco-groups.
                var speciesAbundances = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                var unnamedAbundances = new List<double>();

                foreach (DataRow row in sourceData.Rows)
                {
                    string species = row[0]?.ToString()?.Trim();
                    if (double.TryParse(row[col]?.ToString(), out double value) && value > 0)
                    {
                        if (!string.IsNullOrEmpty(species))
                        {
                            speciesAbundances[species] = speciesAbundances.TryGetValue(species, out double existing)
                                ? existing + value
                                : value;
                        }
                        else
                        {
                            unnamedAbundances.Add(value);
                        }
                    }
                }

                double[] abundArray = speciesAbundances.Values.Concat(unnamedAbundances).ToArray();

                // ========== DIVERSITY INDICES ==========
                result.Shannon_Ln = IsSelected("H'ln")
                    ? BioticIndicesCalculator.CalculateShannonLn(abundArray)
                    : double.NaN;

                result.Shannon_Log2 = IsSelected("H'log2")
                    ? BioticIndicesCalculator.CalculateShannonLog2(abundArray)
                    : double.NaN;

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
                else
                {
                    result.Simpson_1D = double.NaN;
                }

                result.Pielou_J = IsSelected("Pielou's J")
                    ? BioticIndicesCalculator.CalculatePielousEvenness(abundArray)
                    : double.NaN;

                // Always compute core diversity metrics used by downstream indices.
                result.ES100 = BioticIndicesCalculator.CalculateES(abundArray, 100);
                result.SpeciesRichness = BioticIndicesCalculator.CalculateSpeciesRichness(abundArray);
                result.TotalAbundance = BioticIndicesCalculator.CalculateTotalAbundance(abundArray);

                // ========== F-AMBI ECO-GROUPS ==========
                if (_fambiAvailable && needsEcoGroups)
                {
                    result.EcoGroups = BioticIndicesCalculator.CalculateEcoGroupPercentages(speciesAbundances, ecoLookup);
                    result.FAMBI = IsSelected("Foram-AMBI") || IsSelected("Foram-M-AMBI") || IsSelected("NQIf")
                        ? BioticIndicesCalculator.CalculateFAMBI(result.EcoGroups)
                        : double.NaN;
                }
                else
                {
                    result.EcoGroups = new double[] { 0, 0, 0, 0, 0 };
                    result.FAMBI = double.NaN;
                }

                // ========== FSI - uses the specialized Dimiza et al. (2016) databank ==========
                if (IsSelected("FSI"))
                {
                    if (_fsiDatabankAvailable && _fsiDatabank.Count > 0)
                    {
                        var (sensitive, tolerant, assigned) =
                            SpecializedDatabankLoader.CalculateFSIPercentages(speciesAbundances, _fsiDatabank);
                        result.FSI = BioticIndicesCalculator.CalculateFSI(sensitive, tolerant);
                        result.FSI_AssignedPercent = assigned;
                        result.FSI_UsingSpecializedDatabank = true;
                    }
                    else if (_fambiAvailable)
                    {
                        // Fallback: FSI cannot be calculated accurately without the proper databank.
                        // Uses F-AMBI ecogroups as a rough approximation (EG1=sensitive, EG3+4+5=tolerant).
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

                // ========== TSI-Med - mud% plus the Barras et al. (2014) databank ==========
                double mudPct = _mudPercentages?.GetValueOrDefault(result.SampleName, 50.0) ?? 50.0;
                result.MudPercent = mudPct;

                if (IsSelected("TSI-Med"))
                {
                    result.TSIMed_ReferenceValue = BioticIndicesCalculator.CalculateTSIReference(mudPct, _selectedTSIReference);

                    if (_tsiMedDatabankAvailable && _tsiMedDatabank.Count > 0)
                    {
                        var (tolerantPct, _) =
                            SpecializedDatabankLoader.CalculateTSIMedPercentages(speciesAbundances, _tsiMedDatabank);
                        result.TSI_Med = BioticIndicesCalculator.CalculateTSIMed(tolerantPct, mudPct, _selectedTSIReference);
                        result.TSIMed_TolerantPercent = tolerantPct;
                        result.TSIMed_UsingSpecializedDatabank = true;
                    }
                    else if (_fambiAvailable)
                    {
                        // Fallback: use F-AMBI tolerant groups as an approximation.
                        double tolerant = result.EcoGroups[2] + result.EcoGroups[3] + result.EcoGroups[4];
                        result.TSI_Med = BioticIndicesCalculator.CalculateTSIMed(tolerant, mudPct, _selectedTSIReference);
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

                // ========== NQIf (uses F-AMBI) ==========
                result.NQIf = IsSelected("NQIf") && _fambiAvailable && !double.IsNaN(result.FAMBI)
                    ? BioticIndicesCalculator.CalculateNQIf(result.FAMBI, result.ES100)
                    : double.NaN;

                // ========== Foram-M-AMBI - multivariate AMBI for foraminifera ==========
                // Reference: Muxika et al. (2007) adapted for foraminifera
                if (IsSelected("Foram-M-AMBI") && _fambiAvailable && !double.IsNaN(result.FAMBI))
                {
                    double shannonLn = !double.IsNaN(result.Shannon_Ln)
                        ? result.Shannon_Ln
                        : BioticIndicesCalculator.CalculateShannonLn(abundArray);
                    int richness = result.SpeciesRichness > 0
                        ? result.SpeciesRichness
                        : BioticIndicesCalculator.CalculateSpeciesRichness(abundArray);

                    result.ForamMAMBI = BioticIndicesCalculator.CalculateForamMAMBI(result.FAMBI, shannonLn, richness);
                    result.ForamMAMBI_Euclidean =
                        BioticIndicesCalculator.CalculateForamMAMBI_Euclidean(result.FAMBI, shannonLn, richness, null);

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

                // ========== FIEI - approximation using F-AMBI ecogroups ==========
                if (IsSelected("FIEI") && _fambiAvailable)
                {
                    double opportunistic = result.EcoGroups[3] + result.EcoGroups[4];               // EG4+EG5
                    double tolerantFIEI = result.EcoGroups[2] + result.EcoGroups[3] + result.EcoGroups[4]; // EG3+4+5
                    result.FIEI = BioticIndicesCalculator.CalculateFIEI(tolerantFIEI, opportunistic, 100);
                }
                else
                {
                    result.FIEI = double.NaN;
                }

                // ========== BENTIX - simplified ecological groups ==========
                // Reference: Simboura N. & Zenetos A. (2002) Mediterranean Marine Science, 3/2: 77-111
                result.BENTIX = IsSelected("BENTIX") && _fambiAvailable
                    ? BioticIndicesCalculator.CalculateBENTIX(result.EcoGroups)
                    : double.NaN;

                // ========== BQI - Benthic Quality Index adapted for foraminifera ==========
                // Reference: Rosenberg R. et al. (2004) Marine Pollution Bulletin 49:728-739
                result.BQI = IsSelected("BQI") && _fambiAvailable
                    ? BioticIndicesCalculator.CalculateBQI(result.SpeciesRichness, result.EcoGroups, result.TotalAbundance)
                    : double.NaN;

                // ========== FoRAM Index - tropical coral reef environments ==========
                // Reference: Hallock et al. (2003), Prazeres et al. (2020)
                if (_calculateFoRAMIndex && _foramDatabankAvailable && _foramDatabank.Count > 0)
                {
                    var (symbiont, stressTolerant, heterotrophic, assigned) =
                        SpecializedDatabankLoader.CalculateFoRAMPercentages(speciesAbundances, _foramDatabank);
                    result.FoRAM_Index =
                        BioticIndicesCalculator.CalculateFoRAMIndex(symbiont, stressTolerant, heterotrophic);
                    // The index is only ecologically meaningful if a substantial share of the
                    // assemblage could actually be classified into a FoRAM functional group;
                    // otherwise this is likely not a tropical coral-reef environment.
                    result.FoRAM_ApplicableEnvironment = assigned >= BioticIndicesCalculator.FoRAMIndexMinApplicablePercent;
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

                _allResults.Add(result);
            }

            _sampleSelectionList.Items.Clear();
            foreach (var sample in _allResults.Select(r => r.SampleName))
            {
                _sampleSelectionList.Items.Add(sample, true);
            }

            PopulateResultsTable(selectedIndices);

            _dataGridIndices.DataSource = _resultsTable;
            FormatDataGrid();

            UpdateEQSSummary();

            UpdatePlotSelectionList(selectedIndices, _fambiAvailable && needsEcoGroups,
                _calculateFoRAMIndex && _foramDatabankAvailable);

            ShowDatabankWarnings();
        }

        private void UpdatePlotSelectionList(List<string> selectedIndices, bool ecoGroupsCalculated, bool foramCalculated)
        {
            _indexSelectionList.Items.Clear();

            bool IsSelected(string indexName) => selectedIndices == null || selectedIndices.Contains(indexName);

            foreach (var index in _availableIndices)
            {
                bool shouldAdd = false;

                if (index.StartsWith("Eco") && index.EndsWith("%"))
                {
                    shouldAdd = ecoGroupsCalculated;
                }
                else if (index.StartsWith("FoRAM") && index.Contains("%"))
                {
                    shouldAdd = foramCalculated;
                }
                else if (index == "FoRAM Index")
                {
                    shouldAdd = foramCalculated && IsSelected(index);
                }
                else if (IsSelected(index))
                {
                    shouldAdd = !((index == "Foram-AMBI" || index == "BENTIX" || index == "BQI") && !_fambiAvailable);
                }

                if (shouldAdd)
                {
                    _indexSelectionList.Items.Add(index);
                }
            }
        }

        public void PreselectIndices(IEnumerable<string> indices)
        {
            if (indices == null)
            {
                return;
            }

            SetAllIndices(false);

            foreach (var index in indices)
            {
                int listIndex = _indexSelectionList.Items.IndexOf(index);
                if (listIndex >= 0)
                {
                    _indexSelectionList.SetItemChecked(listIndex, true);
                }
            }
        }

        public void FocusPlotTab(string plotType = "Bar Chart")
        {
            if (_tabControl.Pages.Count > 1)
            {
                _tabControl.SelectedIndex = 1;
            }

            if (string.IsNullOrEmpty(plotType))
            {
                return;
            }

            int index = _plotTypeCombo.Items.ToList().FindIndex(i => i.Text == plotType);
            if (index >= 0)
            {
                _plotTypeCombo.SelectedIndex = index;
            }
        }

        public void GenerateSelectedPlot(string plotType)
        {
            GeneratePlot(string.IsNullOrEmpty(plotType) ? SelectedPlotType() : plotType);
        }

        public void ShowCompositePanel() => GenerateCompositePlot();

        private List<IndicesResult> GetSelectedResults()
        {
            var selectedSamples = _sampleSelectionList.CheckedItems.Cast<string>().ToList();
            if (selectedSamples.Count == 0)
            {
                MessageBox.Show("Please select at least one sample to plot.", "No Samples",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return new List<IndicesResult>();
            }

            return _allResults.Where(r => selectedSamples.Contains(r.SampleName)).ToList();
        }

        #endregion

        #region Results table

        private void PopulateResultsTable(List<string> selectedIndices = null)
        {
            _resultsTable.Rows.Clear();

            bool IsSelected(string indexName) => selectedIndices == null || selectedIndices.Contains(indexName);

            if (IsSelected("exp(H'bc)")) AddResultRow("exp(H'bc)", _allResults.Select(r => r.Exp_Hbc).ToArray());
            if (IsSelected("H'log2")) AddResultRow("H'log2", _allResults.Select(r => r.Shannon_Log2).ToArray());
            if (IsSelected("H'ln")) AddResultRow("H'ln", _allResults.Select(r => r.Shannon_Ln).ToArray());
            if (IsSelected("exp(H'bc)")) AddResultRow("H'bc", _allResults.Select(r => r.Shannon_BC).ToArray());

            if (IsSelected("FSI"))
            {
                AddResultRow("FSI", _allResults.Select(r => r.FSI).ToArray());
                AddResultRow("FSI Assigned %", _allResults.Select(r => r.FSI_AssignedPercent).ToArray());
            }

            if (IsSelected("TSI-Med"))
            {
                AddResultRow("TSI-Med", _allResults.Select(r => r.TSI_Med).ToArray());
                AddResultRow("Mud %", _allResults.Select(r => r.MudPercent).ToArray());
            }

            if (IsSelected("NQIf")) AddResultRow("NQIf", _allResults.Select(r => r.NQIf).ToArray());

            if (_fambiAvailable)
            {
                if (IsSelected("FIEI")) AddResultRow("FIEI", _allResults.Select(r => r.FIEI).ToArray());
                if (IsSelected("Foram-AMBI")) AddResultRow("Foram-AMBI", _allResults.Select(r => r.FAMBI).ToArray());

                if (IsSelected("Foram-M-AMBI"))
                {
                    AddResultRow("Foram-M-AMBI", _allResults.Select(r => r.ForamMAMBI).ToArray());
                    AddResultRow("Foram-M-AMBI (Euclidean)", _allResults.Select(r => r.ForamMAMBI_Euclidean).ToArray());
                    AddResultRow("M-AMBI Norm. AMBI", _allResults.Select(r => r.ForamMAMBI_NormAMBI).ToArray());
                    AddResultRow("M-AMBI Norm. H'", _allResults.Select(r => r.ForamMAMBI_NormH).ToArray());
                    AddResultRow("M-AMBI Norm. S", _allResults.Select(r => r.ForamMAMBI_NormS).ToArray());
                }

                if (IsSelected("BENTIX")) AddResultRow("BENTIX", _allResults.Select(r => r.BENTIX).ToArray());
                if (IsSelected("BQI")) AddResultRow("BQI", _allResults.Select(r => r.BQI).ToArray());
            }

            if (_calculateFoRAMIndex && _foramDatabankAvailable)
            {
                AddResultRow("FoRAM Index", _allResults.Select(r => r.FoRAM_Index).ToArray());
                AddResultRow("FoRAM Assigned %", _allResults.Select(r => r.FoRAM_AssignedPercent).ToArray());
                AddResultRow("FoRAM Symbiont %", _allResults.Select(r => r.FoRAM_SymbiontPercent).ToArray());
                AddResultRow("FoRAM Stress-Tolerant %", _allResults.Select(r => r.FoRAM_StressTolerantPercent).ToArray());
                AddResultRow("FoRAM Heterotrophic %", _allResults.Select(r => r.FoRAM_HeterotrophicPercent).ToArray());
            }

            if (IsSelected("Species Richness (S)")) AddResultRow("Species Richness (S)", _allResults.Select(r => (double)r.SpeciesRichness).ToArray());
            if (IsSelected("Total Abundance (N)")) AddResultRow("Total Abundance (N)", _allResults.Select(r => r.TotalAbundance).ToArray());
            if (IsSelected("Simpson (1-D)")) AddResultRow("Simpson (1-D)", _allResults.Select(r => r.Simpson_1D).ToArray());
            if (IsSelected("Pielou's J")) AddResultRow("Pielou's J", _allResults.Select(r => r.Pielou_J).ToArray());
            if (IsSelected("ES100")) AddResultRow("ES100", _allResults.Select(r => r.ES100).ToArray());

            if (_fambiAvailable)
            {
                bool showEcoGroups = IsSelected("Foram-AMBI") || IsSelected("FIEI") || IsSelected("NQIf")
                                     || IsSelected("FSI") || IsSelected("TSI-Med") || IsSelected("BENTIX")
                                     || IsSelected("BQI");

                if (showEcoGroups)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        int group = i;
                        AddResultRow($"Eco{i + 1} %", _allResults.Select(r => r.EcoGroups[group]).ToArray());
                    }
                }
            }
        }

        private void AddResultRow(string indexName, double[] values)
        {
            var row = _resultsTable.NewRow();
            row["Index"] = indexName;

            var validValues = new List<double>();

            // The sample columns are all columns except Index, Mean, StdDev, Min and Max.
            int sampleCount = _resultsTable.Columns.Count - 5;

            for (int i = 0; i < values.Length && i < sampleCount; i++)
            {
                if (double.IsNaN(values[i]))
                {
                    row[i + 1] = DBNull.Value;
                }
                else
                {
                    row[i + 1] = Math.Round(values[i], 4);
                    validValues.Add(values[i]);
                }
            }

            if (validValues.Count > 0)
            {
                double mean = validValues.Average();
                double sumOfSquares = validValues.Sum(v => Math.Pow(v - mean, 2));
                double stdDev = Math.Sqrt(sumOfSquares / (validValues.Count > 1 ? validValues.Count - 1 : 1));

                row["Mean"] = Math.Round(mean, 4);
                row["StdDev"] = Math.Round(stdDev, 4);
                row["Min"] = Math.Round(validValues.Min(), 4);
                row["Max"] = Math.Round(validValues.Max(), 4);
            }
            else
            {
                row["Mean"] = DBNull.Value;
                row["StdDev"] = DBNull.Value;
                row["Min"] = DBNull.Value;
                row["Max"] = DBNull.Value;
            }

            _resultsTable.Rows.Add(row);
        }

        private void FormatDataGrid()
        {
            foreach (var col in _dataGridIndices.Columns)
            {
                col.SortMode = Compat.DataGridViewColumnSortMode.NotSortable;
                if (col.Index > 0)
                {
                    col.DefaultCellStyle.Format = "N4";
                    col.DefaultCellStyle.Alignment = Compat.DataGridViewContentAlignment.MiddleRight;
                }
            }
            _dataGridIndices.Refresh();
        }

        private void UpdateEQSSummary()
        {
            if (_allResults.Count == 0)
            {
                return;
            }

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

            foreach (var result in _allResults)
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

            _eqsSummaryGrid.Columns.Clear();
            _eqsSummaryGrid.AutoGenerateColumns = true;
            _eqsSummaryGrid.DataSource = eqsTable;

            _eqsSummaryGrid.CellFormatting -= EqsSummaryGrid_CellFormatting;
            _eqsSummaryGrid.CellFormatting += EqsSummaryGrid_CellFormatting;
        }

        private void EqsSummaryGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex >= 1 && e.ColumnIndex <= 8 && e.Value != null)
            {
                string value = e.Value.ToString();
                e.CellStyle.BackColor = GetEQSColor(value);
                e.CellStyle.ForeColor = EqsClassificationPalette.UsesLightText(value)
                    ? Colors.White
                    : Colors.Black;
            }

            // Highlight approximation warnings
            if (e.ColumnIndex >= 9 && e.Value?.ToString() == "Approx")
            {
                e.CellStyle.BackColor = Color.FromArgb(255, 255, 224);
            }
        }

        private void ShowDatabankWarnings()
        {
            var warnings = new List<string>();

            if (!_fsiDatabankAvailable)
            {
                warnings.Add("FSI: Specialized databank (fsi_databank.csv) not found.\n" +
                    "Using F-AMBI ecogroups as approximation. Results may not be accurate per Dimiza et al. (2016).");
            }

            if (!_tsiMedDatabankAvailable)
            {
                warnings.Add("TSI-Med: Specialized databank (tsimed_databank.csv) not found.\n" +
                    "Using F-AMBI ecogroups as approximation. Results may not be accurate per Barras et al. (2014).");
            }

            if (_mudPercentages == null || _mudPercentages.Count == 0)
            {
                warnings.Add("TSI-Med: Mud percentages not provided.\n" +
                    "Using default value of 50% for all samples. For accurate results, provide sediment grain-size data.");
            }

            if (_calculateFoRAMIndex && _foramDatabankAvailable)
            {
                warnings.Add("FoRAM Index: CALCULATED.\n" +
                    "This index is designed for tropical/subtropical coral reef environments (Hallock et al. 2003; Prazeres et al. 2020).\n" +
                    "Interpretation: FI > 4 = suitable for coral growth; FI 2-4 = marginal; FI < 2 = unsuitable.");
            }
            else if (!_foramDatabankAvailable)
            {
                warnings.Add("FoRAM Index: Databank (foram_index_databank.csv) not found.\nIndex cannot be calculated.");
            }
            else
            {
                warnings.Add("FoRAM Index: Not calculated (user skipped).\n" +
                    "Note: This index is designed for tropical coral reef environments.");
            }

            if (warnings.Count > 0)
            {
                string message = "DATABANK STATUS:\n\n" + string.Join("\n\n", warnings);
                MessageBox.Show(message, "Index Calculation Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static Color GetEQSColor(string eqs)
        {
            return EqsClassificationPalette.TryGetRgb(eqs, out int red, out int green, out int blue)
                ? Color.FromArgb(red, green, blue)
                : Colors.White;
        }

        #endregion

        #region Plot generation

        private void GeneratePlot(string plotType)
        {
            var selectedIndices = _indexSelectionList.CheckedItems.Cast<string>().ToList();

            // Eco Groups and Composite Panel do not require an index selection.
            if (plotType == "Eco Groups")
            {
                if (!_fambiAvailable)
                {
                    MessageBox.Show("Eco Groups data not available. F-AMBI databank is required.", "Missing Data",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                ShowPlotModels(new List<PlotModel> { CreateEcoGroupsPlot(), CreateEcoGroupsBoxPlot() });
                return;
            }

            if (plotType == "Composite Panel")
            {
                GenerateCompositePlot();
                return;
            }

            var filteredResults = GetSelectedResults();

            if (selectedIndices.Count == 0)
            {
                MessageBox.Show("Please select at least one index to plot.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (filteredResults.Count == 0)
            {
                return;
            }

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
            _currentPlotModels.Clear();

            if (models == null || models.Count == 0)
            {
                _plotHostPanel.Content = null;
                return;
            }

            foreach (var model in models)
            {
                PlotFonts.ApplyTo(model);
            }

            _currentPlotModels.AddRange(models);

            if (models.Count == 1)
            {
                _currentPlotColumns = 1;
                _plotHostPanel.Content = new PlotView { Model = models[0] };
                return;
            }

            int columns = Math.Min(3, (int)Math.Ceiling(Math.Sqrt(models.Count)));
            int rows = (int)Math.Ceiling((double)models.Count / columns);
            _currentPlotColumns = columns;

            var table = new TableLayout(columns, rows)
            {
                Padding = new Padding(10),
                Spacing = new Size(6, 6)
            };

            for (int i = 0; i < models.Count; i++)
            {
                table.Add(new PlotView { Model = models[i] }, i % columns, i / columns, true, true);
            }

            _plotHostPanel.Content = new Scrollable { Border = BorderType.None, Content = table };
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

                var series = new BarSeries
                {
                    StrokeThickness = 1,
                    StrokeColor = OxyColors.Black,
                    IsStacked = !grouped,
                    LabelPlacement = LabelPlacement.Inside
                };

                var statsValues = new List<double>();
                for (int i = 0; i < indices.Count; i++)
                {
                    double value = GetIndexValueForResult(indices[i], result);
                    series.Items.Add(new BarItem(double.IsNaN(value) ? 0 : value) { Color = colors[i % colors.Length] });
                    if (!double.IsNaN(value))
                    {
                        statsValues.Add(value);
                    }
                }

                model.Series.Add(series);

                AddStatisticsAnnotation(model, statsValues, result.SampleName);
                AddBarLegend(model, indices, colors);

                models.Add(model);
            }

            return models;
        }

        private void AddBarLegend(PlotModel model, List<string> indices, OxyColor[] colors)
        {
            // Invisible line series so the bar colours get legend entries.
            for (int i = 0; i < indices.Count; i++)
            {
                model.Series.Add(new LineSeries
                {
                    Title = indices[i],
                    Color = colors[i % colors.Length],
                    StrokeThickness = 0,
                    MarkerType = MarkerType.Square,
                    MarkerSize = 8,
                    MarkerFill = colors[i % colors.Length]
                });
            }
            AddLegend(model);
        }

        private void AddStatisticsAnnotation(PlotModel model, List<double> values, string seriesName = null)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }

            double mean = values.Average();
            double stdDev = values.Count > 1
                ? Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1))
                : 0;

            string statsText = seriesName != null
                ? $"Statistics ({seriesName}): n={values.Count}, Mean={mean:F3}, SD={stdDev:F3}, Min={values.Min():F3}, Max={values.Max():F3}"
                : $"Statistics: n={values.Count}, Mean={mean:F3}, SD={stdDev:F3}, Min={values.Min():F3}, Max={values.Max():F3}";

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
                    }
                }

                if (validValues.Count > 0)
                {
                    double mean = validValues.Average();
                    double sd = validValues.Count > 1
                        ? Math.Sqrt(validValues.Sum(v => Math.Pow(v - mean, 2)) / (validValues.Count - 1))
                        : 0;
                    statsBuilder.Append($"{indices[i]}: μ={mean:F2}±{sd:F2}  ");
                }

                model.Series.Add(series);
            }

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
            var markerTypes = new[]
            {
                MarkerType.Circle, MarkerType.Square, MarkerType.Triangle, MarkerType.Diamond,
                MarkerType.Star, MarkerType.Cross, MarkerType.Plus
            };

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

                if (scatterValues.Count > 0)
                {
                    double mean = scatterValues.Average();
                    double sd = scatterValues.Count > 1
                        ? Math.Sqrt(scatterValues.Sum(v => Math.Pow(v - mean, 2)) / (scatterValues.Count - 1))
                        : 0;
                    model.Subtitle = $"n={scatterValues.Count}, Mean={mean:F3}, SD={sd:F3}, " +
                                     $"Min={scatterValues.Min():F3}, Max={scatterValues.Max():F3}";
                    model.SubtitleFontSize = GetSelectedFontSize() - 1;
                    model.SubtitleColor = OxyColors.DarkGray;
                }

                AddLegend(model);
                models.Add(model);
            }

            return models;
        }

        private List<PlotModel> CreatePerIndexBoxPlots(List<string> indices, List<IndicesResult> results)
        {
            // One plot with every index as a box, showing its distribution across samples.
            var model = new PlotModel { Title = "Biotic Indices - Distribution Across Samples" };
            ConfigurePlotStyle(model);

            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Angle = 45 };
            categoryAxis.Labels.AddRange(indices);
            ApplyAxisStyle(categoryAxis);
            model.Axes.Add(categoryAxis);

            var valueAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Value" };
            ApplyAxisStyle(valueAxis);
            model.Axes.Add(valueAxis);

            var colors = GetColorPalette(indices.Count);
            var statsBuilder = new StringBuilder();
            int validCount = 0;

            for (int i = 0; i < indices.Count; i++)
            {
                var values = results
                    .Select(r => GetIndexValueForResult(indices[i], r))
                    .Where(v => !double.IsNaN(v))
                    .ToList();

                if (values.Count == 0)
                {
                    continue;
                }

                values.Sort();

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

                statsBuilder.Append($"{indices[i]}: Med={median:F2}  ");
                validCount++;
            }

            if (validCount > 0)
            {
                model.Subtitle = statsBuilder.ToString().TrimEnd();
                model.SubtitleFontSize = GetSelectedFontSize() - 2;
                model.SubtitleColor = OxyColors.DarkGray;
            }

            AddLegend(model);

            return new List<PlotModel> { model };
        }

        private PlotModel CreateEcoGroupsPlot()
        {
            var model = new PlotModel { Title = "Ecological Groups (Eco1-5) Across Samples" };
            ConfigurePlotStyle(model);

            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Angle = 45 };
            categoryAxis.Labels.AddRange(_allResults.Select(r => r.SampleName));
            ApplyAxisStyle(categoryAxis);
            model.Axes.Add(categoryAxis);

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Percentage (%)",
                Minimum = 0,
                Maximum = 100
            };
            ApplyAxisStyle(valueAxis);
            model.Axes.Add(valueAxis);

            var ecoGroups = new[] { "Eco1", "Eco2", "Eco3", "Eco4", "Eco5" };
            var ecoColors = EcoGroupColors();
            var statsBuilder = new StringBuilder();

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
                for (int j = 0; j < _allResults.Count; j++)
                {
                    double value = _allResults[j].EcoGroups[i];
                    series.Points.Add(new DataPoint(j, value));
                    ecoValues.Add(value);
                }

                if (ecoValues.Count > 0)
                {
                    statsBuilder.Append($"{ecoGroups[i]}: {ecoValues.Average():F1}%  ");
                }

                model.Series.Add(series);
            }

            if (statsBuilder.Length > 0)
            {
                model.Subtitle = "Mean: " + statsBuilder.ToString().TrimEnd();
                model.SubtitleFontSize = GetSelectedFontSize() - 2;
                model.SubtitleColor = OxyColors.DarkGray;
            }

            AddLegend(model);

            return model;
        }

        private PlotModel CreateEcoGroupsBoxPlot()
        {
            var model = new PlotModel { Title = "Eco Groups - Box Plot" };
            ConfigurePlotStyle(model);

            var ecoGroups = new[] { "Eco1", "Eco2", "Eco3", "Eco4", "Eco5" };
            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom };
            categoryAxis.Labels.AddRange(ecoGroups);
            ApplyAxisStyle(categoryAxis);
            model.Axes.Add(categoryAxis);

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Percentage (%)",
                Minimum = 0,
                Maximum = 100
            };
            ApplyAxisStyle(valueAxis);
            model.Axes.Add(valueAxis);

            var ecoColors = EcoGroupColors();
            var statsBuilder = new StringBuilder();

            for (int i = 0; i < ecoGroups.Length; i++)
            {
                var values = _allResults.Select(r => r.EcoGroups[i]).ToList();
                if (values.Count == 0)
                {
                    continue;
                }
                values.Sort();

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

            model.Subtitle = statsBuilder.ToString().TrimEnd();
            model.SubtitleFontSize = GetSelectedFontSize() - 2;
            model.SubtitleColor = OxyColors.DarkGray;

            AddLegend(model);

            return model;
        }

        /// <summary>Colourblind-friendly palette for the five ecological groups.</summary>
        private static OxyColor[] EcoGroupColors() => new[]
        {
            OxyColor.FromRgb(0, 114, 178),    // Blue
            OxyColor.FromRgb(0, 158, 115),    // Green
            OxyColor.FromRgb(240, 228, 66),   // Yellow
            OxyColor.FromRgb(230, 159, 0),    // Orange
            OxyColor.FromRgb(213, 94, 0)      // Red
        };

        /// <summary>Creates a heatmap plot showing index values across samples.</summary>
        private PlotModel CreateHeatmapPlot(List<string> selectedIndices, List<IndicesResult> results)
        {
            var model = new PlotModel { Title = "Biotic Indices Heatmap" };
            ConfigurePlotStyle(model);

            var data = new double[selectedIndices.Count, results.Count];
            var minMax = new Dictionary<string, (double min, double max)>();

            foreach (var index in selectedIndices)
            {
                var values = results
                    .Select(r => GetIndexValueForResult(index, r))
                    .Where(v => !double.IsNaN(v))
                    .ToList();
                minMax[index] = values.Count > 0 ? (values.Min(), values.Max()) : (0, 1);
            }

            // Normalise each index to 0-1 so they share one colour scale.
            for (int i = 0; i < selectedIndices.Count; i++)
            {
                var (min, max) = minMax[selectedIndices[i]];
                double range = max - min;
                if (range == 0)
                {
                    range = 1;
                }

                for (int j = 0; j < results.Count; j++)
                {
                    double val = GetIndexValueForResult(selectedIndices[i], results[j]);
                    data[i, j] = double.IsNaN(val) ? 0 : (val - min) / range;
                }
            }

            model.Series.Add(new HeatMapSeries
            {
                X0 = 0,
                X1 = results.Count - 1,
                Y0 = 0,
                Y1 = selectedIndices.Count - 1,
                Interpolate = false,
                Data = data,
                RenderMethod = HeatMapRenderMethod.Rectangles
            });

            model.Axes.Add(new LinearColorAxis
            {
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Jet(100),
                Title = "Normalized Value",
                Minimum = 0,
                Maximum = 1
            });

            var xAxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Samples", Angle = 45 };
            foreach (var r in results)
            {
                xAxis.Labels.Add(r.SampleName.Length > 15 ? r.SampleName.Substring(0, 12) + "..." : r.SampleName);
            }
            model.Axes.Add(xAxis);

            var yAxis = new CategoryAxis { Position = AxisPosition.Left, Title = "Indices" };
            foreach (var idx in selectedIndices)
            {
                yAxis.Labels.Add(idx);
            }
            model.Axes.Add(yAxis);

            return model;
        }

        #endregion

        #region Stand-alone plot windows

        private void GenerateCompositePlot()
        {
            var models = new List<PlotModel>
            {
                CreateSubPlot("Diversity Indices", new[] { "exp(H'bc)", "H'log2", "Species Richness (S)" }),
                CreateSubPlot("Sensitivity-Based Indices", new[] { "FSI", "NQIf", "Foram-AMBI" }),
                CreateSubPlotEcoGroups("Ecological Group Distribution"),
                CreateSubPlot("Additional Indices", new[] { "FIEI", "TSI-Med", "FoRAM Index" })
            };

            ShowPlotWindow("Composite Plot Panel", models, 2, new Size(1200, 900), exportStats: true);
        }

        private PlotModel CreateSubPlot(string title, string[] indices)
        {
            var model = CreateLinePlot(indices.ToList(), _allResults);
            model.Title = title;
            return model;
        }

        private PlotModel CreateSubPlotEcoGroups(string title)
        {
            var model = CreateEcoGroupsPlot();
            model.Title = title;
            return model;
        }

        private void GenerateEcoGroupsPlot()
        {
            if (!_fambiAvailable)
            {
                MessageBox.Show("Eco Groups data not available. F-AMBI databank is required.", "Missing Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var linePlot = CreateEcoGroupsPlot();
            linePlot.Title = "Eco Groups - Line Plot";

            ShowPlotWindow("Ecological Groups Distribution",
                new List<PlotModel> { linePlot, CreateEcoGroupsBoxPlot() }, 2,
                new Size(1200, 700), exportEcoGroupStats: true);
        }

        private void GenerateEQSPlot()
        {
            var model = new PlotModel { Title = "Ecological Quality Status by Sample" };
            ConfigurePlotStyle(model);

            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Angle = 45 };
            categoryAxis.Labels.AddRange(_allResults.Select(r => r.SampleName));
            model.Axes.Add(categoryAxis);

            var valueAxis = new CategoryAxis { Position = AxisPosition.Left, Key = "Indices" };
            valueAxis.Labels.AddRange(new[] { "F-AMBI", "FSI", "NQI", "exp(H'bc)" });
            model.Axes.Add(valueAxis);

            var heatmapSeries = new RectangleBarSeries();

            for (int i = 0; i < _allResults.Count; i++)
            {
                var result = _allResults[i];
                AddEQSBar(heatmapSeries, i, 0, result.FAMBI_EQS);
                AddEQSBar(heatmapSeries, i, 1, result.FSI_EQS);
                AddEQSBar(heatmapSeries, i, 2, result.NQI_EQS);
                AddEQSBar(heatmapSeries, i, 3, result.ExpHbc_EQS);
            }

            model.Series.Add(heatmapSeries);

            ShowPlotWindow("Ecological Quality Status Overview",
                new List<PlotModel> { model }, 1, new Size(1000, 700));
        }

        private static void AddEQSBar(RectangleBarSeries series, int x, int y, string eqs)
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

        /// <summary>
        /// Opens a stand-alone window showing the given plots on a grid, with export buttons.
        /// </summary>
        private void ShowPlotWindow(
            string title,
            List<PlotModel> models,
            int columns,
            Size size,
            bool exportStats = false,
            bool exportEcoGroupStats = false)
        {
            var window = new Form
            {
                Title = title,
                ClientSize = size
            };
            window.Prepare();

            int rows = (int)Math.Ceiling((double)models.Count / columns);
            var table = new TableLayout(columns, rows) { Spacing = new Size(4, 4) };
            for (int i = 0; i < models.Count; i++)
            {
                table.Add(new PlotView { Model = PlotFonts.ApplyTo(models[i]) }, i % columns, i / columns, true, true);
            }

            var saveButton = UiHelpers.ActionButton("Save as PNG", AppColors.SteelBlue, null, 140);
            saveButton.Click += (s, e) => SavePlotsAsImage(models, columns, window);

            var buttons = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Padding = new Padding(5),
                Items = { saveButton }
            };

            if (exportStats)
            {
                var exportExcelButton = UiHelpers.ActionButton("Export Stats to Excel", AppColors.SeaGreen, null, 170);
                exportExcelButton.Click += (s, e) => ExportCompositeStatsToExcel(window);
                buttons.Items.Add(exportExcelButton);
            }

            if (exportEcoGroupStats)
            {
                var exportEcoButton = UiHelpers.ActionButton("Export Stats", AppColors.SeaGreen, null, 140);
                exportEcoButton.Click += (s, e) => ExportEcoGroupsStats(window);
                buttons.Items.Add(exportEcoButton);
            }

            window.Content = new TableLayout
            {
                Rows =
                {
                    new TableRow(table) { ScaleHeight = true },
                    new TableRow(buttons)
                }
            };

            window.Show();
        }

        private void SavePlotsAsImage(List<PlotModel> models, int columns, Control parent)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Save Plot",
                FileName = models.Count > 1 ? "Composite_Plot.png" : "Plot.png",
                Filters = { Filters.Png, Filters.Jpeg }
            };

            if (saveDialog.ShowDialog(parent) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                string path = UiHelpers.EnsureExtension(saveDialog.FileName, ".png");
                int dpi = GetSelectedDpi();

                if (models.Count == 1)
                {
                    PlotExport.SaveAsImage(models[0], path, dpi);
                }
                else
                {
                    PlotExport.SaveCompositeAsImage(models, columns, path, dpi);
                }

                MessageBox.Show("Plot saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving plot: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Styling helpers

        private void ConfigurePlotStyle(PlotModel model)
        {
            int fontSize = GetSelectedFontSize();
            model.TitleFontSize = fontSize + 2;
            model.TitleFontWeight = FontWeights.Bold;
            model.PlotAreaBorderThickness = new OxyThickness(1);
            model.PlotAreaBorderColor = OxyColors.Black;
            model.DefaultFont = PlotFonts.Family;
            model.DefaultFontSize = fontSize;
        }

        private void ApplyAxisStyle(Axis axis)
        {
            bool showGrid = _gridCheckbox?.Checked ?? true;
            axis.MajorGridlineStyle = showGrid ? LineStyle.Solid : LineStyle.None;
            axis.MinorGridlineStyle = showGrid ? LineStyle.Dot : LineStyle.None;
            axis.MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.Gray);
            axis.MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.Gray);
            axis.TitleFontSize = GetSelectedFontSize();
            axis.FontSize = GetSelectedFontSize();
        }

        private static void AddLegend(PlotModel model)
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
                "Foram-AMBI" or "F-AMBI" => result.FAMBI,
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
            string scheme = _colorSchemeCombo?.SelectedIndex >= 0
                ? _colorSchemeCombo.Items[_colorSchemeCombo.SelectedIndex].Text
                : null;

            var baseColors = scheme switch
            {
                "Grayscale" => new[]
                {
                    OxyColor.FromRgb(50, 50, 50), OxyColor.FromRgb(100, 100, 100), OxyColor.FromRgb(150, 150, 150),
                    OxyColor.FromRgb(200, 200, 200), OxyColor.FromRgb(80, 80, 80)
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
                "Custom Gradient" => Enumerable.Range(0, Math.Max(count, 5))
                    .Select(i => OxyColor.Interpolate(OxyColors.DarkBlue, OxyColors.OrangeRed, i / (double)Math.Max(1, count - 1)))
                    .ToArray(),
                _ => new[]
                {
                    OxyColor.FromRgb(0, 114, 178),
                    OxyColor.FromRgb(230, 159, 0),
                    OxyColor.FromRgb(0, 158, 115),
                    OxyColor.FromRgb(204, 121, 167),
                    OxyColor.FromRgb(86, 180, 233),
                    OxyColor.FromRgb(213, 94, 0),
                    OxyColor.FromRgb(240, 228, 66),
                    OxyColor.FromRgb(100, 100, 100)
                }
            };

            var colors = new OxyColor[Math.Max(1, count)];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = baseColors[i % baseColors.Length];
            }
            return colors;
        }

        private static OxyColor[] GetScatterColorPalette(int count)
        {
            var baseColors = new[]
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
                OxyColor.FromRgb(23, 190, 207)    // Blue-Teal
            };

            var colors = new OxyColor[Math.Max(1, count)];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = baseColors[i % baseColors.Length];
            }
            return colors;
        }

        private int GetSelectedFontSize()
        {
            if (_fontCombo != null && _fontCombo.SelectedIndex >= 0
                && int.TryParse(_fontCombo.Items[_fontCombo.SelectedIndex].Text, out int size))
            {
                return size;
            }
            return 12;
        }

        private int GetSelectedDpi()
        {
            if (_dpiCombo != null && _dpiCombo.SelectedIndex >= 0
                && int.TryParse(_dpiCombo.Items[_dpiCombo.SelectedIndex].Text, out int dpi))
            {
                return dpi;
            }
            return 300;
        }

        private static double GetMedian(List<double> values)
        {
            int n = values.Count;
            if (n == 0)
            {
                return 0;
            }
            return n % 2 == 0 ? (values[n / 2 - 1] + values[n / 2]) / 2 : values[n / 2];
        }

        private static double GetPercentile(List<double> values, double percentile)
        {
            if (values.Count == 0)
            {
                return 0;
            }
            double n = (values.Count - 1) * percentile / 100.0 + 1;
            if (n == 1)
            {
                return values[0];
            }
            if (n >= values.Count)
            {
                return values.Last();
            }
            int k = (int)n;
            double d = n - k;
            return values[k - 1] + d * (values[k] - values[k - 1]);
        }

        #endregion

        #region Export

        private void SaveResults()
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Save Results to Excel",
                FileName = "BioticIndices_Results.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (saveDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();

                var resultsSheet = workbook.Worksheets.Add("Indices Results");
                for (int i = 0; i < _resultsTable.Columns.Count; i++)
                {
                    resultsSheet.Cell(1, i + 1).Value = _resultsTable.Columns[i].ColumnName;
                }
                for (int i = 0; i < _resultsTable.Rows.Count; i++)
                {
                    for (int j = 0; j < _resultsTable.Columns.Count; j++)
                    {
                        resultsSheet.Cell(i + 2, j + 1).Value = _resultsTable.Rows[i][j]?.ToString() ?? string.Empty;
                    }
                }

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

                for (int i = 0; i < _allResults.Count; i++)
                {
                    eqsSheet.Cell(i + 2, 1).Value = _allResults[i].SampleName;
                    eqsSheet.Cell(i + 2, 2).Value = _allResults[i].FAMBI_EQS;
                    eqsSheet.Cell(i + 2, 3).Value = double.IsNaN(_allResults[i].ForamMAMBI) ? "N/A" : _allResults[i].ForamMAMBI_EQS;
                    eqsSheet.Cell(i + 2, 4).Value = _allResults[i].FSI_EQS;
                    eqsSheet.Cell(i + 2, 5).Value = _allResults[i].NQI_EQS;
                    eqsSheet.Cell(i + 2, 6).Value = _allResults[i].ExpHbc_EQS;
                    eqsSheet.Cell(i + 2, 7).Value = _allResults[i].TSIMed_EQS;
                    eqsSheet.Cell(i + 2, 8).Value = _allResults[i].BENTIX_EQS;
                    eqsSheet.Cell(i + 2, 9).Value = _allResults[i].BQI_EQS;
                    eqsSheet.Cell(i + 2, 10).Value = _allResults[i].FoRAM_Status;
                }

                for (int row = 2; row <= _allResults.Count + 1; row++)
                {
                    for (int column = 2; column <= 10; column++)
                    {
                        var cell = eqsSheet.Cell(row, column);
                        EqsClassificationPalette.ApplyToExcelCell(cell, cell.GetString());
                    }
                }

                AddKappaMatrixSheet(workbook);

                workbook.SaveAs(UiHelpers.EnsureExtension(saveDialog.FileName, ".xlsx"));
                MessageBox.Show("Results saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving results: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Adds Cohen's Kappa matrix and its interpretation guide to the workbook.</summary>
        private void AddKappaMatrixSheet(XLWorkbook workbook)
        {
            var allEqsIndices = new List<string>
            {
                "Foram-AMBI", "FSI", "TSI-Med", "NQIf", "exp(H'bc)", "BENTIX", "BQI", "Foram-M-AMBI"
            };
            var availableIndices = allEqsIndices.Where(HasIndexData).ToList();

            if (availableIndices.Count < 2)
            {
                return;
            }

            var kappaSheet = workbook.Worksheets.Add("Kappa Matrix");

            kappaSheet.Cell(1, 1).Value = "Index";
            for (int i = 0; i < availableIndices.Count; i++)
            {
                kappaSheet.Cell(1, i + 2).Value = availableIndices[i];
            }

            for (int i = 0; i < availableIndices.Count; i++)
            {
                kappaSheet.Cell(i + 2, 1).Value = availableIndices[i];
                for (int j = 0; j < availableIndices.Count; j++)
                {
                    double kappaValue = i == j ? 1.0 : CalculateCohensKappa(availableIndices[i], availableIndices[j]);
                    kappaSheet.Cell(i + 2, j + 2).Value = kappaValue;
                    kappaSheet.Cell(i + 2, j + 2).Style.Fill.BackgroundColor = GetKappaColor(kappaValue).ToXLColor();
                    kappaSheet.Cell(i + 2, j + 2).Style.NumberFormat.Format = "0.000";
                }
            }

            kappaSheet.Range(1, 1, 1, availableIndices.Count + 1).Style.Font.Bold = true;
            kappaSheet.Range(1, 1, 1, availableIndices.Count + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            kappaSheet.Column(1).Style.Font.Bold = true;
            kappaSheet.Columns().AdjustToContents();

            var interpSheet = workbook.Worksheets.Add("Kappa Interpretation");
            interpSheet.Cell(1, 1).Value = "Kappa Range";
            interpSheet.Cell(1, 2).Value = "Interpretation";
            interpSheet.Cell(1, 1).Style.Font.Bold = true;
            interpSheet.Cell(1, 2).Style.Font.Bold = true;

            var guide = new (string range, string text, Color color)[]
            {
                ("0.81 - 1.00", "Almost perfect agreement", Color.FromArgb(0, 128, 0)),
                ("0.61 - 0.80", "Substantial agreement", Color.FromArgb(144, 238, 144)),
                ("0.41 - 0.60", "Moderate agreement", Color.FromArgb(255, 255, 150)),
                ("0.21 - 0.40", "Fair agreement", Color.FromArgb(255, 200, 100)),
                ("0.00 - 0.20", "Slight agreement", Color.FromArgb(255, 150, 150)),
                ("< 0.00", "Poor agreement (worse than chance)", Color.FromArgb(255, 100, 100))
            };

            for (int i = 0; i < guide.Length; i++)
            {
                interpSheet.Cell(i + 2, 1).Value = guide[i].range;
                interpSheet.Cell(i + 2, 2).Value = guide[i].text;
                interpSheet.Cell(i + 2, 1).Style.Fill.BackgroundColor = guide[i].color.ToXLColor();
            }

            interpSheet.Cell(9, 1).Value = "Note:";
            interpSheet.Cell(9, 2).Value = "Cohen's Kappa measures agreement between indices beyond chance.";
            interpSheet.Cell(10, 2).Value = "'Azoic' is kept separate from 'Bad' as it indicates no specimens found.";

            interpSheet.Columns().AdjustToContents();
        }

        private void ExportAllPlots()
        {
            using var folderDialog = new SelectFolderDialog { Title = "Select folder to save plots" };

            if (folderDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            var filtered = GetSelectedResults();
            if (filtered.Count == 0)
            {
                return;
            }

            try
            {
                string folder = folderDialog.Directory;
                int dpi = GetSelectedDpi();

                var diversity = CreateLinePlot(new List<string> { "exp(H'bc)", "H'log2", "Species Richness (S)" }, filtered);
                diversity.Title = "Diversity Indices";
                PlotExport.SaveAsImage(diversity, Path.Combine(folder, "Diversity_Indices.png"), dpi);

                var sensitivity = CreateLinePlot(new List<string> { "FSI", "NQIf", "Foram-AMBI", "FIEI" }, filtered);
                sensitivity.Title = "Sensitivity-Based Indices";
                PlotExport.SaveAsImage(sensitivity, Path.Combine(folder, "Sensitivity_Indices.png"), dpi);

                var ecoGroups = CreateLinePlot(
                    new List<string> { "Eco1 %", "Eco2 %", "Eco3 %", "Eco4 %", "Eco5 %" }, filtered);
                ecoGroups.Title = "Ecological Group Distribution";
                PlotExport.SaveAsImage(ecoGroups, Path.Combine(folder, "EcoGroups.png"), dpi);

                MessageBox.Show($"Plots exported to:\n{folder}", "Export Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting plots: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportCurrentPlot()
        {
            if (_currentPlotModels.Count == 0)
            {
                MessageBox.Show("No plot to export. Generate a plot first.", "No Plot",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Plot",
                FileName = "BioticIndices_Plot.png",
                Filters = { Filters.Png, Filters.Jpeg, Filters.Pdf, Filters.Svg }
            };

            if (saveDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                string path = UiHelpers.EnsureExtension(saveDialog.FileName, ".png");
                string ext = Path.GetExtension(path).ToLowerInvariant();
                int dpi = GetSelectedDpi();

                if (ext == ".pdf" || ext == ".svg")
                {
                    if (_currentPlotModels.Count > 1)
                    {
                        // Vector formats hold a single page, so fall back to a composite image.
                        string pngPath = Path.ChangeExtension(path, ".png");
                        PlotExport.SaveCompositeAsImage(_currentPlotModels, _currentPlotColumns, pngPath, dpi);
                        MessageBox.Show(
                            $"Composite plots were exported as PNG at {pngPath} because {ext} export " +
                            "is not supported for multi-panel figures.",
                            "Export Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (ext == ".pdf")
                    {
                        PlotExport.SaveAsPdf(_currentPlotModels[0], path);
                    }
                    else
                    {
                        PlotExport.SaveAsSvg(_currentPlotModels[0], path);
                    }
                }
                else if (_currentPlotModels.Count == 1)
                {
                    PlotExport.SaveAsImage(_currentPlotModels[0], path, dpi);
                }
                else
                {
                    PlotExport.SaveCompositeAsImage(_currentPlotModels, _currentPlotColumns, path, dpi);
                }

                MessageBox.Show("Plot exported successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting plot: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportCompositeStatsToExcel(Control parent)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Composite Statistics",
                FileName = "CompositeStats.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (saveDialog.ShowDialog(parent) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("Statistics");

                sheet.Cell(1, 1).Value = "Index";
                sheet.Cell(1, 2).Value = "n";
                sheet.Cell(1, 3).Value = "Mean";
                sheet.Cell(1, 4).Value = "StdDev";
                sheet.Cell(1, 5).Value = "Min";
                sheet.Cell(1, 6).Value = "Max";

                int row = 2;
                foreach (var indexName in _availableIndices)
                {
                    var values = _allResults
                        .Select(r => GetIndexValueForResult(indexName, r))
                        .Where(v => !double.IsNaN(v))
                        .ToList();

                    if (values.Count == 0)
                    {
                        continue;
                    }

                    double mean = values.Average();
                    double sd = values.Count > 1
                        ? Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1))
                        : 0;

                    sheet.Cell(row, 1).Value = indexName;
                    sheet.Cell(row, 2).Value = values.Count;
                    sheet.Cell(row, 3).Value = Math.Round(mean, 4);
                    sheet.Cell(row, 4).Value = Math.Round(sd, 4);
                    sheet.Cell(row, 5).Value = Math.Round(values.Min(), 4);
                    sheet.Cell(row, 6).Value = Math.Round(values.Max(), 4);
                    row++;
                }

                workbook.SaveAs(UiHelpers.EnsureExtension(saveDialog.FileName, ".xlsx"));
                MessageBox.Show("Statistics exported successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting statistics: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportEcoGroupsStats(Control parent)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Eco Groups Statistics",
                FileName = "EcoGroups_Stats.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (saveDialog.ShowDialog(parent) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("EcoGroups");

                sheet.Cell(1, 1).Value = "Sample";
                for (int j = 0; j < 5; j++)
                {
                    sheet.Cell(1, j + 2).Value = $"Eco{j + 1} %";
                }

                for (int i = 0; i < _allResults.Count; i++)
                {
                    sheet.Cell(i + 2, 1).Value = _allResults[i].SampleName;
                    for (int j = 0; j < 5; j++)
                    {
                        sheet.Cell(i + 2, j + 2).Value = Math.Round(_allResults[i].EcoGroups[j], 2);
                    }
                }

                int statsRow = _allResults.Count + 3;
                sheet.Cell(statsRow, 1).Value = "Mean";
                for (int j = 0; j < 5; j++)
                {
                    var values = _allResults.Select(r => r.EcoGroups[j]).ToList();
                    sheet.Cell(statsRow, j + 2).Value = Math.Round(values.Average(), 2);
                }

                statsRow++;
                sheet.Cell(statsRow, 1).Value = "StdDev";
                for (int j = 0; j < 5; j++)
                {
                    var values = _allResults.Select(r => r.EcoGroups[j]).ToList();
                    double mean = values.Average();
                    double sd = values.Count > 1
                        ? Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1))
                        : 0;
                    sheet.Cell(statsRow, j + 2).Value = Math.Round(sd, 2);
                }

                workbook.SaveAs(UiHelpers.EnsureExtension(saveDialog.FileName, ".xlsx"));
                MessageBox.Show("Eco Groups statistics exported successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting statistics: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region EQS agreement analysis (Cohen's Kappa and confusion matrix)

        // EQS class order used when building confusion matrices
        private readonly string[] _eqsClasses = { "High", "Good", "Moderate", "Poor", "Bad", "Azoic" };

        // Indices that carry an EQS classification
        private readonly string[] _eqsIndices =
        {
            "Foram-AMBI", "FSI", "TSI-Med", "NQIf", "exp(H'bc)", "BENTIX", "BQI", "Foram-M-AMBI"
        };

        private void ShowEQSAgreementAnalysis()
        {
            if (_allResults.Count == 0)
            {
                MessageBox.Show("No data available. Please calculate indices first.", "No Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dialog = new Dialog
            {
                Title = "EQS Agreement Analysis (Cohen's Kappa and Confusion Matrix)",
                ClientSize = new Size(1200, 800),
                MinimumSize = new Size(1000, 700)
            };
            dialog.Prepare();

            var tabControl = new TabControl
            {
                Pages =
                {
                    new TabPage { Text = "Cohen's Kappa Matrix", Content = BuildKappaTab(dialog) },
                    new TabPage { Text = "Confusion Matrices", Content = BuildConfusionMatrixTab() },
                    new TabPage { Text = "Agreement Heatmap", Content = BuildAgreementHeatmapTab(dialog) },
                    new TabPage { Text = "Summary Statistics", Content = BuildAgreementSummaryTab() }
                }
            };

            dialog.Content = tabControl;
            dialog.ShowModal(this);
        }

        private Control BuildKappaTab(Control parent)
        {
            var availableEqsIndices = _eqsIndices.Where(HasIndexData).ToList();

            if (availableEqsIndices.Count < 2)
            {
                return new Label
                {
                    Text = "At least 2 indices with EQS classifications are needed for agreement analysis.",
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            var kappaTable = new DataTable();
            kappaTable.Columns.Add("Index", typeof(string));
            foreach (var idx in availableEqsIndices)
            {
                kappaTable.Columns.Add(idx, typeof(string));
            }

            foreach (var idx1 in availableEqsIndices)
            {
                var row = kappaTable.NewRow();
                row["Index"] = idx1;
                foreach (var idx2 in availableEqsIndices)
                {
                    row[idx2] = idx1 == idx2 ? "1.00" : CalculateCohensKappa(idx1, idx2).ToString("F3");
                }
                kappaTable.Rows.Add(row);
            }

            var kappaGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = Compat.DataGridViewAutoSizeColumnsMode.AllCells
            };
            kappaGrid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 1 && double.TryParse(e.Value?.ToString(), out double kappa))
                {
                    e.CellStyle.BackColor = GetKappaColor(kappa);
                }
            };
            kappaGrid.DataSource = kappaTable;

            var legendLabel = new Label
            {
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

            var exportButton = new Button { Text = "Export to Excel", Width = 140 };
            exportButton.Click += (s, e) => ExportKappaMatrixToExcel(kappaTable, availableEqsIndices, parent);

            var legendPanel = new Scrollable
            {
                Border = BorderType.None,
                Content = new TableLayout
                {
                    Padding = new Padding(10),
                    Spacing = new Size(0, 10),
                    Rows =
                    {
                        new TableRow(legendLabel),
                        new TableRow(new TableCell(new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Items = { exportButton }
                        }, true)),
                        null
                    }
                }
            };

            return new Splitter
            {
                Orientation = Orientation.Vertical,
                Position = 400,
                Panel1 = kappaGrid,
                Panel2 = legendPanel
            };
        }

        private Control BuildConfusionMatrixTab()
        {
            var availableEqsIndices = _eqsIndices.Where(HasIndexData).ToArray();

            var combo1 = new DropDown { Width = 200 };
            var combo2 = new DropDown { Width = 200 };
            foreach (var idx in availableEqsIndices)
            {
                combo1.Items.Add(idx);
                combo2.Items.Add(idx);
            }
            if (combo1.Items.Count > 0)
            {
                combo1.SelectedIndex = 0;
            }
            if (combo2.Items.Count > 1)
            {
                combo2.SelectedIndex = 1;
            }

            var confusionGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = Compat.DataGridViewAutoSizeColumnsMode.AllCells
            };

            var statsLabel = new Label { Font = new Font(FontFamilies.Monospace, 10) };

            var generateButton = new Button { Text = "Generate Confusion Matrix", Width = 200 };
            generateButton.Click += (s, e) =>
            {
                if (combo1.SelectedIndex < 0 || combo2.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select both indices.", "Selection Required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                GenerateConfusionMatrix(
                    combo1.Items[combo1.SelectedIndex].Text,
                    combo2.Items[combo2.SelectedIndex].Text,
                    confusionGrid,
                    statsLabel);
            };

            return new TableLayout
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
                            new Label { Text = "Index 1:" }, combo1,
                            new Label { Text = "Index 2:" }, combo2,
                            generateButton
                        }
                    }, true)),
                    new TableRow(new Splitter
                    {
                        Orientation = Orientation.Horizontal,
                        Position = 560,
                        Panel1 = confusionGrid,
                        Panel2 = new Scrollable { Border = BorderType.None, Content = statsLabel }
                    }) { ScaleHeight = true }
                }
            };
        }

        private void GenerateConfusionMatrix(string index1, string index2, DataGridView grid, Label statsLabel)
        {
            var classifications1 = _allResults.Select(r => GetEQSClass(r, index1)).ToList();
            var classifications2 = _allResults.Select(r => GetEQSClass(r, index2)).ToList();

            var usedClasses = classifications1.Concat(classifications2)
                .Distinct()
                .OrderBy(c => Array.IndexOf(_eqsClasses, c))
                .ToList();

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

            for (int i = 0; i < _allResults.Count; i++)
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

            var totalsRow = confusionTable.NewRow();
            totalsRow[0] = "Total";
            for (int j = 0; j < usedClasses.Count; j++)
            {
                totalsRow[j + 1] = colTotals[j];
            }
            totalsRow[usedClasses.Count + 1] = _allResults.Count;
            confusionTable.Rows.Add(totalsRow);

            grid.Columns.Clear();
            grid.AutoGenerateColumns = true;
            grid.DataSource = confusionTable;

            // Highlight the diagonal, where both indices agree.
            grid.CellFormatting -= ConfusionDiagonalFormatting;
            grid.CellFormatting += ConfusionDiagonalFormatting;

            double kappa = CalculateCohensKappa(index1, index2);
            int agreement = 0;
            for (int i = 0; i < usedClasses.Count; i++)
            {
                agreement += matrix[i, i];
            }
            double percentAgreement = _allResults.Count > 0 ? (double)agreement / _allResults.Count * 100 : 0;

            statsLabel.Text = $@"Agreement Statistics for {index1} vs {index2}:

Samples Analyzed: {_allResults.Count}
Exact Agreement: {agreement} ({percentAgreement:F1}%)
Cohen's Kappa: {kappa:F3}

Kappa Interpretation:
{GetKappaInterpretation(kappa)}

Note: Diagonal cells (highlighted green) show samples
where both indices assigned the same EQS class.";
        }

        private static void ConfusionDiagonalFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex >= 1 && e.RowIndex == e.ColumnIndex - 1)
            {
                e.CellStyle.BackColor = Color.FromArgb(144, 238, 144);
            }
        }

        private Bitmap _currentKappaHeatmap;

        private Control BuildAgreementHeatmapTab(Control parent)
        {
            var availableEqsIndices = _eqsIndices.Where(HasIndexData).ToList();

            if (availableEqsIndices.Count < 2)
            {
                return new Label
                {
                    Text = "At least 2 indices with EQS classifications are needed for heatmap.",
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            _currentKappaHeatmap = CreateKappaHeatmapBitmap(availableEqsIndices);

            var imageView = new ImageView { Image = _currentKappaHeatmap };

            var exportButton = new Button { Text = "Export as PNG", Width = 140 };
            exportButton.Click += (s, e) => ExportKappaHeatmapToPng(parent);

            return new TableLayout
            {
                Rows =
                {
                    new TableRow(new Scrollable
                    {
                        BackgroundColor = Colors.White,
                        Content = imageView
                    }) { ScaleHeight = true },
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Padding = new Padding(5),
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { exportButton }
                    }, true))
                }
            };
        }

        /// <summary>Draws a Cohen's Kappa heatmap for every pair of indices.</summary>
        private Bitmap CreateKappaHeatmapBitmap(List<string> indices)
        {
            int n = indices.Count;
            var data = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    data[i, j] = i == j ? 1.0 : CalculateCohensKappa(indices[i], indices[j]);
                }
            }

            const int cellSize = 70;
            const int leftMargin = 120;   // Space for Y-axis labels
            const int topMargin = 50;     // Space for the title
            const int bottomMargin = 100; // Space for rotated X-axis labels
            const int rightMargin = 100;  // Space for the colour legend
            const int legendWidth = 30;
            int legendHeight = n * cellSize;

            int width = leftMargin + n * cellSize + rightMargin;
            int height = topMargin + n * cellSize + bottomMargin;

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);

            using (var g = new Graphics(bitmap))
            {
                g.Clear(Colors.White);
                g.AntiAlias = true;

                var titleFont = SystemFonts.Bold(14);
                var labelFont = SystemFonts.Default(9);
                var valueFont = SystemFonts.Bold(9);
                var legendFont = SystemFonts.Default(8);

                // Title
                const string title = "Cohen's Kappa Agreement Heatmap";
                var titleSize = g.MeasureString(titleFont, title);
                g.DrawText(titleFont, Colors.Black, (width - rightMargin / 2f - titleSize.Width) / 2, 10, title);

                // Cells
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        double kappa = data[i, j];
                        var cellColor = GetKappaHeatmapColor(kappa);

                        float x = leftMargin + j * cellSize;
                        float y = topMargin + i * cellSize;

                        g.FillRectangle(cellColor, x, y, cellSize, cellSize);
                        g.DrawRectangle(Colors.DarkGray, x, y, cellSize, cellSize);

                        string valueText = kappa.ToString("F2");
                        var textSize = g.MeasureString(valueFont, valueText);
                        g.DrawText(valueFont, GetContrastingTextColor(cellColor),
                            x + (cellSize - textSize.Width) / 2,
                            y + (cellSize - textSize.Height) / 2,
                            valueText);
                    }
                }

                // Y-axis labels
                for (int i = 0; i < n; i++)
                {
                    float y = topMargin + i * cellSize + cellSize / 2f;
                    var labelSize = g.MeasureString(labelFont, indices[i]);
                    g.DrawText(labelFont, Colors.Black, leftMargin - labelSize.Width - 5, y - labelSize.Height / 2, indices[i]);
                }

                // X-axis labels, rotated
                for (int j = 0; j < n; j++)
                {
                    float x = leftMargin + j * cellSize + cellSize / 2f;
                    float y = topMargin + n * cellSize + 5;

                    g.SaveTransform();
                    g.TranslateTransform(x, y);
                    g.RotateTransform(45);
                    g.DrawText(labelFont, Colors.Black, 0, 0, indices[j]);
                    g.RestoreTransform();
                }

                // Colour legend
                float legendX = leftMargin + n * cellSize + 20;
                const float legendY = topMargin;

                for (int i = 0; i < legendHeight; i++)
                {
                    double value = 1.0 - (double)i / legendHeight * 1.2;
                    value = Math.Max(-0.2, Math.Min(1.0, value));
                    g.DrawLine(GetKappaHeatmapColor(value), legendX, legendY + i, legendX + legendWidth, legendY + i);
                }

                g.DrawRectangle(Colors.Black, legendX, legendY, legendWidth, legendHeight);

                var legendLabels = new[] { "1.0", "0.8", "0.6", "0.4", "0.2", "0.0", "-0.2" };
                for (int i = 0; i < legendLabels.Length; i++)
                {
                    double value = 1.0 - i * 0.2;
                    float labelY = legendY + (float)((1.0 - value) / 1.2 * legendHeight);
                    g.DrawLine(Colors.Black, legendX + legendWidth, labelY, legendX + legendWidth + 3, labelY);
                    g.DrawText(legendFont, Colors.Black, legendX + legendWidth + 5, labelY - 6, legendLabels[i]);
                }

                const string legendTitle = "Cohen's Kappa";
                g.SaveTransform();
                g.TranslateTransform(legendX + legendWidth + 35, legendY + legendHeight / 2f);
                g.RotateTransform(-90);
                var legendTitleSize = g.MeasureString(labelFont, legendTitle);
                g.DrawText(labelFont, Colors.Black, -legendTitleSize.Width / 2, 0, legendTitle);
                g.RestoreTransform();

                // Dispose commits the bitmap. On WPF, Flush reopens an empty
                // drawing context which Dispose would then save over the heatmap.
            }

            return bitmap;
        }

        /// <summary>Red (poor) to white to blue (good) gradient for kappa values.</summary>
        private static Color GetKappaHeatmapColor(double kappa)
        {
            kappa = Math.Max(-0.2, Math.Min(1.0, kappa));
            double normalized = (kappa + 0.2) / 1.2;

            int r, g, b;
            if (normalized < 0.5)
            {
                double t = normalized * 2;
                r = 255;
                g = (int)(255 * t);
                b = (int)(255 * t);
            }
            else
            {
                double t = (normalized - 0.5) * 2;
                r = (int)(255 * (1 - t));
                g = (int)(255 * (1 - t));
                b = 255;
            }

            return Color.FromArgb(r, g, b);
        }

        private static Color GetContrastingTextColor(Color background)
        {
            double luminance = 0.299 * background.R + 0.587 * background.G + 0.114 * background.B;
            return luminance > 0.5 ? Colors.Black : Colors.White;
        }

        private void ExportKappaHeatmapToPng(Control parent)
        {
            if (_currentKappaHeatmap == null)
            {
                MessageBox.Show("No heatmap available to export.", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Kappa Heatmap",
                FileName = "Kappa_Heatmap.png",
                Filters = { Filters.Png }
            };

            if (saveDialog.ShowDialog(parent) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                string path = UiHelpers.EnsureExtension(saveDialog.FileName, ".png");
                _currentKappaHeatmap.Save(path, Eto.Drawing.ImageFormat.Png);
                MessageBox.Show($"Heatmap exported successfully to:\n{path}",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting heatmap: {ex.Message}",
                    "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Control BuildAgreementSummaryTab()
        {
            var availableEqsIndices = _eqsIndices.Where(HasIndexData).ToList();
            var sb = new StringBuilder();

            sb.AppendLine("╔═══════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║              EQS AGREEMENT ANALYSIS SUMMARY                                  ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Samples analyzed: {_allResults.Count}");
            sb.AppendLine($"Indices compared: {string.Join(", ", availableEqsIndices)}");
            sb.AppendLine();

            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("EQS CLASS DISTRIBUTION BY INDEX:");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            foreach (var idx in availableEqsIndices)
            {
                var classifications = _allResults.Select(r => GetEQSClass(r, idx)).ToList();
                var grouped = classifications.GroupBy(c => c).OrderBy(g => Array.IndexOf(_eqsClasses, g.Key));

                sb.AppendLine($"{idx}:");
                foreach (var g in grouped)
                {
                    double pct = (double)g.Count() / _allResults.Count * 100;
                    string bar = new string('█', (int)(pct / 5));
                    sb.AppendLine($"  {g.Key,-10} {g.Count(),4} ({pct,5:F1}%) {bar}");
                }
                sb.AppendLine();
            }

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

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("CONSENSUS ANALYSIS:");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            int fullConsensus = 0;
            int majorityConsensus = 0;
            foreach (var result in _allResults)
            {
                var classes = availableEqsIndices.Select(idx => GetEQSClass(result, idx)).ToList();
                if (classes.Count == 0)
                {
                    continue;
                }
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

            int total = Math.Max(1, _allResults.Count);
            sb.AppendLine($"Full consensus (all indices agree):     {fullConsensus,4} ({(double)fullConsensus / total * 100:F1}%)");
            sb.AppendLine($"Majority consensus (>50% agree):        {majorityConsensus,4} ({(double)majorityConsensus / total * 100:F1}%)");
            sb.AppendLine($"No clear consensus:                     {_allResults.Count - fullConsensus - majorityConsensus,4} " +
                          $"({(double)(_allResults.Count - fullConsensus - majorityConsensus) / total * 100:F1}%)");

            return new TextArea
            {
                Text = sb.ToString(),
                ReadOnly = true,
                Wrap = false,
                Font = new Font(FontFamilies.Monospace, 10)
            };
        }

        /// <summary>Cohen's Kappa between the EQS classifications of two indices.</summary>
        private double CalculateCohensKappa(string index1, string index2)
        {
            var classifications1 = _allResults.Select(r => GetEQSClass(r, index1)).ToList();
            var classifications2 = _allResults.Select(r => GetEQSClass(r, index2)).ToList();

            int n = _allResults.Count;
            if (n == 0)
            {
                return 0;
            }

            var allClasses = classifications1.Concat(classifications2).Distinct().ToList();
            int k = allClasses.Count;

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

            // Observed agreement
            double po = 0;
            for (int i = 0; i < k; i++)
            {
                po += matrix[i, i];
            }
            po /= n;

            // Expected agreement
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

            if (pe >= 1.0)
            {
                return 1.0;
            }
            return (po - pe) / (1 - pe);
        }

        private static string GetEQSClass(IndicesResult result, string index)
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

        private bool HasIndexData(string index)
        {
            if (_allResults.Count == 0)
            {
                return false;
            }

            return index switch
            {
                "Foram-AMBI" or "F-AMBI" => _allResults.Any(r => r.FAMBI > 0),
                "FSI" => _allResults.Any(r => r.FSI > 0),
                "TSI-Med" => _allResults.Any(r => r.TSI_Med >= 0),
                "NQIf" => _allResults.Any(r => r.NQIf > 0),
                "exp(H'bc)" => _allResults.Any(r => r.Exp_Hbc > 0),
                "BENTIX" => _allResults.Any(r => r.BENTIX > 0),
                "BQI" => _allResults.Any(r => r.BQI > 0),
                "Foram-M-AMBI" => _allResults.Any(r => r.ForamMAMBI > 0),
                _ => false
            };
        }

        private static Color GetKappaColor(double kappa)
        {
            if (kappa >= 0.81) return Color.FromArgb(0, 128, 0);       // Dark green - Almost perfect
            if (kappa >= 0.61) return Color.FromArgb(144, 238, 144);   // Light green - Substantial
            if (kappa >= 0.41) return Color.FromArgb(255, 255, 150);   // Light yellow - Moderate
            if (kappa >= 0.21) return Color.FromArgb(255, 200, 100);   // Orange - Fair
            if (kappa >= 0.00) return Color.FromArgb(255, 150, 150);   // Light red - Slight
            return Color.FromArgb(255, 100, 100);                      // Red - Poor
        }

        private static string GetKappaInterpretation(double kappa)
        {
            if (kappa >= 0.81) return "Almost perfect";
            if (kappa >= 0.61) return "Substantial";
            if (kappa >= 0.41) return "Moderate";
            if (kappa >= 0.21) return "Fair";
            if (kappa >= 0.00) return "Slight";
            return "Poor";
        }

        private void ExportKappaMatrixToExcel(DataTable kappaTable, List<string> indices, Control parent)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Kappa Matrix",
                FileName = "EQS_Agreement_Kappa_Matrix.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (saveDialog.ShowDialog(parent) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Kappa Matrix");

                ws.Cell(1, 1).Value = "Index";
                for (int i = 0; i < indices.Count; i++)
                {
                    ws.Cell(1, i + 2).Value = indices[i];
                }

                for (int row = 0; row < kappaTable.Rows.Count; row++)
                {
                    for (int col = 0; col < kappaTable.Columns.Count; col++)
                    {
                        ws.Cell(row + 2, col + 1).Value = kappaTable.Rows[row][col].ToString();
                    }
                }

                ws.Range(1, 1, 1, indices.Count + 1).Style.Font.Bold = true;
                ws.Range(1, 1, 1, indices.Count + 1).Style.Fill.BackgroundColor = XLColor.LightGray;

                for (int row = 0; row < indices.Count; row++)
                {
                    for (int col = 0; col < indices.Count; col++)
                    {
                        if (double.TryParse(kappaTable.Rows[row][col + 1]?.ToString(), out double kappa))
                        {
                            ws.Cell(row + 2, col + 2).Style.Fill.BackgroundColor = GetKappaColor(kappa).ToXLColor();
                        }
                    }
                }

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

                workbook.SaveAs(UiHelpers.EnsureExtension(saveDialog.FileName, ".xlsx"));
                MessageBox.Show("Kappa matrix exported successfully!", "Export Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting kappa matrix: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
