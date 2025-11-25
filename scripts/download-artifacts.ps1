# Download NuGet packages from Azure DevOps pipeline and install them locally
# Requires: Azure CLI, .NET SDK

param(
    [string]$Organization = "dnceng",
    [string]$Project = "internal",
    [string]$DefinitionId = "345",
    [string]$ArtifactName = "PackageArtifacts",
    [string]$Branch = "main"
)

# Ensure Azure CLI is installed
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI (az) is not installed. Please install it first."
    exit 1
}

# Get latest successful build ID for the specified branch using az CLI
$buildId = az pipelines runs list --org "https://dev.azure.com/$Organization" --project "$Project" --pipeline-ids "$DefinitionId" --query "[?status=='completed' && result=='succeeded' && sourceBranch=='refs/heads/$Branch'] | [0].id" -o tsv
if (-not $buildId) {
    Write-Error "Failed to get latest successful build ID for branch '$Branch' via az CLI."
    exit 1
}
Write-Host "Latest successful build ID for branch '${Branch}': ${buildId}"

# Download artifact using REST API only (skip az CLI)
$artifactZipUrl = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/$buildId/artifacts?artifactName=PackageArtifacts&api-version=7.1&%24format=zip"
$outputZip = "./PackageArtifacts.zip"
$pat = $env:PAT
if (-not $pat) {
    Write-Error "PAT environment variable is not set. Please set the PAT variable before running the script."
    exit 1
}
$headers = @{ Authorization = "Basic $([Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$pat")))" }
Invoke-WebRequest -Uri $artifactZipUrl -OutFile $outputZip -Headers $headers
Write-Host "Artifact zip downloaded to $outputZip"
# Extract zip
$extractPath = "./PackageArtifacts"
if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
Expand-Archive -Path $outputZip -DestinationPath $extractPath
Write-Host "Artifact extracted to $extractPath"
Write-Host "Artifact downloaded to ./PackageArtifacts"

Write-Host "Installing .NET tools from NuGet packages..."
# Extract NuGet packages from PackageArtifacts
$packages = Get-ChildItem -Path "./$ArtifactName" -Recurse -Include *.nupkg
if ($packages.Count -eq 0) {
    Write-Host "No NuGet packages found in PackageArtifacts."
} else {
    Write-Host "Found NuGet packages:"
    foreach ($pkg in $packages) {
        Write-Host $pkg.FullName
    }
}

foreach ($pkg in $packages) {
    $fileName = [System.IO.Path]::GetFileNameWithoutExtension($pkg.Name)
    # Regex: everything before last 3 segments is name, last 3 segments (with optional suffix) is version
    $regex = '^(?<id>.+?)\.(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?<suffix>(-[\w\.]+)?)$'
    $match = [regex]::Match($fileName, $regex)
    if ($match.Success) {
        $packageId = $match.Groups['id'].Value
        $allowed = @('microsoft.dotnet-msidentity', 'microsoft.dotnet-scaffold', 'microsoft.dotnet-scaffold-aspire','microsoft.dotnet-scaffold-aspnet', 'dotnet-aspnet-codegenerator')
        if ($allowed -notcontains $packageId) {
            Write-Host "Skipping tool $packageId (not in allowed list)"
            continue
        }
        $version = $match.Groups['major'].Value + '.' + $match.Groups['minor'].Value + '.' + $match.Groups['patch'].Value + $match.Groups['suffix'].Value
        $srcDir = (Get-Location).Path
        $nupkgDir = $pkg.Directory.FullName
        Write-Host "Changing to $nupkgDir"
        Set-Location $nupkgDir
        Write-Host "Uninstalling tool $packageId if already installed..."
        dotnet tool uninstall -g $packageId
        Write-Host "Installing tool $packageId version $version from $nupkgDir..."
        dotnet tool install -g $packageId --add-source $nupkgDir --version $version
        Write-Host "Changing back to $srcDir"
        Set-Location $srcDir
    } else {
        Write-Host "Could not parse packageId and version from $fileName, skipping."
    }
}
Write-Host "Done. .NET tools installed globally from NuGet packages."
Write-Host "Listing globally installed .NET tools:"
dotnet tool list -g
