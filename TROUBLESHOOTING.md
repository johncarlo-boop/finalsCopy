# Troubleshooting Guide

## Common Errors at Solutions

### 1. Firebase Credentials Error
**Error:** `Firebase credentials file not found`
**Solution:**
- I-verify na may `firebase-credentials.json` file sa project root
- I-check kung tama ang path sa `appsettings.json`
- Para sa Render, i-upload ang file bilang secret o i-set bilang environment variable

### 2. Port Already in Use
**Error:** `Address already in use` o `Port 5000 is already in use`
**Solution:**
```bash
# Windows - I-check kung sino ang gumagamit ng port
netstat -ano | findstr :5000

# I-kill ang process
taskkill /PID <process-id> /F
```

### 3. Build Errors sa Render
**Error:** Build fails sa Render
**Solution:**
- I-verify ang build command: `dotnet publish -c Release -o ./publish`
- I-check kung may missing NuGet packages
- I-verify ang .NET version (dapat .NET 8.0)

### 4. Database Connection Error
**Error:** `Cannot connect to database`
**Solution:**
- I-check ang connection string sa `appsettings.json`
- I-verify kung running ang SQL Server
- Para sa Render, i-set ang database connection string bilang environment variable

### 5. Email Sending Error
**Error:** `SMTP connection failed`
**Solution:**
- I-verify ang SMTP settings sa `appsettings.json`
- Para sa Gmail, gumamit ng App Password (hindi regular password)
- I-check ang firewall settings

### 6. Firebase Initialization Error
**Error:** `Firebase initialization failed`
**Solution:**
- I-verify ang Firebase Project ID
- I-check kung valid ang `firebase-credentials.json`
- I-verify ang Firebase service account permissions

### 7. Static Files Not Loading
**Error:** CSS/JS files not loading
**Solution:**
- I-verify na nasa `wwwroot` folder ang static files
- I-check ang `Program.cs` kung may `app.UseStaticFiles()`
- I-verify ang file paths sa HTML

### 8. SignalR Connection Error
**Error:** SignalR hub not connecting
**Solution:**
- I-check kung configured ang SignalR sa `Program.cs`
- I-verify ang SignalR client script sa pages
- I-check ang browser console para sa errors

### 9. Authentication Error
**Error:** `Access denied` o `Unauthorized`
**Solution:**
- I-verify na may Admin role ang user
- I-check ang cookie settings
- I-clear ang browser cookies at i-try ulit

### 10. Render Deployment Error
**Error:** App won't start sa Render
**Solution:**
- I-check ang PORT environment variable
- I-verify ang start command: `cd publish && dotnet PropertyInventory.dll`
- I-check ang application logs sa Render dashboard

## Google Drive Related Issues

Kung may error related sa Google Drive folder:
- I-verify ang file permissions
- I-check kung accessible ang files
- I-download ang files locally kung kailangan

## Getting Help

Kung may error ka na hindi nakalista dito:
1. I-copy ang complete error message
2. I-check ang application logs
3. I-verify ang environment variables
4. I-check ang browser console (F12) para sa client-side errors

---

**Note:** Para sa specific errors, i-share ang complete error message para mas matulungan kita.

