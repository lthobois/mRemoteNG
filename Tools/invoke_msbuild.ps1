[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Solution,

    [Parameter(Mandatory = $true)]
    [string]$Configuration,

    [Parameter(Mandatory = $true)]
    [string]$Platform
)

$vswherePath = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuildPath = $null

if (Test-Path -LiteralPath $vswherePath) {
    $msbuildPath = & $vswherePath -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
}

if (-not $msbuildPath) {
    $msbuildPath = & "$PSScriptRoot\find_vstool.ps1" -FileName MSBuild.exe | Select-Object -First 1
}

if (-not $msbuildPath) {
    throw "MSBuild.exe introuvable. Installez Visual Studio 2022 ou Build Tools 2022 avec le composant MSBuild."
}

& $msbuildPath $Solution "-p:Configuration=$Configuration" "-p:Platform=$Platform" /verbosity:minimal
exit $LASTEXITCODE
