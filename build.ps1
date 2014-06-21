# Globals
$RootDir            = "$PSScriptRoot"
$NugetDir           = "$RootDir/.nuget"
$NugetExe           = "$NugetDir/NuGet.exe"
$PackagesConfigFile = "$NugetDir/packages.config"
$DependenciesDir    = "$RootDir/Dependencies"
$PackagesDir        = "$DependenciesDir/NuGet"
$FakeExe            = "$PackagesDir/FAKE/tools/FAKE.exe"

function Invoke-Fake {
    param([string]$Arguments)

    iex "$FakeExe $Arguments" | Out-Host
    return $LASTEXITCODE
}

function Invoke-Nuget {
    param([string]$Arguments)

    iex "$NugetExe $Arguments" | Out-Host
    return $LASTEXITCODE
}

function Install-PackagesIfNecessary {
    $nugetExitCode = (Invoke-Nuget `
        -Arguments "install `"$PackagesConfigFile`" -ExcludeVersion -OutputDirectory `"$PackagesDir`"")
}

Install-PackagesIfNecessary

$fakeExitCode = (Invoke-Fake -Arguments $args)
exit $fakeExitCode
