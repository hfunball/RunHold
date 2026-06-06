param(
    [string]$ExecutablePath = (Join-Path $PSScriptRoot '..\src\KeyHold\bin\Debug\net10.0-windows\KeyHold.exe')
)

$ErrorActionPreference = 'Stop'

function Wait-For {
    param(
        [scriptblock]$Probe,
        [string]$Description,
        [int]$TimeoutSeconds = 10
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $result = & $Probe
        if ($null -ne $result -and $false -ne $result) {
            return $result
        }

        Start-Sleep -Milliseconds 150
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $Description."
}

function Find-ByName {
    param([string]$Name)

    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $condition = New-Object System.Windows.Automation.PropertyCondition `
        -ArgumentList ([System.Windows.Automation.AutomationElement]::NameProperty), $Name
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $condition)
}

if (!(Test-Path -LiteralPath $ExecutablePath)) {
    throw "KeyHold executable was not found at $ExecutablePath. Run the build first."
}

$existing = Get-Process -Name KeyHold -ErrorAction SilentlyContinue
if ($existing) {
    throw 'Close any running KeyHold process before running this smoke test.'
}

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class KeyHoldSmokeInput
{
    private const uint KeyEventFKeyUp = 0x0002;

    public static void KeyDown(ushort virtualKey)
    {
        Send((byte)virtualKey, 0);
    }

    public static void KeyUp(ushort virtualKey)
    {
        Send((byte)virtualKey, KeyEventFKeyUp);
    }

    private static void Send(byte virtualKey, uint flags)
    {
        keybd_event(virtualKey, 0, flags, UIntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);
}
'@

Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class KeyHoldSmokeWindow
{
    private const uint MouseEventFLeftDown = 0x0002;
    private const uint MouseEventFLeftUp = 0x0004;

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    public static void Click(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(MouseEventFLeftDown, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MouseEventFLeftUp, 0, 0, 0, UIntPtr.Zero);
    }
}
'@

$settingsPath = Join-Path $env:LOCALAPPDATA 'KeyHold\settings.json'
$settingsFolder = Split-Path -Parent $settingsPath
$hadSettings = Test-Path -LiteralPath $settingsPath
$originalSettings = if ($hadSettings) { Get-Content -LiteralPath $settingsPath -Raw } else { $null }
$previousSmokeFlag = [Environment]::GetEnvironmentVariable('KEYHOLD_ACCEPT_EXTERNAL_INJECTED_INPUT_FOR_SMOKE', 'Process')
$keyHoldProcess = $null
$targetProcess = $null
$tempFolder = Join-Path $env:TEMP ('KeyHoldSmoke-' + [guid]::NewGuid().ToString('N'))
$targetScript = Join-Path $tempFolder 'text-target.ps1'
$targetOutput = Join-Path $tempFolder 'text.txt'
$targetStop = Join-Path $tempFolder 'stop.txt'

try {
    New-Item -ItemType Directory -Path $settingsFolder -Force | Out-Null
    New-Item -ItemType Directory -Path $tempFolder -Force | Out-Null
    @'
param(
    [string]$OutputPath,
    [string]$StopPath
)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$form = New-Object System.Windows.Forms.Form
$form.Text = 'KeyHold Smoke Text Target'
$form.Width = 640
$form.Height = 360
$form.StartPosition = 'CenterScreen'
$form.TopMost = $true

$textBox = New-Object System.Windows.Forms.TextBox
$textBox.Multiline = $true
$textBox.Dock = 'Fill'
$textBox.Font = New-Object System.Drawing.Font('Consolas', 18)
$textBox.AcceptsReturn = $true
$textBox.AcceptsTab = $true
$form.Controls.Add($textBox)

$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 100
$timer.Add_Tick({
    [System.IO.File]::WriteAllText($OutputPath, $textBox.Text)
    if (Test-Path -LiteralPath $StopPath) {
        $timer.Stop()
        $form.Close()
    }
})
$timer.Start()

$form.Add_Shown({
    $form.Activate()
    $form.TopMost = $true
    $textBox.Focus()
    [System.IO.File]::WriteAllText($OutputPath, $textBox.Text)
})
$form.Add_Activated({
    $form.TopMost = $true
    $textBox.Focus()
})

[System.Windows.Forms.Application]::Run($form)
[System.IO.File]::WriteAllText($OutputPath, $textBox.Text)
'@ | Set-Content -LiteralPath $targetScript -Encoding UTF8

    $testSettings = @{
        ActivationMode = 0
        EnableBinding = @{ Device = 0; Code = 33; DisplayName = 'Page Up' }
        StopBinding = @{ Device = 0; Code = 34; DisplayName = 'Page Down' }
        EmergencyBinding = @{ Device = 0; Code = 123; DisplayName = 'F12' }
        MouseTrigger = @{ Device = 1; Code = 1; DisplayName = 'Mouse Button 4' }
        KeyEmulationMode = 1
        RepeatedPressIntervalMilliseconds = 45
        Theme = 0
        LaunchToTray = $false
        ShowNotifications = $false
        SuppressTriggerInput = $true
        HasSeenFirstRun = $true
    } | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText($settingsPath, $testSettings)

    [Environment]::SetEnvironmentVariable('KEYHOLD_ACCEPT_EXTERNAL_INJECTED_INPUT_FOR_SMOKE', '1', 'Process')
    $keyHoldStartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $keyHoldStartInfo.FileName = $ExecutablePath
    $keyHoldStartInfo.UseShellExecute = $false
    $keyHoldStartInfo.EnvironmentVariables['KEYHOLD_ACCEPT_EXTERNAL_INJECTED_INPUT_FOR_SMOKE'] = '1'
    $keyHoldProcess = [System.Diagnostics.Process]::Start($keyHoldStartInfo)
    Wait-For { Get-Process -Id $keyHoldProcess.Id -ErrorAction SilentlyContinue } 'KeyHold process' | Out-Null
    Start-Sleep -Seconds 1

    $targetProcess = Start-Process -FilePath powershell.exe `
        -ArgumentList @('-NoProfile', '-STA', '-ExecutionPolicy', 'Bypass', '-File', $targetScript, $targetOutput, $targetStop) `
        -PassThru
    $targetWindow = Wait-For { Find-ByName -Name 'KeyHold Smoke Text Target' } 'text target window'
    [KeyHoldSmokeWindow]::SetForegroundWindow([IntPtr]$targetWindow.Current.NativeWindowHandle) | Out-Null
    $rect = $targetWindow.Current.BoundingRectangle
    [KeyHoldSmokeWindow]::Click([int]($rect.Left + ($rect.Width / 2)), [int]($rect.Top + ($rect.Height / 2)))
    Start-Sleep -Milliseconds 300
    [System.Windows.Forms.SendKeys]::SendWait('z')
    Wait-For {
        if (!(Test-Path -LiteralPath $targetOutput)) {
            return $null
        }

        $currentText = Get-Content -LiteralPath $targetOutput -Raw
        if ($currentText -like '*z*') {
            return $true
        }

        return $null
    } 'text target focus sanity check' | Out-Null
    [KeyHoldSmokeInput]::KeyDown(0x58)
    Start-Sleep -Milliseconds 40
    [KeyHoldSmokeInput]::KeyUp(0x58)
    Wait-For {
        $currentText = Get-Content -LiteralPath $targetOutput -Raw
        if ($currentText -like '*x*') {
            return $true
        }

        return $null
    } 'low-level input sanity check' | Out-Null

    [KeyHoldSmokeInput]::KeyDown(0x57)
    Start-Sleep -Milliseconds 150
    [KeyHoldSmokeInput]::KeyDown(0x21)
    Start-Sleep -Milliseconds 40
    [KeyHoldSmokeInput]::KeyUp(0x21)
    Start-Sleep -Milliseconds 150
    [KeyHoldSmokeInput]::KeyUp(0x57)
    Start-Sleep -Milliseconds 800
    [KeyHoldSmokeInput]::KeyDown(0x22)
    Start-Sleep -Milliseconds 40
    [KeyHoldSmokeInput]::KeyUp(0x22)
    Start-Sleep -Milliseconds 300

    $text = if (Test-Path -LiteralPath $targetOutput) { Get-Content -LiteralPath $targetOutput -Raw } else { '' }
    if ($null -eq $text) {
        $text = ''
    }
    $wCount = ([regex]::Matches($text, 'w', 'IgnoreCase')).Count

    if ($wCount -lt 8) {
        throw "Text target hold smoke failed. Expected repeated W output, but found only $wCount W characters. Text was: '$text'"
    }

    'KeyHold repeated-press text-target smoke passed: W repeated after enable and stopped after stop.'
}
finally {
    [Environment]::SetEnvironmentVariable('KEYHOLD_ACCEPT_EXTERNAL_INJECTED_INPUT_FOR_SMOKE', $previousSmokeFlag, 'Process')

    if ($targetStop) {
        New-Item -ItemType File -Path $targetStop -Force -ErrorAction SilentlyContinue | Out-Null
    }

    Start-Sleep -Milliseconds 200

    if ($targetProcess -and -not $targetProcess.HasExited) {
        Stop-Process -Id $targetProcess.Id -Force
    }

    if ($keyHoldProcess -and -not $keyHoldProcess.HasExited) {
        Stop-Process -Id $keyHoldProcess.Id -Force
    }

    if ($hadSettings) {
        [System.IO.File]::WriteAllText($settingsPath, $originalSettings)
    }
    else {
        Remove-Item -LiteralPath $settingsPath -Force -ErrorAction SilentlyContinue
    }

    Remove-Item -LiteralPath $tempFolder -Recurse -Force -ErrorAction SilentlyContinue
}
