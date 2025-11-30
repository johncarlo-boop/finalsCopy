# Account Request - Render Access Setup

## Overview

Kapag nag-request ng account at na-approve, dapat makakapag-access ang user sa system gamit ang Render URL.

## How It Works

### 1. Account Request Process:
1. User nag-request ng account
2. Admin nag-approve ng request
3. System nagse-send ng email na may:
   - Temporary password
   - Login URL (Render URL)
   - Instructions para sa access

### 2. Email Content:
Ang approval email ay naglalaman ng:
- **Temporary Password** - Para sa first login
- **Login URL** - Points sa Render URL: `https://finalscopy-pdiw.onrender.com/Account/MobileLogin`
- **Instructions** - Paano mag-login at mag-change ng password

## Required Environment Variable

**IMPORTANT:** I-set ang `AppSettings__BaseUrl` sa Render:

```
AppSettings__BaseUrl = https://finalscopy-pdiw.onrender.com
```

(Palitan ang `finalscopy-pdiw.onrender.com` ng actual Render URL mo)

## Step-by-Step Setup

### Step 1: I-set ang BaseUrl sa Render

1. Pumunta sa Render dashboard
2. Pumunta sa service mo → **Environment** tab
3. I-add o i-update ang:
   ```
   AppSettings__BaseUrl = https://finalscopy-pdiw.onrender.com
   ```
4. I-save ang changes

### Step 2: I-verify ang Email Settings

I-verify na mayroon ka ng:
```
EmailSettings__SmtpServer = smtp.gmail.com
EmailSettings__SmtpPort = 587
EmailSettings__SmtpUsername = jeremiahyu050@gmail.com
EmailSettings__SmtpPassword = vwcedwlhgetrrgux
EmailSettings__FromEmail = jeremiahyu050@gmail.com
EmailSettings__FromName = Property Inventory System
```

### Step 3: I-test ang Account Request Flow

1. I-request ng account (gamit ang Request Account page)
2. I-approve ang request (sa Admin dashboard)
3. I-check ang email na natanggap
4. I-verify na may Render URL sa email
5. I-click ang login link at i-test ang access

## Email Content

### Account Request Confirmation Email:
- Naglalaman ng status: "Pending Review"
- May link sa Render URL (para sa reference)
- Instructions kung ano ang susunod na mangyayari

### Account Approval Email:
- Temporary password
- Login URL (Render URL)
- Instructions para sa first login
- Link para mag-change ng password

## Login URL Format

Ang login URL ay:
```
https://finalscopy-pdiw.onrender.com/Account/MobileLogin
```

O kung may email parameter:
```
https://finalscopy-pdiw.onrender.com/Account/SetPassword?email=user@example.com
```

## Troubleshooting

### Issue: Login URL ay localhost
**Solution:** I-verify na naka-set ang `AppSettings__BaseUrl` sa Render environment variables

### Issue: Email walang login link
**Solution:** 
- I-check kung naka-set ang `AppSettings__BaseUrl`
- I-check ang email logs sa Render

### Issue: Link hindi gumagana
**Solution:**
- I-verify na tama ang Render URL
- I-check kung may `https://` prefix
- I-verify na accessible ang Render app

## Verification Checklist

✅ `AppSettings__BaseUrl` naka-set sa Render
✅ Email settings configured
✅ Account request email sent
✅ Approval email may Render URL
✅ Login link gumagana
✅ User makakapag-login gamit ang temporary password

## Next Steps

1. I-set ang `AppSettings__BaseUrl` sa Render
2. I-test ang account request flow
3. I-verify na may Render URL sa approval email
4. I-test ang login gamit ang temporary password

---

**Note:** Ang login URL ay automatic na i-build gamit ang `AppSettings__BaseUrl`. Kailangan lang i-set ito sa Render environment variables.

