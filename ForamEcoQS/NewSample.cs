//MIT License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ForamEcoQS
{
    public partial class NewSample : Form
    {
        public string SampleName { get; private set; }
        public NewSample()
        {
            InitializeComponent();
        }

        private Label label1;
        private Button createSampleButton;
        private Button cancelButton;
        private TextBox textBox1;

        private void InitializeComponent()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(NewSample));
            label1 = new Label();
            createSampleButton = new Button();
            cancelButton = new Button();
            textBox1 = new TextBox();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(50, 55);
            label1.Name = "label1";
            label1.Size = new Size(144, 20);
            label1.TabIndex = 0;
            label1.Text = "New Sample Name: ";
            // 
            // createSampleButton
            //
            createSampleButton.FlatStyle = FlatStyle.Flat;
            createSampleButton.Location = new Point(60, 136);
            createSampleButton.Name = "createSampleButton";
            createSampleButton.Size = new Size(86, 47);
            createSampleButton.TabIndex = 1;
            createSampleButton.Text = "Create";
            createSampleButton.UseVisualStyleBackColor = true;
            createSampleButton.Click += createSampleButton_Click;
            //
            // cancelButton
            //
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.Location = new Point(161, 136);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(81, 47);
            cancelButton.TabIndex = 2;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += cancelButton_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(50, 87);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(201, 27);
            textBox1.TabIndex = 3;
            textBox1.Text = "New_Sample";
            // 
            // NewSample
            // 
            ClientSize = new Size(314, 216);
            Controls.Add(textBox1);
            Controls.Add(cancelButton);
            Controls.Add(createSampleButton);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "NewSample";
            Load += NewSample_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private void createSampleButton_Click(object sender, EventArgs e)
        {
            // Set the SampleName property to the text in textBox1
            SampleName = textBox1.Text;

            // Close the form
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void NewSample_Load(object sender, EventArgs e)
        {

        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
