param (
    [Parameter(Mandatory=$true)][string]$SSISInstanceName,
    [Parameter(Mandatory=$true)][string]$SSISCatalog,
    [Parameter(Mandatory=$true)][string]$SSISDeploymentFolder,
    [Parameter(Mandatory=$true)][string]$SSISProjectName,
    [Parameter(Mandatory=$true)][string]$Password,
    [string]$SourceDBName,
    [string]$SourceDBServer,
    [string]$ReleaseNotesFilePath
)
# Clean
Remove-Item -Path "build" -Recurse
Remove-Item -Path "packages\SSISBuild" -Recurse

# Make sure we have Nuget.exe
.\ensureNuget.ps1

# Download SSISBuild
$nugetExe = [System.IO.Path]::Combine($Env:LOCALAPPDATA, "Nuget", "Nuget.exe")
& $nugetExe install SSISBuild -OutputDirectory "packages" -ExcludeVersion



packages\SSISBuild\tools\ssisbuild 'SampleSSISProject\SampleSSISProject.dtproj' -Configuration Deployment -Password "$Password" -OutputFolder "build" -ReleaseNotes "$ReleaseNotesFilePath" "-Parameter:Project::SourceDBServer" "$SourceDBServer" "-Parameter:Project::SourceDBName" "$SourceDBName"

# Copy deploy.ps1 to the artifacts folder

Copy-Item ".\deploy.ps1" "build\"

# Escape parenthesis in variables

# Now create deploy.cmd file with all proper parameters
$cmd = "@echo off
pushd %~dp0
powershell -Command "".\deploy.ps1"" -SSISInstanceName `'$SSISInstanceName`' -SSISCatalog `'$SSISCatalog`' -SSISDeploymentFolder `'$SSISDeploymentFolder`' -SSISProjectName `'$SSISProjectName`'
popd"
Set-Content -Path build\deploy.cmd -Value $cmd