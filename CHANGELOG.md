# MUDDY Changelog

All notable changes to MUDDY are documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/).

## [0.1.1] - 2025-01-09

### Fixed

- **Installer Path Resolution**: Fixed WiX bootstrapper variable interpolation causing literal `[ProgramFiles64Folder]` display in installer UI. Replaced with concrete path `C:\Program Files\MUDDY\Client\` and `C:\Program Files\MUDDY\Server\`.
- **Directory Duplication**: Eliminated redundant directory nesting that resulted in `Server/Server/` during installation. Restructured Server MSI folder hierarchy to use `INSTALLFOLDER` as the installation target instead of nested subdirectory.
- **Generated WiX Files**: Updated `Generate-WixFiles.ps1` script to reference correct directory IDs (`INSTALLFOLDER` instead of `ServerFolder`), preventing build errors in generated component groups.
- **Start Menu Shortcuts**: Corrected shortcut paths in `ServerFiles.generated.wxs` to use `INSTALLFOLDER` instead of the removed `ServerFolder` reference.

### Changed

- **Installer Branding**: Updated Bundle names to display version prominently (`MUDDY Client v0.1.1` and `MUDDY Server v0.1.1`) for improved user visibility during installation.
- **Version Consistency**: Synchronized all installer versions to 0.1.1 (Client.Installer, Server.Installer, and associated MSI packages).

### Technical Details

- Cleaned up multiple orphaned registry entries left from previous failed installations.
- Verified installer registry entries now create clean, single entries per product.
- Tested fresh installations and upgrades on Windows 10/11.

---

## [0.1.0] - 2025-01-01

### Initial Release

- **Server**: Fully functional text-based MUD server with command pipeline, authentication, and room-based world.
- **Client**: WinUI 3 client with connection management, command history, and colored output.
- **Features**: Login/registration, movement, chat, player status, server administration GUI.
- **Installers**: WiX-based MSI installers with .NET 10 and Windows App Runtime prerequisites.
- **Documentation**: LEARNING.md, CONTRIBUTING.md, CODING_STYLE.md for developers and contributors.

---

## Format

- **Added** for new features
- **Changed** for changes in existing functionality
- **Deprecated** for soon-to-be removed features
- **Removed** for now removed features
- **Fixed** for any bug fixes
- **Security** for any security issue fixes
