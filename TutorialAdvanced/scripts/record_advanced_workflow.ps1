param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")),
    [string]$Ffmpeg = "ffmpeg",
    [switch]$DryRun,
    [switch]$AnalysisOnly,
    [switch]$KeepApplicationOpen
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class AdvancedTutorialNative
{
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string className, string windowName);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    public struct Point { public int X; public int Y; }
}
'@

function Wait-Seconds([double]$Seconds) {
    if ($DryRun) { $Seconds = [Math]::Max(0.25, $Seconds * 0.4) }
    Start-Sleep -Milliseconds ([int]($Seconds * 1000))
}

function Move-TutorialCursor([int]$X, [int]$Y, [int]$DurationMs = 550) {
    $start = New-Object AdvancedTutorialNative+Point
    [AdvancedTutorialNative]::GetCursorPos([ref]$start) | Out-Null
    $steps = [Math]::Max(12, [int]($DurationMs / 16))
    for ($i = 1; $i -le $steps; $i++) {
        $p = $i / $steps
        $eased = $p * $p * (3 - 2 * $p)
        [AdvancedTutorialNative]::SetCursorPos(
            [int]($start.X + (($X - $start.X) * $eased)),
            [int]($start.Y + (($Y - $start.Y) * $eased))
        ) | Out-Null
        Start-Sleep -Milliseconds 16
    }
}

function Click-TutorialPoint([int]$X, [int]$Y, [int]$DurationMs = 550) {
    Move-TutorialCursor -X $X -Y $Y -DurationMs $DurationMs
    [AdvancedTutorialNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 90
    [AdvancedTutorialNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}

function Find-TutorialElement {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [System.Windows.Automation.AutomationElement]$Root = [System.Windows.Automation.AutomationElement]::RootElement,
        [int]$TimeoutSeconds = 12
    )
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $Name
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
        [int]$TimeoutSeconds = 12,
        [double]$PauseAfter = 1.0
    )
    $element = Find-TutorialElement -Name $Name -Root $Root -TimeoutSeconds $TimeoutSeconds
    $bounds = $element.Current.BoundingRectangle
    if ($bounds.Width -le 0 -or $bounds.Height -le 0) { throw "UI element '$Name' has no clickable bounds." }
    Click-TutorialPoint -X ([int]($bounds.X + $bounds.Width / 2)) -Y ([int]($bounds.Y + $bounds.Height / 2))
    Wait-Seconds $PauseAfter
    return $element
}

function Invoke-TutorialElement {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [System.Windows.Automation.AutomationElement]$Root = [System.Windows.Automation.AutomationElement]::RootElement,
        [int]$TimeoutSeconds = 12,
        [double]$PauseAfter = 1.0
    )
    $element = Find-TutorialElement -Name $Name -Root $Root -TimeoutSeconds $TimeoutSeconds
    $patternObject = $null
    if ($element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$patternObject)) {
        try {
            ([System.Windows.Automation.InvokePattern]$patternObject).Invoke()
        }
        catch [System.Runtime.InteropServices.COMException] {
            # A WinForms menu handler that opens ShowDialog blocks the UIA Invoke
            # call until COM times out. The modal window has still opened, and the
            # caller verifies it by title immediately after this helper returns.
        }
    }
    else {
        $bounds = $element.Current.BoundingRectangle
        Click-TutorialPoint -X ([int]($bounds.X + $bounds.Width / 2)) -Y ([int]($bounds.Y + $bounds.Height / 2))
    }
    Wait-Seconds $PauseAfter
    return $element
}

function Get-ProcessWindow([int]$ProcessId, [string]$Name, [int]$TimeoutSeconds = 15) {
    $desktop = [System.Windows.Automation.AutomationElement]::RootElement
    $nameCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Name)
    $windowCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Window)
    $processCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
    $condition = New-Object System.Windows.Automation.AndCondition($nameCondition, $windowCondition, $processCondition)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $handle = [AdvancedTutorialNative]::FindWindow($null, $Name)
        if ($handle -ne [IntPtr]::Zero) {
            [uint32]$ownerProcessId = 0
            [AdvancedTutorialNative]::GetWindowThreadProcessId($handle, [ref]$ownerProcessId) | Out-Null
            if ($ownerProcessId -eq $ProcessId) {
                return [System.Windows.Automation.AutomationElement]::FromHandle($handle)
            }
        }
        try {
            $window = $desktop.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
            if ($null -ne $window) { return $window }
        }
        catch [System.Runtime.InteropServices.COMException] {
            # Some modal WinForms handlers temporarily block their UIA provider.
            # Native title lookup above remains available during that interval.
        }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)
    return $null
}

function Open-CsvDataset {
    param(
        [System.Windows.Automation.AutomationElement]$AppRoot,
        [string]$Path
    )
    Click-TutorialElement -Name "File" -Root $AppRoot -PauseAfter 1.0 | Out-Null
    # Open is the fifth selectable item in the main File menu.
    [System.Windows.Forms.SendKeys]::SendWait("{DOWN}{DOWN}{DOWN}{DOWN}{DOWN}{ENTER}")
    Wait-Seconds 1.5
    [System.Windows.Forms.SendKeys]::SendWait("%n")
    Wait-Seconds 0.4
    [System.Windows.Forms.SendKeys]::SendWait($Path)
    Wait-Seconds 0.8
    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    Wait-Seconds 1.5
    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    Wait-Seconds 4.0
}

function Select-JorissenDatabank([System.Windows.Automation.AutomationElement]$AppRoot) {
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "comboBox1"
    )
    $combo = $AppRoot.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if ($null -eq $combo) { throw "Databank combo was not found." }
    $bounds = $combo.Current.BoundingRectangle
    Click-TutorialPoint -X ([int]($bounds.X + $bounds.Width / 2)) -Y ([int]($bounds.Y + $bounds.Height / 2))
    [System.Windows.Forms.SendKeys]::SendWait("%{DOWN}")
    Wait-Seconds 0.7
    [System.Windows.Forms.SendKeys]::SendWait("j")
    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    Wait-Seconds 2.0
}

function Start-ScreenRecorder([string]$OutputPath) {
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $Ffmpeg
    $info.Arguments = "-y -f gdigrab -framerate 20 -offset_x 440 -offset_y 0 -video_size 2560x1440 -draw_mouse 1 -i desktop -c:v libx264 -preset ultrafast -crf 8 -pix_fmt yuv420p `"$OutputPath`""
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardInput = $true
    $info.RedirectStandardError = $false
    $recorder = New-Object System.Diagnostics.Process
    $recorder.StartInfo = $info
    [void]$recorder.Start()
    return $recorder
}

$appPath = Join-Path $RepositoryRoot "ForamEcoQS\bin\Release\net10.0-windows\ForamEcoQS.exe"
$wormsData = Join-Path $RepositoryRoot "TutorialAdvanced\dataset\WoRMS_Demo_Dataset.csv"
$customData = Join-Path $RepositoryRoot "TutorialAdvanced\dataset\Custom_Foram_AMBI_List.csv"
$geoData = Join-Path $RepositoryRoot "TutorialAdvanced\dataset\Geographic_Areas_Demo.csv"
$analysisData = Join-Path $RepositoryRoot "Tutorial\dataset\ForamEcoQS_Tutorial_Dataset.csv"
$rawOutput = Join-Path $RepositoryRoot "TutorialAdvanced\source\advanced_workflow_raw.mkv"
$backdropScript = Join-Path $PSScriptRoot "privacy_backdrop.ps1"
$userListsDirectory = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)) "ForamEcoQS\user_lists"
$customManifest = Join-Path $userListsDirectory "manifest.csv"
$customImportedFile = Join-Path $userListsDirectory "custom_foram_ambi_list_foram-ambi.csv"
$manifestExisted = Test-Path -LiteralPath $customManifest
$manifestBackup = if ($manifestExisted) { [System.IO.File]::ReadAllBytes($customManifest) } else { $null }

if (Test-Path -LiteralPath $customImportedFile) {
    throw "The tutorial custom-list file already exists; refusing to overwrite user data: $customImportedFile"
}
if ($manifestExisted -and (Select-String -LiteralPath $customManifest -Pattern '^Custom_Foram_AMBI_List;' -Quiet)) {
    throw "A custom list named Custom_Foram_AMBI_List already exists; refusing to overwrite user data."
}

foreach ($path in @($appPath, $wormsData, $customData, $geoData, $analysisData, $backdropScript)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Required file not found: $path" }
}

$backdrop = $null
$process = $null
$recorder = $null
try {
    Get-Process ForamEcoQS -ErrorAction SilentlyContinue | Stop-Process -Force
    $backdrop = Start-Process powershell -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $backdropScript) -WindowStyle Hidden -PassThru
    Wait-Seconds 2.0

    $process = Start-Process -FilePath $appPath -WorkingDirectory (Split-Path $appPath) -PassThru
    $windowDeadline = [DateTime]::UtcNow.AddSeconds(20)
    do {
        Wait-Seconds 0.5
        $process.Refresh()
    } while ($process.MainWindowHandle -eq 0 -and -not $process.HasExited -and [DateTime]::UtcNow -lt $windowDeadline)
    if ($process.HasExited -or $process.MainWindowHandle -eq 0) { throw "ForamEcoQS main window did not become ready." }
    [AdvancedTutorialNative]::ShowWindow($process.MainWindowHandle, 9) | Out-Null
    [AdvancedTutorialNative]::MoveWindow($process.MainWindowHandle, 440, 0, 2560, 1440, $true) | Out-Null
    Wait-Seconds 4.0
    $appRoot = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)

    if (-not $DryRun) {
        $recorder = Start-ScreenRecorder -OutputPath $rawOutput
        Wait-Seconds 3.0
    }

    if (-not $AnalysisOnly) {
        # 1. WoRMS verification: enable the online check, then compare a matrix
        # containing accepted taxa, a synonym, and an intentionally fictional name.
        Open-CsvDataset -AppRoot $appRoot -Path $wormsData
        Select-JorissenDatabank -AppRoot $appRoot
        Click-TutorialElement -Name "Tools" -Root $appRoot -PauseAfter 0.8 | Out-Null
        # Index Calculation Settings is the seventh selectable Tools item.
        [System.Windows.Forms.SendKeys]::SendWait("{DOWN}{DOWN}{DOWN}{DOWN}{DOWN}{DOWN}{DOWN}{ENTER}")
        Wait-Seconds 3.0
        $settings = Get-ProcessWindow -ProcessId $process.Id -Name "Index Calculation Settings"
        if ($null -eq $settings) { throw "Index settings window did not open." }
        Click-TutorialElement -Name "Verify unmatched species against WoRMS (World Register of Marine Species)" -Root $settings -PauseAfter 4.0 | Out-Null
        Click-TutorialElement -Name "OK" -Root $settings -PauseAfter 2.0 | Out-Null
        $saved = Get-ProcessWindow -ProcessId $process.Id -Name "Settings Saved" -TimeoutSeconds 5
        if ($null -ne $saved) { Click-TutorialElement -Name "OK" -Root $saved -PauseAfter 2.0 | Out-Null }

        Click-TutorialElement -Name "Compare" -Root $appRoot -PauseAfter 2.0 | Out-Null
        $wormsSummary = Get-ProcessWindow -ProcessId $process.Id -Name "WoRMS Species Verification" -TimeoutSeconds 30
        if ($null -eq $wormsSummary) { throw "WoRMS verification did not return a summary." }
        Wait-Seconds 7.0
        Click-TutorialElement -Name "OK" -Root $wormsSummary -PauseAfter 6.0 | Out-Null

        # 2. Import and inspect a custom Foram-AMBI list.
        Click-TutorialElement -Name "Tools" -Root $appRoot -PauseAfter 0.8 | Out-Null
        [System.Windows.Forms.SendKeys]::SendWait("{DOWN}{DOWN}{DOWN}{DOWN}{ENTER}")
        Wait-Seconds 3.0
        $customWindow = Get-ProcessWindow -ProcessId $process.Id -Name "User Custom Lists Manager"
        if ($null -eq $customWindow) { throw "Custom lists manager did not open." }
        Click-TutorialElement -Name "File" -Root $customWindow -PauseAfter 0.8 | Out-Null
        [System.Windows.Forms.SendKeys]::SendWait("{DOWN}{ENTER}")
        Wait-Seconds 1.5
        [System.Windows.Forms.SendKeys]::SendWait("%n")
        [System.Windows.Forms.SendKeys]::SendWait($customData)
        [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
        Wait-Seconds 2.0
        $metaWindow = Get-ProcessWindow -ProcessId $process.Id -Name "Import List - Set Properties"
        if ($null -eq $metaWindow) { throw "Custom-list properties window did not open." }
        Wait-Seconds 3.0
        Click-TutorialElement -Name "Import" -Root $metaWindow -PauseAfter 2.0 | Out-Null
        $importComplete = Get-ProcessWindow -ProcessId $process.Id -Name "Import Complete" -TimeoutSeconds 8
        if ($null -ne $importComplete) { Wait-Seconds 4.0; Click-TutorialElement -Name "OK" -Root $importComplete -PauseAfter 5.0 | Out-Null }
        [System.Windows.Forms.SendKeys]::SendWait("%{F4}")
        Wait-Seconds 3.0

        # 3. Import two synthetic geographic records, demonstrate filtering, and
        # discard the unsaved additions when closing so user data is not changed.
        Click-TutorialElement -Name "Tools" -Root $appRoot -PauseAfter 0.8 | Out-Null
        [System.Windows.Forms.SendKeys]::SendWait("{DOWN}{DOWN}{DOWN}{ENTER}")
        Wait-Seconds 3.0
        $geoWindow = Get-ProcessWindow -ProcessId $process.Id -Name "Geographic Areas Database"
        if ($null -eq $geoWindow) { throw "Geographic areas database did not open." }
        Click-TutorialElement -Name "File" -Root $geoWindow -PauseAfter 0.8 | Out-Null
        [System.Windows.Forms.SendKeys]::SendWait("{DOWN}{ENTER}")
        Wait-Seconds 1.5
        [System.Windows.Forms.SendKeys]::SendWait("%n")
        [System.Windows.Forms.SendKeys]::SendWait($geoData)
        [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
        Wait-Seconds 2.0
        $geoImported = Get-ProcessWindow -ProcessId $process.Id -Name "Import Complete" -TimeoutSeconds 8
        if ($null -ne $geoImported) { Wait-Seconds 4.0; Click-TutorialElement -Name "OK" -Root $geoImported -PauseAfter 5.0 | Out-Null }
        Click-TutorialElement -Name "Environment:" -Root $geoWindow -PauseAfter 0.5 | Out-Null
        [System.Windows.Forms.SendKeys]::SendWait("{TAB}")
        [System.Windows.Forms.SendKeys]::SendWait("c")
        [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
        Wait-Seconds 5.0
        [System.Windows.Forms.SendKeys]::SendWait("%{F4}")
        Wait-Seconds 1.5
        $unsaved = Get-ProcessWindow -ProcessId $process.Id -Name "Unsaved Changes" -TimeoutSeconds 5
        if ($null -ne $unsaved) {
            Click-TutorialElement -Name "No" -Root $unsaved -PauseAfter 3.0 | Out-Null
        }
    }

    # 4. Load the validated analysis matrix and calculate EQS-capable indices.
    Open-CsvDataset -AppRoot $appRoot -Path $analysisData
    Select-JorissenDatabank -AppRoot $appRoot
    Click-TutorialElement -Name "Compare" -Root $appRoot -PauseAfter 5.0 | Out-Null
    Click-TutorialElement -Name "Clean and normalize" -Root $appRoot -PauseAfter 4.0 | Out-Null
    Click-TutorialElement -Name "Calculate Indices" -Root $appRoot -PauseAfter 3.0 | Out-Null
    $selection = Get-ProcessWindow -ProcessId $process.Id -Name "Select Indices to Calculate"
    if ($null -eq $selection) { throw "Index selection dialog did not open." }
    Wait-Seconds 3.0
    Click-TutorialElement -Name "Calculate" -Root $selection -PauseAfter 3.0 | Out-Null

    $sedimentPrompt = Get-ProcessWindow -ProcessId $process.Id -Name "TSI-Med: Sediment Data" -TimeoutSeconds 10
    if ($null -ne $sedimentPrompt) { [System.Windows.Forms.SendKeys]::SendWait("{ENTER}"); Wait-Seconds 2.0 }
    $mudWindow = Get-ProcessWindow -ProcessId $process.Id -Name "TSI-Med: Mud Percentage Input" -TimeoutSeconds 10
    if ($null -ne $mudWindow) {
        $mudValues = @(15, 20, 35, 50, 70, 85)
        for ($row = 0; $row -lt $mudValues.Count; $row++) {
            Click-TutorialPoint -X 1825 -Y (410 + (22 * $row)) -DurationMs 250
            [System.Windows.Forms.SendKeys]::SendWait("^a")
            [System.Windows.Forms.SendKeys]::SendWait($mudValues[$row].ToString())
        }
        Wait-Seconds 3.0
        Click-TutorialElement -Name "OK" -Root $mudWindow -PauseAfter 6.0 | Out-Null
    }

    $status = Get-ProcessWindow -ProcessId $process.Id -Name "Index Calculation Status" -TimeoutSeconds 4
    if ($null -ne $status) { Click-TutorialElement -Name "OK" -Root $status -PauseAfter 3.0 | Out-Null }
    $advanced = Get-ProcessWindow -ProcessId $process.Id -Name "Advanced Biotic Indices Calculator" -TimeoutSeconds 15
    if ($null -eq $advanced) { throw "Advanced results window did not open." }
    [AdvancedTutorialNative]::ShowWindow([IntPtr]$advanced.Current.NativeWindowHandle, 9) | Out-Null
    [AdvancedTutorialNative]::MoveWindow([IntPtr]$advanced.Current.NativeWindowHandle, 440, 0, 2560, 1440, $true) | Out-Null
    Wait-Seconds 5.0

    Click-TutorialElement -Name "Plots" -Root $advanced -PauseAfter 0.8 | Out-Null
    # Normalize the menu selection with Home, then move from the first item to
    # the ninth selectable item (EQS Agreement Analysis).
    [System.Windows.Forms.SendKeys]::SendWait("{HOME}{DOWN}{DOWN}{DOWN}{DOWN}{DOWN}{DOWN}{DOWN}{DOWN}{ENTER}")
    Wait-Seconds 7.0
    $agreement = Get-ProcessWindow -ProcessId $process.Id -Name "EQS Agreement Analysis (Cohen's Kappa and Confusion Matrix)" -TimeoutSeconds 12
    if ($null -eq $agreement) { throw "EQS agreement window did not open." }
    Wait-Seconds 7.0
    Click-TutorialElement -Name "Confusion Matrices" -Root $agreement -PauseAfter 5.0 | Out-Null
    Click-TutorialElement -Name "Generate Confusion Matrix" -Root $agreement -PauseAfter 12.0 | Out-Null

    if (-not $DryRun) { Wait-Seconds 3.0 }
}
finally {
    if ($null -ne $recorder -and -not $recorder.HasExited) {
        try { $recorder.StandardInput.WriteLine("q"); $recorder.StandardInput.Flush() } catch {}
        if (-not $recorder.WaitForExit(20000)) { $recorder.Kill() }
    }
    if (-not $KeepApplicationOpen) { Get-Process ForamEcoQS -ErrorAction SilentlyContinue | Stop-Process -Force }
    if ($null -ne $backdrop -and -not $backdrop.HasExited) { $backdrop.Kill() }
    if (Test-Path -LiteralPath $customImportedFile) { Remove-Item -LiteralPath $customImportedFile -Force }
    if ($manifestExisted) {
        [System.IO.File]::WriteAllBytes($customManifest, $manifestBackup)
    }
    elseif (Test-Path -LiteralPath $customManifest) {
        Remove-Item -LiteralPath $customManifest -Force
    }
}

if (-not $DryRun) { Write-Host "Recorded: $rawOutput" }
