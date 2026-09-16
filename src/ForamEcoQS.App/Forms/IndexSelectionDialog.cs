//MIT License
// IndexSelectionDialog.cs - Lets the user pick which indices to calculate.

using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using ForamEcoQS.Compat;

namespace ForamEcoQS
{
    public class IndexSelectionDialog : Dialog<bool>
    {
        private readonly CheckedListBox _indicesListBox;

        public List<string> SelectedIndices { get; private set; } = new List<string>();

        public IndexSelectionDialog(bool fambiAvailable, bool foramIndexAvailable)
        {
            Title = "Select Indices to Calculate";
            ClientSize = new Size(400, 500);
            this.Prepare();

            _indicesListBox = new CheckedListBox();

            // Core indices that depend on the Foram-AMBI databank comparison.
            if (fambiAvailable)
            {
                _indicesListBox.Items.Add("Foram-AMBI", true);
                _indicesListBox.Items.Add("Foram-M-AMBI", true);  // Multivariate AMBI - requires Foram-AMBI
                _indicesListBox.Items.Add("BENTIX", true);
                _indicesListBox.Items.Add("BQI", true);
                _indicesListBox.Items.Add("NQIf", true);
                _indicesListBox.Items.Add("FIEI", true);
            }

            _indicesListBox.Items.Add("FSI", true);
            _indicesListBox.Items.Add("TSI-Med", true);

            if (foramIndexAvailable)
            {
                _indicesListBox.Items.Add("FoRAM Index", false);
            }

            // Diversity and others
            _indicesListBox.Items.Add("exp(H'bc)", true);
            _indicesListBox.Items.Add("H'log2 (Shannon base 2)", true);
            _indicesListBox.Items.Add("H'ln (Shannon natural)", true);
            _indicesListBox.Items.Add("Simpson (1-D)", true);
            _indicesListBox.Items.Add("Pielou's J", true);
            _indicesListBox.Items.Add("Species Richness (S)", true);
            _indicesListBox.Items.Add("Total Abundance (N)", true);
            _indicesListBox.Items.Add("ES100", true);

            var selectAllButton = new Button { Text = "Select All" };
            selectAllButton.Click += (s, e) => SetAllChecked(true);

            var selectNoneButton = new Button { Text = "Select None" };
            selectNoneButton.Click += (s, e) => SetAllChecked(false);

            var calculateButton = new Button { Text = "Calculate", Width = 100 };
            calculateButton.Click += (s, e) =>
            {
                CollectSelection();
                Close(true);
            };

            var cancelButton = new Button { Text = "Cancel" };
            cancelButton.Click += (s, e) => Close(false);

            DefaultButton = calculateButton;
            AbortButton = cancelButton;

            Content = new TableLayout
            {
                Padding = new Padding(12),
                Spacing = new Size(6, 8),
                Rows =
                {
                    new TableRow(new Label
                    {
                        Text = "Choose the indices you want to calculate:",
                        Font = SystemFonts.Bold()
                    }),
                    new TableRow(_indicesListBox) { ScaleHeight = true },
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        Items =
                        {
                            selectAllButton,
                            selectNoneButton,
                            new StackLayoutItem(null, true),
                            calculateButton,
                            cancelButton
                        }
                    }, true))
                }
            };
        }

        private void SetAllChecked(bool state)
        {
            for (int i = 0; i < _indicesListBox.Items.Count; i++)
            {
                _indicesListBox.SetItemChecked(i, state);
            }
        }

        private void CollectSelection()
        {
            SelectedIndices.Clear();
            foreach (var item in _indicesListBox.CheckedItems)
            {
                string indexName = item.ToString();

                // Map the display names back to the internal keys used by the calculator.
                if (indexName.StartsWith("H'log2"))
                {
                    SelectedIndices.Add("H'log2");
                }
                else if (indexName.StartsWith("H'ln"))
                {
                    SelectedIndices.Add("H'ln");
                }
                else
                {
                    SelectedIndices.Add(indexName);
                }
            }
        }
    }
}
