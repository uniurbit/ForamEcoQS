//MIT License
// MudPercentageForm.cs - Form for inputting mud percentage per sample for TSI-Med calculations

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ForamEcoQS
{
    public partial class MudPercentageForm : Form
    {
        private DataGridView mudGrid;
        private Button okButton;
        private Button cancelButton;
        private Button setAllButton;
        private NumericUpDown setAllNumeric;
        private Label infoLabel;
        private Label autoDetectedLabel;
        private Dictionary<string, double> prePopulatedValues;

        public Dictionary<string, double> MudPercentages { get; private set; }

        public MudPercentageForm(IEnumerable<string> sampleNames, Dictionary<string, double> prePopulated = null)
        {
            MudPercentages = new Dictionary<string, double>();
            prePopulatedValues = prePopulated;
            InitializeComponent(sampleNames);
        }

        private void InitializeComponent(IEnumerable<string> sampleNames)
        {
            this.Text = "TSI-Med: Mud Percentage Input";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Info label
            infoLabel = new Label
            {
                Text = "TSI-Med requires the percentage of fine sediment (<63 µm) for each sample.\n" +
                       "This is used to correct for natural trophic conditions (Barras et al. 2014).\n" +
                       "Enter values between 0-100%. Default is 50% if unknown.",
                Location = new Point(10, 10),
                Size = new Size(460, 60),
                AutoSize = false
            };
            this.Controls.Add(infoLabel);

            // Set all controls
            var setAllLabel = new Label
            {
                Text = "Set all samples to:",
                Location = new Point(10, 80),
                AutoSize = true
            };
            this.Controls.Add(setAllLabel);

            setAllNumeric = new NumericUpDown
            {
                Location = new Point(130, 77),
                Size = new Size(70, 25),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                DecimalPlaces = 1
            };
            this.Controls.Add(setAllNumeric);

            setAllButton = new Button
            {
                Text = "Apply to All",
                Location = new Point(210, 75),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat
            };
            setAllButton.Click += SetAllButton_Click;
            this.Controls.Add(setAllButton);

            // Data grid
            mudGrid = new DataGridView
            {
                Location = new Point(10, 115),
                Size = new Size(460, 240),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };

            mudGrid.Columns.Add("Sample", "Sample Name");
            mudGrid.Columns["Sample"].ReadOnly = true;

            var mudColumn = new DataGridViewTextBoxColumn
            {
                Name = "MudPercent",
                HeaderText = "Mud % (<63 µm)",
                ValueType = typeof(double)
            };
            mudGrid.Columns.Add(mudColumn);

            // Populate grid with pre-populated values if available, otherwise use default 50%
            foreach (var sample in sampleNames)
            {
                double mudValue = 50.0;
                if (prePopulatedValues != null && prePopulatedValues.TryGetValue(sample, out double prePopValue))
                {
                    mudValue = prePopValue;
                }
                mudGrid.Rows.Add(sample, mudValue);
            }

            this.Controls.Add(mudGrid);

            // Show auto-detected label if values were pre-populated
            if (prePopulatedValues != null && prePopulatedValues.Count > 0)
            {
                autoDetectedLabel = new Label
                {
                    Text = "Values auto-detected from loaded file. You can modify them if needed.",
                    Location = new Point(10, 358),
                    Size = new Size(270, 20),
                    ForeColor = Color.FromArgb(46, 139, 87),
                    Font = new Font(this.Font, FontStyle.Italic)
                };
                this.Controls.Add(autoDetectedLabel);
            }

            // OK button
            okButton = new Button
            {
                Text = "OK",
                Location = new Point(290, 370),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            // Cancel button
            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(380, 370),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void SetAllButton_Click(object? sender, EventArgs e)
        {
            double value = (double)setAllNumeric.Value;
            foreach (DataGridViewRow row in mudGrid.Rows)
            {
                row.Cells["MudPercent"].Value = value;
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            MudPercentages.Clear();
            
            foreach (DataGridViewRow row in mudGrid.Rows)
            {
                string sample = row.Cells["Sample"].Value?.ToString() ?? "";
                if (double.TryParse(row.Cells["MudPercent"].Value?.ToString(), out double mud))
                {
                    // Clamp to valid range
                    mud = Math.Max(0, Math.Min(100, mud));
                    MudPercentages[sample] = mud;
                }
                else
                {
                    MudPercentages[sample] = 50.0; // Default
                }
            }
        }
    }
}
