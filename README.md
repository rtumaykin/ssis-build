# **SSISBuild**
### A command line utility that builds an ispac file from a Visual Studio SSIS project (project deployment model only). 
####**Syntax**

`ssisbuild [Project File] [-<Switch Name> <Value>] [...[-<Switch Name> <Value>]] [-Parameter:<Name> <Value>] [...[-Parameter: <Name> <Value>]]`

####**Switches**
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
Password to encrypt resulting project if its resulting protection level is either `EncryptAllWithPassword` or `EncryptSensitiveWithPassword`. If ommitted, the value of the **-Password** switch is used for encryption, unless original protection level was `DontSaveSensitive`. In this case the value should be supplied, otherwise build will fail.

- **-Parameter:**
Project or Package parameter. Name is a standard full parameter name including the scope. For example `Project::Parameter1`. During the build, these values will replace existing values regardless of what they were originally.

- **-ReleaseNotes:**
Path to a release notes file. Supports simple or complex release notes format, as defined [here](http://fsharp.github.io/FAKE/apidocs/fake-releasenoteshelper.html).

#### Example:
`ssisbuild example.dtproj -Configuration Release -Parameter:SampleParameter "some value"`

# **SSISDeploy**
### A command line utility that deploys an ispac file to an SSIS catalog. 
####**Syntax**

`ssisdeploy [Ispac File] -ServerInstance <ServerInstanceName> -Catalog <CatalogName> -Folder <FolderName> -ProjectName <ProjectName> [-ProjectPassword <ProjectPassword>] [-EraseSensitiveInfo]`

####**Switches**
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

#### Example:
`ssisdeploy sample.ispac -ServerInstance dbserver\\instance -Catalog SSISDB -Folder SampleFolder -ProjectName Sample -ProjectPassword xyz -EraseSensitiveInfo`