//MIT License
// IndexSettingsDialog.cs - Configuration for index calculation:
// - Foram-AMBI threshold selection (Borja vs Parent vs Bouchet)
// - TSI-Med reference curve and threshold scale
// - exp(H'bc) threshold set and optional EQR reference values
// - Optional WoRMS online species verification

using System;
using Eto.Drawing;
using Eto.Forms;
using ForamEcoQS.Compat;
using MessageBox = ForamEcoQS.Compat.MessageBox;

namespace ForamEcoQS
{
    public class IndexSettingsDialog : Dialog<bool>
    {
        // Settings properties
        public FAMBIThresholdType FAMBIThreshold { get; private set; } = FAMBIThresholdType.Borja2003;
        public TSIReferenceType TSIReference { get; private set; } = TSIReferenceType.Barras2014_150um;
        public TSIThresholdType TSIThreshold { get; private set; } = TSIThresholdType.Parent2021;
        public ExpHbcThresholdType ExpHbcThreshold { get; private set; } = ExpHbcThresholdType.OBrien2021_Norwegian63um;
        public bool UseJorissenTolerantList { get; private set; }
        public double FSIReferenceValue { get; private set; } = 10.0;
        public double ExpHbcReferenceValue { get; private set; } = 20.0;
        public bool CalculateEQR { get; private set; }
        public bool UseWormsVerification { get; private set; }

        private readonly DropDown _fambiThresholdCombo;
        private readonly DropDown _tsiReferenceCombo;
        private readonly DropDown _tsiThresholdCombo;
        private readonly DropDown _expHbcThresholdCombo;
        private readonly CheckBox _jorissenListCheckbox;
        private readonly CheckBox _calculateEqrCheckbox;
        private readonly CheckBox _wormsVerificationCheckbox;
        private readonly NumericStepper _fsiRefNumeric;
        private readonly NumericStepper _expHbcRefNumeric;
        private readonly Label _fambiDescLabel;
        private readonly Label _tsiDescLabel;

        private bool _suppressRecommendation;

        public IndexSettingsDialog(
            FAMBIThresholdType currentFAMBI = FAMBIThresholdType.Borja2003,
            TSIReferenceType currentTSI = TSIReferenceType.Barras2014_150um,
            TSIThresholdType currentTSIThreshold = TSIThresholdType.Parent2021,
            ExpHbcThresholdType currentExpHbcThreshold = ExpHbcThresholdType.OBrien2021_Norwegian63um,
            bool useJorissen = false,
            bool calcEQR = false,
            double fsiRef = 10.0,
            double expHbcRef = 20.0,
            bool useWormsVerification = false)
        {
            Title = "Index Calculation Settings";
            ClientSize = new Size(560, 720);
            this.Prepare();

            // ============ FORAM-AMBI THRESHOLDS ============
            _fambiThresholdCombo = new DropDown { Width = 340 };
            _fambiThresholdCombo.Items.Add("Borja et al. (2003) - Traditional");
            _fambiThresholdCombo.Items.Add("Parent et al. (2021b) - Updated for Foraminifera");
            _fambiThresholdCombo.Items.Add("Bouchet et al. (2025) - Brazilian Transitional Waters");

            _fambiDescLabel = new Label
            {
                Text = GetFAMBIDescription(FAMBIThresholdType.Borja2003),
                Font = SystemFonts.Default(8.5f),
                Height = 60
            };

            _fambiThresholdCombo.SelectedIndexChanged += (s, e) =>
            {
                FAMBIThreshold = (FAMBIThresholdType)_fambiThresholdCombo.SelectedIndex;
                _fambiDescLabel.Text = GetFAMBIDescription(FAMBIThreshold);
            };

            var fambiGroup = new GroupBox
            {
                Text = "Foram-AMBI EQS Thresholds",
                Content = new TableLayout
                {
                    Padding = new Padding(10),
                    Spacing = new Size(6, 6),
                    Rows =
                    {
                        new TableRow(new Label { Text = "Threshold System:", VerticalAlignment = VerticalAlignment.Center },
                                     new TableCell(_fambiThresholdCombo, true)),
                        new TableRow(new TableCell(_fambiDescLabel, true) { }) { }
                    }
                }
            };

            // ============ TSI-MED REFERENCE CURVES ============
            _tsiReferenceCombo = new DropDown { Width = 340 };
            _tsiReferenceCombo.Items.Add("Barras et al. (2014) - >150 µm fraction");
            _tsiReferenceCombo.Items.Add("Parent et al. (2021b) - >125 µm fraction (FOBIMO)");
            _tsiReferenceCombo.Items.Add("Jorissen et al. (2018) - >125 µm homogenized (EG3+EG4+EG5)");

            _jorissenListCheckbox = new CheckBox
            {
                Text = "Use Jorissen et al. (2018) tolerant species list (EG3+EG4+EG5 from Foram-AMBI databank)",
                Checked = false
            };

            _tsiThresholdCombo = new DropDown { Width = 340 };
            _tsiThresholdCombo.Items.Add("Parent et al. (2021) - Low TSI = High quality");
            _tsiThresholdCombo.Items.Add("Barras & Jorissen (2011) - Low TSI = Bad quality");

            _tsiDescLabel = new Label
            {
                Text = GetTSIDescription(TSIReferenceType.Barras2014_150um),
                Font = SystemFonts.Default(8.5f),
                Height = 60
            };

            _tsiReferenceCombo.SelectedIndexChanged += (s, e) =>
            {
                TSIReference = (TSIReferenceType)_tsiReferenceCombo.SelectedIndex;
                _tsiDescLabel.Text = GetTSIDescription(TSIReference);

                // Auto-check the Jorissen list when its matching curve is selected.
                if (TSIReference == TSIReferenceType.Jorissen2018_125um_Homogenized)
                {
                    _suppressRecommendation = true;
                    _jorissenListCheckbox.Checked = true;
                    _suppressRecommendation = false;
                }
            };

            _jorissenListCheckbox.CheckedChanged += JorissenListCheckbox_CheckedChanged;

            var tsiGroup = new GroupBox
            {
                Text = "TSI-Med Settings",
                Content = new TableLayout
                {
                    Padding = new Padding(10),
                    Spacing = new Size(6, 6),
                    Rows =
                    {
                        new TableRow(new Label { Text = "Reference Curve:", VerticalAlignment = VerticalAlignment.Center },
                                     new TableCell(_tsiReferenceCombo, true)),
                        new TableRow(new TableCell(_jorissenListCheckbox, true), null),
                        new TableRow(new Label { Text = "EQS Threshold Scale:", VerticalAlignment = VerticalAlignment.Center },
                                     new TableCell(_tsiThresholdCombo, true)),
                        new TableRow(new TableCell(_tsiDescLabel, true), null)
                    }
                }
            };

            // ============ exp(H'bc) AND EQR ============
            _expHbcThresholdCombo = new DropDown { Width = 290 };
            _expHbcThresholdCombo.Items.Add("O'Brien (2021) Norway >125 µm");
            _expHbcThresholdCombo.Items.Add("O'Brien (2021) Norway >63 µm");
            _expHbcThresholdCombo.Items.Add("O'Brien (2021) Italy >63 µm");

            _calculateEqrCheckbox = new CheckBox { Text = "Calculate EQR for FSI and exp(H'bc)", Checked = false };

            _fsiRefNumeric = new NumericStepper
            {
                MinValue = 1,
                MaxValue = 20,
                Value = 10,
                DecimalPlaces = 1,
                Width = 90,
                Enabled = false
            };

            _expHbcRefNumeric = new NumericStepper
            {
                MinValue = 1,
                MaxValue = 100,
                Value = 20,
                DecimalPlaces = 1,
                Width = 90,
                Enabled = false
            };

            _calculateEqrCheckbox.CheckedChanged += (s, e) =>
            {
                CalculateEQR = _calculateEqrCheckbox.Checked == true;
                _fsiRefNumeric.Enabled = CalculateEQR;
                _expHbcRefNumeric.Enabled = CalculateEQR;
            };

            var eqrGroup = new GroupBox
            {
                Text = "exp(H'bc) Thresholds + Optional EQR",
                Content = new TableLayout
                {
                    Padding = new Padding(10),
                    Spacing = new Size(6, 6),
                    Rows =
                    {
                        new TableRow(new Label { Text = "exp(H'bc) threshold set:", VerticalAlignment = VerticalAlignment.Center },
                                     new TableCell(_expHbcThresholdCombo, true)),
                        new TableRow(new TableCell(_calculateEqrCheckbox, true), null),
                        new TableRow(new Label { Text = "FSI Reference Value (max):", VerticalAlignment = VerticalAlignment.Center },
                                     new TableCell(_fsiRefNumeric, true)),
                        new TableRow(new Label { Text = "exp(H'bc) Reference Value:", VerticalAlignment = VerticalAlignment.Center },
                                     new TableCell(_expHbcRefNumeric, true)),
                        new TableRow(new TableCell(new Label
                        {
                            Text = "EQR = Observed / Reference\n" +
                                   "EQS: 0-0.2 Bad, 0.2-0.4 Poor, 0.4-0.6 Moderate, 0.6-0.8 Good, 0.8-1.0 High",
                            Font = SystemFonts.Default(8f)
                        }, true), null)
                    }
                }
            };

            // ============ SPECIES IDENTIFICATION (WORMS) ============
            _wormsVerificationCheckbox = new CheckBox
            {
                Text = "Verify unmatched species against WoRMS (World Register of Marine Species)",
                Checked = false
            };
            _wormsVerificationCheckbox.CheckedChanged += (s, e) =>
                UseWormsVerification = _wormsVerificationCheckbox.Checked == true;

            var wormsGroup = new GroupBox
            {
                Text = "Species Identification",
                Content = new TableLayout
                {
                    Padding = new Padding(10),
                    Spacing = new Size(6, 6),
                    Rows =
                    {
                        new TableRow(_wormsVerificationCheckbox),
                        new TableRow(new Label
                        {
                            Text = "Requires an internet connection. Species not found in the loaded databank are looked up\n" +
                                   "at marinespecies.org to confirm they are valid marine taxa and to suggest the accepted name.",
                            Font = SystemFonts.Default(8f)
                        })
                    }
                }
            };

            // ============ BUTTONS ============
            var okButton = new Button { Text = "OK", Width = 90 };
            okButton.Click += (s, e) =>
            {
                CaptureSettings();
                Close(true);
            };

            var cancelButton = new Button { Text = "Cancel", Width = 90 };
            cancelButton.Click += (s, e) => Close(false);

            DefaultButton = okButton;
            AbortButton = cancelButton;

            // Apply the incoming values before wiring is complete so no recommendation pops up.
            _suppressRecommendation = true;
            _fambiThresholdCombo.SelectedIndex = (int)currentFAMBI;
            _tsiReferenceCombo.SelectedIndex = (int)currentTSI;
            _tsiThresholdCombo.SelectedIndex = (int)currentTSIThreshold;
            _expHbcThresholdCombo.SelectedIndex = (int)currentExpHbcThreshold;
            _jorissenListCheckbox.Checked = useJorissen;
            _calculateEqrCheckbox.Checked = calcEQR;
            _fsiRefNumeric.Value = fsiRef;
            _expHbcRefNumeric.Value = expHbcRef;
            _wormsVerificationCheckbox.Checked = useWormsVerification;
            _suppressRecommendation = false;

            FAMBIThreshold = currentFAMBI;
            TSIReference = currentTSI;
            TSIThreshold = currentTSIThreshold;
            ExpHbcThreshold = currentExpHbcThreshold;
            UseJorissenTolerantList = useJorissen;
            CalculateEQR = calcEQR;
            FSIReferenceValue = fsiRef;
            ExpHbcReferenceValue = expHbcRef;
            UseWormsVerification = useWormsVerification;

            Content = new Scrollable
            {
                Border = BorderType.None,
                Content = new TableLayout
                {
                    Padding = new Padding(15),
                    Spacing = new Size(0, 10),
                    Rows =
                    {
                        new TableRow(fambiGroup),
                        new TableRow(tsiGroup),
                        new TableRow(eqrGroup),
                        new TableRow(wormsGroup),
                        new TableRow(new TableCell(new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            HorizontalContentAlignment = HorizontalAlignment.Right,
                            Items = { okButton, cancelButton }
                        }, true)),
                        null
                    }
                }
            };
        }

        private void JorissenListCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            UseJorissenTolerantList = _jorissenListCheckbox.Checked == true;

            if (_suppressRecommendation)
            {
                return;
            }

            // When using the Jorissen list, recommend the matching homogenized curve.
            if (UseJorissenTolerantList && _tsiReferenceCombo.SelectedIndex != 2)
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
                    _tsiReferenceCombo.SelectedIndex = 2;
                }
            }
        }

        private void CaptureSettings()
        {
            FAMBIThreshold = (FAMBIThresholdType)_fambiThresholdCombo.SelectedIndex;
            TSIReference = (TSIReferenceType)_tsiReferenceCombo.SelectedIndex;
            TSIThreshold = (TSIThresholdType)_tsiThresholdCombo.SelectedIndex;
            ExpHbcThreshold = (ExpHbcThresholdType)_expHbcThresholdCombo.SelectedIndex;
            UseJorissenTolerantList = _jorissenListCheckbox.Checked == true;
            CalculateEQR = _calculateEqrCheckbox.Checked == true;
            FSIReferenceValue = _fsiRefNumeric.Value;
            ExpHbcReferenceValue = _expHbcRefNumeric.Value;
            UseWormsVerification = _wormsVerificationCheckbox.Checked == true;
        }

        private static string GetFAMBIDescription(FAMBIThresholdType type)
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

                _ => string.Empty
            };
        }

        private static string GetTSIDescription(TSIReferenceType type)
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

                _ => string.Empty
            };
        }
    }
}
