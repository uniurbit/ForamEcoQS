param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "ForamEcoQS\ForamEcoQS.csproj"
$iss = Join-Path $PSScriptRoot "ForamEcoQS.iss"
$iscc = Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"

if (-not (Test-Path -LiteralPath $iscc)) {
    throw "Inno Setup compiler not found at: $iscc"
}

[xml]$projectXml = Get-Content -LiteralPath $project
$version = [string]$projectXml.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Could not read Version from $project"
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\installer"
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ForamEcoQS-installer-" + [guid]::NewGuid().ToString("N"))
$publishDir = Join-Path $tempRoot "publish"
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

try {
    & dotnet publish $project -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=false -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

    & $iscc "/DAppVersion=$version" "/DPublishDir=$publishDir" "/DOutputDir=$OutputDirectory" $iss
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed with exit code $LASTEXITCODE" }

    $installer = Join-Path $OutputDirectory "ForamEcoQS-v$version-win-x64-setup.exe"
    if (-not (Test-Path -LiteralPath $installer)) { throw "Installer was not created: $installer" }

    $hash = Get-FileHash -LiteralPath $installer -Algorithm SHA256
    Write-Host "Installer: $installer"
    Write-Host "SHA-256: $($hash.Hash)"
}
finally {
    $resolvedTemp = [System.IO.Path]::GetFullPath($tempRoot)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($resolvedTemp.StartsWith($systemTemp, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemp)) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}
