@echo off
rem ================================================
rem  SafeKitchen git backup - double click to run
rem  First run: creates repo. After: commits changes.
rem ================================================
cd /d "%~dp0"

where git >nul 2>nul
if errorlevel 1 (
    echo [ERROR] git not installed. Install from https://git-scm.com
    pause
    exit /b 1
)

if not exist ".git" (
    echo [INIT] creating local repository...
    git init -b main
    git config user.name "donghyun"
    git config user.email "edwin.dkim@gmail.com"
)

echo [ADD] staging changes (first run may take a few minutes)...
git add -A

git diff --cached --quiet
if not errorlevel 1 (
    echo [SKIP] nothing changed since last backup.
    pause
    exit /b 0
)

git commit -m "backup %date% %time%"
echo.
echo [OK] backup commit created:
git log --oneline -3
echo.
pause
