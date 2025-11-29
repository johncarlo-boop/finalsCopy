# Email Configuration Guide

Para magpadala ng OTP sa email, kailangan mong i-configure ang email settings sa `appsettings.json`.

## Para sa Gmail:

1. **Gumawa ng App Password:**
   - Pumunta sa: https://myaccount.google.com/apppasswords
   - Piliin ang "Mail" at "Other (Custom name)"
   - I-type ang name (hal: "Property Inventory")
   - I-copy ang 16-character password na ibibigay

2. **I-update ang `appsettings.json`:**
   ```json
   "EmailSettings": {
     "SmtpServer": "smtp.gmail.com",
     "SmtpPort": "587",
     "SmtpUsername": "your-email@gmail.com",
     "SmtpPassword": "your-16-character-app-password",
     "FromEmail": "your-email@gmail.com",
     "FromName": "Property Inventory System"
   }
   ```

3. **I-restart ang application** pagkatapos mag-configure.

## Para sa ibang Email Providers:

### Outlook/Hotmail:
```json
"SmtpServer": "smtp-mail.outlook.com",
"SmtpPort": "587"
```

### Yahoo:
```json
"SmtpServer": "smtp.mail.yahoo.com",
"SmtpPort": "587"
```

## Important Notes:

- **Gmail**: Kailangan ng App Password, hindi regular password
- **2FA**: Dapat naka-enable ang 2-Factor Authentication para makagawa ng App Password
- **Security**: Huwag i-commit ang `appsettings.json` sa git kung may password

## Testing:

Pagkatapos mag-configure:
1. Mag-register ng bagong account
2. Dapat makareceive ka ng email na may OTP code
3. Makikita mo rin ang OTP sa screen bilang backup



