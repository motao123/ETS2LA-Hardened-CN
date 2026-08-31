@echo off
rem Pack the Velopack installer using the version from ETS2LA.csproj.
for /f "tokens=2 delims=<>" %%v in ('findstr /c:"<Version>" ETS2LA\ETS2LA.csproj') do set PACK_VERSION=%%v
if "%PACK_VERSION%"=="" (
    echo Could not read version from ETS2LA\ETS2LA.csproj.
    exit /b 1
)
vpk pack --msi --instLocation PerUser --instLicense .\LICENSE.txt --msiBanner .\Assets\Installer\welcome.bmp --msiLogo .\Assets\Installer\banner.bmp --channel "win-release" --packId ETS2LA --packVersion %PACK_VERSION% --packDir .\publish\win-x64 --mainExe ETS2LA.exe -i .\Assets\Installer\favicon.ico -f net10-x64-desktop --packTitle "ETS2LA" --releaseNotes .\ETS2LA\ReleaseNotes.md
