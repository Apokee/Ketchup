# Globals
$FakeVersion        = "2.18.2"
$RootDir            = "$PSScriptRoot"
$NugetDir           = "$RootDir/.nuget"
$NugetExe           = "$NugetDir/NuGet.exe"
$PackagesConfigFile = "$NugetDir/packages.config"
$DependenciesDir    = "$RootDir/Dependencies"
$PackagesDir        = "$DependenciesDir/NuGet"
$FakeExe            = "$PackagesDir/FAKE.$FakeVersion/tools/FAKE.exe"

# Install build packages
iex "$NugetExe install `"$PackagesConfigFile`" -OutputDirectory `"$PackagesDir`""

# Run FAKE
iex "$FakeExe $args"
exit $LASTEXITCODE
