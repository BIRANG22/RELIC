$ErrorActionPreference = 'Stop'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\RelicAutomation.App\RelicAutomation.App.csproj'
$config = Join-Path $root 'NuGet.config'

dotnet restore $project --configfile $config
dotnet publish $project -c Release -r win-x64 --self-contained false
