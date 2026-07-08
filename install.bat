@echo off
setlocal EnableDelayedExpansion
rem ============================================================================
rem  Nice Stats installer (double-click me)
rem  Runs install.ps1 for you -- no PowerShell knowledge required.
rem  Modifies the game's Assembly-CSharp.dll, so it asks for administrator
rem  rights (game files are often under Program Files).
rem
rem  You can also pass a game path:  install.bat -GameDir "D:\Games\Pillars of Eternity"
rem  With no arguments it auto-detects a Steam install.
rem ============================================================================

rem --- Re-launch elevated if we are not already running as administrator ---
net session >nul 2>&1
if %errorlevel% NEQ 0 (
    echo Requesting administrator rights...
    set "NS_ARGS=%*"
    if defined NS_ARGS (
        powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '!NS_ARGS!' -Verb RunAs"
    ) else (
        powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    )
    exit /b
)

echo.
echo Installing Nice Stats...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
set "NS_EXIT=%errorlevel%"

echo.
if "%NS_EXIT%"=="0" (
    echo Done. You can close this window and launch the game.
) else (
    echo Something went wrong ^(exit code %NS_EXIT%^). See the messages above.
    echo Make sure the game is closed and the folder is correct, then try again.
)
echo.
pause
endlocal
