using System;
using System.Drawing;
using System.Windows.Forms;

namespace ForamEcoQS
{
    public class OverrideForm : Form
    {
        public int SelectedGroup { get; private set; }
        private ComboBox groupCombo;

        public OverrideForm(string speciesName, int currentGroup = 0)
        {
            this.Text = "Override Ecological Group";
            this.Size = new Size(350, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var label = new Label
            {
                Text = $"Select Ecological Group for:\n{speciesName}",
                Location = new Point(20, 20),
                AutoSize = true
            };
            this.Controls.Add(label);

            groupCombo = new ComboBox
            {
                Location = new Point(20, 70),
                Width = 290,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            groupCombo.Items.AddRange(new object[] { 
                "EG1 (Sensitive)", 
                "EG2 (Indifferent)", 
                "EG3 (Tolerant)", 
                "EG4 (Opportunistic 1)", 
                "EG5 (Opportunistic 2)" 
            });

            if (currentGroup >= 1 && currentGroup <= 5)
            {
                groupCombo.SelectedIndex = currentGroup - 1;
            }
            else
            {
                groupCombo.SelectedIndex = 0;
            }
            this.Controls.Add(groupCombo);

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(140, 110),
                DialogResult = DialogResult.OK,
                Width = 80
            };
            okButton.Click += (s, e) => { SelectedGroup = groupCombo.SelectedIndex + 1; };
            this.Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(230, 110),
                DialogResult = DialogResult.Cancel,
                Width = 80
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
    }
}
