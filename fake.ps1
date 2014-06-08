# Configuration
$Package    = "FAKE"
$Version    = "2.17.9"

# Globals
$PackagesPath       = "$PSScriptRoot\packages"
$FakePackagePath    = "$PackagesPath\$Package"
$VersionFile        = "$FakePackagePath\VERSION"
$FakeExe            = "$FakePackagePath\tools\Fake.exe"
$NugetExe           = "$PSScriptRoot\.nuget\nuget.exe"

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
            if ([version]$installedVersion -lt [version]$Version) {
                $verb = "Upgrading"
                $color = "Green"
            } else {
                $verb = "Downgrading"
                $color = "Yellow"
            }

            Write-Host -ForegroundColor $color "$verb $Package $installedVersion to $Version..."            
        }

        if (Test-Path $FakePackagePath) {
            Remove-Item -Recurse -Force $FakePackagePath
        }

        Install-Fake $Version
        Write-Host
    }
}

Install-FakeIfNecessary

iex "$FakeExe $args"
