#**Building this sample SSIS deployment package with SSISBuild**
Open command prompt in the root solution folder and execute the following command:
`powershell -Command ".\build.ps1" -SourceDBName model -SourceDBServer '<Your Server Name>' -ReleaseNotes release_notes.md -Password ssis123 -Configuration Deployment`

#**Deploying this sample SSIS Package with SSISBuild**
Open command prompt in the artifacts folder (.\build\ in the previous example) and execute the following command:
`powershell -Command .\deploy.ps1 -SSISInstanceName <Your SSIS Server Full Instance Name> -SSISCatalog <Target SSIS Catalog> -SSISDeploymentFolder <Target Folder> -SSISProjectName <Target Project Name> -Password <Password To Decrypt Package Contents>`