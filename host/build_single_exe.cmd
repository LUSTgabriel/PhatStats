@echo off
setlocal
cd /d "%~dp0"

echo ============================================================
echo PhatStats Single EXE Build v1.3.1
echo ============================================================
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_single_exe.ps1"
set "BUILD_RC=%ERRORLEVEL%"

echo.
echo ============================================================
if not "%BUILD_RC%"=="0" (
    echo BUILD FAILED
    echo Exit code: %BUILD_RC%
) else (
    echo BUILD SUCCEEDED
    echo Output: dist\single-exe\PhatStats.exe
)
echo ============================================================
echo.
echo The build window will stay open so the complete log can be reviewed.
echo Press any key when you are finished reviewing the log...
pause >nul

endlocal & exit /b %BUILD_RC%
