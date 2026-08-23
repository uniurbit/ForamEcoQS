param(
    [int]$X = 440,
    [int]$Y = 0,
    [int]$Width = 2560,
    [int]$Height = 1440
)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$form = New-Object System.Windows.Forms.Form
$form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::None
$form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
$form.Location = New-Object System.Drawing.Point($X, $Y)
$form.Size = New-Object System.Drawing.Size($Width, $Height)
$form.BackColor = [System.Drawing.Color]::FromArgb(7, 43, 58)
$form.ShowInTaskbar = $false
$form.Text = "ForamEcoQS Tutorial Privacy Backdrop"

$label = New-Object System.Windows.Forms.Label
$label.Dock = [System.Windows.Forms.DockStyle]::Fill
$label.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
$label.Font = New-Object System.Drawing.Font("Segoe UI", 28)
$label.ForeColor = [System.Drawing.Color]::FromArgb(103, 226, 220)
$label.Text = "ForamEcoQS"
$form.Controls.Add($label)

[void]$form.ShowDialog()
