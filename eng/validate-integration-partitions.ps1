[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [PSCustomObject]$Matrix
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_UI_LANGUAGE = 'en-US'
$project = Join-Path $PSScriptRoot '..\test\dotnet-scaffolding\dotnet-scaffold.Tests\dotnet-scaffold.Tests.csproj'

function Get-TestNames {
    param([string]$Label, [string]$Filter)

    $arguments = @('test', $project, '--no-build', '-c', 'Release', '--list-tests', '--nologo')
    if ($Filter) {
        $arguments += @('--filter', $Filter)
    }
    $output = & dotnet @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Test discovery failed for ${Label}:`n$($output -join "`n")"
    }
    $lines = @($output | ForEach-Object { $_.ToString().Trim() })
    $marker = [Array]::IndexOf($lines, 'The following Tests are available:')
    if ($marker -lt 0 -or $marker -ge $lines.Length - 1) {
        throw "No test inventory found for '$Label'."
    }
    $names = @($lines[($marker + 1)..($lines.Length - 1)] | Where-Object { $_ })
    if ($names.Count -eq 0) {
        throw "No tests selected for '$Label'."
    }
    Write-Host "${Label}: $($names.Count) cases"
    return $names
}

$all = @(Get-TestNames -Label All)
$selected = @(Get-TestNames -Label Units -Filter 'Suite!=ScaffoldIntegration')
foreach ($shard in $Matrix.PSObject.Properties) {
    $filter = $shard.Value._Family
    if ([string]::IsNullOrWhiteSpace($filter)) {
        throw "Missing integration filter for '$($shard.Name)'."
    }
    $selected += @(Get-TestNames -Label $shard.Name -Filter "Suite=ScaffoldIntegration&($filter)")
}
$difference = @(Compare-Object -ReferenceObject $all -DifferenceObject $selected -CaseSensitive)
if ($difference.Count -ne 0) {
    throw "PR selections omit or duplicate CLI tests:`n$($difference | Select-Object -First 10 | Out-String)"
}
Write-Host "PR selections cover all $($all.Count) discovered CLI cases exactly once."
