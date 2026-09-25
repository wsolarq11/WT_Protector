@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"

call npm run make
set "RC=%ERRORLEVEL%"
echo.
if not "%RC%"=="0" (
  echo   [x] 构建失败（码 %RC%）
  echo.
  pause
  exit /b %RC%
)

set "ZIPDIR=src\avalonia-launcher\artifacts\friend"
set "ZIP="
for /f "delims=" %%f in ('dir /b /o-d "%ZIPDIR%\WTLauncher-*.zip" 2^>nul') do if not defined ZIP set "ZIP=%%f"
if defined ZIP (
  echo   [OK] %ZIPDIR%\%ZIP%
) else (
  echo   [OK] 完成
)
echo.
pause
exit /b 0