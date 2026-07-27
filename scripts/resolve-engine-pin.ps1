[CmdletBinding()]
param(
    [string] $RepositoryRoot = $(Split-Path -Parent $PSScriptRoot),
    [switch] $AsObject
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$lockPath = Join-Path $RepositoryRoot 'engines\ARIEC61850.lock.json'
if (-not (Test-Path $lockPath -PathType Leaf)) {
    throw "Pinned ARIEC61850 revision file was not found: $lockPath"
}

try {
    $document = Get-Content $lockPath -Raw | ConvertFrom-Json
}
catch {
    throw "Pinned ARIEC61850 revision file is invalid JSON: $($_.Exception.Message)"
}

if ([int] $document.schemaVersion -ne 1) {
    throw "Unsupported ARIEC61850 lock schema version '$($document.schemaVersion)'."
}

$repository = ([string] $document.repository).Trim()
$ref = ([string] $document.ref).Trim()
$commit = ([string] $document.commit).Trim().ToLowerInvariant()

if ($repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
    throw "Invalid ARIEC61850 repository '$repository'."
}
if ([string]::IsNullOrWhiteSpace($ref)) {
    throw 'Pinned ARIEC61850 ref cannot be empty.'
}
if ($commit -notmatch '^[0-9a-f]{40}$') {
    throw "Pinned ARIEC61850 commit '$commit' is not a full 40-character SHA."
}

$result = [pscustomobject]@{
    SchemaVersion = 1
    Repository = $repository
    RepositoryUrl = "https://github.com/$repository.git"
    Ref = $ref
    Commit = $commit
    PairedPullRequest = [int] $document.pairedPullRequest
    LockPath = $lockPath
}

if ($AsObject) {
    return $result
}

$result | ConvertTo-Json -Depth 4
