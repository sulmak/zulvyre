param(
  [string]$Zip = "E:\ai game\Zulvyre-Unity.zip",
  [string]$Root = "E:\ai game\ZulvyreUnity",
  [string]$Rel = "Build\Zulvyre"
)
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [System.IO.Compression.ZipFile]::OpenRead($Zip)
$map = @{}
foreach ($e in $z.Entries) {
  if ($e.FullName.EndsWith("\")) { continue }
  $map[$e.FullName.Replace("/", "\")] = $e.Length
}
$z.Dispose()

$src = Join-Path $Root $Rel
$disk = Get-ChildItem -LiteralPath $src -Recurse -File
$missing = New-Object System.Collections.Generic.List[object]
$mismatch = New-Object System.Collections.Generic.List[object]
foreach ($f in $disk) {
  $key = "Zulvyre-Unity\" + $f.FullName.Substring($Root.Length + 1)
  if (-not $map.ContainsKey($key)) { $missing.Add($f) }
  elseif ($map[$key] -ne $f.Length) { $mismatch.Add(@{ Name = $f.Name; Src = $f.Length; Zip = $map[$key] }) }
}

Write-Output ("DISK FILES UNDER " + $Rel + ": " + $disk.Count)
Write-Output ("ZIP FILES UNDER SAME PREFIX: " + (@($map.Keys | Where-Object { $_.StartsWith("Zulvyre-Unity\" + $Rel + "\") }).Count))
Write-Output ("MISSING FROM ZIP: " + $missing.Count)
foreach ($f in ($missing | Sort-Object Length -Descending | Select-Object -First 25)) {
  Write-Output ("   {0,10:N0} KB  {1}" -f ($f.Length / 1KB), $f.FullName.Substring($Root.Length + 1))
}
Write-Output ("SIZE MISMATCH: " + $mismatch.Count)
foreach ($m in ($mismatch | Sort-Object Src -Descending | Select-Object -First 25)) {
  Write-Output ("   {0,-60} src {1,10:N0}  zip {2,10:N0}" -f $m.Name, $m.Src, $m.Zip)
}
Write-Output ("DISK TOTAL MB: " + [math]::Round(((Get-ChildItem -LiteralPath $src -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 1))
$zipBytes = 0
foreach ($k in $map.Keys) { if ($k.StartsWith("Zulvyre-Unity\" + $Rel + "\")) { $zipBytes += $map[$k] } }
Write-Output ("ZIP  TOTAL MB: " + [math]::Round(($zipBytes / 1MB), 1))
