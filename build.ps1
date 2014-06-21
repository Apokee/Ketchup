# Globals
$RootDir            = "$PSScriptRoot"
$NugetDir           = "$RootDir/.nuget"
$NugetExe           = "$NugetDir/NuGet.exe"
$PackagesConfigFile = "$NugetDir/packages.config"
$DependenciesDir    = "$RootDir/Dependencies"
$PackagesDir        = "$DependenciesDir/NuGet"
$FakeExe            = "$PackagesDir/FAKE/tools/FAKE.exe"

# Install build packages
iex "$NugetExe install `"$PackagesConfigFile`" -ExcludeVersion -OutputDirectory `"$PackagesDir`""

# Run FAKE
iex "$FakeExe $arg"
exit $LASTEXITCODE
