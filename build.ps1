# Configuration
$Package    = "FAKE"
$Version    = "2.18.2"

# Globals
$ScriptPath			= "$PSScriptRoot"
$PackagesPath       = "$ScriptPath\Dependencies\NuGet"
$FakePackagePath    = "$PackagesPath\$Package"
$VersionFile        = "$FakePackagePath\VERSION"
$FakeExe            = "$FakePackagePath\tools\FAKE.exe"
$NugetExe           = "$ScriptPath\.nuget\NuGet.exe"

function Get-InstalledFakeVersion {
    if (Test-Path $VersionFile) {
        return Get-Content $VersionFile
    } else {
        return $null
    }
}

function Install-Fake {
    iex "$NugetExe install $Package -Version $Version -OutputDirectory $PackagesPath -ExcludeVersion"
    echo $Version > $VersionFile
}

function Install-FakeIfNecessary {
    $installedVersion = (Get-InstalledFakeVersion)

    if ($installedVersion -ne $Version) {
        if ($installedVersion -ne $null) {
            Write-Host -ForegroundColor Green "Replacing $Package $installedVersion with $Version..."
        }

        if (Test-Path $FakePackagePath) {
            Remove-Item -Recurse -Force $FakePackagePath
        }

        Install-Fake
    }
}

Install-FakeIfNecessary

iex "$FakeExe $args"
exit $LASTEXITCODE
