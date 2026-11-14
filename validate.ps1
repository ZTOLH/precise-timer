$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$root = $PSScriptRoot
$testDir = Join-Path $root 'validation'
New-Item -ItemType Directory -Force $testDir | Out-Null
$data = Join-Path $testDir 'timer-data.json'
$assembly = [Reflection.Assembly]::LoadFile((Join-Path $root 'PreciseTimer.exe'))
$type = $assembly.GetType('TimerWindow')
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
function Field($window, $name) { return $type.GetField($name,$flags).GetValue($window) }
function InvokeMethod($window,$name) { $type.GetMethod($name,$flags).Invoke($window,@($null,[EventArgs]::Empty)) | Out-Null }
function Assert($value,$message) { if (-not $value) { throw $message }; Write-Output ('PASS ' + $message) }
$first=$null
$second=$null
try {
  $first=[Activator]::CreateInstance($type,$true)
  $type.GetField('dataPath',$flags).SetValue($first,$data)
  InvokeMethod $first 'ResetTimer'
  $first.Show()
  [Windows.Forms.Application]::DoEvents()
  InvokeMethod $first 'Toggle'
  Start-Sleep -Milliseconds 160
  InvokeMethod $first 'RecordLap'
  InvokeMethod $first 'Toggle'
  $paused=(Field $first 'clock').ElapsedMilliseconds
  Assert ($paused -ge 100) 'elapsed time advances'
  Start-Sleep -Milliseconds 90
  Assert ((Field $first 'clock').ElapsedMilliseconds -eq $paused) 'paused time stays unchanged'
  InvokeMethod $first 'Toggle'
  Start-Sleep -Milliseconds 100
  InvokeMethod $first 'RecordLap'
  InvokeMethod $first 'Toggle'
  $state=Get-Content -Raw $data | ConvertFrom-Json
  Assert ($state.elapsedMilliseconds -gt $paused) 'resume adds elapsed time'
  Assert ($state.laps.Count -eq 2 -and $state.laps[0] -ge $state.laps[1]) 'two laps saved newest first'
  $second=[Activator]::CreateInstance($type,$true)
  $type.GetField('dataPath',$flags).SetValue($second,$data)
  $type.GetMethod('LoadSavedLaps',$flags).Invoke($second,@()) | Out-Null
  Assert ((Field $second 'elapsedOffset') -eq $state.elapsedMilliseconds) 'elapsed time restored after reopening'
  $list=Field $second 'lapList'
  Assert ($list.Items.Count -eq 2 -and $list.Items[0].Text -eq ([string][char]0x5708 + ' 02')) 'lap order and numbering restored'
  $second.Show()
  [Windows.Forms.Application]::DoEvents()
  $bitmap=New-Object Drawing.Bitmap($second.Width,$second.Height)
  $second.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$second.Width,$second.Height)))
  $bitmap.Save((Join-Path $testDir 'preview.png'),[Drawing.Imaging.ImageFormat]::Png)
  $bitmap.Dispose()
  InvokeMethod $second 'ResetTimer'
  $reset=Get-Content -Raw $data | ConvertFrom-Json
  Assert ($reset.elapsedMilliseconds -eq 0 -and $reset.laps.Count -eq 0) 'reset clears elapsed time and laps'
  $icon=[Drawing.Icon]::ExtractAssociatedIcon((Join-Path $root 'PreciseTimer.exe'))
  Assert ($null -ne $icon) 'executable includes icon'
  $icon.Dispose()
  Write-Output 'ALL_CHECKS_PASSED'
} finally {
  if ($null -ne $first) { $first.Dispose() }
  if ($null -ne $second) { $second.Dispose() }
}
