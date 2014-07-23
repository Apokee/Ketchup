# Globals
$RootDir            = "$PSScriptRoot"
$NugetDir           = "$RootDir/.nuget"
$NugetExe           = "$NugetDir/NuGet.exe"
$PackagesConfigFile = "$NugetDir/packages.config"
$DependenciesDir    = "$RootDir/Dependencies"
$PackagesDir        = "$DependenciesDir/NuGet"
$FakeVersionXPath   = "//package[@id='FAKE'][1]/@version"
$FakeVersion        = (Select-Xml -Xml ([xml](Get-Content $PackagesConfigFile)) -XPath $FakeVersionXPath).Node.Value
$FakeExe            = "$PackagesDir/FAKE.$FakeVersion/tools/FAKE.exe"

# Install build packages
iex "$NugetExe install `"$PackagesConfigFile`" -OutputDirectory `"$PackagesDir`""

# Run FAKE
iex "$FakeExe $args"
exit $LASTEXITCODE
