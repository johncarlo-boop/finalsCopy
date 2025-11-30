# Render Deployment - Complete Guide

Complete guide para sa pag-deploy ng Property Inventory System sa Render.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Prerequisites](#prerequisites)
3. [Step-by-Step Deployment](#step-by-step-deployment)
4. [Environment Variables](#environment-variables)
5. [Firebase Setup](#firebase-setup)
6. [Email Configuration](#email-configuration)
7. [Account Request & Access](#account-request--access)
8. [Troubleshooting](#troubleshooting)
9. [Common Errors & Solutions](#common-errors--solutions)

---

## Quick Start

### Mabilisang Checklist

1. ✅ I-push ang code sa GitHub
2. ✅ I-create ang Web Service sa Render
3. ✅ I-set ang Environment = `Docker`
4. ✅ I-set ang Dockerfile Path = `Dockerfile`
5. ✅ I-set ang Root Directory = (empty)
6. ✅ I-set ang lahat ng environment variables
7. ✅ I-deploy at i-test

---

## Prerequisites

1. **Render Account**: Mag-sign up sa [render.com](https://render.com)
2. **GitHub Repository**: I-push ang code sa GitHub
3. **Firebase Credentials**: Kailangan mo ng `firebase-credentials.json` file
4. **Gmail App Password**: Para sa email sending

---

## Step-by-Step Deployment

### Step 1: I-push ang Code sa GitHub

```bash
git init
git add .
git commit -m "Ready for Render deployment"
git branch -M main
git remote add origin <your-github-repo-url>
git push -u origin main
```

### Step 2: Create New Web Service sa Render

1. Pumunta sa [Render Dashboard](https://dashboard.render.com)
2. Click **"New +"** → **"Web Service"**
3. I-connect ang GitHub repository
4. Piliin ang repository at branch (`main`)

### Step 3: Configure Service Settings

**IMPORTANT:** I-configure manually sa dashboard:

#### Basic Settings:
- **Name:** `finalsCopy` (o kahit anong name)
- **Region:** Piliin ang pinakamalapit (hal: Singapore)
- **Branch:** `main`
- **Root Directory:** (empty/blank) ⬅️ IMPORTANT!

#### Build & Deploy Settings:

**Environment:**
```
Docker
```

**Dockerfile Path:**
```
Dockerfile
```

**Root Directory:**
```
(empty/blank)
```

**Note:** Hindi mo na kailangan ang Docker Context field.

### Step 4: Set Environment Variables

I-set ang lahat ng environment variables sa **Environment** tab (tingnan ang [Environment Variables](#environment-variables) section).

### Step 5: Deploy

1. Click **"Create Web Service"**
2. Hintayin ang build (5-15 minutes)
3. I-check ang build logs
4. I-test ang application URL

---

## Environment Variables

### Required Variables:

```
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://0.0.0.0:$PORT
```

### Firebase Configuration:

```
FIREBASE_CREDENTIALS_BASE64 = <base64-string>
Firebase__ProjectId = propertyinventory-d6e4c
```

**Note:** `Firebase__CredentialsPath` ay optional na kung may `FIREBASE_CREDENTIALS_BASE64`.

### Email Settings:

```
EmailSettings__SmtpServer = smtp.gmail.com
EmailSettings__SmtpPort = 587
EmailSettings__SmtpUsername = jeremiahyu050@gmail.com
EmailSettings__SmtpPassword = vwcedwlhgetrrgux
EmailSettings__FromEmail = jeremiahyu050@gmail.com
EmailSettings__FromName = Property Inventory System
```

### App Settings:

```
AppSettings__BaseUrl = https://finalscopy-pdiw.onrender.com
```

(Palitan ang `finalscopy-pdiw.onrender.com` ng actual Render URL mo)

**Important:** Gamitin ang **double underscore** `__` para sa nested settings (hindi single `_`).

---

## Firebase Setup

### Option 1: Base64 Environment Variable (Recommended)

1. **I-convert ang `firebase-credentials.json` sa base64:**
   ```powershell
   # Windows PowerShell:
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("firebase-credentials.json"))
   
   # Linux/Mac:
   base64 -i firebase-credentials.json
   ```

2. **I-copy ang base64 string**

3. **I-add sa Render environment variables:**
   ```
   FIREBASE_CREDENTIALS_BASE64 = <paste-your-base64-string>
   ```

4. **I-set ang Firebase Project ID:**
   ```
   Firebase__ProjectId = propertyinventory-d6e4c
   ```

### Option 2: Upload as Secret File

1. Sa Render dashboard, pumunta sa **Secrets**
2. I-upload ang `firebase-credentials.json`
3. I-reference ito sa environment variable

### Option 3: Add to Repository (Not Recommended)

- I-add ang `firebase-credentials.json` sa repository
- **Warning:** Hindi secure, pero pwede para sa testing

### Base64 Credentials String:

I-copy ang buong string na ito at i-paste sa Render:

```
ewogICJ0eXBlIjogInNlcnZpY2VfYWNjb3VudCIsCiAgInByb2plY3RfaWQiOiAicHJvcGVydHlpbnZlbnRvcnktZDZlNGMiLAogICJwcml2YXRlX2tleV9pZCI6ICJiZDY2MWYyNDU0MTk3MDE0ZGRmNmE1NzA1ZWY3NTBiZGQ4YjhlZWVjIiwKICAicHJpdmF0ZV9rZXkiOiAiLS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tXG5NSUlFdmdJQkFEQU5CZ2txaGtpRzl3MEJBUUVGQUFTQ0JLZ3dnZ1NrQWdFQUFvSUJBUUMzZWY1RWpZSXlBNEYwXG5XRFFpVU4yazBmSG1GUXJzODVjdHNKZHZWb3E0SE13Y0dFb2g5NjlvbVZVem1PKzhWVzhrbDFSanJveFNKWHI4XG4vMUZIeU9EVG9pYzJrcUhCdGNYU2pZVXZSckR3SmZ1SURjQ0ZkR0RrUDdRTWd2SFYrazdKdFp3cEVJYUhoUm1HXG5wQTA0NU8xUmR4RlVld2lXSWdhdWFHY3ZjbFE3MWhneHFLSFMzNFdkVHRXbU5YS2dtcU16WWNZOXVDQ2s4azR1XG5HSXloNWZ3OEQyelRvZEk4V0JLVFBEZkdLbUdpTlN5eVdBSC81QTlwMXdXNlR1VWc0REZ1MEJNVjNzNUxzb3FkXG4zOWxFZXdwVGxLcTVWUGxRWGlrVS9PMkEyL29aRkNyb2ErcnFDbHlxRzl1VnpFRFJRYWwwSy9KczRSTTEyanlYXG43cFNJNUQyZkFnTUJBQUVDZ2dFQUY4YmljYVhaQW53NzBZVUF0SlhBTTNUVm9WaUd3dkJLWGl4dFk0dFdqTWVHXG5sL2w1MmU4TU4wVHZxckVlR0UwR0N6cmxQOG5GKzN0SjlmRnNhaDRaTExQdDJ2K2pvTVBhc0ErUSsvQndTNTdRXG5ldkExUzlZcUhFbzVIZ24ySnlHNkJoL1g3ZVpyV0xLaC9UWFRWTlV1QUFtcklFU1ZkMGRQa0ZpTlRyUEZRTCtrXG5pUlJzYXR2T1hrbFZpK0tlK0thZHUyLzdUL2lnamRhb1FSRldkSHEvSllGV1VsM0dPcUdkKzlBWFdDMVZZclZQXG5TYmpMdTM0YzU1ZzZGV0U2T3NacjJtK0Rac3lyN2JHNnQ3SElFb2swYUp2NkYreEtzNWRKVU40c0R2WmFOSHMwXG4wTytMSjY1SnBEWVRudE9rZVNUZTkxQzdXRWF6SzlDU1B0REZZMG80R1FLQmdRRHdLRjQzUjVqaXJCZ2xZTUJ5XG5mMlVvaHlIWHZZM0pHckNUV2YvcGV2Z1V0WWpOT2R5d2txNXpoRmNLSHpwMEVvS0QrYlprOG0ydnBVdW9EYTR1XG4zM3p1ZFNBWHRCWURDZHdIMVhKMk1YWE9hT2NUMWlMeEVlcTJZYzQxRkhqempsa2RHNUIzaUMxQzByOTE0aXk4XG5uRVpZZ0ZGbG1sQzUyQmp1Y1QveHlrMlY3UUtCZ1FERGxHMzRKUllSOUVSWkgzRThyWUVnRUI3UUJ5empZeGhtXG4xR2p6aEhTUDVNd0VRVnY2QTE4SHdwbzFra3loRnE5ZnA2aVh4TnFWK1FTNnJQcm85SG1FbVkxU3NmM0RNcmQ3XG5XdWVYYzh5d1Bmcnhwa1FSbDNaSXhHRVBnRDBDeDNTU1pJWDJ4RHhBUWdOenNNL2JIVGdVZ3VPSzlCUzRxVVVPXG5ZMXZFbDdkd093S0JnUURLS1RFbVY1N2JNUU1pYzgrTWtyRVU3S3dRY0FCcXlZemFmV2h2b3BReTJwM09KR2NpXG5QRmVkNWtsZXUrcjR5cHdUZktHTldJWDgxYWhhVHluUlMxQmZhemtZdXYvTWp3QXBtQVJDZW1BdzRSMmtGUjVVXG4wRUJFUnhET25lMHpHU0RxMzhrODFlVkQyeVJRbDVRUk1Yd0dBLzZCQ09YcmkzMXhPdVFaSmJNcUFRS0JnQVdhXG52VVA5SEJMTG95SENxdVlJT3NrR2JUdWMyUVp6RW9IdjJFb1NJNXp6KzN5cnVzMEJSN25iTTd3UlF5emlqNGY0XG5ML2FaWjRJZ3JxZ0N5UFhmKzVOM0t2dVJxaW5yekNJMnlxZ21ZMWt4algxYlBGd2NzUUVJejVjNEg0ZHkyNzVPXG5MeUNSSXdEY1kyanZTaVdIUXRJanBnMlhUUGkxc1FybWdmeXlORWw5QW9HQkFNd1QzZ2dCdXI2Z2FDeHNQc2lwXG5hQUJhVlhSV1J4QkNHQVJjNno3bVhmbzRyQitHVWgvWHRxVEhDKy90RTFLVXdBL0RFRElWazBKajBCcm4xTTBSXG56Z3dhOC9zNENsRkpaMXNBd3Y3dUQ1QXozRWlVSlg4M3dSaUJ3dk1iTW8waVZRSmMzcHc1dFFoeG1tTi9VekY2XG50YWJxOE44eVRvRjY1Y21PNVh1YlJIcHRcbi0tLS0tRU5EIFBSSVZBVEUgS0VZLS0tLS1cbiIsCiAgImNsaWVudF9lbWFpbCI6ICJmaXJlYmFzZS1hZG1pbnNkay1mYnN2Y0Bwcm9wZXJ0eWludmVudG9yeS1kNmU0Yy5pYW0uZ3NlcnZpY2VhY2NvdW50LmNvbSIsCiAgImNsaWVudF9pZCI6ICIxMTExMjQ3OTk5MzA0OTc0MjQxMTAiLAogICJhdXRoX3VyaSI6ICJodHRwczovL2FjY291bnRzLmdvb2dsZS5jb20vby9vYXV0aDIvYXV0aCIsCiAgInRva2VuX3VyaSI6ICJodHRwczovL29hdXRoMi5nb29nbGVhcGlzLmNvbS90b2tlbiIsCiAgImF1dGhfcHJvdmlkZXJfeDUwOV9jZXJ0X3VybCI6ICJodHRwczovL3d3dy5nb29nbGVhcGlzLmNvbS9vYXV0aDIvdjEvY2VydHMiLAogICJjbGllbnRfeDUwOV9jZXJ0X3VybCI6ICJodHRwczovL3d3dy5nb29nbGVhcGlzLmNvbS9yb2JvdC92MS9tZXRhZGF0YS94NTA5L2ZpcmViYXNlLWFkbWluc2RrLWZic3ZjJTQwcHJvcGVydHlpbnZlbnRvcnktZDZlNGMuaWFtLmdzZXJ2aWNlYWNjb3VudC5jb20iLAogICJ1bml2ZXJzZV9kb21haW4iOiAiZ29vZ2xlYXBpcy5jb20iCn0K
```

---

## Email Configuration

### Gmail App Password Setup

1. Pumunta sa: https://myaccount.google.com/apppasswords
2. I-create ang bagong App Password para sa "Mail"
3. I-copy ang 16-character password (walang spaces)
4. I-update sa Render: `EmailSettings__SmtpPassword`

### Email Environment Variables

I-set ang lahat ng email settings sa Render:

```
EmailSettings__SmtpServer = smtp.gmail.com
EmailSettings__SmtpPort = 587
EmailSettings__SmtpUsername = jeremiahyu050@gmail.com
EmailSettings__SmtpPassword = vwcedwlhgetrrgux
EmailSettings__FromEmail = jeremiahyu050@gmail.com
EmailSettings__FromName = Property Inventory System
```

### Verification

I-check ang Render logs para makita kung:
- `"Email sent successfully"` - Successful
- `"Email settings not configured"` - Missing environment variables
- `"Authentication failed"` - Invalid Gmail password
- `"Connection timeout"` - Network/firewall issue

---

## Account Request & Access

### How It Works

1. **User nag-request ng account** → Nagse-send ng confirmation email
2. **Admin nag-approve** → Nagse-send ng approval email na may:
   - Temporary password
   - Login URL (Render URL)
   - Instructions

### Required Setup

**IMPORTANT:** I-set ang `AppSettings__BaseUrl`:

```
AppSettings__BaseUrl = https://finalscopy-pdiw.onrender.com
```

(Palitan ang `finalscopy-pdiw.onrender.com` ng actual Render URL mo)

### Email Content

**Account Request Confirmation:**
- Status: "Pending Review"
- Link sa Render URL (para sa reference)
- Instructions

**Account Approval Email:**
- Temporary password
- Login URL: `https://finalscopy-pdiw.onrender.com/Account/MobileLogin`
- Instructions para sa first login

---

## Troubleshooting

### Build Errors

#### Error: "dotnet: command not found"
**Solution:** 
- Gumamit ng Docker deployment (Environment = `Docker`)
- I-verify na may `Dockerfile` sa repository

#### Error: "Dockerfile not found"
**Solution:**
- I-verify na na-push ang Dockerfile sa GitHub
- I-check kung tama ang Dockerfile Path
- I-verify na nasa root directory ang Dockerfile

#### Error: "Build failed"
**Solution:**
- I-check ang build logs para sa specific error
- I-verify na tama ang file paths sa Dockerfile
- I-check kung may missing dependencies

### Runtime Errors

#### Error: "Bad Gateway (502)"
**Causes:**
- App crashed
- Timeout sa operations
- Unhandled exception

**Solutions:**
- I-check ang Render logs para sa error details
- I-verify ang Firebase credentials
- I-check ang email settings
- I-verify ang PORT environment variable

#### Error: "Firebase credentials not found"
**Solution:**
- I-verify na naka-set ang `FIREBASE_CREDENTIALS_BASE64`
- I-check kung tama ang base64 string
- I-verify ang Firebase project ID

#### Error: "App won't start"
**Solution:**
- I-verify ang PORT environment variable
- I-check ang application logs
- I-verify ang Firebase credentials

### Email Issues

#### Error: "Email settings not configured"
**Solution:** I-verify na naka-set ang lahat ng email environment variables

#### Error: "Authentication failed"
**Solution:**
- I-verify ang Gmail app password
- I-create ng bagong app password kung kailangan

#### Error: "Connection timeout"
**Solution:**
- I-check kung may firewall blocking
- I-try ang port 465 (SSL) instead ng 587 (TLS)

#### Emails sent pero hindi natatanggap
**Solution:**
- I-check ang spam folder
- I-verify ang recipient email address
- I-check ang Gmail account kung may issues

---

## Common Errors & Solutions

### 1. "dotnet: command not found"
**Solution:** Gumamit ng Docker deployment (Environment = `Docker`)

### 2. "Dockerfile not found"
**Solution:** I-verify na na-push ang Dockerfile at tama ang path

### 3. "Bad Gateway (502)"
**Solution:** I-check ang logs, i-verify ang environment variables, i-check ang Firebase credentials

### 4. "Firebase credentials not found"
**Solution:** I-set ang `FIREBASE_CREDENTIALS_BASE64` environment variable

### 5. "Email not sending"
**Solution:** I-verify ang email settings at Gmail app password

### 6. "Login URL ay localhost"
**Solution:** I-set ang `AppSettings__BaseUrl` sa Render URL

---

## Verification Checklist

### Before Deployment:
✅ Code na-push sa GitHub
✅ Dockerfile nasa repository
✅ Environment variables ready

### After Deployment:
✅ Build successful
✅ App running (walang crashes)
✅ Firebase connection working
✅ Email sending working
✅ Account request flow working
✅ Login URL points sa Render URL

---

## Important Notes

### Docker Deployment
- **Environment:** `Docker` (required)
- **Dockerfile Path:** `Dockerfile`
- **Root Directory:** (empty)
- **Docker Context:** (hindi required)

### Environment Variables
- Gamitin ang **double underscore** `__` para sa nested settings
- Example: `Firebase__ProjectId` = `Firebase:ProjectId` sa appsettings.json
- Case-sensitive ang variable names

### Free Plan Limitations
- Sleeps after 15 minutes of inactivity
- Slower build times
- Consider upgrading kung production use

### Updates
- Automatic deployment kapag nag-push sa GitHub
- O manual deploy sa Render dashboard

---

## Support

Para sa issues:
1. I-check ang Render logs
2. I-check ang application logs
3. I-verify ang environment variables
4. I-contact ang Render support

---

## Quick Reference

### Render Dashboard Settings:
```
Environment: Docker
Dockerfile Path: Dockerfile
Root Directory: (empty)
```

### Required Environment Variables:
```
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://0.0.0.0:$PORT
FIREBASE_CREDENTIALS_BASE64 = (base64 string)
Firebase__ProjectId = propertyinventory-d6e4c
EmailSettings__SmtpServer = smtp.gmail.com
EmailSettings__SmtpPort = 587
EmailSettings__SmtpUsername = (your email)
EmailSettings__SmtpPassword = (app password)
EmailSettings__FromEmail = (your email)
EmailSettings__FromName = Property Inventory System
AppSettings__BaseUrl = https://your-app-name.onrender.com
```

---

**Good luck sa deployment! 🚀**

