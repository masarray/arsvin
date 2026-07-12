[CmdletBinding()]
param(
    [string] $SiteRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'site')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$validator = Join-Path $PSScriptRoot 'validate-public-site.py'
if (-not (Test-Path $validator -PathType Leaf)) {
    throw "Public-site validator was not found: $validator"
}

$pythonCommand = Get-Command python -ErrorAction SilentlyContinue
if (-not $pythonCommand) {
    $pythonCommand = Get-Command python3 -ErrorAction SilentlyContinue
}
if (-not $pythonCommand) {
    throw 'Python 3 is required to validate the public site.'
}

& $pythonCommand.Source $validator --site-root $SiteRoot
if ($LASTEXITCODE -ne 0) {
    throw "Public-site validation failed with exit code $LASTEXITCODE."
}
