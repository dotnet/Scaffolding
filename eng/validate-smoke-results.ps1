[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResultsPath
)

$ErrorActionPreference = 'Stop'
[xml]$results = Get-Content -LiteralPath $ResultsPath -Raw
$manager = [System.Xml.XmlNamespaceManager]::new($results.NameTable)
$manager.AddNamespace('trx', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$tests = @($results.SelectNodes('//trx:UnitTestResult', $manager))
$expectedNames = @(
    'Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.API.MinimalApiNet10IntegrationTests.Scaffold_MinimalApi_Net10_CliInvocation'
    'Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor.BlazorCrudNet10IntegrationTests.Scaffold_BlazorCrud_Net10_CliInvocation'
    'Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC.AreaNet10IntegrationTests.Scaffold_Area_Net10_CliInvocation'
)
if ($tests.Count -ne $expectedNames.Count -or @($tests | Where-Object { $_.outcome -ne 'Passed' }).Count -ne 0) {
    throw "Expected exactly three passing smoke tests, found $($tests.Count) results."
}

foreach ($name in $expectedNames) {
    if (@($tests | Where-Object { $_.testName -eq $name }).Count -ne 1) {
        throw "Expected exactly one passing result for '$name'."
    }
}

Write-Host 'All three cross-platform scaffold-and-build smoke tests passed.'
