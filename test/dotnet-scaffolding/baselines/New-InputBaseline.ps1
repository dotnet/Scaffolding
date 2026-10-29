param(
    [Parameter(Mandatory = $true)]
    [string]$Framework,
    [Parameter(Mandatory = $true)]
    [string]$Name,
    [Parameter(Mandatory = $true)]
    [string]$Template,
    [string[]]$TemplateArguments = @(),
    [string]$SdkVersion = (& dotnet --version)
)

$ErrorActionPreference = 'Stop'
$inputs = Join-Path $PSScriptRoot 'Inputs'
$manifestPath = Join-Path $inputs 'input-baselines.json'
$relative = "$Framework/$Name"
$destination = Join-Path $inputs ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
$entries = @(Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json)

if ($Framework -notmatch '^net\d+\.\d+$' -or $Name -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
    throw 'Framework must be a target framework (for example net11.0), and Name must be a project directory name.'
}
if (Test-Path -LiteralPath $destination) {
    throw "Input baseline already exists: $destination. Use UpdateInputBaselines.ps1 to update existing inputs."
}
if ($entries.path -contains $relative) {
    throw "Input baseline already registered: $relative"
}
if ($TemplateArguments | Where-Object { $_ -match '^(--name|-n|--framework|-f|--no-restore)([=:]|$)' }) {
    throw 'Name, framework, and --no-restore are supplied by the script; do not repeat them in TemplateArguments.'
}

$arguments = @('--name', $Name, '--framework', $Framework) + $TemplateArguments
$candidate = & (Join-Path $PSScriptRoot 'New-InputCandidate.ps1') -SdkVersion $SdkVersion -Template $Template -TemplateArguments $arguments -PassThru
New-Item -ItemType Directory -Path $destination | Out-Null
Get-ChildItem -LiteralPath $candidate -Force | Copy-Item -Destination $destination -Recurse -Force
$entries += [pscustomobject]@{
    path = $relative
    template = $Template
    arguments = $arguments
    sdkVersion = $SdkVersion
}
ConvertTo-Json -InputObject @($entries) -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8

Write-Host "Added $destination. Add this row to README.md and review the input:"
Write-Host ('| `{0}` | `{1}` | `{2} --no-restore` | `{3}` |' -f ($relative.Replace('/', '\')), $Template, ($arguments -join ' '), $SdkVersion)
Remove-Item -LiteralPath (Split-Path $candidate -Parent) -Recurse -Force
