# Verification: Render at Localhost Configuration

## ✅ Configuration Summary

### How It Works:

1. **Environment Detection:**
   - **Localhost:** `ASPNETCORE_ENVIRONMENT = Development` (or not set)
   - **Render:** `ASPNETCORE_ENVIRONMENT = Production` (set via environment variable)

2. **Configuration Priority:**
   - **Environment Variables** (highest priority - Render uses this)
   - **appsettings.json** (fallback - Localhost uses this)

3. **Host Binding:**
   - **Localhost:** Binds to `localhost:5000`
   - **Render:** Binds to `0.0.0.0:$PORT` (Render provides PORT env var)

---

## ✅ Localhost Configuration

### Files Used:
- `appsettings.json` - Contains Gmail SMTP settings
- `firebase-credentials.json` - Local Firebase credentials file

### Settings:
```json
{
  "Firebase": {
    "ProjectId": "propertyinventory-d6e4c",
    "CredentialsPath": "firebase-credentials.json"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "jeremiahyu050@gmail.com",
    "SmtpPassword": "vwcedwlhgetrrgux",
    "FromEmail": "jeremiahyu050@gmail.com",
    "FromName": "Property Inventory System"
  },
  "AppSettings": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### How to Run:
```powershell
cd "finals _NoSQL"
.\run-localhost.ps1
```

Or manually:
```powershell
cd "finals _NoSQL"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

### Access:
- **URL:** http://localhost:5000

---

## ✅ Render Configuration

### Environment Variables Used:
- `ASPNETCORE_ENVIRONMENT = Production`
- `PORT` - Provided by Render automatically
- `FIREBASE_CREDENTIALS_BASE64` - Base64 encoded Firebase credentials
- `Firebase__ProjectId = propertyinventory-d6e4c`
- `EmailSettings__SmtpServer = mail.smtp2go.com` (or smtp.gmail.com)
- `EmailSettings__SmtpPort = 2525` (or 587)
- `EmailSettings__SmtpUsername = (your email)`
- `EmailSettings__SmtpPassword = (app password)`
- `EmailSettings__FromEmail = (your email)`
- `EmailSettings__FromName = Property Inventory System`
- `AppSettings__BaseUrl = https://finalscopy-pdiw.onrender.com`

### How It Works:
1. Render sets `ASPNETCORE_ENVIRONMENT = Production`
2. Program.cs detects Production → binds to `0.0.0.0:$PORT`
3. Services read environment variables (which override appsettings.json)
4. Firebase uses `FIREBASE_CREDENTIALS_BASE64` (base64 string)
5. Email uses SMTP2GO (port 2525) or Gmail (port 587)

### Access:
- **URL:** https://finalscopy-pdiw.onrender.com

---

## ✅ Key Differences

| Feature | Localhost | Render |
|---------|-----------|--------|
| **Environment** | Development | Production |
| **Host Binding** | localhost:5000 | 0.0.0.0:$PORT |
| **Firebase Creds** | File (`firebase-credentials.json`) | Base64 env var |
| **SMTP Server** | Gmail (smtp.gmail.com:587) | SMTP2GO (mail.smtp2go.com:2525) or Gmail |
| **BaseUrl** | http://localhost:5000 | https://finalscopy-pdiw.onrender.com |
| **Config Source** | appsettings.json | Environment Variables |

---

## ✅ Testing Checklist

### Test Localhost:
- [ ] Run `.\run-localhost.ps1`
- [ ] Access http://localhost:5000
- [ ] Test login
- [ ] Test account request
- [ ] Check email (should use Gmail SMTP)
- [ ] Verify Firebase connection works

### Test Render:
- [ ] Access https://finalscopy-pdiw.onrender.com
- [ ] Test login
- [ ] Test account request
- [ ] Check email (should use SMTP2GO or configured SMTP)
- [ ] Verify Firebase connection works
- [ ] Check email links point to Render URL

---

## ✅ Troubleshooting

### Issue: Localhost not working
**Solution:**
1. Clear environment variables using `run-localhost.ps1`
2. Verify `appsettings.json` has correct settings
3. Check if `firebase-credentials.json` exists
4. Ensure port 5000 is not in use

### Issue: Render not working
**Solution:**
1. Check Render environment variables are set
2. Verify `FIREBASE_CREDENTIALS_BASE64` is correct
3. Check SMTP credentials
4. Verify `AppSettings__BaseUrl` points to Render URL

### Issue: Both using same config
**Solution:**
1. Localhost: Clear environment variables (use `run-localhost.ps1`)
2. Render: Ensure environment variables are set in Render dashboard
3. Verify `ASPNETCORE_ENVIRONMENT` is different:
   - Localhost: `Development` or not set
   - Render: `Production`

---

## ✅ Code Verification

### Program.cs
- ✅ Detects environment: `app.Environment.IsDevelopment()`
- ✅ Binds to `localhost` in Development
- ✅ Binds to `0.0.0.0` in Production
- ✅ Uses PORT env var (Render) or defaults to 5000

### FirebaseService.cs
- ✅ Checks `FIREBASE_CREDENTIALS_BASE64` first (Render)
- ✅ Falls back to `firebase-credentials.json` file (Localhost)
- ✅ Uses configuration priority correctly

### EmailService.cs
- ✅ Reads from `_configuration["EmailSettings:..."]`
- ✅ Environment variables override appsettings.json
- ✅ Uses `AppSettings:BaseUrl` for email links

---

## ✅ Summary

**YES, GUMAGANA SA PAREHO!** ✅

- **Localhost:** Uses `appsettings.json` + `firebase-credentials.json` file
- **Render:** Uses environment variables (overrides appsettings.json)
- **Both:** Share the same Firebase Firestore database
- **Both:** Can run simultaneously without conflicts

**Key Point:** Environment variables have higher priority than appsettings.json, so:
- Render uses env vars (set in Render dashboard)
- Localhost uses appsettings.json (when env vars are cleared)

