# PowerShell Script to migrate documentation to GitHub Wiki
# Run this script from the .github-wiki-migration directory

Write-Host "Migrating documentation to GitHub Wiki..." -ForegroundColor Green

# Get the current directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

Write-Host "`nStep 1: Checking if wiki repository exists..." -ForegroundColor Cyan
if (Test-Path "pocx-wallet.wiki") {
    Write-Host "Wiki repository already exists. Pulling latest changes..." -ForegroundColor Yellow
    Set-Location pocx-wallet.wiki
    git pull origin master
    Set-Location ..
} else {
    Write-Host "Cloning wiki repository..." -ForegroundColor Yellow
    git clone https://github.com/ev1ls33d/pocx-wallet.wiki.git
}

Write-Host "`nStep 2: Copying documentation files..." -ForegroundColor Cyan
Copy-Item -Path "*.md" -Destination "pocx-wallet.wiki\" -Force
Write-Host "Files copied successfully." -ForegroundColor Green

Write-Host "`nStep 3: Committing and pushing changes..." -ForegroundColor Cyan
Set-Location pocx-wallet.wiki
git add .
git commit -m "Migrate documentation from wiki/ directory with custom sidebar and footer"
git push origin master

Write-Host "`nMigration complete!" -ForegroundColor Green
Write-Host "Visit https://github.com/ev1ls33d/pocx-wallet/wiki to view your documentation." -ForegroundColor Cyan
Write-Host ""

Read-Host "Press Enter to exit"
