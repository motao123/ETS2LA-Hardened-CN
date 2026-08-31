[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Test,
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnetCommand = $null
$systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($systemDotnet) {
    $sdkList = & $systemDotnet.Source --list-sdks 2>$null
    if ($sdkList -match '^10\.0\.') { $dotnetCommand = $systemDotnet.Source }
}
if (-not $dotnetCommand) {
    $localDotnet = Join-Path $root '..\.tools\dotnet\dotnet.exe'
    if (Test-Path $localDotnet) {
        $dotnetCommand = (Resolve-Path $localDotnet).Path
    } else {
        throw 'A .NET 10 SDK is required.'
    }
}

$required = @(
    'TruckLib\TruckLib\TruckLib.csproj',
    'Assets',
    'ETS2LA.Game\libs\libdeflate.dll'
)
foreach ($path in $required) {
    if (-not (Test-Path (Join-Path $root $path))) {
        throw "Missing build input: $path"
    }
}

Push-Location $root
try {
    $rid = 'win-x64'
    & $dotnetCommand restore ETS2LA.sln -r $rid --force-evaluate
    & $dotnetCommand build ETS2LA.sln -c $Configuration --no-restore
    if ($Test) {
        & $dotnetCommand test tests\ETS2LA.Hardened.Tests\ETS2LA.Hardened.Tests.csproj -c $Configuration --no-restore
    }
    if ($Publish) {
        & $dotnetCommand publish ETS2LA\ETS2LA.csproj -c $Configuration -r $rid --self-contained true -o publish\win-x64 --no-restore
        & $dotnetCommand build Plugins\AutoBehavior\AutoBehavior.csproj -c $Configuration --no-restore
        New-Item -ItemType Directory -Force -Path 'Assets\BundledPlugins', 'publish\win-x64\Assets\BundledPlugins' | Out-Null
        Copy-Item "Plugins\AutoBehavior\bin\$Configuration\net10.0\AutoBehavior.dll" 'Assets\BundledPlugins\' -Force
        Copy-Item 'Assets\BundledPlugins\AutoBehavior.dll' 'publish\win-x64\Assets\BundledPlugins\' -Force
        Copy-Item Assets publish\win-x64\Assets -Recurse -Force
        & (Join-Path $root 'publish\win-x64\ETS2LA.exe') --smoke-test
    }
} finally {
    Pop-Location
}
