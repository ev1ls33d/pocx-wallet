# wiki/sync-wiki.ps1
# Syncs local wiki files to GitHub wiki repository

param(
    [string]$WikiRepoUrl = "https://github.com/ev1ls33d/pocx-wallet.wiki.git",
    [string]$CommitMessage = "Sync wiki from main repo"
)

$ErrorActionPreference = "Stop"

# Get the script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$WikiSourceDir = $ScriptDir
$TempDir = Join-Path $env:TEMP "pocx-wallet-wiki-$(Get-Date -Format 'yyyyMMddHHmmss')"

Write-Host "=== PoCX Wallet Wiki Sync ===" -ForegroundColor Cyan
Write-Host ""

try {
    # Clone wiki repo
    Write-Host "Cloning wiki repository..." -ForegroundColor Yellow
    if (Test-Path $TempDir) { 
        Remove-Item -Recurse -Force $TempDir 
    }
    
    git clone $WikiRepoUrl $TempDir 2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to clone wiki repository. Make sure you have access and the wiki exists." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ Wiki repository cloned" -ForegroundColor Green
    Write-Host ""

    # Copy updated files
    Write-Host "Copying wiki files..." -ForegroundColor Yellow
    $MarkdownFiles = Get-ChildItem -Path $WikiSourceDir -Filter "*.md"
    
    foreach ($File in $MarkdownFiles) {
        Copy-Item -Path $File.FullName -Destination $TempDir -Force
        Write-Host "  Copied: $($File.Name)" -ForegroundColor Gray
    }
    
    Write-Host "✓ Files copied" -ForegroundColor Green
    Write-Host ""

    # Commit and push
    Write-Host "Committing changes..." -ForegroundColor Yellow
    Push-Location $TempDir
    
    try {
        git add -A
        
        # Check if there are changes to commit
        $Status = git status --porcelain
        
        if ([string]::IsNullOrEmpty($Status)) {
            Write-Host "No changes to commit" -ForegroundColor Yellow
            Pop-Location
            Remove-Item -Recurse -Force $TempDir
            exit 0
        }
        
        git commit -m $CommitMessage 2>&1 | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to commit changes" -ForegroundColor Red
            Pop-Location
            exit 1
        }
        
        Write-Host "✓ Changes committed" -ForegroundColor Green
        Write-Host ""
        
        # Detect default branch
        Write-Host "Detecting default branch..." -ForegroundColor Yellow
        $DefaultBranch = git symbolic-ref refs/remotes/origin/HEAD 2>$null | ForEach-Object { $_ -replace '^refs/remotes/origin/', '' }
        
        if ([string]::IsNullOrEmpty($DefaultBranch)) {
            # Fallback to common branch names
            $CommonBranches = @("main", "master")
            foreach ($Branch in $CommonBranches) {
                $BranchExists = git show-ref --verify --quiet "refs/remotes/origin/$Branch"
                if ($LASTEXITCODE -eq 0) {
                    $DefaultBranch = $Branch
                    break
                }
            }
        }
        
        if ([string]::IsNullOrEmpty($DefaultBranch)) {
            Write-Host "Could not detect default branch, defaulting to 'master'" -ForegroundColor Yellow
            $DefaultBranch = "master"
        }
        
        Write-Host "  Using branch: $DefaultBranch" -ForegroundColor Gray
        Write-Host ""
        
        Write-Host "Pushing to wiki repository..." -ForegroundColor Yellow
        git push origin $DefaultBranch 2>&1 | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to push changes. Make sure you have write access to the wiki." -ForegroundColor Red
            Pop-Location
            exit 1
        }
        
        Write-Host "✓ Changes pushed" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }

    # Cleanup
    Write-Host ""
    Write-Host "Cleaning up..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $TempDir
    Write-Host "✓ Cleanup complete" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "=== Wiki synced successfully! ===" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    
    if (Test-Path $TempDir) {
        Remove-Item -Recurse -Force $TempDir
    }
    
    exit 1
}
