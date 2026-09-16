//MIT License
// SimpleDialogs.cs - The small modal dialogs: new sample, rename, ecological-group override,
// databank picker and CSV separator picker.

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using ForamEcoQS.Compat;

namespace ForamEcoQS
{
    /// <summary>Asks for the name of a new sample column.</summary>
    public class NewSampleDialog : Dialog<bool>
    {
        private readonly TextBox _nameBox;

        public string SampleName { get; private set; }

        public NewSampleDialog()
        {
            Title = "New Sample";
            Resizable = false;
            this.Prepare();

            _nameBox = new TextBox { Text = "New_Sample", Width = 220 };

            var createButton = new Button { Text = "Create" };
            createButton.Click += (s, e) =>
            {
                SampleName = _nameBox.Text;
                Close(true);
            };

            var cancelButton = new Button { Text = "Cancel" };
            cancelButton.Click += (s, e) => Close(false);

            DefaultButton = createButton;
            AbortButton = cancelButton;

            Content = new TableLayout
            {
                Padding = new Padding(20),
                Spacing = new Size(6, 10),
                Rows =
                {
                    new TableRow(new Label { Text = "New Sample Name:" }),
                    new TableRow(_nameBox),
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { createButton, cancelButton }
                    }, true))
                }
            };
        }
    }

    /// <summary>Single line text prompt, used for renaming a sample.</summary>
    public class TextPromptDialog : Dialog<bool>
    {
        private readonly TextBox _input;

        public string Value => _input.Text;

        public TextPromptDialog(string title, string prompt, string initialValue)
        {
            Title = title;
            Resizable = false;
            this.Prepare();

            _input = new TextBox { Text = initialValue ?? string.Empty, Width = 330 };

            var okButton = new Button { Text = "OK" };
            okButton.Click += (s, e) => Close(true);

            var cancelButton = new Button { Text = "Cancel" };
            cancelButton.Click += (s, e) => Close(false);

            DefaultButton = okButton;
            AbortButton = cancelButton;

            Content = new TableLayout
            {
                Padding = new Padding(15),
                Spacing = new Size(6, 10),
                Rows =
                {
                    new TableRow(new Label { Text = prompt }),
                    new TableRow(_input),
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { okButton, cancelButton }
                    }, true))
                }
            };
        }
    }

    /// <summary>Drop-down prompt, used for the databank and CSV separator pickers.</summary>
    public class ChoiceDialog : Dialog<bool>
    {
        private readonly DropDown _dropDown;

        public int SelectedIndex => _dropDown.SelectedIndex;

        public string SelectedValue => _dropDown.SelectedIndex >= 0
            ? _dropDown.Items[_dropDown.SelectedIndex].Text
            : null;

        public ChoiceDialog(string title, string prompt, IEnumerable<string> options, string initialSelection = null)
        {
            Title = title;
            Resizable = false;
            this.Prepare();

            _dropDown = new DropDown { Width = 330 };
            foreach (var option in options)
            {
                _dropDown.Items.Add(option);
            }

            int initialIndex = 0;
            if (!string.IsNullOrEmpty(initialSelection))
            {
                int found = _dropDown.Items.ToList().FindIndex(i => i.Text == initialSelection);
                if (found >= 0)
                {
                    initialIndex = found;
                }
            }
            if (_dropDown.Items.Count > 0)
            {
                _dropDown.SelectedIndex = initialIndex;
            }

            var okButton = new Button { Text = "OK" };
            okButton.Click += (s, e) => Close(true);

            var cancelButton = new Button { Text = "Cancel" };
            cancelButton.Click += (s, e) => Close(false);

            DefaultButton = okButton;
            AbortButton = cancelButton;

            Content = new TableLayout
            {
                Padding = new Padding(15),
                Spacing = new Size(6, 10),
                Rows =
                {
                    new TableRow(new Label { Text = prompt }),
                    new TableRow(_dropDown),
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { okButton, cancelButton }
                    }, true))
                }
            };
        }
    }

    /// <summary>Manual ecological group (EG1-EG5) override for a single species.</summary>
    public class OverrideDialog : Dialog<bool>
    {
        private readonly DropDown _groupCombo;

        public int SelectedGroup { get; private set; }

        public OverrideDialog(string speciesName, int currentGroup = 0)
        {
            Title = "Override Ecological Group";
            Resizable = false;
            this.Prepare();

            _groupCombo = new DropDown { Width = 290 };
            _groupCombo.Items.Add("EG1 (Sensitive)");
            _groupCombo.Items.Add("EG2 (Indifferent)");
            _groupCombo.Items.Add("EG3 (Tolerant)");
            _groupCombo.Items.Add("EG4 (Opportunistic 1)");
            _groupCombo.Items.Add("EG5 (Opportunistic 2)");
            _groupCombo.SelectedIndex = currentGroup >= 1 && currentGroup <= 5 ? currentGroup - 1 : 0;

            var okButton = new Button { Text = "OK" };
            okButton.Click += (s, e) =>
            {
                SelectedGroup = _groupCombo.SelectedIndex + 1;
                Close(true);
            };

            var cancelButton = new Button { Text = "Cancel" };
            cancelButton.Click += (s, e) => Close(false);

            DefaultButton = okButton;
            AbortButton = cancelButton;

            Content = new TableLayout
            {
                Padding = new Padding(20),
                Spacing = new Size(6, 10),
                Rows =
                {
                    new TableRow(new Label { Text = $"Select Ecological Group for:\n{speciesName}" }),
                    new TableRow(_groupCombo),
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { okButton, cancelButton }
                    }, true))
                }
            };
        }
    }

    /// <summary>Read-only scrolling text window, used for the long help texts.</summary>
    public class TextViewerDialog : Dialog
    {
        public TextViewerDialog(string title, string text, Size? size = null, bool monospace = false)
        {
            Title = title;
            ClientSize = size ?? new Size(560, 500);
            this.Prepare();

            var textArea = new TextArea
            {
                Text = text,
                ReadOnly = true,
                Wrap = !monospace,
                Font = monospace ? new Font(FontFamilies.Monospace, 10) : SystemFonts.Default(10)
            };

            var closeButton = new Button { Text = "Close" };
            closeButton.Click += (s, e) => Close();
            DefaultButton = closeButton;
            AbortButton = closeButton;

            Content = new TableLayout
            {
                Padding = new Padding(10),
                Spacing = new Size(0, 8),
                Rows =
                {
                    new TableRow(textArea) { ScaleHeight = true },
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { closeButton }
                    }, true))
                }
            };
        }

        public static void Show(string title, string text, Size? size = null, bool monospace = false)
        {
            using var dialog = new TextViewerDialog(title, text, size, monospace);
            dialog.ShowModal(UiContext.ActiveWindow);
        }
    }
}
