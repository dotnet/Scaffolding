param(
    [Parameter(Mandatory = $true)]
    [string]$SdkVersion,
    [Parameter(Mandatory = $true)]
    [string]$Template,
    [string[]]$TemplateArguments = @(),
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'
if ($TemplateArguments | Where-Object { $_ -match '^(--output|-o)([=:]|$)' }) {
    throw 'The output directory is supplied by the script; do not include --output or -o in TemplateArguments.'
}
if ($TemplateArguments -notcontains '--no-restore') {
    $TemplateArguments += '--no-restore'
}

$root = Join-Path ([IO.Path]::GetTempPath()) ('ScaffoldingBaselineInput-' + [Guid]::NewGuid().ToString('N'))
$candidate = Join-Path $root 'candidate'
$hive = Join-Path $root 'hive'
New-Item -ItemType Directory -Path $root | Out-Null

@{ sdk = @{ version = $SdkVersion; rollForward = 'disable'; allowPrerelease = $true } } |
    ConvertTo-Json -Depth 3 |
    Set-Content -Path (Join-Path $root 'global.json') -Encoding Ascii

Push-Location $root
try {
    $selectedSdk = & dotnet --version
    if ($LASTEXITCODE -ne 0 -or $selectedSdk.Trim() -ne $SdkVersion) {
        throw "The requested SDK $SdkVersion is not installed. Selected SDK: $selectedSdk"
    }

    $command = "dotnet new $Template $($TemplateArguments -join ' ')"
    @("SDK: $selectedSdk", "Template: $Template", "Arguments: $($TemplateArguments -join ' ')") |
        Set-Content -Path (Join-Path $root 'generation.txt')

    & dotnet new $Template @TemplateArguments --output $candidate --debug:custom-hive $hive | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Template generation failed: $command"
    }

    Write-Host "Generated $candidate with SDK $selectedSdk"
    Write-Host "Generation details: $(Join-Path $root 'generation.txt')"
    if ($PassThru) {
        Write-Output $candidate
    }
    else {
        Write-Host "Review the candidate before copying it into baselines\Inputs and record the SDK and arguments in README.md."
    }
}
finally {
    Pop-Location
}
