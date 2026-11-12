[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__OfficeDatabase)) {
    throw "Set ConnectionStrings__OfficeDatabase in the protected server environment first."
}

Write-Host "This creates the first administrator only when the office database has no users."
Write-Host "The API must not be exposed to office clients until this setup is completed."
dotnet run --project (Join-Path $PSScriptRoot "../../src/Tools/NotaireDesk.Bootstrap/NotaireDesk.Bootstrap.csproj") --configuration Release
