#!/usr/bin/env pwsh

Set-Location 'C:\Users\Ryzen\source\repos\ev1ls33d\pocx-wallet'

Write-Host "Pulling latest changes..." -ForegroundColor Cyan
git pull

Write-Host "Updating submodules to latest heads..." -ForegroundColor Cyan
git submodule update --remote --merge

Write-Host "Checking git status..." -ForegroundColor Cyan
git status

Write-Host "Staging changes..." -ForegroundColor Cyan
git add -A

Write-Host "Committing changes..." -ForegroundColor Cyan
$hasChanges = git status --porcelain
if ($hasChanges) {
    git commit -m "Update submodules to latest heads" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
    Write-Host "Pushing changes..." -ForegroundColor Cyan
    git push
    Write-Host "✓ Changes pushed successfully!" -ForegroundColor Green
} else {
    Write-Host "No changes to commit." -ForegroundColor Yellow
}

Write-Host "Final status:" -ForegroundColor Cyan
git status
