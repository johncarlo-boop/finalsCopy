# Property Inventory Management System
## Colegio de Montalban

A web-based, real-time tracking property inventory management system built with ASP.NET Core Razor Pages. This system is designed for **admin access only** and provides comprehensive property tracking capabilities.

## Features

- ✅ **Real-Time Updates**: Uses SignalR for real-time property tracking across all connected clients
- ✅ **Admin-Only Access**: Secure authentication and authorization for admin users only
- ✅ **Complete CRUD Operations**: Create, Read, Update, and Delete properties
- ✅ **Advanced Search & Filtering**: Search by name, code, location, or filter by category and status
- ✅ **Property Status Tracking**: Track properties with statuses (Available, InUse, UnderMaintenance, Damaged, Disposed, Lost)
- ✅ **Comprehensive Property Details**: Track property code, name, category, location, quantity, price, purchase date, supplier, serial number, warranty expiry, and more
- ✅ **Modern UI**: Clean and responsive Bootstrap 5 interface

## Technology Stack

- **.NET 8.0**: Latest .NET framework
- **ASP.NET Core Razor Pages**: Server-side web framework
- **Entity Framework Core**: ORM for database operations
- **SQL Server LocalDB**: Database (can be changed to SQL Server)
- **SignalR**: Real-time communication
- **ASP.NET Core Identity**: Authentication and authorization
- **Bootstrap 5**: UI framework

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or Visual Studio Code
- SQL Server LocalDB (included with Visual Studio) or SQL Server

## Installation & Setup

1. **Clone or download the project**

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Update database connection string** (if needed)
   - Open `appsettings.json`
   - Modify the `ConnectionStrings:DefaultConnection` if you're using a different database

4. **Create the database**
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
   
   Or run the application - it will automatically create the database and seed initial data.

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application**
   - Navigate to `https://localhost:5001` or `http://localhost:5000`
   - You will be redirected to the login page

## Database Seeding

The application automatically seeds:
- Admin role
- Sample property data (if database is empty)

**Note**: No default admin user is created. Users must register through the Sign Up page.

## Project Structure

```
PropertyInventory/
├── Data/
│   ├── ApplicationDbContext.cs    # Database context
│   └── SeedData.cs                 # Database seeding
├── Hubs/
│   └── PropertyHub.cs              # SignalR hub for real-time updates
├── Models/
│   ├── ApplicationUser.cs          # User model
│   └── Property.cs                 # Property model
├── Pages/
│   ├── Account/                    # Authentication pages
│   │   ├── Login.cshtml
│   │   ├── Logout.cshtml
│   │   └── AccessDenied.cshtml
│   ├── Create.cshtml               # Create property page
│   ├── Details.cshtml              # Property details page
│   ├── Edit.cshtml                 # Edit property page
│   ├── Index.cshtml                # Property list page
│   ├── _Layout.cshtml              # Main layout
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── wwwroot/
│   ├── css/
│   │   └── site.css                # Custom styles
│   └── js/
│       ├── propertyUpdates.js      # SignalR client code
│       └── site.js                 # Site-wide JavaScript
├── appsettings.json                # Application settings
├── Program.cs                      # Application entry point
└── PropertyInventory.csproj        # Project file
```

## Real-Time Features

The system uses SignalR to provide real-time updates:

- When a property is **created**, all connected clients are notified
- When a property is **updated**, all connected clients see the update
- When a property is **deleted**, all connected clients see the removal

All updates are displayed as toast notifications and the property list automatically refreshes.

## Property Status Types

- **Available**: Property is available for use
- **InUse**: Property is currently in use
- **UnderMaintenance**: Property is being maintained
- **Damaged**: Property is damaged
- **Disposed**: Property has been disposed
- **Lost**: Property is lost

## Security Features

- Admin-only access enforced via `[Authorize(Roles = "Admin")]` attribute
- Password requirements: Minimum 6 characters, requires digit, lowercase, and uppercase
- Account lockout after 5 failed login attempts (5 minutes lockout)
- Secure cookie-based authentication
- HTTPS redirection in production

## Customization

### Adding New Admin Users

1. Register a new user through Identity (you may need to create a registration page)
2. Assign the user to the "Admin" role using:
   ```csharp
   await userManager.AddToRoleAsync(user, "Admin");
   ```

### Changing Database

To use SQL Server instead of LocalDB, update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=PropertyInventoryDB;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
  }
}
```

## Troubleshooting

### Database Connection Issues

- Ensure SQL Server LocalDB is installed
- Check the connection string in `appsettings.json`
- Verify the database server is running

### SignalR Not Working

- Ensure the SignalR hub is properly configured in `Program.cs`
- Check browser console for JavaScript errors
- Verify the SignalR client script is loaded

### Authentication Issues

- Clear browser cookies
- Verify the admin user exists in the database
- Check that the user has the "Admin" role assigned

## License

This project is created for Colegio de Montalban.

## Support

For issues or questions, please contact the system administrator.

---

**Note**: This is an admin-only system. All pages require admin authentication and authorization.

