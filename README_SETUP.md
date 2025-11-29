# Setup Instructions

## Required Configuration Files

This project requires certain configuration files that are not included in the repository for security reasons.

### 1. Firebase Credentials

Copy `firebase-credentials.template.json` to `firebase-credentials.json` and fill in your Firebase service account credentials.

### 2. App Settings

Copy `appsettings.template.json` to `appsettings.json` and fill in:
- Database connection string
- Email SMTP settings
- Firebase project ID

### 3. Development Settings

Copy `appsettings.template.json` to `appsettings.Development.json` for development-specific settings.

### 4. Profile Pictures Directory

Create the directory `wwwroot/profile-pictures/` for storing user profile pictures.

## Getting Started

1. Clone the repository
2. Set up the configuration files as described above
3. Run `dotnet restore`
4. Run `dotnet run`

**IMPORTANT:** Never commit the actual `firebase-credentials.json`, `appsettings.json`, or `appsettings.Development.json` files to the repository!
