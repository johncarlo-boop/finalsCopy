# PowerShell script to push to GitHub
# This script will help you push your code to GitHub using a Personal Access Token

Write-Host "=== Push to GitHub ===" -ForegroundColor Green
Write-Host ""

# Check if we're in a git repository
if (-not (Test-Path .git)) {
    Write-Host "Error: Not a git repository!" -ForegroundColor Red
    exit 1
}

# Get Personal Access Token
Write-Host "To push to GitHub, you need a Personal Access Token." -ForegroundColor Yellow
Write-Host "If you don't have one, create it here: https://github.com/settings/tokens" -ForegroundColor Yellow
Write-Host ""
$token = Read-Host "Enter your GitHub Personal Access Token (or press Enter to use existing credentials)"

if ($token) {
    # Use token in URL
    $remoteUrl = "https://jeremiah-yu:$token@github.com/jeremiah-yu/finals.git"
    git remote set-url origin $remoteUrl
    Write-Host "Remote URL updated with token." -ForegroundColor Green
}

# Try to push
Write-Host ""
Write-Host "Pushing to GitHub..." -ForegroundColor Cyan
git push -u origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Success! Code pushed to GitHub!" -ForegroundColor Green
    Write-Host "Repository: https://github.com/jeremiah-yu/finals" -ForegroundColor Cyan
    
    # Remove token from URL for security
    if ($token) {
        git remote set-url origin https://github.com/jeremiah-yu/finals.git
        Write-Host "Remote URL cleaned (token removed for security)." -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "Push failed. Please check your credentials." -ForegroundColor Red
    Write-Host "Make sure you have a valid Personal Access Token with 'repo' permissions." -ForegroundColor Yellow
}









