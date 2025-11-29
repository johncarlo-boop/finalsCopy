# Firebase Setup Guide (Taglish)

Ito yung guide para sa pag-setup ng Firebase para sa application mo. Kailangan mo i-configure ang Firebase Firestore para gumana yung app.

## Kailangan Mo

1. Google account
2. Firebase project na naka-create sa https://console.firebase.google.com/

## Step-by-Step Setup

### Step 1: Gumawa ng Firebase Project

1. Pumunta sa https://console.firebase.google.com/
2. Click mo yung "Add project" o piliin mo yung existing project mo
3. Sundin mo yung setup wizard para ma-create yung project
4. Tandaan mo yung Project ID mo (kailangan mo to later)

### Step 2: I-enable ang Cloud Firestore API

**IMPORTANT**: Kailangan mo munang i-enable ang Cloud Firestore API sa Google Cloud Console!

1. Pumunta sa: https://console.developers.google.com/apis/api/firestore.googleapis.com/overview?project=propertyinventory-d6e4c
   - O kaya pumunta sa https://console.cloud.google.com/ → Piliin yung project → APIs & Services → Library → Search "Cloud Firestore API" → Enable
2. Click mo yung **"Enable"** button
3. Maghintay ka ng 2-3 minutes para mag-propagate

### Step 3: I-enable ang Firestore Database

1. Sa Firebase project mo, pumunta sa "Firestore Database"
2. Click mo yung "Create database"
3. Piliin mo yung **production mode** (pwede mo i-setup yung security rules later)
4. Piliin mo yung location ng database:
   - **Para sa Philippines: Piliin mo `asia-southeast1` (Singapore)** - Pinakamalapit at mabilis
   - O kaya `asia-southeast2` (Jakarta) - Alternative option
   - O kaya `asia-east1` (Taiwan) - Alternative option
5. Click mo yung "Enable"

### Step 4: Gumawa ng Service Account Key

1. Sa Firebase Console, pumunta sa Project Settings (yung gear icon)
2. Pumunta sa "Service accounts" tab
3. Click mo yung "Generate new private key"
4. I-save mo yung JSON file (may sensitive credentials to, so ingatan mo!)
5. Ilagay mo yung file sa project root directory mo at i-name mo as `firebase-credentials.json`
   - **IMPORTANT**: I-add mo yung `firebase-credentials.json` sa `.gitignore` para hindi ma-commit yung credentials

### Step 5: I-configure ang Application

1. Buksan mo yung `appsettings.json`
2. I-update mo yung Firebase configuration:
   ```json
   {
     "Firebase": {
       "ProjectId": "your-project-id",
       "CredentialsPath": "firebase-credentials.json"
     }
   }
   ```
3. Palitan mo yung `your-project-id` ng actual Firebase Project ID mo

### Step 6: Alternative - Environment Variables

Pwede mo rin gamitin yung environment variables instead ng `appsettings.json`:

- `FIREBASE_PROJECT_ID`: Yung Firebase project ID mo
- `GOOGLE_APPLICATION_CREDENTIALS`: Path sa service account JSON file mo

## Security Rules (Optional pero Recommended)

Sa Firebase Console, pumunta sa Firestore Database > Rules at i-set mo:

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Users collection - authenticated users can read/write their own data
    match /users/{userId} {
      allow read, write: if request.auth != null && request.auth.uid == userId;
    }
    
    // Properties collection - authenticated users can read, admins can write
    match /properties/{propertyId} {
      allow read: if request.auth != null;
      allow write: if request.auth != null && 
                     get(/databases/$(database)/documents/users/$(request.auth.uid)).data.isAdmin == true;
    }
    
    // Account requests - authenticated admins can read/write
    match /accountRequests/{requestId} {
      allow read, write: if request.auth != null && 
                            get(/databases/$(database)/documents/users/$(request.auth.uid)).data.isAdmin == true;
    }
    
    // OTP verifications - users can create, read their own
    match /otpVerifications/{otpId} {
      allow create: if true;
      allow read: if request.auth != null;
    }
  }
}
```

## Testing the Setup

1. I-run mo yung application: `dotnet run`
2. Dapat automatic na ma-connect sa Firebase
3. Check mo yung console logs para makita yung "Firestore initialized with project: [your-project-id]"

## Troubleshooting

### Error: "Failed to initialize Firestore"

- I-verify mo na nandun yung `firebase-credentials.json` file mo at nasa tamang location
- Check mo na yung Project ID sa `appsettings.json` ay match sa Firebase project mo
- Siguraduhin mo na yung service account may Firestore permissions

### Error: "Cloud Firestore API has not been used... or it is disabled"

- **I-enable mo ang Cloud Firestore API** sa Google Cloud Console
- Pumunta sa: https://console.developers.google.com/apis/api/firestore.googleapis.com/overview?project=propertyinventory-d6e4c
- Click "Enable" at maghintay ng 2-3 minutes
- I-restart mo yung application

### Error: "Permission denied"

- Check mo yung Firestore security rules mo
- I-verify mo na yung service account may necessary permissions sa Firebase Console

### Error: "Project not found"

- Double-check mo yung Project ID sa `appsettings.json`
- Siguraduhin mo na yung Firebase project exists at active

## Migration from MySQL

Kung may existing data ka sa MySQL:

1. I-export mo yung data mo from MySQL
2. Gamitin mo yung Firebase Console o migration script para i-import yung data sa Firestore
3. Siguraduhin mo na yung data structure ay match sa Firestore document structure

## Collections Structure

Ginagamit ng application yung following Firestore collections:

- `users` - User accounts (ApplicationUser documents)
- `properties` - Property inventory items (Property documents)
- `accountRequests` - Account request records (AccountRequest documents)
- `otpVerifications` - OTP verification records (OtpVerification documents)

## Quick Start Checklist

- [ ] Firebase project created
- [ ] **Cloud Firestore API enabled** (IMPORTANT!)
- [ ] Firestore Database enabled
- [ ] Service account key downloaded
- [ ] `firebase-credentials.json` placed in project root
- [ ] `appsettings.json` updated with Project ID
- [ ] Application tested and running
- [ ] Security rules configured (optional)

Good luck! 🚀
