param(
    [string] $SiteRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'site')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-True {
    param(
        [Parameter(Mandatory)]
        [bool] $Condition,
        [Parameter(Mandatory)]
        [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sitePath = (Resolve-Path $SiteRoot).Path
$indexPath = Join-Path $sitePath 'index.html'
$manifestPath = Join-Path $sitePath 'site.webmanifest'
$sitemapPath = Join-Path $sitePath 'sitemap.xml'
$robotsPath = Join-Path $sitePath 'robots.txt'

$requiredFiles = @(
    $indexPath,
    (Join-Path $sitePath 'styles.css'),
    $manifestPath,
    $sitemapPath,
    $robotsPath
)

foreach ($file in $requiredFiles) {
    Assert-True (Test-Path $file -PathType Leaf) "Required public-site file is missing: $file"
}

$html = Get-Content $indexPath -Raw

$requiredHtmlPatterns = @(
    '<meta\s+name="viewport"',
    '<meta\s+name="description"',
    '<link\s+rel="canonical"',
    '<meta\s+property="og:title"',
    '<meta\s+property="og:image"',
    '<meta\s+name="twitter:card"',
    'application/ld\+json',
    'ARSVIN-Suite-Setup-win-x64\.exe',
    'ARSVIN-Publisher-win-x64\.exe',
    'ArSubsv-Subscriber-win-x64\.exe',
    'SHA256SUMS\.txt'
)

foreach ($pattern in $requiredHtmlPatterns) {
    Assert-True ([regex]::IsMatch($html, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)) "Required HTML content was not found: $pattern"
}

$h1Count = [regex]::Matches($html, '<h1\b', [Text.RegularExpressions.RegexOptions]::IgnoreCase).Count
Assert-True ($h1Count -eq 1) "The landing page must contain exactly one h1 element; found $h1Count."

$jsonLdPattern = '<script\s+type=["'']application/ld\+json["''][^>]*>(.*?)</script>'
$jsonLdMatches = [regex]::Matches(
    $html,
    $jsonLdPattern,
    [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [Text.RegularExpressions.RegexOptions]::Singleline
)
Assert-True ($jsonLdMatches.Count -gt 0) 'No JSON-LD structured data blocks were found.'

foreach ($match in $jsonLdMatches) {
    $null = $match.Groups[1].Value | ConvertFrom-Json
}

$attributeMatches = [regex]::Matches(
    $html,
    '(?:src|href)=["'']([^"'']+)["'']',
    [Text.RegularExpressions.RegexOptions]::IgnoreCase
)

$missingLocalReferences = [System.Collections.Generic.List[string]]::new()
foreach ($match in $attributeMatches) {
    $reference = $match.Groups[1].Value.Trim()
    if (
        [string]::IsNullOrWhiteSpace($reference) -or
        $reference.StartsWith('#') -or
        $reference -match '^(?i:https?:|mailto:|data:|javascript:)'
    ) {
        continue
    }

    $relativePath = ($reference -split '[?#]', 2)[0]
    if ([string]::IsNullOrWhiteSpace($relativePath)) {
        continue
    }

    $normalizedPath = $relativePath.Replace('/', [IO.Path]::DirectorySeparatorChar)
    $resolvedPath = Join-Path $sitePath $normalizedPath
    if (-not (Test-Path $resolvedPath -PathType Leaf)) {
        $missingLocalReferences.Add($reference)
    }
}

Assert-True ($missingLocalReferences.Count -eq 0) "Missing local site references: $($missingLocalReferences -join ', ')"

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
Assert-True (-not [string]::IsNullOrWhiteSpace($manifest.name)) 'The web manifest name is missing.'
Assert-True (-not [string]::IsNullOrWhiteSpace($manifest.short_name)) 'The web manifest short_name is missing.'
Assert-True ($manifest.icons.Count -gt 0) 'The web manifest does not declare any icons.'

foreach ($icon in $manifest.icons) {
    $iconPath = Join-Path $sitePath ($icon.src.Replace('/', [IO.Path]::DirectorySeparatorChar))
    Assert-True (Test-Path $iconPath -PathType Leaf) "Web-manifest icon is missing: $($icon.src)"
}

[xml] $sitemap = Get-Content $sitemapPath -Raw
$location = $sitemap.urlset.url.loc
Assert-True ($location -eq 'https://masarray.github.io/arsvin/') "Unexpected sitemap canonical URL: $location"

$robots = Get-Content $robotsPath -Raw
Assert-True ($robots -match 'Sitemap:\s*https://masarray\.github\.io/arsvin/sitemap\.xml') 'robots.txt does not reference the public sitemap.'

Write-Host "Public site validation passed: $sitePath"
