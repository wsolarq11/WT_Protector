@echo off
rem WT Launcher one-click launcher: build (Release) + run via npm run start.
cd /d "%~dp0"
npm run start
if errorlevel 1 pause
