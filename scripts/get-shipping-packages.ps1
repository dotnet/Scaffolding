<#
.SYNOPSIS
    Gets the latest scaffolding builds from multiple channels and gathers their drops.

.DESCRIPTION
    This script queries the latest builds for the scaffolding repository across multiple
    .NET channels, then downloads the build drops to a specified output directory.
    Alternatively, you can specify build IDs directly to skip channel queries.
    
    REQUIREMENTS: If the Darc CLI tool is not installed, this script will attempt to install it automatically.
    This requires a working .NET SDK and network access to the Darc package feed.

.PARAMETER Channels
    Comma-delimited list of .NET channels to query (e.g., ".NET 10,.NET 9,.NET 8")
    Either Channels or Ids must be specified, but not both.

.PARAMETER Ids
    Comma-delimited list of build identifiers to gather drops for directly.
    Each value can be a Darc build ID, a commit SHA (full or short), an Azure DevOps build ID,
    or an Azure DevOps build results URL. Azure DevOps lookup may require authentication.
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
    
    [Parameter(Mandatory=$false)]
    [string]$OutputDir,
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeMSIdentity,
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeExtensibility,

    [Alias('h')]
    [Parameter(Mandatory=$false)]
    [switch]$Help,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

function Normalize-InputValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $Value
    }

    return $Value.Trim().Trim('"').Trim("'")
}

function Show-Usage {
    $scriptName = Split-Path -Leaf $PSCommandPath

    Write-Host @"
Gets the latest scaffolding shipping packages from Darc.

Usage:
  .\$scriptName -Channels ".NET 11,.NET 10" -OutputDir "C:\drop"
  .\$scriptName -Ids "311293" -OutputDir "C:\drop"
  .\$scriptName -Ids "867fb4f2" -OutputDir "C:\drop"
  .\$scriptName -Ids "2956938" -OutputDir "C:\drop"
  .\$scriptName -Ids "20260421.2" -OutputDir "C:\drop"
  .\$scriptName --help

Pipeline:
  https://dev.azure.com/dnceng/internal/_build?definitionId=345

Options:
  -Channels             Comma-delimited list of channel names to query.
  -Ids                  Comma-delimited list of build identifiers. Supports Darc IDs,
                        commit SHAs, Azure DevOps build IDs, build numbers, and build URLs.
  -OutputDir            Target folder for downloaded packages and manifests.
  -IncludeMSIdentity    Include Microsoft.dotnet-msidentity packages.
  -IncludeExtensibility Include Microsoft.DotNet.Scaffolding extensibility packages.
  -Help, -h, --help     Show this help and exit.

Examples:
  .\$scriptName -Channels ".NET 11" -OutputDir "C:\temp\drops"
  .\$scriptName -Ids "https://dev.azure.com/dnceng/internal/_build/results?buildId=2956938&view=results" -OutputDir "C:\temp\drops"
"@
}

$Channels = Normalize-InputValue $Channels
$Ids = Normalize-InputValue $Ids
$OutputDir = Normalize-InputValue $OutputDir

$helpTokens = @('--help', '-help', '-h', '-?')
$showHelp = $Help -or ($RemainingArgs | Where-Object { $_ -in $helpTokens }) -or ($Channels -in $helpTokens) -or ($Ids -in $helpTokens) -or ($OutputDir -in $helpTokens)
if ($showHelp) {
    Show-Usage
    exit 0
}

function Get-DarcExecutable {
    $existingDarc = Get-Command darc -ErrorAction SilentlyContinue
    if ($existingDarc) {
        return $existingDarc.Source
    }

    Write-Host "Darc CLI not found. Installing it now..." -ForegroundColor Yellow

    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
    $darcInitScript = Join-Path $repoRoot 'eng\common\darc-init.ps1'

    if (-not (Test-Path $darcInitScript)) {
        Write-Host "ERROR: Unable to find the Darc installer script at $darcInitScript" -ForegroundColor Red
        exit 1
    }

    try {
        & $darcInitScript | Out-Host

        $globalToolPath = Join-Path $env:USERPROFILE '.dotnet\tools'
        if ((Test-Path $globalToolPath) -and ($env:PATH -notlike "*$globalToolPath*")) {
            $env:PATH = "$globalToolPath;$env:PATH"
        }

        $installedDarc = Get-Command darc -ErrorAction SilentlyContinue
        if ($installedDarc) {
            Write-Host "Darc installed successfully." -ForegroundColor Green
            return $installedDarc.Source
        }

        $fallbackDarcPath = Join-Path $globalToolPath 'darc.exe'
        if (Test-Path $fallbackDarcPath) {
            Write-Host "Darc installed successfully." -ForegroundColor Green
            return $fallbackDarcPath
        }

        throw "Darc executable was not found after installation."
    }
    catch {
        Write-Host "ERROR: Failed to install the 'darc' CLI automatically." -ForegroundColor Red
        Write-Host "Exception: $_" -ForegroundColor Red
        Write-Host "Docs: https://github.com/dotnet/arcade-services/blob/main/docs/Darc.md" -ForegroundColor Yellow
        exit 1
    }
}

function Invoke-AdoApi {
    param([string]$Uri)

    $headers = @{ Accept = 'application/json' }

    if (-not [string]::IsNullOrWhiteSpace($env:SYSTEM_ACCESSTOKEN)) {
        $headers['Authorization'] = "Bearer $($env:SYSTEM_ACCESSTOKEN)"
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:AZURE_DEVOPS_EXT_PAT)) {
        $patBytes = [System.Text.Encoding]::ASCII.GetBytes(":$($env:AZURE_DEVOPS_EXT_PAT)")
        $headers['Authorization'] = "Basic $([Convert]::ToBase64String($patBytes))"
    }
    elseif (Get-Command az -ErrorAction SilentlyContinue) {
        try {
            $accessToken = az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv 2>$null
            if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($accessToken)) {
                $headers['Authorization'] = "Bearer $($accessToken.Trim())"
            }
        }
        catch {
            # Fall back to default credentials below.
        }
    }

    $requestParams = @{
        Uri = $Uri
        Method = 'Get'
        Headers = $headers
        UseBasicParsing = $true
        ErrorAction = 'Stop'
    }

    if (-not $headers.ContainsKey('Authorization')) {
        $requestParams['UseDefaultCredentials'] = $true
    }

    $response = Invoke-WebRequest @requestParams
    if ([string]::IsNullOrWhiteSpace($response.Content)) {
        throw 'Azure DevOps returned an empty response.'
    }

    $trimmedContent = $response.Content.TrimStart()
    if ($trimmedContent.StartsWith('<')) {
        throw 'Authentication is required to query Azure DevOps build metadata. Set AZURE_DEVOPS_EXT_PAT or use a commit SHA with -Ids.'
    }

    return $trimmedContent | ConvertFrom-Json
}

function Resolve-BuildRecord {
    param(
        [string]$Identifier,
        [string]$DarcExe,
        [string]$ExpectedRepo
    )

    $normalizedIdentifier = Normalize-InputValue $Identifier
    $adoBuildId = $null

    if ($normalizedIdentifier -match 'buildId=(\d+)') {
        $adoBuildId = $Matches[1]
        $normalizedIdentifier = $adoBuildId
    }

    if ($normalizedIdentifier -match '^\d+(\.\d+)+$') {
        try {
            $encodedBuildNumber = [System.Uri]::EscapeDataString($normalizedIdentifier)
            $adoResponse = Invoke-AdoApi -Uri "https://dev.azure.com/dnceng/internal/_apis/build/builds?buildNumber=$encodedBuildNumber&api-version=7.1-preview.7"
            $adoBuilds = @($adoResponse.value | Where-Object { $_.sourceVersion })

            foreach ($adoBuild in $adoBuilds) {
                if ($adoBuild.repository) {
                    $repoName = [string]$adoBuild.repository.name
                    $repoUrl = [string]$adoBuild.repository.url
                    if (($repoName -notmatch 'scaffolding') -and ($repoUrl -notmatch 'scaffolding')) {
                        continue
                    }
                }

                Write-Host "Resolved Azure DevOps build number $normalizedIdentifier to build ID $($adoBuild.id) and commit $($adoBuild.sourceVersion)." -ForegroundColor Green
                $jsonOutput = & $DarcExe get-build --repo $ExpectedRepo --commit $adoBuild.sourceVersion --output-format json 2>&1
                if ($LASTEXITCODE -eq 0) {
                    $buildInfo = $jsonOutput | ConvertFrom-Json
                    if ($buildInfo -and $buildInfo.Count -gt 0) {
                        return $buildInfo[0]
                    }
                }
            }
        }
        catch {
            throw "Azure DevOps build number lookup for '$normalizedIdentifier' requires authentication. Set AZURE_DEVOPS_EXT_PAT or use the commit SHA shown on the build page. Details: $_"
        }

        throw "No Darc build was found for Azure DevOps build number '$normalizedIdentifier'."
    }

    if ($normalizedIdentifier -match '^\d+$') {
        $jsonOutput = & $DarcExe get-build --id $normalizedIdentifier --output-format json 2>&1
        if ($LASTEXITCODE -eq 0) {
            $buildInfo = $jsonOutput | ConvertFrom-Json
            if ($buildInfo -and $buildInfo.Count -gt 0) {
                return $buildInfo[0]
            }
        }

        try {
            $adoResponse = Invoke-AdoApi -Uri "https://dev.azure.com/dnceng/internal/_apis/build/builds/$normalizedIdentifier`?api-version=7.1-preview.7"
            if ($adoResponse.sourceVersion) {
                Write-Host "Resolved Azure DevOps build ID $normalizedIdentifier to commit $($adoResponse.sourceVersion)." -ForegroundColor Green
                $jsonOutput = & $DarcExe get-build --repo $ExpectedRepo --commit $adoResponse.sourceVersion --output-format json 2>&1
                if ($LASTEXITCODE -eq 0) {
                    $buildInfo = $jsonOutput | ConvertFrom-Json
                    if ($buildInfo -and $buildInfo.Count -gt 0) {
                        return $buildInfo[0]
                    }
                }
            }
        }
        catch {
            throw "Azure DevOps build ID lookup for '$normalizedIdentifier' requires authentication. Set AZURE_DEVOPS_EXT_PAT or use the commit SHA shown on the build page. Details: $_"
        }

        throw "No Darc build was found for identifier '$normalizedIdentifier'."
    }

    if ($normalizedIdentifier -match '^[0-9a-fA-F]{7,40}$') {
        $commit = $normalizedIdentifier

        if ($commit.Length -lt 40) {
            $resolvedCommit = git rev-parse $commit 2>$null
            if ($LASTEXITCODE -eq 0 -and $resolvedCommit) {
                $commit = $resolvedCommit.Trim()
            }
        }

        $jsonOutput = & $DarcExe get-build --repo $ExpectedRepo --commit $commit --output-format json 2>&1
        if ($LASTEXITCODE -eq 0) {
            $buildInfo = $jsonOutput | ConvertFrom-Json
            if ($buildInfo -and $buildInfo.Count -gt 0) {
                return $buildInfo[0]
            }
        }

        throw "No Darc build was found for commit '$normalizedIdentifier'."
    }

    throw "Unsupported build identifier '$normalizedIdentifier'. Use a Darc ID, commit SHA, Azure DevOps build ID, Azure DevOps build number, or build results URL."
}

# Validate that either Channels or Ids is specified, but not both
if ([string]::IsNullOrWhiteSpace($Channels) -and [string]::IsNullOrWhiteSpace($Ids)) {
    Write-Host "ERROR: Either -Channels or -Ids must be specified" -ForegroundColor Red
    exit 1
}

if (![string]::IsNullOrWhiteSpace($Channels) -and ![string]::IsNullOrWhiteSpace($Ids)) {
    Write-Host "ERROR: Cannot specify both -Channels and -Ids" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    Write-Host "ERROR: -OutputDir must be specified unless you are using --help" -ForegroundColor Red
    Write-Host "" 
    Show-Usage
    exit 1
}

# Ensure output directory exists
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "Created output directory: $OutputDir"
}

$DarcExe = Get-DarcExecutable

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
            $jsonOutput = & $DarcExe get-latest-build --repo scaffolding --channel $channel --output-format json 2>&1
            
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
                    AzdoBuildId = $build.azdoBuildId
                    BuildNumber = $build.buildNumber
                    Commit = $build.commit
                    DateProduced = $build.dateProduced
                    Source = "Channel: $channel"
                }
                
                Write-Host "Found build:" -ForegroundColor Green
                Write-Host "  Darc ID: $($build.id)" -ForegroundColor White
                if ($build.azdoBuildId) {
                    Write-Host "  Azure DevOps Build ID: $($build.azdoBuildId)" -ForegroundColor White
                }
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
        Write-Host "Resolving build identifier: $id..." -ForegroundColor Gray

        try {
            $build = Resolve-BuildRecord -Identifier $id -DarcExe $DarcExe -ExpectedRepo $expectedRepo
        }
        catch {
            Write-Host "ERROR: Failed to resolve build identifier '$id'" -ForegroundColor Red
            Write-Host "Error output: $_" -ForegroundColor Red
            exit 1
        }

        # Validate repository
        if ($build.repository -ne $expectedRepo) {
            Write-Host "ERROR: Build identifier '$id' resolved to repository '$($build.repository)'" -ForegroundColor Red
            Write-Host "       Expected: '$expectedRepo'" -ForegroundColor Red
            exit 1
        }
        
        # Check for branch overlap
        $branch = $build.branch
        if ($branchesSeen.ContainsKey($branch)) {
            Write-Host "ERROR: Multiple builds specified from the same branch '$branch'" -ForegroundColor Red
            Write-Host "       Build IDs: $($branchesSeen[$branch]), $($build.id)" -ForegroundColor Red
            exit 1
        }
        $branchesSeen[$branch] = $build.id
        
        Write-Host "  Repository: $($build.repository)" -ForegroundColor Green
        Write-Host "  Branch: $($build.branch)" -ForegroundColor Green
        Write-Host "  Darc ID: $($build.id)" -ForegroundColor Green
        if ($build.azdoBuildId) {
            Write-Host "  Azure DevOps Build ID: $($build.azdoBuildId)" -ForegroundColor Green
        }
        Write-Host "  Build Number: $($build.buildNumber)" -ForegroundColor Green
        Write-Host ""
        
        $buildsToProcess += @{
            Id = $build.id
            AzdoBuildId = $build.azdoBuildId
            BuildNumber = $build.buildNumber
            Commit = $build.commit
            DateProduced = $build.dateProduced
            Source = "Build Input: $id (Branch: $branch)"
        }
        
        Write-Host "Added Darc build ID: $($build.id)" -ForegroundColor Green
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
    $azdoBuildId = $buildItem.AzdoBuildId
    
    Write-Host "----------------------------------------" -ForegroundColor Gray
    Write-Host "Processing: $($buildItem.Source)" -ForegroundColor Yellow
    Write-Host "Darc ID: $buildId" -ForegroundColor Gray
    if ($azdoBuildId) {
        Write-Host "Azure DevOps Build ID: $azdoBuildId" -ForegroundColor Gray
    }
    if ($buildNumber) {
        Write-Host "Build Number: $buildNumber" -ForegroundColor Gray
    }
    Write-Host "----------------------------------------" -ForegroundColor Gray
    
    # Build Darc arguments with asset filter if needed
    $darcArgs = @('gather-drop', '--id', $buildId, '--output-dir', $OutputDir, '--overwrite', '--separated', '--use-relative-paths')
    
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
        $darcArgs += @('--asset-filter', $filterPattern)
    }
    
    # Gather drop for this build
    Write-Host "Gathering drop for build $buildId..." -ForegroundColor Gray
    & $DarcExe @darcArgs
    
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
