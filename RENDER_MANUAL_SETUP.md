# Render Manual Setup - Step by Step

## Problem: "dotnet: command not found" Error

Kung nakakakuha ka pa rin ng error na ito, kailangan mong i-configure manually sa Render dashboard.

## Solution: Manual Configuration

### Step 1: Delete/Recreate the Service (Kung may existing service)

1. Pumunta sa Render dashboard
2. I-delete ang existing service (kung may error)
3. O i-update ang existing service settings

### Step 2: Create New Web Service

1. Pumunta sa https://dashboard.render.com
2. Click **"New +"** → **"Web Service"**
3. I-connect ang GitHub repository: `johncarlo-boop/finalsCopy`
4. Piliin ang branch: `main`

### Step 3: Configure Service Settings

**IMPORTANT:** I-configure manually sa dashboard, hindi sa render.yaml:

#### Basic Settings:
- **Name:** `property-inventory` (o kahit anong name)
- **Region:** Piliin ang pinakamalapit sa iyo
- **Branch:** `main`
- **Root Directory:** `finals _NoSQL` (kung nasa subfolder ang project)

#### Build & Deploy Settings:

**Environment:**
```
Docker
```

**Dockerfile Path:**
```
Dockerfile
```

**O kung nasa subfolder:**
```
finals _NoSQL/Dockerfile
```

**Docker Context:**
```
finals _NoSQL
```

**O kung nasa root:**
```
.
```

### Step 4: Set Environment Variables

Sa **Environment** tab, i-add ang mga sumusunod:

#### Required Variables:
```
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://0.0.0.0:$PORT
PORT = 8080
```

#### Firebase Configuration:
```
Firebase__ProjectId = propertyinventory-d6e4c
Firebase__CredentialsPath = firebase-credentials.json
```

#### Email Settings:
```
EmailSettings__SmtpServer = smtp.gmail.com
EmailSettings__SmtpPort = 587
EmailSettings__SmtpUsername = jeremiahyu050@gmail.com
EmailSettings__SmtpPassword = vwcedwlhgetrrgux
EmailSettings__FromEmail = jeremiahyu050@gmail.com
EmailSettings__FromName = Property Inventory System
```

#### App Settings:
```
AppSettings__BaseUrl = https://your-app-name.onrender.com
```
(Palitan ang `your-app-name` ng actual app name mo)

### Step 5: Firebase Credentials Setup

**Option 1: Base64 Environment Variable (Recommended)**

1. Sa local machine, i-convert ang file:
   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("firebase-credentials.json"))
   ```

2. I-copy ang base64 string

3. Sa Render, i-add ang environment variable:
   ```
   FIREBASE_CREDENTIALS_BASE64 = <paste-your-base64-string>
   ```

4. I-update ang `FirebaseService.cs` para i-read ito (kung kailangan)

**Option 2: I-add sa Repository (Para sa Testing)**

1. I-temporarily i-add ang `firebase-credentials.json` sa repository
2. **Warning:** Hindi secure, pero pwede para sa testing
3. I-remove ito pagkatapos ng successful deployment

### Step 6: Deploy

1. Click **"Create Web Service"** o **"Save Changes"**
2. Hintayin ang build process (5-15 minutes)
3. I-monitor ang build logs

### Step 7: Verify Deployment

1. I-check ang build logs kung successful
2. I-test ang application URL
3. I-check ang application logs kung may runtime errors

## Alternative: If Docker Still Doesn't Work

Kung hindi pa rin gumana ang Docker, i-try ang **Native .NET** approach:

### Configuration:
- **Environment:** `dotnet` o `Node` (Render might auto-detect)
- **Build Command:** 
  ```
  cd "finals _NoSQL" && dotnet restore && dotnet publish -c Release -o ./publish
  ```
- **Start Command:**
  ```
  cd "finals _NoSQL/publish" && dotnet PropertyInventory.dll
  ```

**Pero kung may "dotnet: command not found" error pa rin, ang free plan ng Render ay hindi supported ang .NET 8.0 natively.**

## Troubleshooting

### Error: "Dockerfile not found"
**Solution:** 
- I-verify ang Dockerfile path
- I-check kung nasa tamang directory ang Dockerfile
- I-try ang full path: `finals _NoSQL/Dockerfile`

### Error: "Build failed"
**Solution:**
- I-check ang build logs para sa specific error
- I-verify na tama ang file paths sa Dockerfile
- I-check kung may missing dependencies

### Error: "App won't start"
**Solution:**
- I-verify ang PORT environment variable
- I-check ang application logs
- I-verify ang Firebase credentials

## Important Notes

1. **Free Plan Limitations:**
   - Render free plan ay may limitations
   - Baka kailangan ng paid plan para sa .NET 8.0
   - O i-consider ang ibang hosting (Railway, Fly.io, etc.)

2. **Root Directory:**
   - Kung ang project ay nasa `finals _NoSQL` subfolder, i-set ang Root Directory
   - O i-move ang files sa root ng repository

3. **Dockerfile Location:**
   - Ang Dockerfile ay dapat nasa same directory ng `.csproj` file
   - O i-update ang Dockerfile paths

## Next Steps

1. I-follow ang manual setup steps above
2. I-monitor ang build logs
3. I-test ang deployed application
4. I-update ang environment variables kung kailangan

---

**Kung may error pa rin, i-share ang complete build logs para mas matulungan kita!**

