# Script to run the application on localhost
# This ensures no environment variables interfere with localhost configuration

Write-Host "Starting Property Inventory System on localhost..." -ForegroundColor Green

# Clear any environment variables that might interfere
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:PORT = $null
$env:ASPNETCORE_URLS = $null

# Clear Firebase environment variables (use appsettings.json instead)
$env:FIREBASE_CREDENTIALS_BASE64 = $null
$env:FIREBASE_PROJECT_ID = $null
$env:GOOGLE_APPLICATION_CREDENTIALS = $null

# Clear EmailSettings environment variables (use appsettings.json instead)
$env:EmailSettings__SmtpServer = $null
$env:EmailSettings__SmtpPort = $null
$env:EmailSettings__SmtpUsername = $null
$env:EmailSettings__SmtpPassword = $null
$env:EmailSettings__FromEmail = $null
$env:EmailSettings__FromName = $null

# Clear AppSettings environment variables (use appsettings.json instead)
$env:AppSettings__BaseUrl = $null

Write-Host "Environment cleared. Using appsettings.json configuration." -ForegroundColor Yellow
Write-Host "Access the app at: http://localhost:5000" -ForegroundColor Cyan
Write-Host ""

# Run the application
dotnet run

