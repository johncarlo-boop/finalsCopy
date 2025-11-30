# Render Deployment - Quick Start Guide

## Mabilisang Checklist

### 1. I-prepare ang Code
- ✅ `Program.cs` - Updated na para sa Render PORT
- ✅ `render.yaml` - Configuration file na ready
- ✅ `.renderignore` - Exclude unnecessary files

### 2. I-push sa GitHub
```bash
git init
git add .
git commit -m "Ready for Render deployment"
git remote add origin <your-repo-url>
git push -u origin main
```

### 3. Sa Render Dashboard

#### Create New Web Service
1. Pumunta sa https://dashboard.render.com
2. Click **"New +"** → **"Web Service"**
3. I-connect ang GitHub account
4. Piliin ang repository

#### Build & Start Commands
- **Build Command:** `dotnet publish -c Release -o ./publish`
- **Start Command:** `cd publish && dotnet PropertyInventory.dll`

#### Environment Variables (I-set sa Render Dashboard)

**Required:**
```
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://0.0.0.0:$PORT
Firebase__ProjectId = propertyinventory-d6e4c
Firebase__CredentialsPath = firebase-credentials.json
AppSettings__BaseUrl = https://your-app-name.onrender.com
```

**Email Settings:**
```
EmailSettings__SmtpServer = smtp.gmail.com
EmailSettings__SmtpPort = 587
EmailSettings__SmtpUsername = jeremiahyu050@gmail.com
EmailSettings__SmtpPassword = vwcedwlhgetrrgux
EmailSettings__FromEmail = jeremiahyu050@gmail.com
EmailSettings__FromName = Property Inventory System
```

#### Firebase Credentials File

**Option 1: I-upload bilang Secret File (Recommended)**
1. Sa Render dashboard, pumunta sa **Secrets**
2. I-upload ang `firebase-credentials.json`
3. I-reference ito sa environment variable

**Option 2: I-add sa Repository (Not Recommended for Production)**
- I-add ang `firebase-credentials.json` sa repository
- **Warning:** Hindi secure, pero pwede para sa testing

**Option 3: Base64 Environment Variable**
1. I-convert ang file sa base64:
   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("firebase-credentials.json"))
   ```
2. I-add sa Render environment variables:
   ```
   FIREBASE_CREDENTIALS_BASE64 = <base64-string>
   ```
3. I-update ang `FirebaseService.cs` para i-decode ito (kung kailangan)

### 4. Deploy
1. Click **"Create Web Service"**
2. Hintayin ang build (5-10 minutes)
3. I-check ang logs kung may errors

### 5. I-verify
1. Pumunta sa URL: `https://your-app-name.onrender.com`
2. I-test ang login
3. I-check ang logs sa Render dashboard

## Common Issues

### Build Fails
- I-check kung may missing NuGet packages
- I-verify ang .NET version (dapat .NET 8.0)

### App Won't Start
- I-check ang PORT environment variable
- I-verify ang start command
- I-check ang application logs

### Firebase Error
- I-verify ang `firebase-credentials.json` path
- I-check kung tama ang Firebase Project ID
- I-verify ang file permissions

### 404 Errors
- I-check kung tama ang routing
- I-verify ang static files configuration

## Tips

1. **Free Plan Limitations:**
   - Sleeps after 15 minutes of inactivity
   - Slower build times
   - Consider upgrading kung production use

2. **Environment Variables:**
   - I-use ang double underscore `__` para sa nested settings
   - Example: `Firebase__ProjectId` = `Firebase:ProjectId` sa appsettings.json

3. **Logs:**
   - I-check ang logs sa Render dashboard para sa debugging
   - I-enable ang detailed logging kung may issues

4. **Updates:**
   - Automatic deployment kapag nag-push sa GitHub
   - O manual deploy sa Render dashboard

## Support

Para sa detailed instructions, tingnan ang `RENDER_DEPLOYMENT.md`

---

**Good luck! 🚀**

