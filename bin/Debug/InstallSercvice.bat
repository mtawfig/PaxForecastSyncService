REM The following directory is for .NET v4.0.30319
set DOTNETFX4=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set PATH=%PATH%;%DOTNETFX4%
set CURDIR=%~dp0

echo %PATH%
echo Installing SvcXovisPaxForecastFeeding Windows Service...
echo ---------------------------------------------------
InstallUtil %CURDIR%\XovisPaxForecastFeedWinSvc.exe
echo ---------------------------------------------------
echo Done.
pause