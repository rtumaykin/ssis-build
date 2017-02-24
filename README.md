
# **SSISBuild**
A set of utilities that allow to autonomously build a Visual Studio SSIS project (dtproj) into a deployment package (ispac), and deploy the package to an SSIS catalog. Project deployment model only. This set is distributed via a nuget package and can be dewnloaded locally and used from and build server environment through a Windows batch file or a Windows Powershell script. Utilities do not use any Microsoft SSIS or Visual Studio components, so there is no additional installation is needed on the build server.
## **ssisbuild.exe** 
Command line utility that builds a deployment package from a Visual Studio Project File
####**Syntax:**

`ssisbuild [Project File] -Configuration <Value> [-OutputFolder <Value>] [-ProtectionLevel <Value>] [-Password <Value>] [-NewPassword <Value>] [-ReleaseNotes <Value>] [-Parameter:<Name> <Value>] [...[-Parameter:<Name> <Value>]]`

####**Switches:**
- **Project File:**
Full path to a SSIS project file (with dtproj extension). If a project file is not specified, ssisbuild searches current working directory for a file with dtproj extension and uses that file.

- **-Configuration:**
Required. Name of project configuration to use.

- **-OutputFolder:**
Full path to a folder where the ispac file will be created. If ommitted, then the ispac file will be created in the bin/&lt;Configuration&gt; subfolder of the project folder.

- **-ProtectionLevel:**
Overrides current project protection level. Available values are `DontSaveSensitive`, `EncryptAllWithPassword`, `EncryptSensitiveWithPassword`.

- **-Password:**
Password to decrypt original project data if its current protection level is either `EncryptAllWithPassword` or `EncryptSensitiveWithPassword`,  in which case the value should be supplied, otherwise build will fail.

- **-NewPassword:**
Password to encrypt resulting deployment packageif its resulting protection level is either `EncryptAllWithPassword` or `EncryptSensitiveWithPassword`. If ommitted, the value of the **-Password** switch is used for encryption, unless original protection level was `DontSaveSensitive`. In this case the value should be supplied, otherwise build will fail.

- **-Parameter:**
Project or Package parameter. Name is a standard full parameter name including the scope. For example `Project::Parameter1`. During the build, these values will replace existing values regardless of what they were originally.

- **-ReleaseNotes:**
Path to a release notes file. Supports simple or complex release notes format, as defined [here](http://fsharp.github.io/FAKE/apidocs/fake-releasenoteshelper.html).

#### **Example:**
`ssisbuild.exe sample.dtproj -Configuration Release -Parameter:SampleParameter "some value"`

## **New-SsisDeploymentPackage**
A PowerShell Cmdlet that builds a deployment package from a Visual Studio Project File
####**Syntax:**

`New-SsisDeploymentPackage [[-ProjectPath] <string>] -Configuration <string> [-OutputFolder <string>] [-ProtectionLevel <string>] [-Password <string>] [-NewPassword <string>] [-ReleaseNotes <string>] [-Parameters <hashtable>]  [<CommonParameters>]`

####**Example:**

`New-SsisDeploymentPackage sample.dtproj -Configuration Release -Parameters @{"SampleParameter" = "some value"}`

## **ssisdeploy.exe**
A command line utility that deploys an SSIS deployment package to an SSIS catalog. 
####**Syntax:**

`ssisdeploy [Ispac File] -ServerInstance <ServerInstanceName> -Catalog <CatalogName> -Folder <FolderName> -ProjectName <ProjectName> [-ProjectPassword <ProjectPassword>] [-EraseSensitiveInfo]`

####**Switches:**
- **Ispac File:**
Full path to an SSIS deployment file (with ispac extension). If a deployment file is not specified, ssisdeploy searches current working directory for a file with ispac extension and uses that file.

- **-ServerInstance:**
Required. Full Name of the target SQL Server instance.

- **-Catalog:**
 Required. Name of the SSIS Catalog on the target server.

- **-Folder:**
Required. Deployment folder within destination catalog..

- **-ProjectName:**
Required. Name of the project in the destination folder.

- **-ProjectPassword:**
Password to decrypt sensitive data for deployment.

- **-EraseSensitiveInfo:**
Option to remove all sensitive info from the deployment ispac and deploy all sensitive parameters separately. If not specified then sensitive data will not be removed.

####**Example:**
`ssisdeploy.exe sample.ispac -ServerInstance dbserver\\instance -Catalog SSISDB -Folder SampleFolder -ProjectName Sample -ProjectPassword xyz -EraseSensitiveInfo`

##**Publish-SsisDeploymentPackage**
A PowerShell Cmdlet that that deploys an SSIS deployment package to an SSIS catalog.
####**Syntax:**

`Publish-SsisDeploymentPackage [[-DeploymentFilePath] <string>] -ServerInstance <string> -Catalog <string> -Folder <string> -ProjectName <string> [-ProjectPassword <string>] [-EraseSensitiveInfo <bool>]  [<CommonParameters>]`

####**Example:**

`Publish-SsisDeploymentPackage sample.ispac -ServerInstance sql01 -Catalog SSISDB -Folder SomeFolder -ProjectName SampleSSISProject -ProjectPassword ssis1234`

##**Sample Build Powershell Script**

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
    
    New-SsisDeploymentPackage .\SampleSSISProject\SampleSSISProject.dtproj -ProtectionLevel $DeploymentProtectionLevel -Configuration $Configuration -Password $Password -NewPassword $NewPassword -OutputFolder .\build -ReleaseNotes $ReleaseNotesFilePath -Parameters @{"Project::SourceDBServer" = $SourceDBServer; "Project::SourceDBName" = $SourceDBName}
    
    # Copy module to the artifacts folder so we can use it during the deployment without having to redownload it
    Copy-Item .\packages\SSISBuild\tools\*.dll .\build
    
    # Add deployment script to the build artifacts
	Copy-Item .\deploy.ps1 .\build

##**Sample Deployment Powershell Script**
	param (
	    [Parameter(Mandatory=$true)][string]$SSISInstanceName,
	    [Parameter(Mandatory=$true)][string]$SSISCatalog,
	    [Parameter(Mandatory=$true)][string]$SSISDeploymentFolder,
	    [Parameter(Mandatory=$true)][string]$SSISProjectName,
	    [Parameter(Mandatory=$true)][string]$Password
	)
	
	# Import SSISBuild Modules
	Import-Module .\SsisBuild.Core.dll
	
	# Publish 
	Publish-SsisDeploymentPackage -ServerInstance $SSISInstanceName -Catalog $SSISCatalog -Folder $SSISDeploymentFolder -ProjectName $SSISProjectName -ProjectPassword $Password
