param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputRoot = "artifacts\portable\Starforged-Atlas-Portable"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$desktopProject = Join-Path $repoRoot "StarWin.Desktop\StarWin.Desktop.csproj"
$launcherProject = Join-Path $repoRoot "StarforgedAtlas.PortableLauncher\StarforgedAtlas.PortableLauncher.csproj"
$packageRoot = Join-Path $repoRoot $OutputRoot
$appRoot = Join-Path $packageRoot "app"
$zipPath = "$packageRoot.zip"

if (Test-Path $packageRoot)
{
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}

if (Test-Path $zipPath)
{
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $appRoot -Force | Out-Null

dotnet publish $desktopProject `
    -c $Configuration `
    -f net10.0-windows `
    -r $RuntimeIdentifier `
    --self-contained true `
    -o $appRoot

if ($LASTEXITCODE -ne 0)
{
    throw "Desktop publish failed."
}

dotnet publish $launcherProject `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained true `
    -o $packageRoot

if ($LASTEXITCODE -ne 0)
{
    throw "Launcher publish failed."
}

$launcherPublishArtifact = Join-Path $packageRoot "Starforged Atlas.pdb"
if (Test-Path $launcherPublishArtifact)
{
    Remove-Item -LiteralPath $launcherPublishArtifact -Force
}

$readmePath = Join-Path $packageRoot "README.txt"
@"
Starforged Atlas Portable

Launch the app by double-clicking:
Starforged Atlas.exe

Notes:
- Keep the 'app' folder beside the launcher.
- Your local data is stored in %LocalAppData%\Starforged Atlas.
"@ | Set-Content -Path $readmePath -Encoding ASCII

Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath

Write-Host "Portable package created:"
Write-Host "  Folder: $packageRoot"
Write-Host "  Zip:    $zipPath"
