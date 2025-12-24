@echo off
REM Script to push wiki documentation to GitHub Wiki
REM This script should be run with appropriate GitHub credentials

echo Migrating documentation to GitHub Wiki...

cd /d %~dp0

echo.
echo Step 1: Checking if wiki repository exists...
if exist "pocx-wallet.wiki" (
    echo Wiki repository already exists. Pulling latest changes...
    cd pocx-wallet.wiki
    git pull origin master
    cd ..
) else (
    echo Cloning wiki repository...
    git clone https://github.com/ev1ls33d/pocx-wallet.wiki.git
)

echo.
echo Step 2: Copying documentation files...
xcopy /Y *.md pocx-wallet.wiki\

echo.
echo Step 3: Committing and pushing changes...
cd pocx-wallet.wiki
git add .
git commit -m "Migrate documentation from wiki/ directory with custom sidebar and footer"
git push origin master

echo.
echo Migration complete! Visit https://github.com/ev1ls33d/pocx-wallet/wiki to view your documentation.
echo.
pause
