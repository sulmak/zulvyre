# Drives the menu's LEAVE ZULVYRE? prompt with a real mouse.
# STAY and QUIT TO DESKTOP sit on top of the hero cards' invisible hit areas, so a click
# used to be swallowed by the card underneath; this checks both buttons still respond.
#
# SAFETY: the window is made topmost and forced to the foreground, and every click is
# gated on GetForegroundWindow() being the player's own window. If the player is not in
# front, the script aborts before sending any input to whatever else is on screen.

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Mouse32 {
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int max);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint attach, uint attachTo, bool on);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
    public struct RECT { public int Left, Top, Right, Bottom; }
    public struct POINT { public int X, Y; }
}
"@

$exe = "E:\ai game\ZulvyreUnity\Build\Zulvyre\Zulvyre.exe"
$dir = "E:\ai game\ZulvyreUnity\Shots"
$HWND_TOPMOST = -1
$HWND_NOTOPMOST = -2
$SWP_NOSIZE = 0x0001
$SWP_NOMOVE = 0x0002

Get-Process Zulvyre -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800
$p = Start-Process -FilePath $exe -ArgumentList "-menu" -PassThru

$h = [IntPtr]::Zero
for ($i = 0; $i -lt 40 -and $h -eq [IntPtr]::Zero; $i++) {
    Start-Sleep -Milliseconds 500
    $p.Refresh()
    if ($p.HasExited) { throw "player exited during startup" }
    $h = $p.MainWindowHandle
}
if ($h -eq [IntPtr]::Zero) { throw "no main window after 20 s" }

$saved = New-Object Mouse32+POINT
[Mouse32]::GetCursorPos([ref]$saved) | Out-Null

function ForceFront() {
    [Mouse32]::ShowWindow($h, 9) | Out-Null
    [Mouse32]::SetWindowPos($h, [IntPtr]$HWND_TOPMOST, 0, 0, 0, 0, $SWP_NOSIZE -bor $SWP_NOMOVE) | Out-Null
    for ($i = 0; $i -lt 8; $i++) {
        if ([Mouse32]::GetForegroundWindow() -eq $h) { return $true }
        $fg = [Mouse32]::GetForegroundWindow()
        $fgThread = [Mouse32]::GetWindowThreadProcessId($fg, [ref]([uint32]0))
        $me = [Mouse32]::GetCurrentThreadId()
        [Mouse32]::AttachThreadInput($me, $fgThread, $true) | Out-Null
        [Mouse32]::SetForegroundWindow($h) | Out-Null
        [Mouse32]::AttachThreadInput($me, $fgThread, $false) | Out-Null
        Start-Sleep -Milliseconds 500
    }
    return ([Mouse32]::GetForegroundWindow() -eq $h)
}

function WindowTitle([IntPtr]$hnd) {
    $sb = New-Object System.Text.StringBuilder 256
    [Mouse32]::GetWindowText($hnd, $sb, 256) | Out-Null
    return $sb.ToString()
}

if (-not (ForceFront)) {
    $fg = [Mouse32]::GetForegroundWindow()
    Write-Host ("player hwnd {0} alive={1} visible={2} title='{3}'" -f $h, [Mouse32]::IsWindow($h), [Mouse32]::IsWindowVisible($h), (WindowTitle $h))
    Write-Host ("foreground hwnd {0} title='{1}'" -f $fg, (WindowTitle $fg))
    throw "player window could not be brought to the front; no input sent"
}
Start-Sleep -Seconds 2

$rect = New-Object Mouse32+RECT
[Mouse32]::GetClientRect($h, [ref]$rect) | Out-Null
$origin = New-Object Mouse32+POINT
[Mouse32]::ClientToScreen($h, [ref]$origin) | Out-Null
$w = $rect.Right
$hh = $rect.Bottom
$scale = [Math]::Min($w / 1600.0, $hh / 900.0)
$offX = ($w - 1600 * $scale) / 2.0
$offY = ($hh - 900 * $scale) / 2.0
Write-Host ("client {0}x{1} at {2},{3}  scale {4:N3}" -f $w, $hh, $origin.X, $origin.Y, $scale)

function VirtualPoint([double]$x, [double]$y) {
    return @([int]($origin.X + $offX + $x * $scale), [int]($origin.Y + $offY + $y * $scale))
}

function AssertFront() {
    if ([Mouse32]::GetForegroundWindow() -ne $h) { throw "player lost the foreground; aborting before sending input" }
}

function TapKey([byte]$k) {
    AssertFront
    [Mouse32]::keybd_event($k, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [Mouse32]::keybd_event($k, 0, 2, [UIntPtr]::Zero)
}

function Shot([string]$name) {
    Start-Sleep -Milliseconds 400
    $bmp = New-Object System.Drawing.Bitmap $w, $hh
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($origin.X, $origin.Y, 0, 0, $bmp.Size)
    $bmp.Save((Join-Path $dir $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

function ClickAt([double]$x, [double]$y) {
    AssertFront
    $pt = VirtualPoint $x $y
    [Mouse32]::SetCursorPos($pt[0], $pt[1]) | Out-Null
    Start-Sleep -Milliseconds 250
    AssertFront
    [Mouse32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 90
    [Mouse32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 250
}

try {
    Shot "live-menu.png"

    # Escape opens the prompt; hover STAY so the highlight confirms the mapping.
    TapKey 0x1B
    Start-Sleep -Milliseconds 700
    $stay = VirtualPoint 699 503
    [Mouse32]::SetCursorPos($stay[0], $stay[1]) | Out-Null
    Start-Sleep -Milliseconds 400
    Shot "live-quit-hover-stay.png"

    ClickAt 699 503
    Start-Sleep -Milliseconds 700
    Shot "live-quit-stay.png"

    # Reopen and quit for real.
    TapKey 0x1B
    Start-Sleep -Milliseconds 700
    $quit = VirtualPoint 901 503
    [Mouse32]::SetCursorPos($quit[0], $quit[1]) | Out-Null
    Start-Sleep -Milliseconds 400
    Shot "live-quit-hover-quit.png"

    ClickAt 901 503
    Start-Sleep -Seconds 3
    $p.Refresh()
    Write-Host ("EXITED={0}" -f $p.HasExited)
}
finally {
    [Mouse32]::SetWindowPos($h, [IntPtr]$HWND_NOTOPMOST, 0, 0, 0, 0, $SWP_NOSIZE -bor $SWP_NOMOVE) | Out-Null
    [Mouse32]::SetCursorPos($saved.X, $saved.Y) | Out-Null
    $p.Refresh()
    if (-not $p.HasExited) { $p.Kill() }
}
