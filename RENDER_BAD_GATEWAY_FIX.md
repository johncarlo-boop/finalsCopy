# Fix Bad Gateway (502) Error sa Account Request

## Problem: Bad Gateway Error kapag nag-request ng account

Ang "Bad Gateway" (502) error ay nangyayari kapag:
- Nag-crash ang application
- May timeout sa operations
- May unhandled exception

## Solutions Applied

### 1. Fixed Program.cs Port Configuration
- Na-update para gumamit ng PORT environment variable mula sa Render
- Hindi na hardcoded ang ports

### 2. Added Timeout Protection sa Email Sending
- May timeout na 15-20 seconds para sa email operations
- Hindi na magha-hang ang app kung may email issue

### 3. Improved Error Handling
- Mas mahigpit na error handling sa lahat ng operations
- SignalR notifications ay non-blocking
- Email failures ay hindi na mag-cause ng crash

## Common Causes ng Bad Gateway:

### 1. Firebase Connection Timeout
**Solution:** I-check ang Firebase credentials at connection

### 2. Email Sending Timeout
**Solution:** 
- I-verify ang email settings
- I-check kung may firewall blocking
- I-verify ang Gmail app password

### 3. SignalR Connection Issue
**Solution:** Hindi na critical - non-blocking na

### 4. Port Configuration Issue
**Solution:** Na-fix na - gumagamit na ng PORT env var

## I-check ang Render Logs

Pumunta sa Render dashboard → **Logs** tab at i-check:

### Look for:
- `"Error creating account request"` - Firebase issue
- `"Email sending timed out"` - Email configuration issue
- `"Failed to send SignalR notification"` - Non-critical
- `"Firebase credentials not found"` - Firebase setup issue

## Verification Steps

1. **I-check ang Environment Variables:**
   ```
   ASPNETCORE_ENVIRONMENT = Production
   ASPNETCORE_URLS = http://0.0.0.0:$PORT
   FIREBASE_CREDENTIALS_BASE64 = (base64 string)
   Firebase__ProjectId = propertyinventory-d6e4c
   EmailSettings__SmtpServer = smtp.gmail.com
   EmailSettings__SmtpPort = 587
   EmailSettings__SmtpUsername = (your email)
   EmailSettings__SmtpPassword = (app password)
   AppSettings__BaseUrl = https://finalscopy-pdiw.onrender.com
   ```

2. **I-test ang Account Request:**
   - Pumunta sa Request Account page
   - I-fill ang form
   - I-submit
   - I-check kung successful (hindi na Bad Gateway)

3. **I-check ang Logs:**
   - I-verify na walang critical errors
   - I-check kung may timeout warnings

## If Still Getting Bad Gateway:

### Option 1: I-check ang Application Logs
- Pumunta sa Render → Logs
- I-look for stack traces o error messages
- I-share ang error para mas matulungan

### Option 2: I-verify ang Firebase Connection
- I-check kung naka-set ang `FIREBASE_CREDENTIALS_BASE64`
- I-verify ang Firebase project ID

### Option 3: I-disable ang Email Temporarily
- Kung email ang issue, pwede mong i-disable temporarily
- Pero dapat hindi na mag-crash dahil may timeout protection na

## Next Steps

1. I-commit at i-push ang fixes
2. I-deploy sa Render
3. I-test ang account request
4. I-check ang logs kung may errors pa rin

---

**Note:** Ang fixes ay naglalagay ng timeout protection at better error handling para hindi na mag-crash ang app kahit may issues sa email o SignalR.

