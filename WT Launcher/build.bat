@echo off
chcp 65001 >nul
set "csc=%windir%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
echo Compiling WT Launcher...
"%csc%" /nologo /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /out:"WT Launcher.exe" Program.cs
if %errorlevel% equ 0 (
    echo OK: WT Launcher.exe
) else (
    echo FAILED
)
pause
