# Globals
$FakeVersion        = "2.18.2"
$RootDir            = "$PSScriptRoot"
$NugetDir           = "$RootDir/.nuget"
$NugetExe           = "$NugetDir/NuGet.exe"
$PackagesConfigFile = "$NugetDir/packages.config"
$DependenciesDir    = "$RootDir/Dependencies"
$PackagesDir        = "$DependenciesDir/NuGet"
$FakeExe            = "$PackagesDir/FAKE.$FakeVersion/tools/FAKE.exe"

function Invoke-NugetInstall {
    Param(
        [Parameter(Mandatory=$true,Position=1)]
        [string]$PackageOrConfig,

        [string]$Version,
        [string]$OutputDirectory,

        [Switch]
        $ExcludeVersion
    )

    $arguments = @()
    $arguments += "install"
    $arguments += $PackageOrConfig
    if ($Version) {
        $arguments += "-Version $Version"
    }    
    if ($OutputDirectory) {
        $arguments += "-OutputDirectory $OutputDirectory"
    }
    if ($ExcludeVersion) {
        $arguments += "-ExcludeVersion"
    }

    $nuget = start -PassThru -Wait -NoNewWindow $NugetExe $arguments
    
    return $nuget.ExitCode
}

# Install solution packages
$exitCode = (Invoke-NugetInstall $PackagesConfigFile -OutputDirectory $PackagesDir)
if ($exitCode -ne 0) {
    throw "Installing solution packages failed"
}

# Run FAKE
if ($args) {
    $fake = start -PassThru -Wait -NoNewWindow $FakeExe $args
    exit $fake.ExitCode
} else {
    $fake = start -PassThru -Wait -NoNewWindow $FakeExe
    exit $fake.ExitCode
}
