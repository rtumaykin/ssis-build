$nugetFolder = Join-Path $env:LOCALAPPDATA "Nuget"

$nugetExe = Join-Path $nugetFolder "Nuget.exe"

if (-not (Test-Path $nugetFolder)) {
    New-Item $nugetFolder -ItemType Directory
}

if (-not (Test-Path $nugetExe)) {
    $ProgressPreference = "SilentlyContinue"
    Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nugetExe
}

& $nugetExe update -self