$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:winexe /optimize+ /codepage:65001 /win32icon:"$root\src\timer.ico" /out:"$root\PreciseTimer.exe" /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "$root\src\TimerApp.cs"
if ($LASTEXITCODE -ne 0) { throw "C# compilation failed with exit code $LASTEXITCODE" }
Write-Output 'BUILD_OK'
Get-Item "$root\PreciseTimer.exe" | Select-Object FullName,Length | Format-List
