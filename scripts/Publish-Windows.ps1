param(
    [Parameter(Mandatory = $true)][string]$MediaDirectory,
    [string]$OutputDirectory = "$PSScriptRoot/../artifacts",
    [switch]$SelfContained,
    [switch]$Installer
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $IsWindows) { throw 'Publish the Windows release from Windows.' }
$root = Split-Path $PSScriptRoot -Parent
$media = (Resolve-Path $MediaDirectory).Path
$out = [IO.Path]::GetFullPath($OutputDirectory)
$version = '0.1.0'
$package = Join-Path $out "Crumb-$version-win-x64"
if (Test-Path $package) { throw "Output already exists: $package. Choose a clean output directory." }
foreach ($required in @('tools/ffmpeg.exe', 'tools/ffprobe.exe', 'sources/ffmpeg.tar.gz', 'sources/x264.tar.gz', 'licenses/media/COPYING.GPLv3')) {
    if (-not (Test-Path (Join-Path $media $required))) { throw "Missing media payload: $required" }
}
New-Item -ItemType Directory -Force $package | Out-Null
$aot = if ($SelfContained) { 'false' } else { 'true' }
dotnet publish "$root/src/Crumb.Desktop/Crumb.Desktop.csproj" -c Release -r win-x64 --self-contained true `
    -p:PublishAot=$aot -p:StripSymbols=true -p:DebugType=None -o $package
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
Copy-Item "$media/tools" $package -Recurse
Copy-Item "$media/licenses" $package -Recurse
Copy-Item "$media/sources" $package -Recurse
Copy-Item "$media/build-logs" $package -Recurse
Copy-Item "$media/SHA256SUMS.txt" "$package/MEDIA-SHA256SUMS.txt"
Copy-Item "$root/LICENSE" "$package/LICENSE.txt"
Copy-Item "$root/docs/third-party.md" "$package/THIRD-PARTY.md"
Copy-Item "$root/docs/usage.md" "$package/README.md"

# Preserve package metadata and license/notice files from the actual resolved dependency graph.
$assets = Get-Content "$root/src/Crumb.Desktop/obj/project.assets.json" -Raw | ConvertFrom-Json -AsHashtable
$noticeDir = Join-Path $package 'licenses/nuget'
New-Item -ItemType Directory -Force $noticeDir | Out-Null
foreach ($entry in $assets.libraries.GetEnumerator()) {
    if ($entry.Value.type -ne 'package') { continue }
    foreach ($folder in $assets.packageFolders.Keys) {
        $source = Join-Path $folder $entry.Value.path
        if (-not (Test-Path $source)) { continue }
        $target = Join-Path $noticeDir ($entry.Key -replace '/', '-')
        New-Item -ItemType Directory -Force $target | Out-Null
        $index = 0
        Get-ChildItem $source -File -Recurse | Where-Object {
            $_.Name -match '^(LICENSE|COPYING|NOTICE|THIRD.PARTY)' -or $_.Extension -eq '.nuspec'
        } | ForEach-Object {
            $index++
            Copy-Item $_.FullName (Join-Path $target "$index-$($_.Name)")
        }
    }
}

# Check the published application with its own relative tool resolution.
$smoke = Join-Path $out ('smoke-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $smoke | Out-Null
try {
    $inputVideo = Join-Path $smoke 'input.mkv'
    $outputVideo = Join-Path $smoke 'output.mp4'
    & "$package/tools/ffmpeg.exe" -v error -y -f lavfi -i 'testsrc2=size=640x360:rate=24' -t 3 -c:v libx264 -threads 2 -qp 0 $inputVideo
    if ($LASTEXITCODE -ne 0) { throw 'Bundled FFmpeg could not generate the smoke-test clip.' }
    $savedTools = $env:CRUMB_FFMPEG_DIR
    try {
        Remove-Item Env:CRUMB_FFMPEG_DIR -ErrorAction SilentlyContinue
        $process = Start-Process -FilePath "$package/Crumb.exe" -ArgumentList @('--verify-bundle', "`"$inputVideo`"", "`"$outputVideo`"", '180000') -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw 'Published application failed the bundled compression check.' }
    } finally { $env:CRUMB_FFMPEG_DIR = $savedTools }
    if (-not (Test-Path $outputVideo) -or (Get-Item $outputVideo).Length -gt 180000) { throw 'Published output exceeded its size cap.' }
} finally { Remove-Item $smoke -Recurse -Force }

$zip = "$package.zip"
Compress-Archive -Path $package -DestinationPath $zip -CompressionLevel Optimal
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($zip))" | Set-Content "$zip.sha256" -Encoding utf8NoBOM
if ($Installer) {
    $isccCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    $isccPath = if ($isccCommand) { $isccCommand.Source } else { "${env:ProgramFiles(x86)}/Inno Setup 6/ISCC.exe" }
    if (-not (Test-Path $isccPath)) { throw 'Install Inno Setup 6 to build the optional installer.' }
    & $isccPath "/DPackageDir=$package" "/DOutputDir=$out" "$root/scripts/installer.iss"
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
}
Write-Output "Portable ZIP ready: $zip"
