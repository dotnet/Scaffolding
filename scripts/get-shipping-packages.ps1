<#
.SYNOPSIS
    Gets the latest scaffolding builds from multiple channels and gathers their drops.

.DESCRIPTION
    This script queries the latest builds for the scaffolding repository across multiple
    .NET channels, then downloads the build drops to a specified output directory.
    Alternatively, you can specify build IDs directly to skip channel queries.
    
    REQUIREMENTS: This script requires the Darc CLI tool to be installed and available in your PATH.
    For installation instructions, see: https://github.com/dotnet/arcade-services/blob/main/docs/Darc.md

.PARAMETER Channels
    Comma-delimited list of .NET channels to query (e.g., ".NET 10,.NET 9,.NET 8")
    Either Channels or Ids must be specified, but not both.

.PARAMETER Ids
    Comma-delimited list of build IDs to gather drops for directly (e.g., "292781,292886,292890")
    Either Channels or Ids must be specified, but not both.

.PARAMETER OutputDir
    Directory where build drops will be downloaded

.PARAMETER IncludeMSIdentity
    Include Microsoft.dotnet-msidentity packages in the output (excluded by default)

.PARAMETER IncludeExtensibility
    Include Microsoft.DotNet.Scaffolding.* extensibility packages (excluded by default, except Microsoft.DotNet.Scaffolding.Shared)

.EXAMPLE
    .\get-shipping-packages.ps1 -Channels ".NET 10,.NET 9,.NET 8" -OutputDir "d:\drop"
    
    Typical use case: Downloads the latest builds for .NET 10, 9, and 8, excluding MSIdentity and extensibility packages by default.

.EXAMPLE
    .\get-shipping-packages.ps1 -Channels ".NET 10" -OutputDir "c:\temp\drops" -IncludeMSIdentity -IncludeExtensibility
    
    Downloads the latest .NET 10 build including all MSIdentity and extensibility packages.

.EXAMPLE
    .\get-shipping-packages.ps1 -Ids "292781,292886,292890" -OutputDir "d:\drop"
    
    Downloads specific builds by their IDs.
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$Channels,
    
    [Parameter(Mandatory=$false)]
    [string]$Ids,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputDir,
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeMSIdentity,
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeExtensibility
)

# Validate that either Channels or Ids is specified, but not both
if ([string]::IsNullOrWhiteSpace($Channels) -and [string]::IsNullOrWhiteSpace($Ids)) {
    Write-Host "ERROR: Either -Channels or -Ids must be specified" -ForegroundColor Red
    exit 1
}

if (![string]::IsNullOrWhiteSpace($Channels) -and ![string]::IsNullOrWhiteSpace($Ids)) {
    Write-Host "ERROR: Cannot specify both -Channels and -Ids" -ForegroundColor Red
    exit 1
}

# Ensure output directory exists
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "Created output directory: $OutputDir"
}

# Determine mode: Channels or Ids
$buildsToProcess = @()

if (![string]::IsNullOrWhiteSpace($Channels)) {
    # Channel mode: Query latest builds
    $channelList = $Channels -split ',' | ForEach-Object { $_.Trim() }
    
    Write-Host "Processing $($channelList.Count) channel(s)..." -ForegroundColor Cyan
    Write-Host ""
    
    foreach ($channel in $channelList) {
        Write-Host "========================================" -ForegroundColor Yellow
        Write-Host "Processing channel: $channel" -ForegroundColor Yellow
        Write-Host "========================================" -ForegroundColor Yellow
        
        try {
            # Get latest build for this channel
            Write-Host "Querying latest build for '$channel'..." -ForegroundColor Gray
            $jsonOutput = darc get-latest-build --repo scaffolding --channel $channel --output-format json 2>&1
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "ERROR: Failed to get latest build for channel '$channel'" -ForegroundColor Red
                Write-Host "Error output: $jsonOutput" -ForegroundColor Red
                continue
            }
            
            # Parse JSON output
            $buildInfo = $jsonOutput | ConvertFrom-Json
            
            if ($buildInfo -and $buildInfo.Count -gt 0) {
                $build = $buildInfo[0]
                $buildsToProcess += @{
                    Id = $build.id
                    BuildNumber = $build.buildNumber
                    Commit = $build.commit
                    DateProduced = $build.dateProduced
                    Source = "Channel: $channel"
                }
                
                Write-Host "Found build:" -ForegroundColor Green
                Write-Host "  Build ID: $($build.id)" -ForegroundColor White
                Write-Host "  Build Number: $($build.buildNumber)" -ForegroundColor White
                Write-Host "  Commit: $($build.commit)" -ForegroundColor White
                Write-Host "  Date: $($build.dateProduced)" -ForegroundColor White
            } else {
                Write-Host "WARNING: No builds found for channel '$channel'" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "ERROR: Exception occurred while processing channel '$channel'" -ForegroundColor Red
            Write-Host "Exception: $_" -ForegroundColor Red
        }
        
        Write-Host ""
    }
} else {
    # ID mode: Use specified build IDs directly
    $idList = $Ids -split ',' | ForEach-Object { $_.Trim() }
    
    Write-Host "Processing $($idList.Count) build ID(s)..." -ForegroundColor Cyan
    Write-Host ""
    
    # Validate each build ID
    $expectedRepo = "https://github.com/dotnet/scaffolding"
    $branchesSeen = @{}
    
    foreach ($id in $idList) {
        Write-Host "Validating build ID: $id..." -ForegroundColor Gray
        
        # Query build information
        $jsonOutput = darc get-build --id $id --output-format json 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Failed to retrieve build information for ID $id" -ForegroundColor Red
            Write-Host "Error output: $jsonOutput" -ForegroundColor Red
            exit 1
        }
        
        # Parse JSON output
        $buildInfo = $jsonOutput | ConvertFrom-Json
        
        if (-not $buildInfo -or $buildInfo.Count -eq 0) {
            Write-Host "ERROR: No build information returned for ID $id" -ForegroundColor Red
            exit 1
        }
        
        $build = $buildInfo[0]
        
        # Validate repository
        if ($build.repository -ne $expectedRepo) {
            Write-Host "ERROR: Build ID $id is from repository '$($build.repository)'" -ForegroundColor Red
            Write-Host "       Expected: '$expectedRepo'" -ForegroundColor Red
            exit 1
        }
        
        # Check for branch overlap
        $branch = $build.branch
        if ($branchesSeen.ContainsKey($branch)) {
            Write-Host "ERROR: Multiple builds specified from the same branch '$branch'" -ForegroundColor Red
            Write-Host "       Build IDs: $($branchesSeen[$branch]), $id" -ForegroundColor Red
            exit 1
        }
        $branchesSeen[$branch] = $id
        
        Write-Host "  Repository: $($build.repository)" -ForegroundColor Green
        Write-Host "  Branch: $($build.branch)" -ForegroundColor Green
        Write-Host "  Build Number: $($build.buildNumber)" -ForegroundColor Green
        Write-Host ""
        
        $buildsToProcess += @{
            Id = $id
            BuildNumber = $build.buildNumber
            Commit = $build.commit
            DateProduced = $build.dateProduced
            Source = "Build ID: $id (Branch: $branch)"
        }
        
        Write-Host "Added build ID: $id" -ForegroundColor Green
    }
    
    Write-Host ""
}

# Process all gathered builds
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Gathering drops for $($buildsToProcess.Count) build(s)..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

foreach ($buildItem in $buildsToProcess) {
    $buildId = $buildItem.Id
    $buildNumber = $buildItem.BuildNumber
    
    Write-Host "----------------------------------------" -ForegroundColor Gray
    Write-Host "Processing: $($buildItem.Source)" -ForegroundColor Yellow
    if ($buildNumber) {
        Write-Host "Build Number: $buildNumber" -ForegroundColor Gray
    }
    Write-Host "----------------------------------------" -ForegroundColor Gray
    
    # Build darc command with asset filter if needed
    $darcCommand = "darc gather-drop --id $buildId --output-dir `"$OutputDir`" --overwrite --separated --use-relative-paths"
    
    # Build list of patterns to exclude
    $excludePatterns = @()
    
    if (-not $IncludeMSIdentity) {
        # Exclude msidentity packages and symbols
        $excludePatterns += "Microsoft\.DotNet\.MSIdentity"
        $excludePatterns += "Microsoft\.dotnet-msidentity"
        $excludePatterns += "msidentity.*symbols"
        Write-Host "Excluding Microsoft MSIdentity packages and symbols..." -ForegroundColor Gray
    }
    
    if (-not $IncludeExtensibility) {
        # Exclude Microsoft.DotNet.Scaffolding.* except Shared
        # Pattern: Microsoft.DotNet.Scaffolding. followed by anything except "Shared"
        $excludePatterns += "Microsoft\.DotNet\.Scaffolding\.(?!Shared\b)\w+"
        Write-Host "Excluding Microsoft.DotNet.Scaffolding extensibility packages (keeping Shared)..." -ForegroundColor Gray
    }
    
    # Apply filter if any patterns exist
    if ($excludePatterns.Count -gt 0) {
        $filterPattern = "^(?!.*(" + ($excludePatterns -join "|") + ")).*"
        $darcCommand += " --asset-filter `"$filterPattern`""
    }
    
    # Gather drop for this build
    Write-Host "Gathering drop for build $buildId..." -ForegroundColor Gray
    Invoke-Expression $darcCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Successfully gathered drop (Build $buildId)" -ForegroundColor Green
    } else {
        Write-Host "Failed to gather drop (Build $buildId)" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "All channels processed!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Rename build folders to version numbers and merge manifests
Write-Host ""
Write-Host "Processing build folders..." -ForegroundColor Cyan

# Delete release.json in root if it exists
$releaseJsonPath = Join-Path $OutputDir "release.json"
if (Test-Path $releaseJsonPath) {
    Remove-Item $releaseJsonPath -Force
}

$scaffoldingDir = Join-Path $OutputDir "scaffolding"
if (Test-Path $scaffoldingDir) {
    $versionFolders = Get-ChildItem $scaffoldingDir -Directory
    $allBuilds = @()
    
    foreach ($versionFolder in $versionFolders) {
        $manifestPath = Join-Path $versionFolder.FullName "manifest.json"
        if (Test-Path $manifestPath) {
            Write-Host "Reading manifest from $($versionFolder.Name)..." -ForegroundColor Gray
            $manifestContent = Get-Content $manifestPath -Raw
            $manifest = $manifestContent | ConvertFrom-Json
            
            # Add builds from this manifest to the collection first
            if ($manifest.builds) {
                $allBuilds += $manifest.builds
            }
            
            # Try to rename folder to version number (after reading is complete)
            $codegenAsset = $manifest.builds[0].assets | Where-Object { $_.name -eq "dotnet-aspnet-codegenerator" }
            if ($codegenAsset -and $codegenAsset.version -ne $versionFolder.Name) {
                $version = $codegenAsset.version
                $buildNumber = $manifest.builds[0].buildNumber
                $newFolderPath = Join-Path $scaffoldingDir $version
                
                Write-Host "Renaming build folder from $($versionFolder.Name) to $version..." -ForegroundColor Gray
                
                # Update paths in manifest assets from buildNumber to version
                foreach ($asset in $manifest.builds[0].assets) {
                    if ($asset.targets) {
                        for ($i = 0; $i -lt $asset.targets.Count; $i++) {
                            $asset.targets[$i] = $asset.targets[$i] -replace "scaffolding\\$buildNumber\\", "scaffolding\\$version\\"
                        }
                    }
                }
                
                # Remove destination if it exists
                if (Test-Path $newFolderPath) {
                    Remove-Item $newFolderPath -Recurse -Force
                }
                
                Rename-Item -Path $versionFolder.FullName -NewName $version
                Write-Host "Renamed to: $version" -ForegroundColor Green
            }
        }
    }
    
    if ($allBuilds.Count -gt 0) {
        # Create merged manifest
        $mergedManifest = @{
          outputPath = $OutputDir
          builds = $allBuilds
        }
        
        # Write merged manifest to root
        $rootManifestPath = Join-Path $OutputDir "manifest.json"
        
        # Delete existing root manifest if it exists
        if (Test-Path $rootManifestPath) {
            Remove-Item $rootManifestPath -Force
        }
        
        $mergedManifest | ConvertTo-Json -Depth 10 -Compress | Set-Content $rootManifestPath
        Write-Host "Created merged manifest with $($allBuilds.Count) build(s) at: $rootManifestPath" -ForegroundColor Green
    } else {
        Write-Host "WARNING: No builds found to merge" -ForegroundColor Yellow
    }
} else {
    Write-Host "WARNING: Scaffolding directory not found at $scaffoldingDir" -ForegroundColor Yellow
}

# Create zip file with all packages from shipping\packages
Write-Host ""
Write-Host "Creating package zip file..." -ForegroundColor Cyan

$shippingPackagesDir = Join-Path $OutputDir "shipping\packages"
if (Test-Path $shippingPackagesDir) {
    $dateStamp = Get-Date -Format "yyyyMMdd"
    $zipFileName = "ScaffoldingPackages$dateStamp.zip"
    $zipFilePath = Join-Path $OutputDir $zipFileName
    
    # Delete existing zip if it exists
    if (Test-Path $zipFilePath) {
        Remove-Item $zipFilePath -Force
    }
    
    # Get all package files
    $packageFiles = Get-ChildItem $shippingPackagesDir -File
    
    if ($packageFiles.Count -gt 0) {
        # Create zip file without directory structure
        Compress-Archive -Path $packageFiles.FullName -DestinationPath $zipFilePath -CompressionLevel Optimal
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "PACKAGE ZIP CREATED!" -ForegroundColor Green
        Write-Host "File: $zipFileName" -ForegroundColor Green
        Write-Host "Location: $zipFilePath" -ForegroundColor Green
        Write-Host "Contains: $($packageFiles.Count) package file(s)" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
    } else {
        Write-Host "WARNING: No package files found in shipping\packages" -ForegroundColor Yellow
    }
} else {
    Write-Host "WARNING: Shipping packages directory not found at $shippingPackagesDir" -ForegroundColor Yellow
}
