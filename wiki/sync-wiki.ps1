#!/usr/bin/env pwsh
# Sync wiki content from main repository to wiki repository

Write-Host "Syncing wiki content..." -ForegroundColor Green

# Clone wiki repository
Write-Host "Cloning wiki repository..." -ForegroundColor Cyan
git clone https://github.com/ev1ls33d/pocx-wallet.wiki.git wiki-repo

if (-not $?) {
    Write-Host "Failed to clone wiki repository" -ForegroundColor Red
    exit 1
}

# Copy wiki files
Write-Host "Copying wiki files..." -ForegroundColor Cyan
Copy-Item -Path wiki/* -Destination wiki-repo/ -Recurse -Force

# Commit and push changes
Set-Location wiki-repo
git add .
git commit -m "Update wiki from main repository"

if ($?) {
    Write-Host "Pushing changes..." -ForegroundColor Cyan
    git push
    
    if ($?) {
        Write-Host "Wiki synced successfully!" -ForegroundColor Green
    } else {
        Write-Host "Failed to push changes" -ForegroundColor Red
    }
} else {
    Write-Host "No changes to commit" -ForegroundColor Yellow
}

# Cleanup
Set-Location ..
Remove-Item -Recurse -Force wiki-repo

Write-Host "Done!" -ForegroundColor Green
