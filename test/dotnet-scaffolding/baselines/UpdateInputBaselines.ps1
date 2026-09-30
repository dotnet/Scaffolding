param(
    [string]$SdkVersion = (& dotnet --version),
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$inputs = Join-Path $PSScriptRoot 'Inputs'
$manifestPath = Join-Path $inputs 'input-baselines.json'
$readmePath = Join-Path $PSScriptRoot 'README.md'
$entries = @(Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json)
$readme = [IO.File]::ReadAllText($readmePath)
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$pending = @()

foreach ($entry in $entries) {
    if ($entry.path -notmatch '^net\d+\.\d+/[A-Za-z0-9][A-Za-z0-9._-]*$' -or -not $entry.template -or -not $entry.arguments -or $entry.arguments -contains '--no-restore' -or -not $entry.sdkVersion -or -not $seen.Add($entry.path)) {
        throw "Invalid or duplicate input baseline entry: $($entry.path)"
    }

    $destination = Join-Path $inputs ($entry.path.Replace('/', [IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $destination -PathType Container)) {
        throw "Input baseline is missing: $destination"
    }

    $displayPath = $entry.path.Replace('/', '\')
    $row = '| `{0}` | `{1}` | `{2} --no-restore` | `{3}` |' -f $displayPath, $entry.template, ($entry.arguments -join ' '), $entry.sdkVersion
    if (-not $readme.Contains($row)) {
        throw "README.md is missing the registered input baseline row: $row"
    }

    if ($Force -or $entry.sdkVersion -ne $SdkVersion) {
        $pending += [pscustomobject]@{ Entry = $entry; Destination = $destination; Row = $row; DisplayPath = $displayPath }
    }
}

foreach ($framework in Get-ChildItem -LiteralPath $inputs -Directory) {
    foreach ($project in Get-ChildItem -LiteralPath $framework.FullName -Directory) {
        if (-not $seen.Contains("$($framework.Name)/$($project.Name)")) {
            throw "Unregistered input baseline: $($project.FullName)"
        }
    }
}

$repository = & git -C $PSScriptRoot rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -eq 0) {
    foreach ($item in $pending) {
        $status = & git -C $repository status --porcelain --untracked-files=all -- $item.Destination
        if ($status) {
            throw "Input baseline has uncommitted changes: $($item.Destination)"
        }
    }
}

foreach ($item in $pending) {
    $candidate = & (Join-Path $PSScriptRoot 'New-InputCandidate.ps1') -SdkVersion $SdkVersion -Template $item.Entry.template -TemplateArguments $item.Entry.arguments -PassThru
    $item | Add-Member -NotePropertyName Candidate -NotePropertyValue $candidate
}

foreach ($item in $pending) {
    $files = @(Get-ChildItem -LiteralPath $item.Candidate -Recurse -File -Force)
    $generated = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in $files) {
        [void]$generated.Add($file.FullName.Substring($item.Candidate.Length + 1))
    }
    foreach ($file in Get-ChildItem -LiteralPath $item.Destination -Recurse -File -Force) {
        if (-not $generated.Contains($file.FullName.Substring($item.Destination.Length + 1))) {
            Remove-Item -LiteralPath $file.FullName -Force
        }
    }
    foreach ($file in $files) {
        $target = Join-Path $item.Destination ($file.FullName.Substring($item.Candidate.Length + 1))
        New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }

    $item.Entry.sdkVersion = $SdkVersion
    $replacement = '| `{0}` | `{1}` | `{2} --no-restore` | `{3}` |' -f $item.DisplayPath, $item.Entry.template, ($item.Entry.arguments -join ' '), $SdkVersion
    $readme = $readme.Replace($item.Row, $replacement)
    Write-Host "Updated $($item.Destination). Review this input and reevaluate its output baselines."
    Remove-Item -LiteralPath (Split-Path $item.Candidate -Parent) -Recurse -Force
}

if ($pending.Count -eq 0) {
    Write-Host "All input baselines already use SDK $SdkVersion."
}
else {
    $entries | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    [IO.File]::WriteAllText($readmePath, $readme, [Text.UTF8Encoding]::new($false))
}
