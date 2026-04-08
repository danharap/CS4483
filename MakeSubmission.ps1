# Group21 / ThePit — zip Unity project + Windows build + report PDF for submission.
# Run AFTER building in Unity: CS4483 -> Build Windows x64 (outputs Build\ThePit\ThePit.exe).
# Edit $reportPdf if your PDF is not in Downloads.

$ErrorActionPreference = "Stop"

$projectRoot = "C:\Users\danie\CS4483\CS4483"
$buildDir    = Join-Path $projectRoot "Build\ThePit"
$reportPdf   = "C:\Users\danie\Downloads\DELIVERABLE 2 Report.pdf"
$stagingDir  = "C:\Users\danie\CS4483\Group21_ThePit_Submission"
$outputZip   = "C:\Users\danie\Desktop\Group21_ThePit_Submission.zip"

if (-not (Test-Path "$buildDir\ThePit.exe"))  { Write-Error "Build exe not found at $buildDir\ThePit.exe — run the Unity menu build first."; exit 1 }
if (-not (Test-Path "$buildDir\ThePit_Data")) { Write-Error "ThePit_Data folder not found next to ThePit.exe."; exit 1 }
if (-not (Test-Path $reportPdf))              { Write-Error "Report PDF not found: $reportPdf — update `$reportPdf in this script."; exit 1 }

Write-Host "Staging Group21 / ThePit submission package..." -ForegroundColor Cyan

if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $stagingDir | Out-Null

Write-Host "  Copying Unity project (Assets, Packages, ProjectSettings)..."
foreach ($folder in @("Assets", "Packages", "ProjectSettings")) {
    Copy-Item -Path (Join-Path $projectRoot $folder) -Destination (Join-Path $stagingDir $folder) -Recurse -Force
}

Write-Host "  Copying Windows x64 build (ThePit.exe)..."
$buildDst = Join-Path $stagingDir "Build"
New-Item -ItemType Directory -Path $buildDst | Out-Null
Copy-Item -Path "$buildDir\ThePit.exe" -Destination $buildDst -Force
Copy-Item -Path "$buildDir\UnityPlayer.dll" -Destination $buildDst -Force -ErrorAction SilentlyContinue
Copy-Item -Path "$buildDir\UnityCrashHandler64.exe" -Destination $buildDst -Force -ErrorAction SilentlyContinue
Copy-Item -Path "$buildDir\ThePit_Data" -Destination (Join-Path $buildDst "ThePit_Data") -Recurse -Force
if (Test-Path "$buildDir\MonoBleedingEdge") {
    Copy-Item -Path "$buildDir\MonoBleedingEdge" -Destination (Join-Path $buildDst "MonoBleedingEdge") -Recurse -Force
}

Write-Host "  Copying report PDF..."
Copy-Item -Path $reportPdf -Destination (Join-Path $stagingDir "Group21_ThePit_Report.pdf") -Force

Write-Host "  Creating zip..."
if (Test-Path $outputZip) { Remove-Item $outputZip -Force }
Compress-Archive -Path "$stagingDir\*" -DestinationPath $outputZip -CompressionLevel Optimal

$sizeMB = [math]::Round((Get-Item $outputZip).Length / 1MB, 1)
Write-Host ""
Write-Host "Done: $outputZip ($sizeMB MB)" -ForegroundColor Green
Write-Host "  Folder: $stagingDir"
Write-Host "  Contents: Assets, Packages, ProjectSettings, Build (ThePit.exe + ThePit_Data), Group21_ThePit_Report.pdf"
