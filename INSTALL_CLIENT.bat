@echo off
:: MUDDY Game Client - Easy Installer
:: Double-click this file to install the Game Client

echo.
echo ================================================================================
echo   MUDDY Game Client - Installation
echo   Conestoga College - Project IV - Group 3
echo ================================================================================
echo.
echo This will install the MUDDY Game Client on your computer.
echo.
echo IMPORTANT: You need Administrator rights to install apps.
echo            If prompted, click YES to allow changes.
echo.
echo Press any key to start installation, or close this window to cancel...
pause > nul

echo.
echo Starting installation...
echo.

:: Check if running as admin
net session >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] Running with Administrator privileges
    echo.
    goto :install
) else (
    echo [!] Not running as Administrator
    echo.
    echo Requesting Administrator privileges...
    echo You will see a User Account Control prompt - click YES
    echo.
    
    :: Re-launch as admin
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

:install
set CERT_PATH=%~dp0Client.GUI\AppPackages\Client.GUI_1.0.0.0_x64_Test\Client.GUI_1.0.0.0_x64.cer
set INSTALL_SCRIPT=%~dp0Client.GUI\AppPackages\Client.GUI_1.0.0.0_x64_Test\Install.ps1

:: Step 1 - Install the signing certificate so Windows trusts the package
echo Installing signing certificate...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Import-Certificate -FilePath '%CERT_PATH%' -CertStoreLocation Cert:\LocalMachine\Root" >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] Certificate installed successfully
) else (
    echo [!] Certificate installation failed - installation may not work
)
echo.

:: Step 2 - Run the PowerShell installation script
echo Installing application package...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%INSTALL_SCRIPT%"

if %errorLevel% == 0 (
    echo.
    echo ================================================================================
    echo   Installation Complete!
    echo ================================================================================
    echo.
    echo The MUDDY Game Client has been installed.
    echo.
    echo You can now:
    echo   1. Close this window
    echo   2. Open the Start Menu
    echo   3. Search for "Client.GUI"
    echo   4. Click to launch!
    echo.
    echo To play:
    echo   - Enter the server IP address (or 127.0.0.1 for local)
    echo   - Port: 5000
    echo   - Click Connect
    echo   - Enjoy the game!
    echo.
) else (
    echo.
    echo ================================================================================
    echo   Installation Failed
    echo ================================================================================
    echo.
    echo Something went wrong during installation.
    echo Please check the error messages above.
    echo.
    echo Common solutions:
    echo   - Make sure you clicked YES on the Administrator prompt
    echo   - Check your antivirus isn't blocking the installation
    echo   - Try running this file again
    echo.
)

echo Press any key to exit...
pause > nul
