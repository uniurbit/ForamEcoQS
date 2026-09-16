//MIT License
// MudPercentageDialog.cs - Per-sample mud (<63 um) input used by the TSI-Med calculation.

using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using ForamEcoQS.Compat;
using DataGridView = ForamEcoQS.Compat.DataGridView;

namespace ForamEcoQS
{
    public class MudPercentageDialog : Dialog<bool>
    {
        private readonly DataGridView _mudGrid;
        private readonly NumericStepper _setAllNumeric;
        private readonly Dictionary<string, double> _prePopulatedValues;

        public Dictionary<string, double> MudPercentages { get; } = new Dictionary<string, double>();

        public MudPercentageDialog(IEnumerable<string> sampleNames, Dictionary<string, double> prePopulated = null)
        {
            _prePopulatedValues = prePopulated;

            Title = "TSI-Med: Mud Percentage Input";
            ClientSize = new Size(500, 450);
            this.Prepare();

            var infoLabel = new Label
            {
                Text = "TSI-Med requires the percentage of fine sediment (<63 µm) for each sample.\n" +
                       "This is used to correct for natural trophic conditions (Barras et al. 2014).\n" +
                       "Enter values between 0-100%. Default is 50% if unknown."
            };

            _setAllNumeric = new NumericStepper
            {
                MinValue = 0,
                MaxValue = 100,
                Value = 50,
                DecimalPlaces = 1,
                Width = 80
            };

            var setAllButton = new Button { Text = "Apply to All" };
            setAllButton.Click += SetAllButton_Click;

            _mudGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = Compat.DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = Compat.DataGridViewSelectionMode.CellSelect
            };

            _mudGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Sample",
                HeaderText = "Sample Name",
                ReadOnly = true
            });
            _mudGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MudPercent",
                HeaderText = "Mud % (<63 µm)",
                ValueType = typeof(double)
            });

            foreach (var sample in sampleNames)
            {
                double mudValue = 50.0;
                if (_prePopulatedValues != null && _prePopulatedValues.TryGetValue(sample, out double prePopValue))
                {
                    mudValue = prePopValue;
                }
                _mudGrid.Rows.Add(sample, mudValue);
            }

            var okButton = new Button { Text = "OK", Width = 90 };
            okButton.Click += (s, e) =>
            {
                CollectValues();
                Close(true);
            };

            var cancelButton = new Button { Text = "Cancel" };
            cancelButton.Click += (s, e) => Close(false);

            DefaultButton = okButton;
            AbortButton = cancelButton;

            var layout = new TableLayout
            {
                Padding = new Padding(10),
                Spacing = new Size(6, 8),
                Rows =
                {
                    new TableRow(infoLabel),
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items = { new Label { Text = "Set all samples to:" }, _setAllNumeric, setAllButton }
                    }, true)),
                    new TableRow(_mudGrid) { ScaleHeight = true }
                }
            };

            if (_prePopulatedValues != null && _prePopulatedValues.Count > 0)
            {
                layout.Rows.Add(new TableRow(new Label
                {
                    Text = "Values auto-detected from loaded file. You can modify them if needed.",
                    TextColor = AppColors.SeaGreen,
                    Font = SystemFonts.Default(9)
                }));
            }

            layout.Rows.Add(new TableRow(new TableCell(new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Items = { okButton, cancelButton }
            }, true)));

            Content = layout;
        }

        private void SetAllButton_Click(object sender, EventArgs e)
        {
            double value = _setAllNumeric.Value;
            foreach (var row in _mudGrid.Rows)
            {
                row.Cells["MudPercent"].Value = value;
            }
            _mudGrid.Refresh();
        }

        private void CollectValues()
        {
            MudPercentages.Clear();

            foreach (var row in _mudGrid.Rows)
            {
                string sample = row.Cells["Sample"].Value?.ToString() ?? string.Empty;
                if (double.TryParse(row.Cells["MudPercent"].Value?.ToString(), out double mud))
                {
                    MudPercentages[sample] = Math.Max(0, Math.Min(100, mud));
                }
                else
                {
                    MudPercentages[sample] = 50.0;
                }
            }
        }
    }
}
