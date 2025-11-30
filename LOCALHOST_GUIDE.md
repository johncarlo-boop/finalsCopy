# Running on Localhost While Deployed to Render

Puwede mong i-run ang app sa localhost **at the same time** na naka-deploy sa Render. Parehong puwedeng tumakbo nang sabay!

---

## How It Works

### Automatic Detection

Ang app ay **auto-detect** kung saan siya tumatakbo:

- **Localhost (Development):**
  - Gumagamit ng `appsettings.json`
  - Gmail SMTP (port 587) - **works locally**
  - Local Firebase credentials file
  - Local BaseUrl

- **Render (Production):**
  - Gumagamit ng **environment variables** (mas priority)
  - SMTP2GO (port 2525) - **works on Render free tier**
  - Base64 Firebase credentials
  - Render URL

---

## Running on Localhost

### Step 1: Make sure `appsettings.json` is configured

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
    "SmtpPassword": "your-gmail-app-password",
    "FromEmail": "jeremiahyu050@gmail.com",
    "FromName": "Property Inventory System"
  },
  "AppSettings": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### Step 2: Run the app

```bash
dotnet run
```

O kaya:

```bash
dotnet watch run
```

### Step 3: Access the app

- **Localhost:** http://localhost:5000
- **Render:** https://finalscopy-pdiw.onrender.com

**Pareho silang gumagana nang sabay!** ✅

---

## Configuration Priority

Ang app ay gumagamit ng **configuration priority**:

1. **Environment Variables** (highest priority - Render uses this)
2. **appsettings.json** (fallback - Localhost uses this)

Kaya:
- **Sa Render:** Environment variables ang ginagamit (SMTP2GO, base64 Firebase)
- **Sa Localhost:** `appsettings.json` ang ginagamit (Gmail SMTP, local Firebase file)

---

## Important Notes

### ✅ What Works on Both

- Firebase Firestore (same database)
- User accounts (shared)
- Property data (shared)
- Real-time updates (SignalR)

### ⚠️ Differences

| Feature | Localhost | Render |
|---------|-----------|--------|
| **SMTP** | Gmail (port 587) | SMTP2GO (port 2525) |
| **BaseUrl** | `http://localhost:5000` | `https://finalscopy-pdiw.onrender.com` |
| **Firebase Creds** | File (`firebase-credentials.json`) | Base64 env var |
| **Environment** | Development | Production |

### 📧 Email Links

- **Localhost emails:** Links point to `http://localhost:5000`
- **Render emails:** Links point to `https://finalscopy-pdiw.onrender.com`

**Tip:** I-set ang `AppSettings__BaseUrl` sa Render environment variables para sa correct email links.

---

## Testing

### Test Localhost:

1. I-run: `dotnet run`
2. I-open: http://localhost:5000
3. I-test ang account request
4. I-check ang email (dapat Gmail SMTP ang ginamit)

### Test Render:

1. I-open: https://finalscopy-pdiw.onrender.com
2. I-test ang account request
3. I-check ang email (dapat SMTP2GO ang ginamit)

---

## Troubleshooting

### Issue: Localhost not working

**Solution:**
- I-check kung may `firebase-credentials.json` file
- I-verify ang Gmail app password sa `appsettings.json`
- I-check kung may process na gumagamit ng port 5000

### Issue: Render not working

**Solution:**
- I-check ang Render environment variables
- I-verify ang SMTP2GO credentials
- I-check ang `FIREBASE_CREDENTIALS_BASE64`

### Issue: Both using same config

**Solution:**
- I-verify na walang environment variables set locally
- I-check kung `ASPNETCORE_ENVIRONMENT` ay `Development` locally

---

## Quick Commands

```bash
# Run locally (Development mode)
dotnet run

# Run with watch (auto-reload on changes)
dotnet watch run

# Build for production
dotnet build -c Release

# Publish
dotnet publish -c Release
```

---

## Summary

✅ **Puwede mong i-run pareho nang sabay**
✅ **Automatic detection** - walang manual switching
✅ **Different configs** - localhost uses `appsettings.json`, Render uses env vars
✅ **Same database** - pareho silang connected sa Firebase
✅ **Independent** - changes sa localhost hindi makaka-affect sa Render

**Happy coding!** 🚀

