<#
================================================================================
Generate-WixFiles.ps1

Generates WiX ComponentGroup files for Client and Server publish directories.

MSI RULES ENFORCED:
- One file per component
- No shortcuts inside file components
- Shortcuts live in their OWN component
- Shortcut component uses HKCU registry KeyPath
================================================================================
#>

[CmdletBinding()]
param()

Write-Host "Initializing configuration..." -ForegroundColor Cyan

# Repository root = script location
$repoRoot = $PSScriptRoot

$clientSource = Join-Path $repoRoot "Client.GUI\publish"
$serverSource = Join-Path $repoRoot "Server.GUI\publish"

$clientOutput = Join-Path $repoRoot "Client.msi\ClientFiles.generated.wxs"
$serverOutput = Join-Path $repoRoot "Server.msi\ServerFiles.generated.wxs"

# --------------------------------------------------------------------------
# HELPER FUNCTION
# --------------------------------------------------------------------------
function Generate-WixFileList {
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$SourceDir,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$ComponentGroupId,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$DirectoryId,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$WixSourceRoot,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$OutputFile,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$MainExeName,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$ShortcutName,

        [Parameter()]
        [string]$IconId = ""
    )

    Write-Host ""
    Write-Host "Generating WiX file list for $ComponentGroupId"
    Write-Host " SourceDir : $SourceDir"
    Write-Host " OutputFile: $OutputFile"
    Write-Host ""

    if (-not (Test-Path $SourceDir)) {
        Write-Warning "Skipping $ComponentGroupId - publish folder not found: $SourceDir"
        Write-Warning "Please publish the project first, then re-run this script."
        return
    }

    # Ensure output directory exists
    $outDir = Split-Path $OutputFile
    if (-not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Path $outDir | Out-Null
    }

    $files = Get-ChildItem -Path $SourceDir -Recurse -File

    Write-Host "Scanning files in: $SourceDir"
    Write-Host "Found $($files.Count) files." -ForegroundColor Green

    $xml = @()
    $xml += '<?xml version="1.0" encoding="utf-8"?>'
    $xml += '<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">'
    $xml += '  <Fragment>'
    $xml += "    <ComponentGroup Id=`"$ComponentGroupId`" Directory=`"$DirectoryId`">"

    foreach ($file in $files) {

        $relativePath = $file.FullName.Substring($SourceDir.Length).TrimStart('\')

        # Stable hash for component id
        $hashBytes = [Text.Encoding]::UTF8.GetBytes($relativePath)
        $hash = [BitConverter]::ToString(
            (New-Object Security.Cryptography.SHA1Managed).ComputeHash($hashBytes)
        ).Replace('-', '').Substring(0, 8)

        $componentId = "cmp_${ComponentGroupId}_$hash"
        $guid = [Guid]::NewGuid()

        # Determine subdirectory relative to SourceDir root
        $subDir = Split-Path $relativePath -Parent

        if ($subDir) {
            $xml += "      <Component Id=`"$componentId`" Guid=`"$guid`" Subdirectory=`"$subDir`">"
        } else {
            $xml += "      <Component Id=`"$componentId`" Guid=`"$guid`">"
        }

        if ($file.Name -ieq $MainExeName) {
            # Main EXE component (file only)
            $xml += "        <File Id=`"${ComponentGroupId}_MainExe`" Source=`"$WixSourceRoot\$relativePath`" KeyPath=`"yes`" />"
        }
        else {
            $xml += "        <File Source=`"$WixSourceRoot\$relativePath`" />"
        }

        $xml += "      </Component>"
    }

    # ------------------------------------------------------------------
    # SEPARATE SHORTCUT COMPONENT (PER-USER, REGISTRY KEYPATH)
    # ------------------------------------------------------------------
    $shortcutGuid = [Guid]::NewGuid()
    $shortcutComponentId = "cmp_${ComponentGroupId}_StartMenuShortcut"

    $xml += "      <Component Id=`"$shortcutComponentId`" Guid=`"$shortcutGuid`">"
    $xml += "        <RegistryValue"
    $xml += "          Root=`"HKCU`""
    $xml += "          Key=`"Software\MUDDY\$ComponentGroupId`""
    $xml += "          Name=`"Installed`""
    $xml += "          Type=`"integer`""
    $xml += "          Value=`"1`""
    $xml += "          KeyPath=`"yes`" />"

    $iconAttr = if ($IconId) { "`r`n          Icon=`"$IconId`"" } else { "" }
    $xml += "        <Shortcut"
    $xml += "          Id=`"${ComponentGroupId}StartMenuShortcut`""
    $xml += "          Directory=`"ProgramMenuFolder`""
    $xml += "          Name=`"$ShortcutName`""
    $xml += "          Target=`"[$DirectoryId]$MainExeName`""
    $xml += "          WorkingDirectory=`"$DirectoryId`"$iconAttr />"

    $xml += "        <RemoveFolder"
    $xml += "          Id=`"RemoveMUDDYProgramMenuFolder_$ComponentGroupId`""
    $xml += "          Directory=`"ProgramMenuFolder`""
    $xml += "          On=`"uninstall`" />"
    $xml += "      </Component>"

    $xml += "    </ComponentGroup>"
    $xml += "  </Fragment>"
    $xml += "</Wix>"

    ($xml -join "`r`n") + "`r`n" | Set-Content -Path $OutputFile -Encoding UTF8 -NoNewline
}

# --------------------------------------------------------------------------
# MAIN EXECUTION
# --------------------------------------------------------------------------

Write-Host "============================================================"
Write-Host "Starting WiX generation for Client and Server"
Write-Host "============================================================"

# Client
Generate-WixFileList `
    -SourceDir $clientSource `
    -ComponentGroupId "ClientFiles" `
    -DirectoryId "ClientFolder" `
    -WixSourceRoot "..\Client.GUI\publish" `
    -OutputFile $clientOutput `
    -MainExeName "Client.GUI.exe" `
    -ShortcutName "MUDDY Client" `
    -IconId "ClientIcon"

# Server
Generate-WixFileList `
    -SourceDir $serverSource `
    -ComponentGroupId "ServerFiles" `
    -DirectoryId "INSTALLFOLDER" `
    -WixSourceRoot "..\Server.GUI\publish" `
    -OutputFile $serverOutput `
    -MainExeName "Server.GUI.exe" `
    -ShortcutName "MUDDY Server" `
    -IconId "ServerIcon"

Write-Host ""
Write-Host "============================================================"
Write-Host "WiX file generation completed successfully."
Write-Host "============================================================"