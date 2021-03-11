param ([string] $configuration = 'Debug')

$ext = if ($IsWindows) { ".exe" } else { "" }
$dotnet = Join-Path $PSScriptRoot ../bin/dotnet/dotnet$ext
$sln = Join-Path $PSScriptRoot ../Microsoft.Maui-net6.sln
$artifacts = Join-Path $PSScriptRoot ../artifacts

if (-not (Test-Path $dotnet))
{
    $csproj = Join-Path $PSScriptRoot ../src/DotNet/DotNet.csproj
    & dotnet build $csproj -bl:$artifacts/dotnet.binlog
}

& $dotnet pack $sln `
    -c:$configuration `
    -p:SymbolPackageFormat=snupkg `
    -bl:$artifacts/maui.binlog
