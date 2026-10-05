<#
 ISI IDE for Python - local build (Windows).
 Usage:  .\build.ps1 [-Rid win-x64|win-x86]
#>
param([ValidateSet('win-x64', 'win-x86')][string]$Rid = 'win-x64')

$ErrorActionPreference = 'Stop'
$Root      = $PSScriptRoot
$NativeOut = Join-Path $Root 'build-native\native-out'
$Stage     = Join-Path $Root "dist\$Rid\ISI IDE for Python"
$Arch      = if ($Rid -eq 'win-x86') { 'Win32' } else { 'x64' }

Write-Host "==> Native core ($Rid)"
cmake -S $Root -B (Join-Path $Root 'build-native') -A $Arch "-DISI_NATIVE_OUT=$NativeOut"
if ($LASTEXITCODE) { throw 'CMake configure failed' }
cmake --build (Join-Path $Root 'build-native') --config Release --parallel
if ($LASTEXITCODE) { throw 'CMake build failed' }

Write-Host "==> .NET application ($Rid)"
if (Test-Path $Stage) { Remove-Item $Stage -Recurse -Force }
dotnet publish (Join-Path $Root 'src\ISI.IDE\ISI.IDE.csproj') -c Release -r $Rid --self-contained true `
  "-p:NativeLibDir=$NativeOut" -p:DebugType=None -p:DebugSymbols=false -o $Stage
if ($LASTEXITCODE) { throw 'dotnet publish failed' }

New-Item -ItemType Directory -Force (Join-Path $Stage 'Python 3.14\bin') | Out-Null
Copy-Item (Join-Path $Root 'packaging\PUT_PYTHON_HERE.txt') (Join-Path $Stage 'Python 3.14')
Copy-Item (Join-Path $Root 'LICENSE'), (Join-Path $Root 'README.md') $Stage

Write-Host "==> Done: $Stage"
