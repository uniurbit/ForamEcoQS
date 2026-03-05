//MIT License

using ClosedXML.Excel;
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.WindowsForms;
using OxyPlot.Axes;
using System.Text;
using System.Data;
using System.Linq;
using OxyPlot.Legends;

namespace ForamEcoQS
{
    public partial class FambiResultsForm : Form
    {
        public FambiResultsForm()
        {
            InitializeComponent();
        }

        private void FambiResultsForm_Load(object sender, EventArgs e)
        {

        }
        public void PopulateDataGridFAMBI(DataTable dataTable)
        {
            dataGridFAMBI.DataSource = dataTable;
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create SaveFileDialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Save as Excel File",
                FileName = "FAMBI_Data.xlsx"
            };

            // Show the dialog and check if the user clicked "Save"
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName;

                // Save the DataGridView data to an Excel file
                SaveDataGridViewToExcel(filePath);
            }
        }

        private void SaveDataGridViewToExcel(string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("FAMBI_Data");

                // Add the column headers
                for (int i = 0; i < dataGridFAMBI.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = dataGridFAMBI.Columns[i].HeaderText;
                }

                // Add the rows
                for (int i = 0; i < dataGridFAMBI.Rows.Count; i++)
                {
                    for (int j = 0; j < dataGridFAMBI.Columns.Count; j++)
                    {
                        worksheet.Cell(i + 2, j + 1).Value = dataGridFAMBI.Rows[i].Cells[j].Value?.ToString() ?? string.Empty;
                    }
                }

                // Save the file
                workbook.SaveAs(filePath);
            }
        }

        private void plotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Extract F-AMBI values from the last row of the dataGridFAMBI
            var fambiRow = dataGridFAMBI.Rows[dataGridFAMBI.Rows.Count - 1];

            // Prepare data for plotting
            double[] fambiValues = new double[dataGridFAMBI.Columns.Count - 1];
            string[] sampleNames = new string[dataGridFAMBI.Columns.Count - 1];
            StringBuilder statsBuilder = new StringBuilder();

            for (int i = 1; i < dataGridFAMBI.Columns.Count; i++)
            {
                fambiValues[i - 1] = Convert.ToDouble(fambiRow.Cells[i].Value);
                sampleNames[i - 1] = dataGridFAMBI.Columns[i].HeaderText;
            }

            // Create a new form for the plot and make it larger
            Form plotForm = new Form();
            plotForm.Text = "Foram-AMBI Box Plot";
            plotForm.Size = new System.Drawing.Size(900, 700);
            plotForm.Icon = this.Icon;

            // Create a PlotView control and add it to the form
            var plotView = new PlotView
            {
                Dock = DockStyle.Fill
            };
            plotForm.Controls.Add(plotView);

            // Create the plot model
            var plotModel = new PlotModel { Title = "Foram-AMBI Values by Sample" };

            // Adjust plot margins to prevent boxes from being cut off
            plotModel.PlotMargins = new OxyThickness(60, 20, 80, 40);

            // Create a BoxPlotSeries
            var boxPlotSeries = new BoxPlotSeries
            {
                Title = "Foram-AMBI",
                BoxWidth = 0.3,
                Stroke = OxyColors.Black,
                Fill = OxyColor.FromRgb(100, 149, 237),
                WhiskerWidth = 0.5,
                MedianThickness = 2
            };

            var allValues = new List<double>();

            // Calculate and add BoxPlot items for each sample
            for (int i = 1; i < dataGridFAMBI.Columns.Count; i++)
            {
                List<double> sampleValues = new List<double>();

                // Collect all F-AMBI values from the column (ignoring empty cells or non-numeric values)
                foreach (DataGridViewRow row in dataGridFAMBI.Rows)
                {
                    if (row.Cells[i].Value != null && double.TryParse(row.Cells[i].Value.ToString(), out double value))
                    {
                        sampleValues.Add(value);
                        allValues.Add(value);
                    }
                }

                // Ensure we have enough data points to calculate statistics
                if (sampleValues.Count > 0)
                {
                    sampleValues.Sort();

                    double min = sampleValues.First();
                    double max = sampleValues.Last();
                    double median = GetMedian(sampleValues.ToArray());
                    double q1 = GetPercentile(sampleValues.ToArray(), 25);
                    double q3 = GetPercentile(sampleValues.ToArray(), 75);

                    // Add the BoxPlotItem using the calculated values
                    boxPlotSeries.Items.Add(new BoxPlotItem(i - 1, min, q1, median, q3, max));

                    // Append statistics to the StringBuilder
                    statsBuilder.AppendLine($"{sampleNames[i - 1]}: Min={min:F3}, Q1={q1:F3}, Med={median:F3}, Q3={q3:F3}, Max={max:F3}");
                }
            }

            // Add the series to the plot model
            plotModel.Series.Add(boxPlotSeries);

            // Add overall statistics as subtitle
            if (allValues.Count > 0)
            {
                double mean = allValues.Average();
                double sd = allValues.Count > 1 ? Math.Sqrt(allValues.Sum(v => Math.Pow(v - mean, 2)) / (allValues.Count - 1)) : 0;
                plotModel.Subtitle = $"Overall: n={allValues.Count}, Mean={mean:F3}, SD={sd:F3}, Min={allValues.Min():F3}, Max={allValues.Max():F3}";
                plotModel.SubtitleFontSize = 10;
                plotModel.SubtitleColor = OxyColors.DarkGray;
            }

            // Explicitly add labels to the X-axis using a CategoryAxis
            var categoryAxis = new OxyPlot.Axes.CategoryAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                Angle = 45
            };

            // Explicitly add each sample name as a label
            foreach (var sampleName in sampleNames)
            {
                categoryAxis.Labels.Add(sampleName);
            }

            plotModel.Axes.Add(categoryAxis);

            // Add a Y-axis
            var valueAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Foram-AMBI Value"
            };
            plotModel.Axes.Add(valueAxis);

            // Add legend
            plotModel.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendBackground = OxyColors.White,
                LegendBorder = OxyColor.FromRgb(180, 180, 180),
                LegendBorderThickness = 1,
                LegendPadding = 10,
                LegendItemSpacing = 8,
                LegendLineSpacing = 4,
                LegendSymbolMargin = 10
            });

            // Assign the plot model to the PlotView
            plotView.Model = plotModel;

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };

            var saveButton = new Button
            {
                Text = "Save as PNG",
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveButton.Click += (s, ev) => SavePlotAsPng(plotView.Model);

            var exportStatsButton = new Button
            {
                Text = "Export Stats",
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            exportStatsButton.Click += (s, ev) => ExportFambiStats(sampleNames, statsBuilder.ToString());

            buttonPanel.Controls.AddRange(new Control[] { saveButton, exportStatsButton });
            plotForm.Controls.Add(buttonPanel);

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

        private void SavePlotAsPng(PlotModel plotModel)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg";
                saveFileDialog.Title = "Save the plot";
                saveFileDialog.FileName = "FAMBI_Plot.png";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    int width = 800;
                    int height = 600;
                    var pngExporter = new PngExporter { Width = width, Height = height };
                    using (var stream = System.IO.File.Create(saveFileDialog.FileName))
                    {
                        pngExporter.Export(plotModel, stream);
                    }
                    MessageBox.Show("Plot exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExportFambiStats(string[] sampleNames, string statsText)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Excel Files|*.xlsx|Text Files|*.txt";
                saveFileDialog.Title = "Export Foram-AMBI Statistics";
                saveFileDialog.FileName = "FAMBI_Stats.xlsx";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string ext = System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower();

                    if (ext == ".xlsx")
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var sheet = workbook.Worksheets.Add("FAMBI_Stats");

                            // Copy the statistics text
                            string[] lines = statsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < lines.Length; i++)
                            {
                                sheet.Cell(i + 1, 1).Value = lines[i];
                            }

                            workbook.SaveAs(saveFileDialog.FileName);
                        }
                    }
                    else
                    {
                        System.IO.File.WriteAllText(saveFileDialog.FileName, statsText);
                    }

                    MessageBox.Show("Statistics exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }


        private double GetMedian(double[] values)
        {
            int size = values.Length;
            int mid = size / 2;
            if (size % 2 != 0)
                return values[mid];
            return (values[mid - 1] + values[mid]) / 2.0;
        }

        private double GetPercentile(double[] values, double percentile)
        {
            Array.Sort(values);
            int N = values.Length;
            double n = (N - 1) * percentile / 100.0 + 1;

            if (n == 1d) return values[0];
            else if (n == N) return values[N - 1];
            else
            {
                int k = (int)n;
                double d = n - k;
                return values[k - 1] + d * (values[k] - values[k - 1]);
            }
        }



        private void ecoGroupsPlotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // List of Eco rows to plot
            string[] ecoGroupNames = { "Eco1", "Eco2", "Eco3", "Eco4", "Eco5" };

            // Prepare data for plotting
            var ecoGroupData = new Dictionary<string, double[]>();
            var sampleNames = new string[dataGridFAMBI.Columns.Count - 1];

            for (int i = 0; i < ecoGroupNames.Length; i++)
            {
                var row = dataGridFAMBI.Rows.Cast<DataGridViewRow>()
                           .FirstOrDefault(r => r.Cells[0].Value?.ToString() == ecoGroupNames[i]);

                if (row != null)
                {
                    double[] values = new double[dataGridFAMBI.Columns.Count - 1];
                    for (int j = 1; j < dataGridFAMBI.Columns.Count; j++)
                    {
                        if (double.TryParse(row.Cells[j].Value?.ToString(), out double value))
                        {
                            values[j - 1] = value;
                        }
                        else
                        {
                            values[j - 1] = 0;
                        }
                        sampleNames[j - 1] = dataGridFAMBI.Columns[j].HeaderText;
                    }
                    ecoGroupData[ecoGroupNames[i]] = values;
                }
            }

            // Create a new form for the plot with two subplots
            Form plotForm = new Form();
            plotForm.Text = "Eco Groups Plot";
            plotForm.Size = new System.Drawing.Size(1200, 700);
            plotForm.Icon = this.Icon;

            // Create a split panel for two plots
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Define colorblind-friendly colors for eco groups
            OxyColor[] ecoColors = new[]
            {
                OxyColor.FromRgb(0, 114, 178),    // Blue
                OxyColor.FromRgb(0, 158, 115),    // Green
                OxyColor.FromRgb(240, 228, 66),   // Yellow
                OxyColor.FromRgb(230, 159, 0),    // Orange
                OxyColor.FromRgb(213, 94, 0)      // Red
            };

            // ========== Plot 1: Line Plot ==========
            var linePlotModel = new PlotModel { Title = "Eco Groups - Line Plot" };

            int colorIndex = 0;
            StringBuilder statsBuilder = new StringBuilder();
            StringBuilder subtitleBuilder = new StringBuilder("Mean: ");

            foreach (var ecoGroup in ecoGroupData)
            {
                var lineSeries = new LineSeries
                {
                    Title = ecoGroup.Key,
                    Color = ecoColors[colorIndex % ecoColors.Length],
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 6,
                    MarkerStroke = ecoColors[colorIndex % ecoColors.Length],
                    MarkerFill = ecoColors[colorIndex % ecoColors.Length],
                    StrokeThickness = 2
                };

                for (int i = 0; i < ecoGroup.Value.Length; i++)
                {
                    lineSeries.Points.Add(new DataPoint(i, ecoGroup.Value[i]));
                }

                linePlotModel.Series.Add(lineSeries);

                // Calculate statistics
                double min = ecoGroup.Value.Min();
                double max = ecoGroup.Value.Max();
                double avg = ecoGroup.Value.Average();
                double sd = ecoGroup.Value.Length > 1 ? Math.Sqrt(ecoGroup.Value.Sum(v => Math.Pow(v - avg, 2)) / (ecoGroup.Value.Length - 1)) : 0;
                statsBuilder.AppendLine($"{ecoGroup.Key}: Min={min:F2}%, Max={max:F2}%, Mean={avg:F2}%, SD={sd:F2}%");
                subtitleBuilder.Append($"{ecoGroup.Key}={avg:F1}%  ");

                colorIndex++;
            }

            linePlotModel.Subtitle = subtitleBuilder.ToString().TrimEnd();
            linePlotModel.SubtitleFontSize = 10;
            linePlotModel.SubtitleColor = OxyColors.DarkGray;

            var lineCategoryAxis = new OxyPlot.Axes.CategoryAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                Angle = 45
            };
            foreach (var sampleName in sampleNames)
            {
                lineCategoryAxis.Labels.Add(sampleName);
            }
            linePlotModel.Axes.Add(lineCategoryAxis);

            var lineValueAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Percentage (%)",
                Minimum = 0,
                Maximum = 100
            };
            linePlotModel.Axes.Add(lineValueAxis);

            linePlotModel.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendBackground = OxyColors.White,
                LegendBorder = OxyColor.FromRgb(180, 180, 180),
                LegendBorderThickness = 1,
                LegendPadding = 10,
                LegendItemSpacing = 8,
                LegendLineSpacing = 4,
                LegendSymbolMargin = 10
            });

            var linePlotView = new PlotView { Dock = DockStyle.Fill, Model = linePlotModel };
            tableLayout.Controls.Add(linePlotView, 0, 0);

            // ========== Plot 2: Box Plot ==========
            var boxPlotModel = new PlotModel { Title = "Eco Groups - Box Plot" };

            var boxCategoryAxis = new OxyPlot.Axes.CategoryAxis { Position = OxyPlot.Axes.AxisPosition.Bottom };
            boxCategoryAxis.Labels.AddRange(ecoGroupNames);
            boxPlotModel.Axes.Add(boxCategoryAxis);

            var boxValueAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Percentage (%)",
                Minimum = 0,
                Maximum = 100
            };
            boxPlotModel.Axes.Add(boxValueAxis);

            StringBuilder boxStatsBuilder = new StringBuilder();

            for (int i = 0; i < ecoGroupNames.Length; i++)
            {
                if (ecoGroupData.TryGetValue(ecoGroupNames[i], out double[] values))
                {
                    var sortedValues = values.OrderBy(v => v).ToList();

                    if (sortedValues.Count > 0)
                    {
                        double min = sortedValues.First();
                        double max = sortedValues.Last();
                        double median = GetMedian(sortedValues.ToArray());
                        double q1 = GetPercentile(sortedValues.ToArray(), 25);
                        double q3 = GetPercentile(sortedValues.ToArray(), 75);

                        var boxSeries = new BoxPlotSeries
                        {
                            Title = ecoGroupNames[i],
                            Fill = ecoColors[i],
                            Stroke = OxyColors.Black,
                            StrokeThickness = 1.5,
                            BoxWidth = 0.5,
                            WhiskerWidth = 0.6,
                            MedianThickness = 2
                        };
                        boxSeries.Items.Add(new BoxPlotItem(i, min, q1, median, q3, max));
                        boxPlotModel.Series.Add(boxSeries);

                        boxStatsBuilder.Append($"{ecoGroupNames[i]}: Med={median:F1}%  ");
                    }
                }
            }

            boxPlotModel.Subtitle = boxStatsBuilder.ToString().TrimEnd();
            boxPlotModel.SubtitleFontSize = 10;
            boxPlotModel.SubtitleColor = OxyColors.DarkGray;

            boxPlotModel.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendBackground = OxyColors.White,
                LegendBorder = OxyColor.FromRgb(180, 180, 180),
                LegendBorderThickness = 1,
                LegendPadding = 10,
                LegendItemSpacing = 8,
                LegendLineSpacing = 4,
                LegendSymbolMargin = 10
            });

            var boxPlotView = new PlotView { Dock = DockStyle.Fill, Model = boxPlotModel };
            tableLayout.Controls.Add(boxPlotView, 1, 0);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };

            var saveButton = new Button
            {
                Text = "Save as PNG",
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveButton.Click += (s, ev) => SaveEcoGroupsCompositePng(tableLayout);

            var exportStatsButton = new Button
            {
                Text = "Export Stats",
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            exportStatsButton.Click += (s, ev) => ExportEcoGroupsStatsForm3(ecoGroupData, sampleNames);

            buttonPanel.Controls.AddRange(new Control[] { saveButton, exportStatsButton });

            // Create a label to display the statistics
            var statsLabel = new Label
            {
                Text = statsBuilder.ToString(),
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(10),
                Font = new Font("Consolas", 9)
            };

            plotForm.Controls.Add(tableLayout);
            plotForm.Controls.Add(buttonPanel);
            plotForm.Controls.Add(statsLabel);

            // Display the form
            plotForm.ShowDialog();
        }

        private void SaveEcoGroupsCompositePng(TableLayoutPanel layout)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PNG Image|*.png";
                saveFileDialog.Title = "Save Eco Groups Plot";
                saveFileDialog.FileName = "EcoGroups_Composite.png";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var bitmap = new Bitmap(layout.Width, layout.Height))
                    {
                        layout.DrawToBitmap(bitmap, new Rectangle(0, 0, layout.Width, layout.Height));
                        bitmap.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    MessageBox.Show("Plot saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExportEcoGroupsStatsForm3(Dictionary<string, double[]> ecoGroupData, string[] sampleNames)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Excel Files|*.xlsx";
                saveFileDialog.Title = "Export Eco Groups Statistics";
                saveFileDialog.FileName = "EcoGroups_Stats.xlsx";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var sheet = workbook.Worksheets.Add("EcoGroups");

                        // Headers
                        sheet.Cell(1, 1).Value = "Sample";
                        int col = 2;
                        foreach (var ecoGroup in ecoGroupData.Keys)
                        {
                            sheet.Cell(1, col).Value = ecoGroup + " %";
                            col++;
                        }

                        // Data
                        for (int i = 0; i < sampleNames.Length; i++)
                        {
                            sheet.Cell(i + 2, 1).Value = sampleNames[i];
                            col = 2;
                            foreach (var ecoGroup in ecoGroupData.Values)
                            {
                                sheet.Cell(i + 2, col).Value = Math.Round(ecoGroup[i], 2);
                                col++;
                            }
                        }

                        // Statistics
                        int statsRow = sampleNames.Length + 3;
                        sheet.Cell(statsRow, 1).Value = "Mean";
                        col = 2;
                        foreach (var ecoGroup in ecoGroupData.Values)
                        {
                            sheet.Cell(statsRow, col).Value = Math.Round(ecoGroup.Average(), 2);
                            col++;
                        }

                        statsRow++;
                        sheet.Cell(statsRow, 1).Value = "StdDev";
                        col = 2;
                        foreach (var ecoGroup in ecoGroupData.Values)
                        {
                            double mean = ecoGroup.Average();
                            double sd = Math.Sqrt(ecoGroup.Sum(v => Math.Pow(v - mean, 2)) / (ecoGroup.Length - 1));
                            sheet.Cell(statsRow, col).Value = Math.Round(sd, 2);
                            col++;
                        }

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show("Eco Groups statistics exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // Function to generate gradient colors between two colors
        private OxyColor[] GenerateGradientColors(int count, OxyColor startColor, OxyColor endColor)
        {
            OxyColor[] colors = new OxyColor[count];
            for (int i = 0; i < count; i++)
            {
                double ratio = (double)i / (count - 1);
                byte r = (byte)((endColor.R - startColor.R) * ratio + startColor.R);
                byte g = (byte)((endColor.G - startColor.G) * ratio + startColor.G);
                byte b = (byte)((endColor.B - startColor.B) * ratio + startColor.B);
                colors[i] = OxyColor.FromRgb(r, g, b);
            }
            return colors;
        }
    }
}
