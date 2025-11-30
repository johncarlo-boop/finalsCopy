# Render Deployment - Docker Setup

## Problem Solved

Ang error na `dotnet: command not found` ay naayos na gamit ang **Docker deployment** sa Render.

## What Changed

### 1. Created `Dockerfile`
Ginawa ang Dockerfile na gumagamit ng official .NET 8.0 images para sa reliable deployment.

### 2. Updated `render.yaml`
- Changed from `env: dotnet` to `env: docker`
- Removed build/start commands (Dockerfile na ang magha-handle)
- Added `dockerfilePath: ./Dockerfile`

### 3. Created `.dockerignore`
Para i-optimize ang Docker build at i-exclude ang unnecessary files.

## How to Deploy

### Step 1: I-commit at i-push ang changes

```powershell
git add Dockerfile .dockerignore render.yaml
git commit -m "Add Dockerfile for Render deployment"
git push origin main
```

### Step 2: Sa Render Dashboard

**Option A: Using render.yaml (Automatic)**
1. I-push ang code sa GitHub
2. Render ay automatic na magde-deploy gamit ang `render.yaml`

**Option B: Manual Configuration**
1. Pumunta sa Render dashboard
2. I-create o i-update ang Web Service
3. I-set ang **Environment**: `Docker`
4. I-set ang **Dockerfile Path**: `./Dockerfile`
5. I-set ang **Root Directory**: (leave blank o `finals _NoSQL` kung nasa subfolder)

### Step 3: Set Environment Variables

Sa Render dashboard, i-set ang mga environment variables:

**Required:**
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
PORT=8080
```

**Firebase:**
```
Firebase__ProjectId=propertyinventory-d6e4c
Firebase__CredentialsPath=firebase-credentials.json
```

**Email Settings:**
```
EmailSettings__SmtpServer=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__SmtpUsername=jeremiahyu050@gmail.com
EmailSettings__SmtpPassword=vwcedwlhgetrrgux
EmailSettings__FromEmail=jeremiahyu050@gmail.com
EmailSettings__FromName=Property Inventory System
```

**App Settings:**
```
AppSettings__BaseUrl=https://your-app-name.onrender.com
```

### Step 4: Firebase Credentials

Para sa Firebase credentials, may dalawang options:

**Option 1: Base64 Environment Variable (Recommended)**
1. I-convert ang `firebase-credentials.json` sa base64:
   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("firebase-credentials.json"))
   ```
2. I-add sa Render environment variables:
   ```
   FIREBASE_CREDENTIALS_BASE64=<your-base64-string>
   ```
3. I-update ang `FirebaseService.cs` para i-decode ito (kung kailangan)

**Option 2: I-upload bilang Secret File**
1. Sa Render dashboard, pumunta sa **Secrets**
2. I-upload ang `firebase-credentials.json`
3. I-reference ito sa environment variable

**Option 3: I-add sa Repository (Not Recommended)**
- I-add ang `firebase-credentials.json` sa repository
- **Warning:** Hindi secure, pero pwede para sa testing

## Docker Build Process

Ang Dockerfile ay:
1. Gumagamit ng .NET 8.0 SDK para sa build
2. I-restore ang NuGet packages
3. I-build ang project
4. I-publish ang app
5. Gumagamit ng .NET 8.0 runtime para sa production
6. I-expose ang port 8080 (Render ay magse-set ng PORT env var)

## Verification

Pagkatapos ng deployment:
1. I-check ang build logs sa Render dashboard
2. I-verify na successful ang build
3. I-test ang application URL
4. I-check ang application logs kung may errors

## Troubleshooting

### Build Fails
- I-check ang Dockerfile syntax
- I-verify na tama ang file paths
- I-check ang build logs para sa specific errors

### App Won't Start
- I-verify ang PORT environment variable
- I-check ang application logs
- I-verify ang Firebase credentials

### Firebase Connection Issues
- I-check kung tama ang credentials path
- I-verify ang Firebase project ID
- I-check ang file permissions

## Advantages of Docker Deployment

✅ **Reliable**: Hindi na dependent sa Render's .NET detection
✅ **Consistent**: Same environment sa local at production
✅ **Flexible**: Pwede mong i-customize ang build process
✅ **Portable**: Pwede mong i-deploy sa ibang platforms (AWS, Azure, etc.)

## Next Steps

1. I-commit at i-push ang changes
2. I-trigger ang deployment sa Render
3. I-monitor ang build logs
4. I-test ang deployed application

---

**Note:** Ang Docker deployment ay mas reliable kaysa sa direct .NET deployment sa Render, especially sa free plan.

