# Package the Zulvyre Unity edition for distribution.
# Writes E:\ai game\Zulvyre-Unity.zip with the Windows player, the Unity project
# sources (no Library/Logs), the verified screenshots and the docs.

$ErrorActionPreference = 'Stop'

$root = 'E:\ai game\ZulvyreUnity'
$stage = Join-Path $env:TEMP 'Zulvyre-Unity-package'
$target = 'E:\ai game\Zulvyre-Unity.zip'
$inner = Join-Path $stage 'Zulvyre-Unity'

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $inner -Force | Out-Null

function Copy-Tree($source, $dest) {
    if (-not (Test-Path $source)) { Write-Warning "missing $source"; return }
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
    Get-ChildItem $source -Force | ForEach-Object {
        $to = Join-Path $dest $_.Name
        if ($_.PSIsContainer) { Copy-Tree $_.FullName $to } else { Copy-Item $_.FullName $to -Force }
    }
}

# Playable build.
Copy-Tree (Join-Path $root 'Build\Zulvyre') (Join-Path $inner 'Build\Zulvyre')

# Project sources, minus generated caches and logs.
foreach ($dir in @('Assets', 'Packages', 'ProjectSettings')) {
    Copy-Tree (Join-Path $root $dir) (Join-Path $inner $dir)
}
foreach ($drop in @('Assets\Library', 'Assets\Temp', 'Assets\Obj')) {
    $p = Join-Path $inner $drop
    if (Test-Path $p) { Remove-Item $p -Recurse -Force }
}
Copy-Item (Join-Path $root 'ZulvyreUnity.slnx') $inner -Force -ErrorAction SilentlyContinue

# Tools and docs.
Copy-Tree (Join-Path $root 'Tools') (Join-Path $inner 'Tools')
foreach ($drop in @('Tools\maya\__pycache__', 'Tools\__pycache__')) {
    $p = Join-Path $inner $drop
    if (Test-Path $p) { Remove-Item $p -Recurse -Force }
}
foreach ($doc in @('README.md', 'CHANGELOG.md')) { Copy-Item (Join-Path $root $doc) $inner -Force }

# Screenshots: menu/duel tour, world tour and the humanoid gallery.
New-Item -ItemType Directory -Path (Join-Path $inner 'Shots') -Force | Out-Null
Copy-Tree (Join-Path $root 'Shots\humans') (Join-Path $inner 'Shots\humans')
Copy-Tree (Join-Path $root 'Shots\flow') (Join-Path $inner 'Shots\flow')
Copy-Tree (Join-Path $root 'Shots\world') (Join-Path $inner 'Shots\world')
Get-ChildItem (Join-Path $root 'Shots') -File | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $inner 'Shots') -Force
}

if (Test-Path $target) { Remove-Item $target -Force }
Compress-Archive -Path $inner -DestinationPath $target -CompressionLevel Optimal
Remove-Item $stage -Recurse -Force

$info = Get-Item $target
Write-Host ("PACKAGED {0}  {1:N1} MB" -f $info.FullName, ($info.Length / 1MB))
