[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__OfficeDatabase)) {
    throw "Set ConnectionStrings__OfficeDatabase to the temporary office migration-owner connection before running bootstrap."
}

$bootstrapExe = Join-Path $PSScriptRoot "../../artifacts/bootstrap/NotaireDesk.Bootstrap.exe"
$bootstrapExe = [System.IO.Path]::GetFullPath($bootstrapExe)

if (-not (Test-Path -LiteralPath $bootstrapExe -PathType Leaf)) {
    throw "Published bootstrap executable not found at: $bootstrapExe. Extract the complete notaire-desk-office-server-x64 artifact and run this script from deployment/Server."
}

Write-Host "This applies reviewed migrations and creates the first administrator only when the office database has no users."
Write-Host "Use the temporary office_migrator connection for this step. The API must use office_runtime afterwards."
& $bootstrapExe
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
