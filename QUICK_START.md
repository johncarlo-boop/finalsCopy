# Quick Start Guide - Property Inventory Management System

## Prerequisites

Before running the system, ensure you have:

1. **.NET 8.0 SDK** installed
   - Check if installed: `dotnet --version`
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0

2. **SQL Server LocalDB** (usually comes with Visual Studio)
   - Or use SQL Server Express/Full SQL Server

3. **Visual Studio 2022** (recommended) or **Visual Studio Code**
   - Or any code editor with .NET support

## Step-by-Step Instructions

### Step 1: Open the Project

1. Open the project folder in your terminal/command prompt
2. Navigate to the project directory:
   ```bash
   cd "C:\Users\Admin\OneDrive\Desktop\finals"
   ```

### Step 2: Restore NuGet Packages

Restore all required packages:
```bash
dotnet restore
```

### Step 3: Update Database Connection (Optional)

If you're not using LocalDB, edit `appsettings.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=PropertyInventoryDB;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
  }
}
```

### Step 4: Create and Seed the Database

The database will be created automatically when you run the application. However, if you want to create it manually:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Note**: If you get an error about `dotnet ef`, install the EF Core tools:
```bash
dotnet tool install --global dotnet-ef
```

### Step 5: Run the Application

Run the application using one of these methods:

**Option A: Using dotnet CLI**
```bash
dotnet run
```

**Option B: Using Visual Studio**
- Press `F5` or click the "Run" button

**Option C: Using Visual Studio Code**
- Press `F5` or use the terminal: `dotnet run`

### Step 6: Access the Application

Once the application starts, you'll see output like:
```
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

1. Open your web browser
2. Navigate to: `https://localhost:5001` or `http://localhost:5000`
3. You will be redirected to the login page

### Step 7: Register and Login

1. Click on "Sign Up" to create a new account
2. Fill in your details (Full Name, Email, Password)
3. After registration, you'll need to be assigned the Admin role manually (or through database)
4. Login with your registered credentials

## Troubleshooting

### Issue: "dotnet command not found"
**Solution**: Install .NET 8.0 SDK from https://dotnet.microsoft.com/download

### Issue: Database connection error
**Solution**: 
- Ensure SQL Server LocalDB is installed
- Check the connection string in `appsettings.json`
- Verify SQL Server service is running

### Issue: "Cannot find package" errors
**Solution**: Run `dotnet restore` again

### Issue: Port already in use
**Solution**: 
- Close other applications using ports 5000/5001
- Or update `launchSettings.json` to use different ports

### Issue: SignalR not working
**Solution**: 
- Check browser console for errors
- Ensure you're logged in as admin
- Verify SignalR CDN is accessible

### Issue: Migration errors
**Solution**: 
- Delete the `Migrations` folder if it exists
- Run: `dotnet ef migrations add InitialCreate`
- Run: `dotnet ef database update`

## Development vs Production

### Development Mode
- Detailed error pages
- Hot reload enabled
- Development logging

### Production Mode
- Generic error pages
- HTTPS enforced
- Optimized performance

To run in production mode:
```bash
dotnet run --environment Production
```

## Stopping the Application

- Press `Ctrl+C` in the terminal
- Or close the terminal window

## Next Steps After Running

1. **Register Admin Account**: Create your admin account through Sign Up page
2. **Assign Admin Role**: Assign the Admin role to your user (via database or code)
3. **Add Properties**: Start adding your property inventory
4. **Test Real-Time Updates**: Open multiple browser windows to see real-time updates
5. **Customize**: Modify categories, statuses, and fields as needed

## Additional Commands

### View all properties in database
```bash
# Use SQL Server Management Studio or Azure Data Studio
# Connect to: (localdb)\mssqllocaldb
# Database: PropertyInventoryDB
```

### Clear database and start fresh
```bash
dotnet ef database drop
dotnet ef database update
```

### Create a new migration
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Support

If you encounter any issues:
1. Check the error messages in the console
2. Review the README.md file
3. Check browser console for JavaScript errors
4. Verify all prerequisites are installed

---

**Happy Inventory Management!** 📦



