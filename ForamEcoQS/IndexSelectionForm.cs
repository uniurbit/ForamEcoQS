using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ForamEcoQS
{
    public class IndexSelectionForm : Form
    {
        private CheckedListBox indicesListBox;
        private Button calculateButton;
        private Button cancelButton;
        private Button selectAllButton;
        private Button selectNoneButton;

        public List<string> SelectedIndices { get; private set; }

        public IndexSelectionForm(bool fambiAvailable, bool foramIndexAvailable)
        {
            InitializeComponent(fambiAvailable, foramIndexAvailable);
            SelectedIndices = new List<string>();
        }

        private void InitializeComponent(bool fambiAvailable, bool foramIndexAvailable)
        {
            this.Text = "Select Indices to Calculate";
            this.Size = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var mainLabel = new Label
            {
                Text = "Choose the indices you want to calculate:",
                Location = new Point(12, 15),
                AutoSize = true,
                Font = new Font(Control.DefaultFont, FontStyle.Bold)
            };

            indicesListBox = new CheckedListBox
            {
                Location = new Point(12, 40),
                Size = new Size(360, 350),
                CheckOnClick = true
            };

            // Add indices
            // Core indices
            if (fambiAvailable)
            {
                indicesListBox.Items.Add("Foram-AMBI", true);
                indicesListBox.Items.Add("Foram-M-AMBI", true);  // Multivariate AMBI - requires Foram-AMBI
                indicesListBox.Items.Add("BENTIX", true);
                indicesListBox.Items.Add("BQI", true);
                indicesListBox.Items.Add("NQIf", true);
                indicesListBox.Items.Add("FIEI", true);
            }

            indicesListBox.Items.Add("FSI", true);
            indicesListBox.Items.Add("TSI-Med", true);

            if (foramIndexAvailable)
            {
                indicesListBox.Items.Add("FoRAM Index", false);
            }

            // Diversity & others
            indicesListBox.Items.Add("exp(H'bc)", true);
            indicesListBox.Items.Add("H'log2 (Shannon base 2)", true);
            indicesListBox.Items.Add("H'ln (Shannon natural)", true);
            indicesListBox.Items.Add("Simpson (1-D)", true);
            indicesListBox.Items.Add("Pielou's J", true);
            indicesListBox.Items.Add("Species Richness (S)", true);
            indicesListBox.Items.Add("Total Abundance (N)", true);
            indicesListBox.Items.Add("ES100", true);

            selectAllButton = new Button
            {
                Text = "Select All",
                Location = new Point(12, 400),
                Size = new Size(85, 30)
            };
            selectAllButton.Click += (s, e) => SetAllChecked(true);

            selectNoneButton = new Button
            {
                Text = "Select None",
                Location = new Point(105, 400),
                Size = new Size(85, 30)
            };
            selectNoneButton.Click += (s, e) => SetAllChecked(false);

            calculateButton = new Button
            {
                Text = "Calculate",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 400),
                Size = new Size(85, 30),
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            calculateButton.Click += CalculateButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(295, 400),
                Size = new Size(77, 30)
            };

            this.Controls.AddRange(new Control[] {
                mainLabel, indicesListBox, selectAllButton, selectNoneButton, calculateButton, cancelButton
            });
            this.AcceptButton = calculateButton;
            this.CancelButton = cancelButton;
        }

        private void SetAllChecked(bool state)
        {
            for (int i = 0; i < indicesListBox.Items.Count; i++)
            {
                indicesListBox.SetItemChecked(i, state);
            }
        }

        private void CalculateButton_Click(object sender, EventArgs e)
        {
            SelectedIndices.Clear();
            foreach (var item in indicesListBox.CheckedItems)
            {
                string indexName = item.ToString();
                // Map display names to internal keys if necessary
                if (indexName.StartsWith("H'log2")) SelectedIndices.Add("H'log2");
                else if (indexName.StartsWith("H'ln")) SelectedIndices.Add("H'ln");
                else SelectedIndices.Add(indexName);
            }

            // Note: EcoGroups are not in the list but implied if needed by calculation logic
        }
    }
}