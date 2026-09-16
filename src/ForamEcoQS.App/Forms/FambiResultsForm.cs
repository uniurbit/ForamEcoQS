//MIT License
// FambiResultsForm.cs - Stand-alone Foram-AMBI results viewer with box plot and eco-group plots.

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

namespace ForamEcoQS
{
    public class FambiResultsForm : Form
    {
        private readonly DataGridView _dataGridFambi;

        public FambiResultsForm()
        {
            Title = "FAMBI";
            ClientSize = new Size(1014, 648);
            this.Prepare();

            _dataGridFambi = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true
            };

            Menu = new MenuBar
            {
                Items =
                {
                    new SubMenuItem
                    {
                        Text = "&Save",
                        Items = { new Command((s, e) => SaveToExcel()) { MenuText = "Save as Excel..." } }
                    },
                    new SubMenuItem
                    {
                        Text = "&BoxPlot",
                        Items = { new Command((s, e) => ShowBoxPlot()) { MenuText = "Show Box Plot" } }
                    },
                    new SubMenuItem
                    {
                        Text = "&EcoGroups Plot",
                        Items = { new Command((s, e) => ShowEcoGroupsPlot()) { MenuText = "Show Eco Groups Plot" } }
                    }
                }
            };

            Content = _dataGridFambi;
        }

        public void PopulateDataGridFAMBI(DataTable dataTable)
        {
            _dataGridFambi.Columns.Clear();
            _dataGridFambi.AutoGenerateColumns = true;
            _dataGridFambi.DataSource = dataTable;
        }

        #region Excel export

        private void SaveToExcel()
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Save as Excel File",
                FileName = "FAMBI_Data.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (saveDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("FAMBI_Data");

                for (int i = 0; i < _dataGridFambi.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = _dataGridFambi.Columns[i].HeaderText;
                }

                for (int i = 0; i < _dataGridFambi.Rows.Count; i++)
                {
                    for (int j = 0; j < _dataGridFambi.Columns.Count; j++)
                    {
                        worksheet.Cell(i + 2, j + 1).Value =
                            _dataGridFambi.Rows[i].Cells[j].Value?.ToString() ?? string.Empty;
                    }
                }

                workbook.SaveAs(UiHelpers.EnsureExtension(saveDialog.FileName, ".xlsx"));
                MessageBox.Show("Data saved successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Box plot

        private void ShowBoxPlot()
        {
            if (_dataGridFambi.Rows.Count == 0 || _dataGridFambi.Columns.Count < 2)
            {
                MessageBox.Show("No data to plot.", "Box Plot", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int sampleCount = _dataGridFambi.Columns.Count - 1;
            var sampleNames = new string[sampleCount];
            var statsBuilder = new StringBuilder();

            for (int i = 1; i < _dataGridFambi.Columns.Count; i++)
            {
                sampleNames[i - 1] = _dataGridFambi.Columns[i].HeaderText;
            }

            var plotModel = new PlotModel
            {
                Title = "Foram-AMBI Values by Sample",
                PlotMargins = new OxyThickness(60, 20, 80, 40)
            };

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

            for (int i = 1; i < _dataGridFambi.Columns.Count; i++)
            {
                var sampleValues = new List<double>();

                foreach (var row in _dataGridFambi.Rows)
                {
                    if (row.Cells[i].Value != null && double.TryParse(row.Cells[i].Value.ToString(), out double value))
                    {
                        sampleValues.Add(value);
                        allValues.Add(value);
                    }
                }

                if (sampleValues.Count == 0)
                {
                    continue;
                }

                sampleValues.Sort();

                double min = sampleValues.First();
                double max = sampleValues.Last();
                double median = GetMedian(sampleValues);
                double q1 = GetPercentile(sampleValues, 25);
                double q3 = GetPercentile(sampleValues, 75);

                boxPlotSeries.Items.Add(new BoxPlotItem(i - 1, min, q1, median, q3, max));

                statsBuilder.AppendLine(
                    $"{sampleNames[i - 1]}: Min={min:F3}, Q1={q1:F3}, Med={median:F3}, Q3={q3:F3}, Max={max:F3}");
            }

            plotModel.Series.Add(boxPlotSeries);

            if (allValues.Count > 0)
            {
                double mean = allValues.Average();
                double sd = allValues.Count > 1
                    ? Math.Sqrt(allValues.Sum(v => Math.Pow(v - mean, 2)) / (allValues.Count - 1))
                    : 0;
                plotModel.Subtitle = $"Overall: n={allValues.Count}, Mean={mean:F3}, SD={sd:F3}, " +
                                     $"Min={allValues.Min():F3}, Max={allValues.Max():F3}";
                plotModel.SubtitleFontSize = 10;
                plotModel.SubtitleColor = OxyColors.DarkGray;
            }

            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                Angle = 45
            };
            categoryAxis.Labels.AddRange(sampleNames);
            plotModel.Axes.Add(categoryAxis);

            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Foram-AMBI Value" });

            ConfigurePlotStyle(plotModel);
            AddLegend(plotModel);

            ShowPlotWindow("Foram-AMBI Box Plot", new List<PlotModel> { plotModel }, 1,
                new Size(900, 700), statsBuilder.ToString(), "FAMBI_Plot.png", "FAMBI_Stats.xlsx");
        }

        #endregion

        #region Eco groups plot

        private void ShowEcoGroupsPlot()
        {
            string[] ecoGroupNames = { "Eco1", "Eco2", "Eco3", "Eco4", "Eco5" };

            var ecoGroupData = new Dictionary<string, double[]>();
            int sampleCount = _dataGridFambi.Columns.Count - 1;
            if (sampleCount <= 0)
            {
                MessageBox.Show("No data to plot.", "Eco Groups Plot",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sampleNames = new string[sampleCount];
            for (int j = 1; j < _dataGridFambi.Columns.Count; j++)
            {
                sampleNames[j - 1] = _dataGridFambi.Columns[j].HeaderText;
            }

            foreach (var groupName in ecoGroupNames)
            {
                var row = _dataGridFambi.Rows.FirstOrDefault(r => r.Cells[0].Value?.ToString() == groupName);
                if (row == null)
                {
                    continue;
                }

                var values = new double[sampleCount];
                for (int j = 1; j < _dataGridFambi.Columns.Count; j++)
                {
                    values[j - 1] = double.TryParse(row.Cells[j].Value?.ToString(), out double value) ? value : 0;
                }
                ecoGroupData[groupName] = values;
            }

            if (ecoGroupData.Count == 0)
            {
                MessageBox.Show("No Eco1-Eco5 rows found in the results.", "Eco Groups Plot",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ecoColors = new[]
            {
                OxyColor.FromRgb(0, 114, 178),    // Blue
                OxyColor.FromRgb(0, 158, 115),    // Green
                OxyColor.FromRgb(240, 228, 66),   // Yellow
                OxyColor.FromRgb(230, 159, 0),    // Orange
                OxyColor.FromRgb(213, 94, 0)      // Red
            };

            // ---- Line plot ----
            var linePlotModel = new PlotModel { Title = "Eco Groups - Line Plot" };
            var statsBuilder = new StringBuilder();
            var subtitleBuilder = new StringBuilder("Mean: ");
            int colorIndex = 0;

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

                double min = ecoGroup.Value.Min();
                double max = ecoGroup.Value.Max();
                double avg = ecoGroup.Value.Average();
                double sd = ecoGroup.Value.Length > 1
                    ? Math.Sqrt(ecoGroup.Value.Sum(v => Math.Pow(v - avg, 2)) / (ecoGroup.Value.Length - 1))
                    : 0;
                statsBuilder.AppendLine($"{ecoGroup.Key}: Min={min:F2}%, Max={max:F2}%, Mean={avg:F2}%, SD={sd:F2}%");
                subtitleBuilder.Append($"{ecoGroup.Key}={avg:F1}%  ");

                colorIndex++;
            }

            linePlotModel.Subtitle = subtitleBuilder.ToString().TrimEnd();
            linePlotModel.SubtitleFontSize = 10;
            linePlotModel.SubtitleColor = OxyColors.DarkGray;

            var lineCategoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                Angle = 45
            };
            lineCategoryAxis.Labels.AddRange(sampleNames);
            linePlotModel.Axes.Add(lineCategoryAxis);
            linePlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Percentage (%)",
                Minimum = 0,
                Maximum = 100
            });
            ConfigurePlotStyle(linePlotModel);
            AddLegend(linePlotModel);

            // ---- Box plot ----
            var boxPlotModel = new PlotModel { Title = "Eco Groups - Box Plot" };
            var boxCategoryAxis = new CategoryAxis { Position = AxisPosition.Bottom };
            boxCategoryAxis.Labels.AddRange(ecoGroupNames);
            boxPlotModel.Axes.Add(boxCategoryAxis);
            boxPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Percentage (%)",
                Minimum = 0,
                Maximum = 100
            });

            var boxStatsBuilder = new StringBuilder();

            for (int i = 0; i < ecoGroupNames.Length; i++)
            {
                if (!ecoGroupData.TryGetValue(ecoGroupNames[i], out double[] values) || values.Length == 0)
                {
                    continue;
                }

                var sortedValues = values.OrderBy(v => v).ToList();

                double min = sortedValues.First();
                double max = sortedValues.Last();
                double median = GetMedian(sortedValues);
                double q1 = GetPercentile(sortedValues, 25);
                double q3 = GetPercentile(sortedValues, 75);

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

            boxPlotModel.Subtitle = boxStatsBuilder.ToString().TrimEnd();
            boxPlotModel.SubtitleFontSize = 10;
            boxPlotModel.SubtitleColor = OxyColors.DarkGray;
            ConfigurePlotStyle(boxPlotModel);
            AddLegend(boxPlotModel);

            ShowPlotWindow("Eco Groups Plot", new List<PlotModel> { linePlotModel, boxPlotModel }, 2,
                new Size(1200, 700), statsBuilder.ToString(), "EcoGroups_Composite.png", "EcoGroups_Stats.xlsx",
                () => ExportEcoGroupsStats(ecoGroupData, sampleNames));
        }

        private void ExportEcoGroupsStats(Dictionary<string, double[]> ecoGroupData, string[] sampleNames)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Eco Groups Statistics",
                FileName = "EcoGroups_Stats.xlsx",
                Filters = { Filters.Xlsx }
            };

            if (saveDialog.ShowDialog(this) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("EcoGroups");

                sheet.Cell(1, 1).Value = "Sample";
                int col = 2;
                foreach (var ecoGroup in ecoGroupData.Keys)
                {
                    sheet.Cell(1, col).Value = ecoGroup + " %";
                    col++;
                }

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
                    double sd = ecoGroup.Length > 1
                        ? Math.Sqrt(ecoGroup.Sum(v => Math.Pow(v - mean, 2)) / (ecoGroup.Length - 1))
                        : 0;
                    sheet.Cell(statsRow, col).Value = Math.Round(sd, 2);
                    col++;
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

        #region Helpers

        private void ShowPlotWindow(
            string title,
            List<PlotModel> models,
            int columns,
            Size size,
            string statsText,
            string defaultImageName,
            string defaultStatsName,
            Action customStatsExport = null)
        {
            var window = new Form { Title = title, ClientSize = size };
            window.Prepare();

            int rows = (int)Math.Ceiling((double)models.Count / columns);
            var table = new TableLayout(columns, rows) { Spacing = new Size(4, 4) };
            for (int i = 0; i < models.Count; i++)
            {
                table.Add(new PlotView { Model = PlotFonts.ApplyTo(models[i]) }, i % columns, i / columns, true, true);
            }

            var saveButton = UiHelpers.ActionButton("Save as PNG", AppColors.SteelBlue, null, 140);
            saveButton.Click += (s, e) => SavePlots(models, columns, defaultImageName, window);

            var exportStatsButton = UiHelpers.ActionButton("Export Stats", AppColors.SeaGreen, null, 140);
            exportStatsButton.Click += (s, e) =>
            {
                if (customStatsExport != null)
                {
                    customStatsExport();
                }
                else
                {
                    ExportStatsText(statsText, defaultStatsName, window);
                }
            };

            var statsLabel = new Label
            {
                Text = statsText,
                Font = new Font(FontFamilies.Monospace, 9)
            };

            window.Content = new TableLayout
            {
                Padding = new Padding(6),
                Spacing = new Size(0, 6),
                Rows =
                {
                    new TableRow(table) { ScaleHeight = true },
                    new TableRow(new Scrollable { Border = BorderType.None, Content = statsLabel, Height = 110 }),
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Items = { saveButton, exportStatsButton }
                    }, true))
                }
            };

            window.Show();
        }

        private static void SavePlots(List<PlotModel> models, int columns, string defaultName, Control parent)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Save the plot",
                FileName = defaultName,
                Filters = { Filters.Png, Filters.Jpeg }
            };

            if (saveDialog.ShowDialog(parent) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                string path = UiHelpers.EnsureExtension(saveDialog.FileName, ".png");

                if (models.Count == 1)
                {
                    PlotExport.SaveAsImage(models[0], path);
                }
                else
                {
                    PlotExport.SaveCompositeAsImage(models, columns, path);
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

        private static void ExportStatsText(string statsText, string defaultName, Control parent)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Statistics",
                FileName = defaultName,
                Filters = { Filters.Xlsx, Filters.Text }
            };

            if (saveDialog.ShowDialog(parent) != DialogResult.Ok)
            {
                return;
            }

            try
            {
                string path = UiHelpers.EnsureExtension(saveDialog.FileName, ".xlsx");

                if (Path.GetExtension(path).ToLowerInvariant() == ".xlsx")
                {
                    using var workbook = new XLWorkbook();
                    var sheet = workbook.Worksheets.Add("Stats");

                    string[] lines = statsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        sheet.Cell(i + 1, 1).Value = lines[i];
                    }

                    workbook.SaveAs(path);
                }
                else
                {
                    File.WriteAllText(path, statsText);
                }

                MessageBox.Show("Statistics exported successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting statistics: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Applies the shared plot styling, including a font family that exists here.</summary>
        private static void ConfigurePlotStyle(PlotModel model)
        {
            model.DefaultFont = PlotFonts.Family;
            model.PlotAreaBorderThickness = new OxyThickness(1);
            model.PlotAreaBorderColor = OxyColors.Black;
        }

        private static void AddLegend(PlotModel model)
        {
            model.Legends.Add(new Legend
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
        }

        private static double GetMedian(List<double> values)
        {
            int size = values.Count;
            if (size == 0)
            {
                return 0;
            }
            int mid = size / 2;
            return size % 2 != 0 ? values[mid] : (values[mid - 1] + values[mid]) / 2.0;
        }

        private static double GetPercentile(List<double> values, double percentile)
        {
            if (values.Count == 0)
            {
                return 0;
            }

            int n = values.Count;
            double position = (n - 1) * percentile / 100.0 + 1;

            if (position <= 1)
            {
                return values[0];
            }
            if (position >= n)
            {
                return values[n - 1];
            }

            int k = (int)position;
            double d = position - k;
            return values[k - 1] + d * (values[k] - values[k - 1]);
        }

        #endregion
    }
}
