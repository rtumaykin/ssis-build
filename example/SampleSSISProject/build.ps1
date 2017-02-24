param (
    [Parameter(Mandatory=$true)][string]$Configuration,
    [string]$DeploymentProtectionLevel,
    [string]$Password,
    [string]$NewPassword,
    [string]$SourceDBName,
    [string]$SourceDBServer,
    [string]$ReleaseNotesFilePath
)
# Clean
Remove-Item -Path "build" -Recurse
Remove-Item -Path "packages\SSISBuild" -Recurse

# Make sure we have Nuget.exe
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

# Download SSISBuild
$nugetExe = [System.IO.Path]::Combine($Env:LOCALAPPDATA, "Nuget", "Nuget.exe")
& $nugetExe install SSISBuild -OutputDirectory "packages" -ExcludeVersion

# Import SSISBuild Modules
Import-Module .\packages\SSISBuild\tools\SsisBuild.Core.dll

New-SsisDeploymentPackage '.\SampleSSISProject\SampleSSISProject.dtproj' -ProtectionLevel $DeploymentProtectionLevel -Configuration $Configuration -Password "$Password" -NewPassword "$NewPassword" -OutputFolder ".\build" -ReleaseNotes "$ReleaseNotesFilePath" -Parameters @{"Project::SourceDBServer" = $SourceDBServer; "Project::SourceDBName" = $SourceDBName}

# Copy module to the artifacts folder so we can use it during the deployment without having to redownload it
Copy-Item .\packages\SSISBuild\tools\*.dll .\build

# Copy deployment script to the artifacts folder
Copy-Item .\deploy.ps1 .\build

