Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
"@

$exe = "E:\ai game\ZulvyreUnity\Build\Zulvyre\Zulvyre.exe"
$dir = "E:\ai game\ZulvyreUnity\Shots"
$p = Start-Process -FilePath $exe -ArgumentList "-world" -PassThru
Start-Sleep -Seconds 4
$p.Refresh()
$h = $p.MainWindowHandle
[Win32]::ShowWindow($h, 9) | Out-Null
[Win32]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 500

function HoldKey([byte]$k, [int]$ms) {
    [Win32]::keybd_event($k, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $ms
    [Win32]::keybd_event($k, 0, 2, [UIntPtr]::Zero)
}

function TapKey([byte]$k) {
    [Win32]::keybd_event($k, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [Win32]::keybd_event($k, 0, 2, [UIntPtr]::Zero)
}

function Shot([string]$name) {
    Start-Sleep -Milliseconds 300
    $bmp = New-Object System.Drawing.Bitmap 1600, 900
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen(0, 0, 0, 0, $bmp.Size)
    $bmp.Save((Join-Path $dir $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

HoldKey 0x57 1700
Start-Sleep -Milliseconds 400
TapKey 0x20
Start-Sleep -Milliseconds 450
TapKey 0x20
Start-Sleep -Milliseconds 450
Shot "live-world-move.png"

TapKey 0x4D
Start-Sleep -Milliseconds 1000
Shot "live-world-map.png"
TapKey 0x4D
Start-Sleep -Milliseconds 500

TapKey 0x1B
Start-Sleep -Milliseconds 700
Shot "live-world-pause.png"
TapKey 0x1B
Start-Sleep -Milliseconds 300

$p.CloseMainWindow() | Out-Null
Start-Sleep -Milliseconds 900
if (-not $p.HasExited) { $p.Kill() }
