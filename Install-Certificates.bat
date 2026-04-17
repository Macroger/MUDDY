@echo off
:: MUDDY - Install Signing Certificates
:: Run this once before installing the Client or Server package.

echo.
echo ================================================================================
echo   MUDDY - Install Signing Certificates
echo   Conestoga College - Project IV - Group 3
echo ================================================================================
echo.

:: Must run as admin to write to LocalMachine\Root
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo Requesting Administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo [OK] Running with Administrator privileges
echo.

set CLIENT_CERT=%~dp0Client.GUI\AppPackages\Client.GUI_1.0.0.0_x64_Test\Client.GUI_1.0.0.0_x64.cer
set SERVER_CERT=%~dp0Server.GUI\AppPackages\Server.GUI_1.0.0.0_x64_Test\Server.GUI_1.0.0.0_x64.cer

:: Install Client certificate
if exist "%CLIENT_CERT%" (
    echo Installing Client certificate...
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Import-Certificate -FilePath '%CLIENT_CERT%' -CertStoreLocation Cert:\LocalMachine\Root" >nul 2>&1
    if %errorLevel% == 0 (
        echo [OK] Client certificate installed
    ) else (
        echo [!] Client certificate installation failed
    )
) else (
    echo [!] Client certificate not found - skipping
)

:: Install Server certificate
if exist "%SERVER_CERT%" (
    echo Installing Server certificate...
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Import-Certificate -FilePath '%SERVER_CERT%' -CertStoreLocation Cert:\LocalMachine\Root" >nul 2>&1
    if %errorLevel% == 0 (
        echo [OK] Server certificate installed
    ) else (
        echo [!] Server certificate installation failed
    )
) else (
    echo [!] Server certificate not found - skipping
)

echo.
echo Done. You can now run the Client or Server installer.
echo.
pause
