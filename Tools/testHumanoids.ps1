Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class HumanWin32 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
"@

$exe = "E:\ai game\ZulvyreUnity\Build\Zulvyre\Zulvyre.exe"
$dir = "E:\ai game\ZulvyreUnity\Shots"
$p = Start-Process -FilePath $exe -ArgumentList "-world" -PassThru
Start-Sleep -Seconds 15
$p.Refresh()
$h = $p.MainWindowHandle
[HumanWin32]::ShowWindow($h, 9) | Out-Null
[HumanWin32]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Seconds 3

function Down([byte]$k) { [HumanWin32]::keybd_event($k, 0, 0, [UIntPtr]::Zero) }
function Up([byte]$k) { [HumanWin32]::keybd_event($k, 0, 2, [UIntPtr]::Zero) }

function Shot([string]$name) {
    Start-Sleep -Milliseconds 250
    $bmp = New-Object System.Drawing.Bitmap 1600, 900
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen(0, 0, 0, 0, $bmp.Size)
    $bmp.Save((Join-Path $dir $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

# The player spawns south of the village square and walks north, so every shot
# catches the animated body from behind while it moves past the villagers.
Shot "human-01-idle.png"

Down 0x57
Start-Sleep -Milliseconds 900
Shot "human-02-walk.png"
Start-Sleep -Milliseconds 1400
Up 0x57

Start-Sleep -Milliseconds 400
Down 0x10
Down 0x57
Start-Sleep -Milliseconds 1100
Shot "human-03-run.png"
Start-Sleep -Milliseconds 900
Up 0x57
Up 0x10

Start-Sleep -Milliseconds 600
Down 0x20
Start-Sleep -Milliseconds 60
Up 0x20
Start-Sleep -Milliseconds 110
Shot "human-04-attack.png"

Start-Sleep -Milliseconds 1200
Shot "human-05-village.png"

Start-Sleep -Milliseconds 700
$p.CloseMainWindow() | Out-Null
Start-Sleep -Milliseconds 900
if (-not $p.HasExited) { $p.Kill() }
