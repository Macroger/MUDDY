<#
================================================================================
Generate-WixFiles.ps1

PURPOSE:
--------
This script generates WiX (.wxs) source files that describe all files
contained in the Client and Server WinUI publish directories.

Because MSI installers require a *static file list at build time*,
and because WiX v7 no longer provides free automatic harvesting (heat.exe),
we generate these file lists via scripting instead.

The generated files are then:
  - Included in the WiX MSI project
  - Referenced by MSI Features (Client / Server selection)

This script is intentionally:
  - Hard-coded
  - Explicit
  - Verbose

to maximize clarity and reliability.

================================================================================
#>

[CmdletBinding()]
param()

# ------------------------------------------------------------------------------
# CONFIGURATION SECTION
# ------------------------------------------------------------------------------
# These paths are intentionally hard-coded for now.
# They reflect the current solution layout and are not expected to change often.
# If they ever do, this is the *only* place that needs updating.
# ------------------------------------------------------------------------------



Write-Host "Initializing configuration..." -ForegroundColor Cyan


# The script lives at the repository root
$repoRoot = $PSScriptRoot

# Child directories
$installerDir = Join-Path $repoRoot "MUDDY.Installer"
$clientSource = Join-Path $repoRoot "Client.GUI\publish"
$serverSource = Join-Path $repoRoot "Server.GUI\publish"


# Output files (written into MUDDY.Installer)
$clientOutput = Join-Path $installerDir "ClientFiles.generated.wxs"
$serverOutput = Join-Path $installerDir "ServerFiles.generated.wxs"

Write-Host "Repository root   : $repoRoot"
Write-Host "Installer dir     : $installerDir"
Write-Host "Client publish dir: $clientSource"
Write-Host "Server publish dir: $serverSource"



# ------------------------------------------------------------------------------
# HELPER FUNCTION
# ------------------------------------------------------------------------------
# Generates a WiX ComponentGroup (.wxs) from a directory of files.
#
# IMPORTANT MSI NOTES:
# - MSI installs FILES via COMPONENTS
# - Each file should belong to exactly one component
# - No wildcards are allowed in MSI authoring
#
# Therefore:
# - We generate ONE Component per file
# - Each Component gets a GUID
# - All Components are grouped into a ComponentGroup
# ------------------------------------------------------------------------------



function Generate-WixFileList {
    param (
        [string]$SourceDir,          # Directory containing published files
        [string]$ComponentGroupId,   # ComponentGroup Id used by MSI Feature
        [string]$DirectoryId,        # WiX Directory Id (ClientFolder / ServerFolder)
        [string]$BindPathName,       # Optional: Path to bind the files to
        [string]$OutputFile,         # Target .wxs file path        
        [string]$MainExeName,        # NEW (e.g., Client.GUI.exe)
        [string]$ShortcutName        # NEW (e.g., MUDDY Client)

    )

    Write-Host ""
    Write-Host "------------------------------------------------------------"
    Write-Host "Generating WiX file list..."
    Write-Host " Source Directory : $SourceDir"
    Write-Host " Component Group  : $ComponentGroupId"
    Write-Host " Target Directory : $DirectoryId"
    Write-Host " Output File      : $OutputFile"
    Write-Host "------------------------------------------------------------"
    Write-Host ""

    # Validate that the source directory exists
    if (-not (Test-Path $SourceDir)) {
        throw "ERROR: Source directory does not exist: $SourceDir"
    }

    # Ensure the installer output directory exists
    $outputDir = Split-Path $OutputFile
    if (-not (Test-Path $outputDir)) {
        Write-Host "Output directory does not exist. Creating it..."
        New-Item -ItemType Directory -Path $outputDir | Out-Null
    }

    # Enumerate all files recursively
    Write-Host "Scanning files..."
    $files = Get-ChildItem -Path $SourceDir -Recurse -File

    Write-Host "Found $($files.Count) files."

    # Start building the WiX XML content
    $xml = @()
    $xml += '<?xml version="1.0" encoding="utf-8"?>'
    $xml += '<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">'
    $xml += '  <Fragment>'
    $xml += "    <ComponentGroup Id=`"$ComponentGroupId`" Directory=`"$DirectoryId`">"

    foreach ($file in $files) {

        # Create a stable, WiX-safe identifier from the relative file path
        $relativePath = $file.FullName.Substring($SourceDir.Length).TrimStart('\')
        
        # Create a short, deterministic hash from the relative file path
        $hashBytes = [System.Text.Encoding]::UTF8.GetBytes($relativePath)
        $hash = [System.BitConverter]::ToString(
            (New-Object System.Security.Cryptography.SHA1Managed).ComputeHash($hashBytes)
        ).Replace("-", "").Substring(0, 8)

        $componentId = "cmp_${ComponentGroupId}_$hash"


        # Generate a new GUID for this component
        # For this project, random GUIDs are sufficient and acceptable
        $guid = [Guid]::NewGuid()

        Write-Verbose "  + $relativePath"

        $xml += "      <Component Id=`"$componentId`" Guid=`"$guid`">"
        
        if ($file.Name -ieq $MainExeName) {

            # Main EXE: owns the shortcut
            $xml += "        <File Id=`"fil_MainExe`" Source=`"$BindPathName\$relativePath`" KeyPath=`"yes`">"
            $xml += "          <Shortcut"
            $xml += "            Id=`"${ComponentGroupId}StartMenuShortcut`""
            $xml += "            Directory=`"MUDDYProgramMenuFolder`""
            $xml += "            Name=`"$ShortcutName`""
            $xml += "            WorkingDirectory=`"$DirectoryId`" />"
            $xml += "        </File>"
            $xml += "        <RemoveFolder"
            $xml += "          Id=`"RemoveMUDDYProgramMenuFolder_$ComponentGroupId`""
            $xml += "          Directory=`"MUDDYProgramMenuFolder`""
            $xml += "          On=`"uninstall`" />"

        } else {

            # Normal file
            $xml += "        <File Source=`"$BindPathName\$relativePath`" />"
        }

        $xml += "      </Component>"
    }

    $xml += '    </ComponentGroup>'
    $xml += '  </Fragment>'
    $xml += '</Wix>'

    # Write the output file to disk
    Write-Host "Writing output file..."
    $xml | Out-File -FilePath $OutputFile -Encoding UTF8

    Write-Host "Generation complete."
}


# ------------------------------------------------------------------------------
# MAIN EXECUTION
# ------------------------------------------------------------------------------

Write-Host ""
Write-Host "============================================================"
Write-Host "Starting WiX file generation for Client and Server"
Write-Host "============================================================"

# Generate Client WiX file list
Generate-WixFileList `
    -SourceDir $clientSource `
    -ComponentGroupId "ClientFiles" `
    -DirectoryId "ClientFolder" `
    -BindPathName "ClientGuiPublish" `
    -OutputFile $clientOutput `
    -MainExeName "Client.GUI.exe" `
    -ShortcutName "MUDDY Client"


# Generate Server WiX file list
Generate-WixFileList `
    -SourceDir $serverSource `
    -ComponentGroupId "ServerFiles" `
    -DirectoryId "ServerFolder" `
    -BindPathName "ServerGuiPublish" `
    -OutputFile $serverOutput `
    -MainExeName "Server.GUI.exe" `
    -ShortcutName "MUDDY Server"


Write-Host ""
Write-Host "============================================================"
Write-Host "All WiX file lists generated successfully."
Write-Host "============================================================"