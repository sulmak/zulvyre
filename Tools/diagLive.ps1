Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32D {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

$exe = "E:\ai game\ZulvyreUnity\Build\Zulvyre\Zulvyre.exe"
$dir = "E:\ai game\ZulvyreUnity\Shots\diag"
New-Item -ItemType Directory -Force -Path $dir | Out-Null

function HoldKey([byte]$k, [int]$ms) {
    [Win32D]::keybd_event($k, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds $ms
    [Win32D]::keybd_event($k, 0, 2, [UIntPtr]::Zero)
}
function TapKey([byte]$k) { HoldKey $k 60 }

function Focus([Diagnostics.Process]$p) {
    $p.Refresh()
    $h = $p.MainWindowHandle
    [Win32D]::ShowWindow($h, 3) | Out-Null
    [Win32D]::BringWindowToTop($h) | Out-Null
    [Win32D]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 250
}

function Shot([Diagnostics.Process]$p, [string]$name) {
    Focus $p
    Start-Sleep -Milliseconds 350
    $r = New-Object Win32D+RECT
    [Win32D]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
    $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save((Join-Path $dir $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "shot $name ${w}x${h}"
}

# ---- world: baseline, map overlay, art/character panel
$pw = Start-Process -FilePath $exe -ArgumentList "-world" -PassThru
Start-Sleep -Seconds 6
Focus $pw
Shot $pw "world-plain.png"
TapKey 0x4D; Start-Sleep -Milliseconds 800; Shot $pw "world-map.png"
TapKey 0x4D; Start-Sleep -Milliseconds 400
TapKey 0x51; Start-Sleep -Milliseconds 700; Shot $pw "world-art.png"
TapKey 0x51; Start-Sleep -Milliseconds 300
TapKey 0x1B; Start-Sleep -Milliseconds 700; Shot $pw "world-pause.png"
$pw.CloseMainWindow() | Out-Null; Start-Sleep -Milliseconds 900
if (-not $pw.HasExited) { $pw.Kill() }

# ---- duel: baseline, ability panel, target lock
$pd = Start-Process -FilePath $exe -ArgumentList "-quickduel" -PassThru
Start-Sleep -Seconds 6
Focus $pd
Shot $pd "duel-plain.png"
HoldKey 0x51 120; Start-Sleep -Milliseconds 900; Shot $pd "duel-q.png"
HoldKey 0x44 120; Start-Sleep -Milliseconds 900; Shot $pd "duel-d.png"
$pd.CloseMainWindow() | Out-Null; Start-Sleep -Milliseconds 900
if (-not $pd.HasExited) { $pd.Kill() }
