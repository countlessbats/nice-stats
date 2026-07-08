<#
.SYNOPSIS
    Builds the Nice Stats sidecar assembly.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameDir,

    [string]$OutputDir,

    [string]$Csc
)

$ErrorActionPreference = 'Stop'

$managed = Join-Path $GameDir 'PillarsOfEternity_Data\Managed'
if (-not (Test-Path $managed)) {
    throw "Managed folder not found: $managed  (is -GameDir correct?)"
}

if (-not $Csc) {
    $candidates = @(
        'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
    )
    $Csc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $Csc) {
        $cmd = Get-Command csc.exe -ErrorAction SilentlyContinue
        if ($cmd) { $Csc = $cmd.Source }
    }
}
if (-not $Csc -or -not (Test-Path $Csc)) {
    throw "Could not locate csc.exe. Pass it explicitly with -Csc."
}

if (-not $OutputDir) { $OutputDir = $managed }
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$src    = Join-Path $PSScriptRoot 'src\NiceStats.cs'
$outDll = Join-Path $OutputDir 'LoomNiceStats.dll'

$refs = @(
    'Assembly-CSharp.dll',
    'UnityEngine.dll',
    'UnityEngine.CoreModule.dll'
) | ForEach-Object { "/reference:$(Join-Path $managed $_)" }

Write-Host "Compiler : $Csc"
Write-Host "Source   : $src"
Write-Host "Output   : $outDll"

$argList = @('/nologo', '/target:library', "/out:$outDll") + $refs + @($src)
& $Csc @argList
if ($LASTEXITCODE -ne 0) { throw "Compilation failed ($LASTEXITCODE)." }

Write-Host "`nBuilt LoomNiceStats.dll." -ForegroundColor Green
