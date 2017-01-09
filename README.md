# **SSISBuild**
### A command line utility that builds an ispac file from an Visual Studio SSIS project (project deployment model only). 
####**Syntax**

	ssisbuild [Project File] [-<Switch Name> <Value>] [...[-<Switch Name> <Value>]] [-Parameter:<Name> <Value>] [...[-Parameter: <Name> <Value>]]

####**Switches**
Project File:

: Full path to a SSIS project file (with dtproj extension). If a project file is not specified, ssisbuild searches current working directory for a file with dtproj extension and uses that file.

-Configuration

: Required. Name of project configuration to use.

-OutputFolder:

: Full path to a folder where the ispac file will be created. If ommitted, then the ispac file will be created in the bin/&lt;Configuration&gt; subfolder of the project folder.

-ProtectionLevel:

: Overrides current project protection level. Available values are `DontSaveSensitive`, `EncryptAllWithPassword`, `EncryptSensitiveWithPassword`.

-Password:

: Password to decrypt original project data if its current protection level is either `EncryptAllWithPassword` or `EncryptSensitiveWithPassword`,  in which case the value should be supplied, otherwise build will fail.

-NewPassword:

: Password to encrypt resulting project if its resulting protection level is either `EncryptAllWithPassword` or `EncryptSensitiveWithPassword`. If ommitted, the value of the &lt;Password&gt; switch is used for encryption, unless original protection level was `DontSaveSensitive`. In this case the value should be supplied, otherwise build will fail.

-Parameter:

: Project or Package parameter. Name is a standard full parameter name including the scope. For example `Project::Parameter1`. During the build, these values will replace existing values regardless of what they were originally.

-SensitiveParameter:

: Same as `-Parameter`, but during the build this value will be set to Sensitive, and therefore will be encrypted (or not saved if the target protection level is `DontSaveSensitive`.

-ReleaseNotes:

: Path to a release notes file. Supports simple or complex release notes format, as defined [here](http://fsharp.github.io/FAKE/apidocs/fake-releasenoteshelper.html).

Example:

: `ssisbuild example.dtproj -Configuration Release -Parameter:SampleParameter "some value"`