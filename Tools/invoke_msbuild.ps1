[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Solution,

    [Parameter(Mandatory = $true)]
    [string]$Configuration,

    [Parameter(Mandatory = $true)]
    [string]$Platform
)

function Get-SolutionConfigurationNames {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath
    )

    $lines = Get-Content -LiteralPath $SolutionPath
    $startIndex = [Array]::IndexOf($lines, "`tGlobalSection(SolutionConfigurationPlatforms) = preSolution")
    if ($startIndex -lt 0) {
        return @()
    }

    $configurations = New-Object System.Collections.Generic.List[string]
    for ($i = $startIndex + 1; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        if ($line -eq "`tEndGlobalSection") {
            break
        }

        if ($line -match "^\t\t(.+?)\s*=") {
            $configurations.Add($matches[1].Trim())
        }
    }

    return $configurations
}

function Resolve-SolutionConfiguration {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath,

        [Parameter(Mandatory = $true)]
        [string]$RequestedConfiguration,

        [Parameter(Mandatory = $true)]
        [string]$RequestedPlatform
    )

    $requestedSolutionConfig = "$RequestedConfiguration|$RequestedPlatform"
    $solutionConfigurations = Get-SolutionConfigurationNames -SolutionPath $SolutionPath
    if ($solutionConfigurations -contains $requestedSolutionConfig) {
        return $RequestedConfiguration
    }

    $escapedConfig = [regex]::Escape($RequestedConfiguration)
    $escapedPlatform = [regex]::Escape($RequestedPlatform)
    $mappingPattern = "^\t\t\{[^}]+\}\.(.+?)\|$escapedPlatform\.(?:ActiveCfg|Build\.0)\s*=\s*$escapedConfig\|$escapedPlatform$"
    $mappedConfigurations = @(Get-Content -LiteralPath $SolutionPath |
        Select-String -Pattern $mappingPattern |
        ForEach-Object { $_.Matches[0].Groups[1].Value.Trim() } |
        Sort-Object -Unique)

    if ($mappedConfigurations.Count -eq 1) {
        return $mappedConfigurations[0]
    }

    $validConfigurations = $solutionConfigurations |
        Where-Object { $_ -like "*|$RequestedPlatform" } |
        ForEach-Object { ($_ -split '\|', 2)[0] } |
        Sort-Object -Unique

    $validMessage = if ($validConfigurations) {
        "Configurations valides pour la plateforme '$RequestedPlatform' : $($validConfigurations -join ', ')."
    }
    else {
        "Aucune configuration de solution valide n'a ete trouvee pour la plateforme '$RequestedPlatform'."
    }

    throw "La configuration de solution '$requestedSolutionConfig' est invalide. $validMessage"
}

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

$resolvedConfiguration = Resolve-SolutionConfiguration -SolutionPath $Solution -RequestedConfiguration $Configuration -RequestedPlatform $Platform

$process = Start-Process -FilePath $msbuildPath `
    -ArgumentList @(
        $Solution,
        "/p:Configuration=""$resolvedConfiguration""",
        "/p:Platform=""$Platform""",
        "/verbosity:minimal"
    ) `
    -NoNewWindow `
    -Wait `
    -PassThru

exit $process.ExitCode
