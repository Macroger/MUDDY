# Count non-empty, non-comment lines in all .cs files (recursive)
$Total = 0
Get-ChildItem -Recurse -Include *.cs | ForEach-Object {
    $lines = Get-Content $_.FullName | Where-Object {
        $_.Trim() -ne "" -and -not ($_.Trim().StartsWith("//"))
    }
    $Total += $lines.Count
}
Write-Host "Non-empty, non-comment lines of code: $Total"