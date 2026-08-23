param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")),
    [switch]$KeepApplicationOpen
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class TutorialNative
{
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    public struct Point { public int X; public int Y; }
}
'@

function Wait-Seconds([double]$Seconds) {
    Start-Sleep -Milliseconds ([int]($Seconds * 1000))
}

function Move-TutorialCursor([int]$X, [int]$Y, [int]$DurationMs = 650) {
    $start = New-Object TutorialNative+Point
    [TutorialNative]::GetCursorPos([ref]$start) | Out-Null
    $steps = [Math]::Max(12, [int]($DurationMs / 16))
    for ($i = 1; $i -le $steps; $i++) {
        $p = $i / $steps
        $eased = $p * $p * (3 - 2 * $p)
        $nextX = [int]($start.X + (($X - $start.X) * $eased))
        $nextY = [int]($start.Y + (($Y - $start.Y) * $eased))
        [TutorialNative]::SetCursorPos($nextX, $nextY) | Out-Null
        Start-Sleep -Milliseconds 16
    }
}

function Click-TutorialPoint([int]$X, [int]$Y, [int]$DurationMs = 650) {
    Move-TutorialCursor -X $X -Y $Y -DurationMs $DurationMs
    [TutorialNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 90
    [TutorialNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}

function Find-TutorialElement {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [System.Windows.Automation.AutomationElement]$Root = [System.Windows.Automation.AutomationElement]::RootElement,
        [int]$TimeoutSeconds = 10
    )

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name
    )
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $element = $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
        if ($null -ne $element) { return $element }
        Start-Sleep -Milliseconds 200
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "UI element '$Name' was not found within $TimeoutSeconds seconds."
}

function Click-TutorialElement {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [System.Windows.Automation.AutomationElement]$Root = [System.Windows.Automation.AutomationElement]::RootElement,
        [int]$TimeoutSeconds = 10,
        [double]$PauseAfter = 1.0
    )

    $element = Find-TutorialElement -Name $Name -Root $Root -TimeoutSeconds $TimeoutSeconds
    $bounds = $element.Current.BoundingRectangle
    if ($bounds.Width -le 0 -or $bounds.Height -le 0) {
        throw "UI element '$Name' has no clickable bounds."
    }
    Click-TutorialPoint -X ([int]($bounds.X + $bounds.Width / 2)) -Y ([int]($bounds.Y + $bounds.Height / 2))
    Wait-Seconds $PauseAfter
    return $element
}

function Get-ProcessWindow([int]$ProcessId, [string]$Name, [int]$TimeoutSeconds = 15) {
    $desktop = [System.Windows.Automation.AutomationElement]::RootElement
    $nameCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name
    )
    $windowCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Window
    )
    $processCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
        $ProcessId
    )
    $condition = New-Object System.Windows.Automation.AndCondition(
        $nameCondition,
        $windowCondition,
        $processCondition
    )
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $window = $desktop.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
        if ($null -ne $window) { return $window }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    return $null
}

$appPath = Join-Path $RepositoryRoot "ForamEcoQS\bin\Release\net10.0-windows\ForamEcoQS.exe"
$datasetPath = Join-Path $RepositoryRoot "Tutorial\dataset\ForamEcoQS_Tutorial_Dataset.csv"

if (-not (Test-Path -LiteralPath $appPath)) { throw "Release executable not found: $appPath" }
if (-not (Test-Path -LiteralPath $datasetPath)) { throw "Tutorial dataset not found: $datasetPath" }

Get-Process ForamEcoQS -ErrorAction SilentlyContinue | Stop-Process -Force
$process = Start-Process -FilePath $appPath -WorkingDirectory (Split-Path $appPath) -PassThru
Wait-Seconds 5.0
$process.Refresh()

[TutorialNative]::ShowWindow($process.MainWindowHandle, 9) | Out-Null
[TutorialNative]::MoveWindow($process.MainWindowHandle, 440, 0, 2560, 1440, $true) | Out-Null
Wait-Seconds 3.0

$appRoot = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)

# Import the synthetic dataset.
Click-TutorialElement -Name "File" -Root $appRoot -PauseAfter 1.5 | Out-Null
Click-TutorialElement -Name "Open" -PauseAfter 2.0 | Out-Null
[System.Windows.Forms.SendKeys]::SendWait("%n")
Wait-Seconds 0.5
[System.Windows.Forms.SendKeys]::SendWait($datasetPath)
Wait-Seconds 1.5
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
Wait-Seconds 2.0
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
Wait-Seconds 5.0

# Select the Jorissen Mediterranean databank through the now non-editable combo.
$comboCondition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
    "comboBox1"
)
$combo = $appRoot.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $comboCondition)
$comboBounds = $combo.Current.BoundingRectangle
Click-TutorialPoint -X ([int]($comboBounds.X + $comboBounds.Width / 2)) -Y ([int]($comboBounds.Y + $comboBounds.Height / 2))
[System.Windows.Forms.SendKeys]::SendWait("%{DOWN}")
Wait-Seconds 1.0
[System.Windows.Forms.SendKeys]::SendWait("j")
Wait-Seconds 1.0
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
Wait-Seconds 3.0

Click-TutorialElement -Name "Compare" -Root $appRoot -PauseAfter 5.0 | Out-Null

# Select Sample_01 to populate the Statistics panel.
Click-TutorialPoint -X 640 -Y 93
Wait-Seconds 4.0

Click-TutorialElement -Name "Clean and normalize" -Root $appRoot -PauseAfter 5.0 | Out-Null

# Show the calculation settings without changing the documented defaults.
Click-TutorialElement -Name "Tools" -Root $appRoot -PauseAfter 1.0 | Out-Null
Click-TutorialElement -Name "Index Calculation Settings..." -PauseAfter 5.0 | Out-Null
$settingsWindow = Get-ProcessWindow -ProcessId $process.Id -Name "Index Calculation Settings"
if ($null -ne $settingsWindow) {
    Click-TutorialElement -Name "OK" -Root $settingsWindow -PauseAfter 3.0 | Out-Null
} else {
    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    Wait-Seconds 3.0
}

$settingsSavedWindow = Get-ProcessWindow -ProcessId $process.Id -Name "Settings Saved" -TimeoutSeconds 5
if ($null -ne $settingsSavedWindow) {
    Wait-Seconds 3.0
    Click-TutorialElement -Name "OK" -Root $settingsSavedWindow -PauseAfter 3.0 | Out-Null
}

# Calculate the default set of indices.
Click-TutorialElement -Name "Calculate Indices" -Root $appRoot -PauseAfter 4.0 | Out-Null
$selectionWindow = Get-ProcessWindow -ProcessId $process.Id -Name "Select Indices to Calculate"
if ($null -eq $selectionWindow) { throw "Index selection dialog did not open." }
$selectionBounds = $selectionWindow.Current.BoundingRectangle
[TutorialNative]::MoveWindow(
    [IntPtr]$selectionWindow.Current.NativeWindowHandle,
    1520,
    300,
    [int]$selectionBounds.Width,
    [int]$selectionBounds.Height,
    $true
) | Out-Null
Wait-Seconds 2.0
Click-TutorialElement -Name "Calculate" -Root $selectionWindow -PauseAfter 4.0 | Out-Null

$sedimentPrompt = Get-ProcessWindow -ProcessId $process.Id -Name "TSI-Med: Sediment Data" -TimeoutSeconds 10
if ($null -ne $sedimentPrompt) {
    Wait-Seconds 4.0
    # Yes is the default button. Using Enter is language-independent on localized Windows.
    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    Wait-Seconds 3.0
}

$mudWindow = Get-ProcessWindow -ProcessId $process.Id -Name "TSI-Med: Mud Percentage Input" -TimeoutSeconds 10
if ($null -ne $mudWindow) {
    $mudBounds = $mudWindow.Current.BoundingRectangle
    [TutorialNative]::MoveWindow(
        [IntPtr]$mudWindow.Current.NativeWindowHandle,
        1470,
        260,
        [int]$mudBounds.Width,
        [int]$mudBounds.Height,
        $true
    ) | Out-Null
    Wait-Seconds 3.0

    # Enter sample-specific sediment values in the second grid column.
    $mudValues = @(15, 20, 35, 50, 70, 85)
    for ($row = 0; $row -lt $mudValues.Count; $row++) {
        Click-TutorialPoint -X 1825 -Y (410 + (22 * $row)) -DurationMs 350
        [System.Windows.Forms.SendKeys]::SendWait("^a")
        [System.Windows.Forms.SendKeys]::SendWait($mudValues[$row].ToString())
        Wait-Seconds 0.35
    }
    Wait-Seconds 4.0
    Click-TutorialElement -Name "OK" -Root $mudWindow -PauseAfter 7.0 | Out-Null
}

$statusWindow = Get-ProcessWindow -ProcessId $process.Id -Name "Index Calculation Status" -TimeoutSeconds 4
if ($null -ne $statusWindow) {
    Wait-Seconds 4.0
    Click-TutorialElement -Name "OK" -Root $statusWindow -PauseAfter 4.0 | Out-Null
}

$advancedWindow = Get-ProcessWindow -ProcessId $process.Id -Name "Advanced Biotic Indices Calculator" -TimeoutSeconds 15
if ($null -eq $advancedWindow) { throw "Advanced indices window did not open." }
[TutorialNative]::ShowWindow([IntPtr]$advancedWindow.Current.NativeWindowHandle, 9) | Out-Null
[TutorialNative]::MoveWindow([IntPtr]$advancedWindow.Current.NativeWindowHandle, 440, 0, 2560, 1440, $true) | Out-Null
Wait-Seconds 6.0

Click-TutorialElement -Name "EQS Summary" -Root $advancedWindow -PauseAfter 6.0 | Out-Null
Click-TutorialElement -Name "Plot Options" -Root $advancedWindow -PauseAfter 6.0 | Out-Null

# Keep the tutorial plot readable: one biotic, one sensitivity, and one
# diversity index. Selecting every available index produces an unusable chart.
foreach ($indexName in @("Foram-AMBI", "FSI", "exp(H'bc)")) {
    Click-TutorialElement -Name $indexName -Root $advancedWindow -PauseAfter 1.2 | Out-Null
}

$comboTypeCondition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::ComboBox
)
$plotCombos = $advancedWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants, $comboTypeCondition)
$plotTypeCombo = $plotCombos | Sort-Object { $_.Current.BoundingRectangle.Y } | Select-Object -First 1
$plotTypeBounds = $plotTypeCombo.Current.BoundingRectangle
Click-TutorialPoint -X ([int]($plotTypeBounds.X + $plotTypeBounds.Width / 2)) -Y ([int]($plotTypeBounds.Y + $plotTypeBounds.Height / 2))
[System.Windows.Forms.SendKeys]::SendWait("%{DOWN}")
Wait-Seconds 0.8
[System.Windows.Forms.SendKeys]::SendWait("l")
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
Wait-Seconds 3.0

Click-TutorialElement -Name "Generate Plot" -Root $advancedWindow -PauseAfter 10.0 | Out-Null

# Open the composite dashboard from the Plots menu.
Click-TutorialElement -Name "Plots" -Root $advancedWindow -PauseAfter 1.0 | Out-Null
Click-TutorialElement -Name "Composite Panel" -PauseAfter 12.0 | Out-Null

if (-not $KeepApplicationOpen) {
    Get-Process ForamEcoQS -ErrorAction SilentlyContinue | Stop-Process -Force
}
