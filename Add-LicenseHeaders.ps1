<#
================================================================================
Add-LicenseHeaders.ps1

Prepends a two-line Apache 2.0 / SPDX copyright header to every authored .cs
file in the repository that does not already have one.

Safe to re-run: files that already contain the SPDX identifier are skipped.

Excludes:
  - bin/ and obj/ build output
  - *.g.cs, *.designer.cs, AssemblyInfo.cs  (generated files)
================================================================================
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RepoRoot = $PSScriptRoot
)

$header = @"
// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0

"@

$excludePathPattern  = '\\(bin|obj)\\'
$excludeNamePattern  = '\.g\.cs$|\.g\.i\.cs$|\.designer\.cs$|AssemblyInfo\.cs$'
$spdxMarker          = 'SPDX-License-Identifier'

$files = Get-ChildItem -Path $RepoRoot -Recurse -Filter '*.cs' |
         Where-Object { $_.FullName -notmatch $excludePathPattern } |
         Where-Object { $_.Name    -notmatch $excludeNamePattern  }

$tagged  = 0
$skipped = 0

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw

    if ($content -match $spdxMarker) {
        $skipped++
        continue
    }

    if ($PSCmdlet.ShouldProcess($file.FullName, 'Prepend license header')) {
        Set-Content -Path $file.FullName -Value ($header + $content) -Encoding UTF8 -NoNewline
        $tagged++
        Write-Verbose "Tagged: $($file.FullName)"
    }
}

Write-Host ""
Write-Host "Done. Tagged: $tagged file(s), Skipped (already tagged): $skipped file(s)." -ForegroundColor Green
