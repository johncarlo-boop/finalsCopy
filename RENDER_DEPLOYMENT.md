# Render Deployment Guide

Gabay para sa pag-deploy ng Property Inventory System sa Render.

## Prerequisites

1. **Render Account**: Mag-sign up sa [render.com](https://render.com)
2. **GitHub Repository**: I-push ang code sa GitHub (kung wala pa)
3. **Firebase Credentials**: Kailangan mo ng `firebase-credentials.json` file

## Step-by-Step Deployment

### Step 1: I-upload ang Firebase Credentials

1. I-copy ang `firebase-credentials.json` file
2. Sa Render dashboard, pumunta sa **Environment** section
3. I-upload ang file o i-convert ito sa base64 at i-set bilang environment variable

**Option A: Upload as Secret File**
- Sa Render, pumunta sa **Secrets** section
- I-upload ang `firebase-credentials.json` bilang secret file
- I-reference ito sa environment variables

**Option B: Base64 Encoding (Recommended)**
```bash
# Sa local machine, i-convert ang file sa base64
# Windows PowerShell:
[Convert]::ToBase64String([IO.File]::ReadAllBytes("firebase-credentials.json"))

# Linux/Mac:
base64 -i firebase-credentials.json
```

### Step 2: I-push ang Code sa GitHub

```bash
git init
git add .
git commit -m "Initial commit for Render deployment"
git branch -M main
git remote add origin <your-github-repo-url>
git push -u origin main
```

### Step 3: Create New Web Service sa Render

1. Pumunta sa [Render Dashboard](https://dashboard.render.com)
2. Click **"New +"** → **"Web Service"**
3. I-connect ang GitHub repository
4. Piliin ang repository at branch

### Step 4: Configure Build Settings

Sa Render dashboard, i-set ang mga sumusunod:

**Build Command:**
```
dotnet publish -c Release -o ./publish
```

**Start Command:**
```
cd publish && dotnet PropertyInventory.dll
```

**Environment:**
```
.NET
```

### Step 5: Set Environment Variables

Sa Render dashboard, pumunta sa **Environment** section at i-add ang mga sumusunod:

#### Firebase Configuration
```
Firebase__ProjectId = propertyinventory-d6e4c
Firebase__CredentialsPath = firebase-credentials.json
```

**Para sa Firebase Credentials:**
- Kung ginamit mo ang Base64 encoding, i-add:
```
FIREBASE_CREDENTIALS_BASE64 = <your-base64-encoded-credentials>
```
- Kailangan mong i-update ang `FirebaseService.cs` para i-decode ito, O
- I-upload ang file bilang secret at i-reference ito

#### Email Settings
```
EmailSettings__SmtpServer = smtp.gmail.com
EmailSettings__SmtpPort = 587
EmailSettings__SmtpUsername = jeremiahyu050@gmail.com
EmailSettings__SmtpPassword = vwcedwlhgetrrgux
EmailSettings__FromEmail = jeremiahyu050@gmail.com
EmailSettings__FromName = Property Inventory System
```

#### App Settings
```
AppSettings__BaseUrl = https://your-app-name.onrender.com
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://0.0.0.0:$PORT
```

**Note:** Palitan ang `your-app-name.onrender.com` ng actual URL ng Render app mo.

### Step 6: Deploy

1. Click **"Create Web Service"**
2. Hintayin ang build at deployment process
3. Makikita mo ang logs sa Render dashboard

### Step 7: Verify Deployment

1. Pumunta sa URL na binigay ng Render (hal: `https://property-inventory.onrender.com`)
2. I-test ang login functionality
3. I-verify ang Firebase connection
4. I-test ang email sending (kung applicable)

## Important Notes

### Firebase Credentials Setup

Kung gusto mong i-automate ang Firebase credentials setup, maaari mong:

1. **I-convert sa Base64 at i-set bilang environment variable:**
   - I-add ang `FIREBASE_CREDENTIALS_BASE64` sa Render
   - I-update ang `FirebaseService.cs` para i-read at i-decode ito

2. **O i-upload bilang file:**
   - I-upload ang `firebase-credentials.json` sa Render secrets
   - I-reference ito sa `Firebase__CredentialsPath`

### HTTPS at Port Configuration

- Render automatically provides HTTPS
- Ang app ay automatic na magli-listen sa PORT environment variable
- Hindi mo na kailangan i-configure ang HTTPS manually

### Database

Kung gumagamit ka ng SQL Server database:
- I-set up ang database connection string sa environment variables
- O gumamit ng Render PostgreSQL database (kung available)

### Static Files

Ang `wwwroot` folder ay automatic na i-serve ng ASP.NET Core, kaya hindi mo na kailangan ng additional configuration.

## Troubleshooting

### Build Fails
- I-check ang build logs sa Render dashboard
- I-verify na lahat ng NuGet packages ay available
- I-check kung may missing dependencies

### App Won't Start
- I-check ang start command
- I-verify ang PORT environment variable
- I-check ang application logs

### Firebase Connection Issues
- I-verify ang Firebase credentials
- I-check kung tama ang path ng credentials file
- I-verify ang Firebase project ID

### Email Not Sending
- I-check ang SMTP settings
- I-verify ang Gmail app password (kung gumagamit ng Gmail)
- I-check ang firewall settings sa Render

## Updating the App

Para i-update ang app:
1. I-push ang changes sa GitHub
2. Render ay automatic na magde-deploy ng bagong version
3. O manual trigger sa Render dashboard

## Cost

- **Free Plan**: May limitations (sleeps after inactivity, slower builds)
- **Starter Plan**: $7/month - No sleep, faster builds
- **Professional Plan**: $25/month - Better performance

## Support

Para sa issues:
1. I-check ang Render logs
2. I-check ang application logs
3. I-verify ang environment variables
4. I-contact ang Render support

---

**Good luck sa deployment! 🚀**

