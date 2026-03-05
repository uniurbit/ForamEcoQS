//MIT License
// IndexSettingsForm.cs - Configuration form for index calculation settings

using System;
using System.Drawing;
using System.Windows.Forms;

namespace ForamEcoQS
{
    /// <summary>
    /// Form for configuring index calculation settings including:
    /// - Foram-AMBI threshold selection (Borja vs Parent)
    /// - TSI-Med reference curve selection
    /// - EQR reference values for positive indices
    /// </summary>
    public partial class IndexSettingsForm : Form
    {
        // Settings properties
        public FAMBIThresholdType FAMBIThreshold { get; private set; } = FAMBIThresholdType.Borja2003;
        public TSIReferenceType TSIReference { get; private set; } = TSIReferenceType.Barras2014_150um;
        public TSIThresholdType TSIThreshold { get; private set; } = TSIThresholdType.Parent2021;
        public ExpHbcThresholdType ExpHbcThreshold { get; private set; } = ExpHbcThresholdType.OBrien2021_Norwegian63um;
        public bool UseJorissenTolerantList { get; private set; } = false;
        public double FSIReferenceValue { get; private set; } = 10.0;
        public double ExpHbcReferenceValue { get; private set; } = 20.0;
        public bool CalculateEQR { get; private set; } = false;

        // Controls
        private ComboBox fambiThresholdCombo;
        private ComboBox tsiReferenceCombo;
        private ComboBox tsiThresholdCombo;
        private ComboBox expHbcThresholdCombo;
        private CheckBox jorissenListCheckbox;
        private CheckBox calculateEqrCheckbox;
        private NumericUpDown fsiRefNumeric;
        private NumericUpDown expHbcRefNumeric;
        private Label fambiDescLabel;
        private Label tsiDescLabel;

        public IndexSettingsForm()
        {
            InitializeComponent();
        }

        public IndexSettingsForm(FAMBIThresholdType currentFAMBI, TSIReferenceType currentTSI,
            TSIThresholdType currentTSIThreshold, ExpHbcThresholdType currentExpHbcThreshold,
            bool useJorissen, bool calcEQR, double fsiRef, double expHbcRef) : this()
        {
            fambiThresholdCombo.SelectedIndex = (int)currentFAMBI;
            tsiReferenceCombo.SelectedIndex = (int)currentTSI;
            tsiThresholdCombo.SelectedIndex = (int)currentTSIThreshold;
            expHbcThresholdCombo.SelectedIndex = (int)currentExpHbcThreshold;
            jorissenListCheckbox.Checked = useJorissen;
            calculateEqrCheckbox.Checked = calcEQR;
            fsiRefNumeric.Value = (decimal)fsiRef;
            expHbcRefNumeric.Value = (decimal)expHbcRef;

            FAMBIThreshold = currentFAMBI;
            TSIReference = currentTSI;
            TSIThreshold = currentTSIThreshold;
            ExpHbcThreshold = currentExpHbcThreshold;
            UseJorissenTolerantList = useJorissen;
            CalculateEQR = calcEQR;
            FSIReferenceValue = fsiRef;
            ExpHbcReferenceValue = expHbcRef;
        }

        private void InitializeComponent()
        {
            this.Text = "Index Calculation Settings";
            this.Size = new Size(550, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int yPos = 20;

            // ============ FORAM-AMBI THRESHOLDS ============
            var fambiGroup = new GroupBox
            {
                Text = "Foram-AMBI EQS Thresholds",
                Location = new Point(20, yPos),
                Size = new Size(495, 135)
            };

            var fambiLabel = new Label
            {
                Text = "Threshold System:",
                Location = new Point(15, 25),
                AutoSize = true
            };

            fambiThresholdCombo = new ComboBox
            {
                Location = new Point(130, 22),
                Size = new Size(340, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            fambiThresholdCombo.Items.AddRange(new string[]
            {
                "Borja et al. (2003) - Traditional",
                "Parent et al. (2021b) - Updated for Foraminifera",
                "Bouchet et al. (2025) - Brazilian Transitional Waters"
            });
            fambiThresholdCombo.SelectedIndex = 0;
            fambiThresholdCombo.SelectedIndexChanged += FambiThresholdCombo_SelectedIndexChanged;

            fambiDescLabel = new Label
            {
                Text = GetFAMBIDescription(FAMBIThresholdType.Borja2003),
                Location = new Point(15, 55),
                Size = new Size(465, 72),
                Font = new Font(Font.FontFamily, 8.5f)
            };

            fambiGroup.Controls.AddRange(new Control[] { fambiLabel, fambiThresholdCombo, fambiDescLabel });
            this.Controls.Add(fambiGroup);
            yPos += 145;

            // ============ TSI-MED REFERENCE CURVES ============
            var tsiGroup = new GroupBox
            {
                Text = "TSI-Med Settings",
                Location = new Point(20, yPos),
                Size = new Size(495, 190)
            };

            var tsiLabel = new Label
            {
                Text = "Reference Curve:",
                Location = new Point(15, 25),
                AutoSize = true
            };

            tsiReferenceCombo = new ComboBox
            {
                Location = new Point(130, 22),
                Size = new Size(340, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            tsiReferenceCombo.Items.AddRange(new string[]
            {
                "Barras et al. (2014) - >150 µm fraction",
                "Parent et al. (2021b) - >125 µm fraction (FOBIMO)",
                "Jorissen et al. (2018) - >125 µm homogenized (EG3+EG4+EG5)"
            });
            tsiReferenceCombo.SelectedIndex = 0;
            tsiReferenceCombo.SelectedIndexChanged += TsiReferenceCombo_SelectedIndexChanged;

            jorissenListCheckbox = new CheckBox
            {
                Text = "Use Jorissen et al. (2018) tolerant species list (EG3+EG4+EG5 from Foram-AMBI databank)",
                Location = new Point(15, 55),
                Size = new Size(465, 20),
                Checked = false
            };
            jorissenListCheckbox.CheckedChanged += JorissenListCheckbox_CheckedChanged;

            var tsiThresholdLabel = new Label
            {
                Text = "EQS Threshold Scale:",
                Location = new Point(15, 82),
                AutoSize = true
            };

            tsiThresholdCombo = new ComboBox
            {
                Location = new Point(130, 79),
                Size = new Size(340, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            tsiThresholdCombo.Items.AddRange(new string[]
            {
                "Parent et al. (2021) - Low TSI = High quality",
                "Barras & Jorissen (2011) - Low TSI = Bad quality"
            });
            tsiThresholdCombo.SelectedIndex = 0;

            tsiDescLabel = new Label
            {
                Text = GetTSIDescription(TSIReferenceType.Barras2014_150um),
                Location = new Point(15, 112),
                Size = new Size(465, 72),
                Font = new Font(Font.FontFamily, 8.5f)
            };

            tsiGroup.Controls.AddRange(new Control[] { tsiLabel, tsiReferenceCombo, jorissenListCheckbox, tsiThresholdLabel, tsiThresholdCombo, tsiDescLabel });
            this.Controls.Add(tsiGroup);
            yPos += 200;

            // ============ EQR SETTINGS ============
            var eqrGroup = new GroupBox
            {
                Text = "exp(H'bc) Thresholds + Optional EQR",
                Location = new Point(20, yPos),
                Size = new Size(495, 175)
            };

            var expThresholdLabel = new Label
            {
                Text = "exp(H'bc) threshold set:",
                Location = new Point(15, 28),
                AutoSize = true
            };

            expHbcThresholdCombo = new ComboBox
            {
                Location = new Point(180, 25),
                Size = new Size(290, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            expHbcThresholdCombo.Items.AddRange(new string[]
            {
                "O'Brien (2021) Norway >125 µm",
                "O'Brien (2021) Norway >63 µm",
                "O'Brien (2021) Italy >63 µm"
            });
            expHbcThresholdCombo.SelectedIndex = 1;

            calculateEqrCheckbox = new CheckBox
            {
                Text = "Calculate EQR for FSI and exp(H'bc)",
                Location = new Point(15, 60),
                AutoSize = true,
                Checked = false
            };
            calculateEqrCheckbox.CheckedChanged += CalculateEqrCheckbox_CheckedChanged;

            var fsiRefLabel = new Label
            {
                Text = "FSI Reference Value (max):",
                Location = new Point(15, 92),
                AutoSize = true
            };

            fsiRefNumeric = new NumericUpDown
            {
                Location = new Point(180, 89),
                Size = new Size(80, 25),
                Minimum = 1,
                Maximum = 20,
                Value = 10,
                DecimalPlaces = 1,
                Enabled = false
            };

            var expHbcRefLabel = new Label
            {
                Text = "exp(H'bc) Reference Value:",
                Location = new Point(15, 122),
                AutoSize = true
            };

            expHbcRefNumeric = new NumericUpDown
            {
                Location = new Point(180, 119),
                Size = new Size(80, 25),
                Minimum = 1,
                Maximum = 100,
                Value = 20,
                DecimalPlaces = 1,
                Enabled = false
            };

            var eqrInfoLabel = new Label
            {
                Text = "EQR = Observed / Reference\nEQS: 0-0.2 Bad, 0.2-0.4 Poor, 0.4-0.6 Moderate, 0.6-0.8 Good, 0.8-1.0 High",
                Location = new Point(280, 89),
                Size = new Size(200, 60),
                Font = new Font(Font.FontFamily, 8f)
            };

            eqrGroup.Controls.AddRange(new Control[] {
                expThresholdLabel, expHbcThresholdCombo, calculateEqrCheckbox,
                fsiRefLabel, fsiRefNumeric,
                expHbcRefLabel, expHbcRefNumeric, eqrInfoLabel
            });
            this.Controls.Add(eqrGroup);
            yPos += 185;

            // ============ BUTTONS ============
            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(320, yPos),
                Size = new Size(90, 35),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            okButton.Click += OkButton_Click;

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(420, yPos),
                Size = new Size(90, 35),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat
            };

            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void FambiThresholdCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            FAMBIThreshold = (FAMBIThresholdType)fambiThresholdCombo.SelectedIndex;
            fambiDescLabel.Text = GetFAMBIDescription(FAMBIThreshold);
        }

        private void TsiReferenceCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            TSIReference = (TSIReferenceType)tsiReferenceCombo.SelectedIndex;
            tsiDescLabel.Text = GetTSIDescription(TSIReference);

            // Auto-check Jorissen list if that curve is selected
            if (TSIReference == TSIReferenceType.Jorissen2018_125um_Homogenized)
            {
                jorissenListCheckbox.Checked = true;
            }
        }

        private void JorissenListCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            UseJorissenTolerantList = jorissenListCheckbox.Checked;

            // If using Jorissen list, recommend the homogenized curve
            if (jorissenListCheckbox.Checked && tsiReferenceCombo.SelectedIndex != 2)
            {
                var result = MessageBox.Show(
                    "When using the Jorissen et al. (2018) tolerant species list (EG3+EG4+EG5),\n" +
                    "it's recommended to use the homogenized reference curve.\n\n" +
                    "Switch to Jorissen et al. (2018) reference curve?",
                    "Recommendation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    tsiReferenceCombo.SelectedIndex = 2;
                }
            }
        }

        private void CalculateEqrCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            CalculateEQR = calculateEqrCheckbox.Checked;
            fsiRefNumeric.Enabled = calculateEqrCheckbox.Checked;
            expHbcRefNumeric.Enabled = calculateEqrCheckbox.Checked;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            FAMBIThreshold = (FAMBIThresholdType)fambiThresholdCombo.SelectedIndex;
            TSIReference = (TSIReferenceType)tsiReferenceCombo.SelectedIndex;
            TSIThreshold = (TSIThresholdType)tsiThresholdCombo.SelectedIndex;
            ExpHbcThreshold = (ExpHbcThresholdType)expHbcThresholdCombo.SelectedIndex;
            UseJorissenTolerantList = jorissenListCheckbox.Checked;
            CalculateEQR = calculateEqrCheckbox.Checked;
            FSIReferenceValue = (double)fsiRefNumeric.Value;
            ExpHbcReferenceValue = (double)expHbcRefNumeric.Value;
        }

        private string GetFAMBIDescription(FAMBIThresholdType type)
        {
            return type switch
            {
                FAMBIThresholdType.Borja2003 =>
                    "Traditional AMBI thresholds (Borja et al. 2003):\n" +
                    "High: ≤1.2 | Good: 1.2-3.3 | Moderate: 3.3-4.3 | Poor: 4.3-5.5 | Bad: >5.5\n" +
                    "Reference: doi:10.1016/S0025-326X(03)00090-0",

                FAMBIThresholdType.Parent2021 =>
                    "Updated Foram-AMBI thresholds (Parent et al. 2021b):\n" +
                    "High: <1.4 | Good: 1.4-2.4 | Moderate: 2.4-3.4 | Poor: 3.4-4.4 | Bad: ≥4.4\n" +
                    "Reference: doi:10.3390/w13223193",

                FAMBIThresholdType.Bouchet2025Brazil =>
                    "Brazilian transitional waters thresholds (Bouchet et al. 2025):\n" +
                    "High: <1.4 | Good: 1.4-1.8 | Moderate: 1.8-3.0 | Poor: 3.0-4.0 | Bad: >4.0\n" +
                    "Reference: doi:10.5194/jm-44-237-2025",

                _ => ""
            };
        }

        private string GetTSIDescription(TSIReferenceType type)
        {
            return type switch
            {
                TSIReferenceType.Barras2014_150um =>
                    "Original reference curve for >150 µm fraction (Barras et al. 2014):\n" +
                    "%TSref = 5.0 + 0.3 × %mud\n" +
                    "Uses original tolerant species list. Reference: doi:10.1016/j.ecolind.2013.09.028",

                TSIReferenceType.Parent2021_125um =>
                    "Reference curve for >125 µm fraction - FOBIMO standard (Parent et al. 2021b):\n" +
                    "%TSref = 4.5 + 0.28 × %mud\n" +
                    "Uses original tolerant species list. Reference: doi:10.3390/w13223193",

                TSIReferenceType.Jorissen2018_125um_Homogenized =>
                    "Homogenized reference curve with Jorissen et al. (2018) species list:\n" +
                    "%TSref = 3.6718 + 0.3247 × %mud\n" +
                    "Tolerant = EG3+EG4+EG5. Reference: doi:10.1016/j.marpolbul.2021.112071",

                _ => ""
            };
        }
    }
}
