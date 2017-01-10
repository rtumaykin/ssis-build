
# **Example of SSISBuild usage**
Open commend prompt in the root solution folder and execute the following command:
`powershell -Command ".\build.ps1" -SSISInstanceName '<Your Server Name>' -SSISCatalog 'SSISDB' -SSISDeploymentFolder '<Your Deployment Folder Here>' -SSISProjectName 'SampleSSISProject' -SourceDBName model -SourceDBServer '<Your Server Name>' -ReleaseNotes release_notes.md -Password ssis123`

You will need to make sure that the latest SSDT (SQL Server Data Tools) are installed. If not please download and install the tools from [https://msdn.microsoft.com/en-us/mt186501](https://msdn.microsoft.com/en-us/mt186501). Once finished, copy the contents of build folder from the solution folder to the server you want to deploy this to (`'<Your Server Name>'`) and execute deploy.cmd there. Of course, the Integration Services must be installed on the destination server, as well as Integration Services Catalog must be created prior to deployment.