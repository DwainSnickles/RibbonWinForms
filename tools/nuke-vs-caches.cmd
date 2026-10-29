@echo off
setlocal
echo ======================================================
echo   RibbonLib - Visual Studio designer cache reset
echo ======================================================
echo.
echo Closes Visual Studio, then deletes the caches that keep a stale ribbon
echo designer, or a stale RibbonLib entry in the toolbox, alive:
echo.
echo   ComponentModelCache                     - per-type component metadata
echo   toolbox*.tbd                            - toolbox state
echo   ToolboxItemDiscoveryCache*.dat          - discovered toolbox items
echo   WinFormsDesigner                        - design server shadow copies
echo.
echo Your custom toolbox tabs are removed, and the next start is slower.
echo Nothing here touches source files, project files or NuGet credentials.
echo.
echo Save your work first: Visual Studio is closed, and force-closed if it has not
echo exited after 8 seconds. Nothing unsaved can be recovered afterwards.
echo.

if /i "%~1"=="/y" goto :run
set "RL_ANSWER="
set /p "RL_ANSWER=Continue? [y/N] "
if /i not "%RL_ANSWER%"=="y" (
    echo Cancelled.
    exit /b 1
)

:run
set "VSROOT=%LOCALAPPDATA%\Microsoft\VisualStudio"
rem RIBBONLIB_VSROOT is a test hook: point it at a scratch tree to try this out safely.
if defined RIBBONLIB_VSROOT set "VSROOT=%RIBBONLIB_VSROOT%"

echo.
echo Stopping Visual Studio...
taskkill /im devenv.exe >nul 2>&1
timeout.exe /t 8 /nobreak >nul
tasklist /fi "IMAGENAME eq devenv.exe" 2>nul | find /i "devenv.exe" >nul
if not errorlevel 1 (
    echo   did not close on its own, forcing it
    taskkill /f /im devenv.exe >nul 2>&1
    timeout.exe /t 2 /nobreak >nul
)

if not exist "%VSROOT%" (
    echo No Visual Studio settings folder at "%VSROOT%". Nothing to do.
    exit /b 1
)

for /d %%I in ("%VSROOT%\*") do (
    echo.
    echo Instance %%~nxI
    if exist "%%~I\ComponentModelCache" (
        rmdir /s /q "%%~I\ComponentModelCache" 2>nul
        echo   removed ComponentModelCache
    )
    for %%F in ("%%~I\toolbox.tbd" "%%~I\toolboxIndex.tbd" "%%~I\toolboxIndex_reset.tbd") do (
        if exist "%%~F" (
            del /f /q "%%~F" >nul 2>&1
            echo   removed %%~nxF
        )
    )
    for %%F in ("%%~I\ToolboxItemDiscoveryCache*.dat") do (
        if exist "%%~F" (
            del /f /q "%%~F" >nul 2>&1
            echo   removed %%~nxF
        )
    )
    if exist "%%~I\WinFormsDesigner" (
        rmdir /s /q "%%~I\WinFormsDesigner" 2>nul
        echo   removed WinFormsDesigner shadow copies
    )
)

echo.
echo Done. If the designer was showing an old build, also delete bin and obj in the
echo project that consumes RibbonLib, then start Visual Studio again.
endlocal
exit /b 0
