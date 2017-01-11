param (
    [Parameter(Mandatory=$true)][string]$SSISInstanceName,
    [Parameter(Mandatory=$true)][string]$SSISCatalog,
    [Parameter(Mandatory=$true)][string]$SSISDeploymentFolder,
    [Parameter(Mandatory=$true)][string]$SSISProjectName
)

# Load the IntegrationServices Assembly
[Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.Management.IntegrationServices")

# Store the IntegrationServices Assembly namespace to avoid typing it every time
$ISNamespace = "Microsoft.SqlServer.Management.IntegrationServices"

Write-Host "Connecting to server ..."

# Create a connection to the server
$sqlConnectionString = "Data Source=$SSISInstanceName;Initial Catalog=master;Integrated Security=SSPI;"
$sqlConnection = New-Object System.Data.SqlClient.SqlConnection $sqlConnectionString

$integrationServices = New-Object "$ISNamespace.IntegrationServices" $sqlConnection

$catalog = $integrationServices.Catalogs[$SSISCatalog]

if (-not ($catalog.Folders.Contains($SSISDeploymentFolder)))
{
    #Create a folder in SSISDB
    Write-Host "Creating Folder ..."
    $folder = New-Object "$ISNamespace.CatalogFolder" ($catalog, $SSISDeploymentFolder, $SSISDeploymentFolder)            
    $folder.Create()  
}

$folder = $catalog.Folders[$SSISDeploymentFolder]

# Read the project file, and deploy it to the folder
Write-Host "Deploying Project ..."
[byte[]] $projectFile = [System.IO.File]::ReadAllBytes(".\$SSISProjectName.ispac")
$folder.DeployProject($SSISProjectName, $projectFile)