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
