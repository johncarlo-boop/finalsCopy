using Google.Cloud.Firestore;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using PropertyInventory.Models;
using Microsoft.AspNetCore.Hosting;

namespace PropertyInventory.Services;

public class FirebaseService
{
    private readonly FirestoreDb _db;
    private readonly ILogger<FirebaseService> _logger;

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public FirebaseService(IConfiguration configuration, ILogger<FirebaseService> logger, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
        
        // Initialize Firestore
        try
        {
            var projectId = _configuration["Firebase:ProjectId"] ?? Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID") ?? "propertyinventory-d6e4c";
            var credentialsPath = _configuration["Firebase:CredentialsPath"] ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            
            // Resolve relative path to absolute path if needed
            if (!string.IsNullOrEmpty(credentialsPath) && !Path.IsPathRooted(credentialsPath))
            {
                credentialsPath = Path.Combine(_environment.ContentRootPath, credentialsPath);
            }
            
            _logger.LogInformation("Attempting to initialize Firebase with ProjectId: {ProjectId}, CredentialsPath: {CredentialsPath}", projectId, credentialsPath);
            
            GoogleCredential? credential = null;
            
            // Initialize Firebase Admin if not already initialized
            if (FirebaseApp.DefaultInstance == null)
            {
                // Try to load from base64 environment variable first (for Render deployment)
                var base64Credentials = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_BASE64");
                if (!string.IsNullOrEmpty(base64Credentials))
                {
                    try
                    {
                        _logger.LogInformation("Loading Firebase credentials from base64 environment variable");
                        var credentialsBytes = Convert.FromBase64String(base64Credentials);
                        var credentialsJson = System.Text.Encoding.UTF8.GetString(credentialsBytes);
                        credential = GoogleCredential.FromJson(credentialsJson)
                            .CreateScoped("https://www.googleapis.com/auth/cloud-platform", 
                                         "https://www.googleapis.com/auth/datastore");
                        
                        FirebaseApp.Create(new FirebaseAdmin.AppOptions()
                        {
                            Credential = credential,
                            ProjectId = projectId
                        });
                        _logger.LogInformation("Firebase Admin initialized successfully from base64 environment variable");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse base64 credentials: {Error}", ex.Message);
                        throw new InvalidOperationException($"Failed to parse Firebase credentials from base64: {ex.Message}", ex);
                    }
                }
                // Try to load from file
                else if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
                {
                    _logger.LogInformation("Loading Firebase credentials from file: {CredentialsPath}", credentialsPath);
                    credential = GoogleCredential.FromFile(credentialsPath)
                        .CreateScoped("https://www.googleapis.com/auth/cloud-platform", 
                                     "https://www.googleapis.com/auth/datastore");
                    
                    // Set environment variable so FirestoreDb.Create can use it
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
                    
                    FirebaseApp.Create(new FirebaseAdmin.AppOptions()
                    {
                        Credential = credential,
                        ProjectId = projectId
                    });
                    _logger.LogInformation("Firebase Admin initialized successfully with credentials file");
                }
                else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")))
                {
                    _logger.LogInformation("Using GOOGLE_APPLICATION_CREDENTIALS environment variable");
                    FirebaseApp.Create(new FirebaseAdmin.AppOptions()
                    {
                        ProjectId = projectId
                    });
                }
                else
                {
                    var errorMsg = $"Firebase credentials not found. Please set FIREBASE_CREDENTIALS_BASE64 environment variable or ensure firebase-credentials.json exists at: {credentialsPath}";
                    _logger.LogError(errorMsg);
                    throw new FileNotFoundException(errorMsg);
                }
            }
            
            // Create FirestoreDb with explicit credentials if available
            if (credential != null)
            {
                _db = new FirestoreDbBuilder
                {
                    ProjectId = projectId,
                    Credential = credential
                }.Build();
            }
            else
            {
                // Use default (will use FirebaseApp.DefaultInstance or environment variable)
                _db = FirestoreDb.Create(projectId);
            }
            
            _logger.LogInformation("Firestore initialized successfully with project: {ProjectId}", projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firestore. Make sure Firebase credentials are configured. Error: {Error}", ex.Message);
            throw new InvalidOperationException($"Firebase initialization failed. Please check your Firebase configuration. Error: {ex.Message}", ex);
        }
    }

    // User operations
    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        try
        {
            // Normalize email for case-insensitive matching
            var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
            
            // Try with normalized email first
            var query = _db.Collection("users").WhereEqualTo("Email", normalizedEmail).Limit(1);
            var snapshot = await query.GetSnapshotAsync();
            
            if (snapshot.Count > 0)
            {
                var doc = snapshot.Documents[0];
                var user = doc.ConvertTo<ApplicationUser>();
                user.Id = doc.Id;
                return user;
            }
            
            // If not found with normalized email, try with original email (for backward compatibility)
            if (!string.Equals(email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                query = _db.Collection("users").WhereEqualTo("Email", email).Limit(1);
                snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var doc = snapshot.Documents[0];
                    var user = doc.ConvertTo<ApplicationUser>();
                    user.Id = doc.Id;
                    return user;
                }
            }
            
            // If still not found, get all users and check manually (case-insensitive)
            var allUsersQuery = _db.Collection("users").Limit(1000);
            var allSnapshot = await allUsersQuery.GetSnapshotAsync();
            
            foreach (var doc in allSnapshot.Documents)
            {
                var user = doc.ConvertTo<ApplicationUser>();
                user.Id = doc.Id;
                
                var storedEmail = user.Email?.Trim().ToLowerInvariant() ?? string.Empty;
                if (string.Equals(storedEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
                {
                    return user;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            return null;
        }
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string id)
    {
        try
        {
            var doc = await _db.Collection("users").Document(id).GetSnapshotAsync();
            if (doc.Exists)
            {
                var user = doc.ConvertTo<ApplicationUser>();
                user.Id = doc.Id;
                return user;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by id: {Id}", id);
            return null;
        }
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        var user = await GetUserByEmailAsync(email);
        return user != null;
    }

    public async Task<string> CreateUserAsync(ApplicationUser user)
    {
        try
        {
            var docRef = _db.Collection("users").Document();
            user.Id = docRef.Id;
            await docRef.SetAsync(user);
            return docRef.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", user.Email);
            throw;
        }
    }

    public async Task<bool> UpdateUserAsync(string id, ApplicationUser user)
    {
        try
        {
            await _db.Collection("users").Document(id).SetAsync(user, SetOptions.MergeAll);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {Id}", id);
            return false;
        }
    }

    public async Task<List<ApplicationUser>> GetAllAdminUsersAsync()
    {
        try
        {
            var snapshot = await _db.Collection("users").GetSnapshotAsync();
            var admins = new List<ApplicationUser>();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var user = doc.ConvertTo<ApplicationUser>();
                    user.Id = doc.Id;
                    
                    // Get admin users (IsAdmin = true or UserType = Admin)
                    if (user.IsAdmin || user.UserType == UserType.Admin)
                    {
                        admins.Add(user);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting user document {DocId}", doc.Id);
                    continue;
                }
            }
            
            return admins;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all admin users");
            return new List<ApplicationUser>();
        }
    }

    public async Task<List<ApplicationUser>> GetAllActiveUsersAsync()
    {
        try
        {
            var snapshot = await _db.Collection("users").GetSnapshotAsync();
            var activeUsers = new List<ApplicationUser>();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var user = doc.ConvertTo<ApplicationUser>();
                    user.Id = doc.Id;
                    
                    // Get active users (approved and not admin)
                    if (user.IsApproved && !user.IsAdmin && user.UserType != UserType.Admin)
                    {
                        activeUsers.Add(user);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting user document {DocId}", doc.Id);
                    continue;
                }
            }
            
            return activeUsers.OrderBy(u => u.FullName ?? u.Email).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all active users");
            return new List<ApplicationUser>();
        }
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        try
        {
            await _db.Collection("users").Document(id).DeleteAsync();
            _logger.LogInformation("User deleted successfully: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {Id}", id);
            return false;
        }
    }

    // Property operations
    public async Task<List<Property>> GetAllPropertiesAsync()
    {
        try
        {
            var snapshot = await _db.Collection("properties").GetSnapshotAsync();
            var properties = new List<Property>();
            
            foreach (var doc in snapshot.Documents)
            {
                var property = doc.ConvertTo<Property>();
                property.Id = doc.Id;
                properties.Add(property);
            }
            
            return properties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all properties");
            return new List<Property>();
        }
    }

    public async Task<List<Property>> GetGroupedPropertiesAsync(string? searchString = null, string? categoryFilter = null, PropertyStatus? statusFilter = null)
    {
        try
        {
            // Get all properties first (without grouping)
            Query query = _db.Collection("properties");
            var snapshot = await query.GetSnapshotAsync();
            var allProperties = new List<Property>();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var property = doc.ConvertTo<Property>();
                    property.Id = doc.Id;
                    
                    // Apply category filter (case-insensitive)
                    if (!string.IsNullOrEmpty(categoryFilter))
                    {
                        if (string.IsNullOrEmpty(property.Category) || 
                            !property.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }
                    
                    // Apply status filter
                    if (statusFilter.HasValue)
                    {
                        var docData = doc.ToDictionary();
                        var statusMatches = false;
                        
                        if (docData.ContainsKey("Status"))
                        {
                            var statusValue = docData["Status"];
                            
                            if (statusValue is string statusStr)
                            {
                                statusMatches = statusStr.Equals(statusFilter.Value.ToString(), StringComparison.OrdinalIgnoreCase);
                            }
                            else if (statusValue is long statusInt)
                            {
                                statusMatches = (int)statusInt == (int)statusFilter.Value;
                            }
                            else
                            {
                                statusMatches = property.Status == statusFilter.Value;
                            }
                        }
                        else
                        {
                            statusMatches = property.Status == statusFilter.Value;
                        }
                        
                        if (!statusMatches)
                        {
                            continue;
                        }
                    }
                    
                    // Apply search filter if provided
                    if (string.IsNullOrEmpty(searchString) ||
                        property.PropertyName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        property.PropertyCode.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        (property.Description != null && property.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        property.Location.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    {
                        allProperties.Add(property);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting property document {DocId}", doc.Id);
                    continue;
                }
            }
            
            // Group by ImageUrl (case-insensitive)
            var groupedProperties = new List<Property>();
            var processedImageUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var property in allProperties)
            {
                // If property has ImageUrl, group it
                if (!string.IsNullOrWhiteSpace(property.ImageUrl))
                {
                    var normalizedImageUrl = property.ImageUrl.Trim();
                    
                    // Skip if we already processed this ImageUrl
                    if (processedImageUrls.Contains(normalizedImageUrl))
                    {
                        continue;
                    }
                    
                    // Get all properties with same ImageUrl
                    var sameImageProperties = allProperties
                        .Where(p => !string.IsNullOrWhiteSpace(p.ImageUrl) && 
                                   p.ImageUrl.Trim().Equals(normalizedImageUrl, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    if (sameImageProperties.Count > 0)
                    {
                        // Create a grouped property entry
                        var firstProperty = sameImageProperties.First();
                        var groupedProperty = new Property
                        {
                            Id = firstProperty.Id, // Use first property's ID for navigation
                            PropertyCode = firstProperty.PropertyCode.Split('-').Length >= 2 
                                ? $"{firstProperty.PropertyCode.Split('-')[0]}-{firstProperty.PropertyCode.Split('-')[1]}" 
                                : firstProperty.PropertyCode, // Base property code
                            PropertyName = firstProperty.PropertyName,
                            Category = firstProperty.Category,
                            Description = firstProperty.Description,
                            Location = firstProperty.Location,
                            Status = firstProperty.Status, // Use first property's status as primary
                            Quantity = sameImageProperties.Sum(p => p.Quantity), // Total quantity
                            DateReceived = firstProperty.DateReceived,
                            SerialNumber = firstProperty.SerialNumber?.Split('-').Length >= 2
                                ? string.Join("-", firstProperty.SerialNumber.Split('-').Take(firstProperty.SerialNumber.Split('-').Length - 1))
                                : firstProperty.SerialNumber, // Base tag number
                            ImageUrl = firstProperty.ImageUrl,
                            LastUpdated = sameImageProperties.Max(p => p.LastUpdated), // Most recent update
                            UpdatedBy = firstProperty.UpdatedBy,
                            Remarks = firstProperty.Remarks,
                            BorrowerName = sameImageProperties.FirstOrDefault(p => p.Status == PropertyStatus.InUse)?.BorrowerName,
                            BorrowedDate = sameImageProperties.FirstOrDefault(p => p.Status == PropertyStatus.InUse)?.BorrowedDate,
                            ReturnDate = sameImageProperties.FirstOrDefault(p => p.Status == PropertyStatus.InUse)?.ReturnDate
                        };
                        
                        groupedProperties.Add(groupedProperty);
                        processedImageUrls.Add(normalizedImageUrl);
                    }
                }
                else
                {
                    // Properties without ImageUrl are shown individually
                    groupedProperties.Add(property);
                }
            }
            
            return groupedProperties.OrderByDescending(p => p.LastUpdated).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grouped properties");
            return new List<Property>();
        }
    }



    public async Task<bool> DeletePropertiesAsync(List<string> ids)
    {
        try
        {
            var batch = _db.StartBatch();
            var collection = _db.Collection("properties");

            foreach (var id in ids)
            {
                var docRef = collection.Document(id);
                batch.Delete(docRef);
            }

            await batch.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting multiple properties");
            return false;
        }
    }

    public async Task<List<Property>> GetPropertiesAsync(string? searchString = null, string? categoryFilter = null, PropertyStatus? statusFilter = null)
    {
        try
        {
            Query query = _db.Collection("properties");
            
            // Note: We'll apply category and status filters after fetching to handle case-insensitive matching
            // Firestore queries are case-sensitive, so we need to filter in memory
            
            var snapshot = await query.GetSnapshotAsync();
            var properties = new List<Property>();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var property = doc.ConvertTo<Property>();
                    property.Id = doc.Id;
                    
                    // Apply category filter (case-insensitive)
                    if (!string.IsNullOrEmpty(categoryFilter))
                    {
                        if (string.IsNullOrEmpty(property.Category) || 
                            !property.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }
                    
                    // Apply status filter
                    if (statusFilter.HasValue)
                    {
                        // Check if status matches (handle both enum and string storage)
                        var statusMatches = false;
                        var docData = doc.ToDictionary();
                        
                        if (docData.ContainsKey("Status"))
                        {
                            var statusValue = docData["Status"];
                            
                            // Handle string storage
                            if (statusValue is string statusStr)
                            {
                                statusMatches = statusStr.Equals(statusFilter.Value.ToString(), StringComparison.OrdinalIgnoreCase);
                            }
                            // Handle enum storage (if stored as int)
                            else if (statusValue is long statusInt)
                            {
                                statusMatches = (int)statusInt == (int)statusFilter.Value;
                            }
                            // Handle enum from ConvertTo
                            else
                            {
                                statusMatches = property.Status == statusFilter.Value;
                            }
                        }
                        else
                        {
                            // Fallback to property.Status
                            statusMatches = property.Status == statusFilter.Value;
                        }
                        
                        if (!statusMatches)
                        {
                            continue;
                        }
                    }
                    
                    // Apply search filter if provided
                    if (string.IsNullOrEmpty(searchString) ||
                        property.PropertyName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        property.PropertyCode.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        (property.Description != null && property.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        property.Location.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    {
                        properties.Add(property);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting property document {DocId}", doc.Id);
                    continue;
                }
            }
            
            return properties.OrderByDescending(p => p.LastUpdated).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting properties");
            return new List<Property>();
        }
    }
    
    public async Task<List<string>> GetAllCategoriesAsync()
    {
        try
        {
            var snapshot = await _db.Collection("properties").GetSnapshotAsync();
            var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var property = doc.ConvertTo<Property>();
                    if (!string.IsNullOrWhiteSpace(property.Category))
                    {
                        categories.Add(property.Category.Trim());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting category from document {DocId}", doc.Id);
                    continue;
                }
            }
            
            return categories.OrderBy(c => c).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            return new List<string>();
        }
    }

    public async Task<Property?> GetPropertyByIdAsync(string id)
    {
        try
        {
            var doc = await _db.Collection("properties").Document(id).GetSnapshotAsync();
            if (doc.Exists)
            {
                var property = doc.ConvertTo<Property>();
                property.Id = doc.Id;
                return property;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property by id: {Id}", id);
            return null;
        }
    }

    public async Task<Property?> GetPropertyByCodeAsync(string propertyCode)
    {
        try
        {
            var query = _db.Collection("properties").WhereEqualTo("PropertyCode", propertyCode).Limit(1);
            var snapshot = await query.GetSnapshotAsync();
            
            if (snapshot.Count > 0)
            {
                var doc = snapshot.Documents[0];
                var property = doc.ConvertTo<Property>();
                property.Id = doc.Id;
                return property;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property by code: {Code}", propertyCode);
            return null;
        }
    }

    public async Task<Property?> GetPropertyByImageUrlAsync(string imageUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            // Normalize the image URL (trim and compare case-insensitively)
            var normalizedImageUrl = imageUrl.Trim();
            
            // Get all properties and filter in memory for case-insensitive matching
            var snapshot = await _db.Collection("properties").GetSnapshotAsync();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var property = doc.ConvertTo<Property>();
                    property.Id = doc.Id;
                    
                    // Compare ImageUrl case-insensitively
                    if (!string.IsNullOrWhiteSpace(property.ImageUrl) &&
                        property.ImageUrl.Trim().Equals(normalizedImageUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        return property;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting property document {DocId}", doc.Id);
                    continue;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property by image URL: {ImageUrl}", imageUrl);
            return null;
        }
    }

    public async Task<List<Property>> GetPropertiesByImageUrlAsync(string imageUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return new List<Property>();
            }

            // Normalize the image URL (trim and compare case-insensitively)
            var normalizedImageUrl = imageUrl.Trim();
            var properties = new List<Property>();
            
            // Get all properties and filter in memory for case-insensitive matching
            var snapshot = await _db.Collection("properties").GetSnapshotAsync();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var property = doc.ConvertTo<Property>();
                    property.Id = doc.Id;
                    
                    // Compare ImageUrl case-insensitively
                    if (!string.IsNullOrWhiteSpace(property.ImageUrl) &&
                        property.ImageUrl.Trim().Equals(normalizedImageUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        properties.Add(property);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting property document {DocId}", doc.Id);
                    continue;
                }
            }
            
            return properties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting properties by image URL: {ImageUrl}", imageUrl);
            return new List<Property>();
        }
    }

    public async Task<Dictionary<string, int>> GetQuantityBreakdownByImageUrlAsync(string imageUrl)
    {
        try
        {
            var properties = await GetPropertiesByImageUrlAsync(imageUrl);
            var breakdown = new Dictionary<string, int>
            {
                { "Total", 0 },
                { "Available", 0 },
                { "InUse", 0 },
                { "UnderMaintenance", 0 },
                { "Damaged", 0 }
            };

            foreach (var property in properties)
            {
                breakdown["Total"] += property.Quantity;
                
                switch (property.Status)
                {
                    case PropertyStatus.Available:
                        breakdown["Available"] += property.Quantity;
                        break;
                    case PropertyStatus.InUse:
                        breakdown["InUse"] += property.Quantity;
                        break;
                    case PropertyStatus.UnderMaintenance:
                        breakdown["UnderMaintenance"] += property.Quantity;
                        break;
                    case PropertyStatus.Damaged:
                        breakdown["Damaged"] += property.Quantity;
                        break;
                }
            }

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quantity breakdown by image URL: {ImageUrl}", imageUrl);
            return new Dictionary<string, int>();
        }
    }

    public async Task<List<Property>> GetIndividualItemsByImageUrlAsync(string imageUrl)
    {
        try
        {
            return await GetPropertiesByImageUrlAsync(imageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting individual items by image URL: {ImageUrl}", imageUrl);
            return new List<Property>();
        }
    }

    public async Task<List<Property>> GetIndividualItemsByPropertyCodeAsync(string propertyCode)
    {
        try
        {
            // Get all properties with the same base property code (e.g., PROP-001, PROP-001-001, etc.)
            var allProperties = await GetAllPropertiesAsync();
            var matchingProperties = allProperties
                .Where(p => !string.IsNullOrEmpty(p.PropertyCode) && 
                           (p.PropertyCode == propertyCode || 
                            p.PropertyCode.StartsWith(propertyCode + "-", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(p => p.SerialNumber)
                .ToList();

            return matchingProperties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting individual items by property code: {PropertyCode}", propertyCode);
            return new List<Property>();
        }
    }

    public async Task<Dictionary<string, List<Property>>> GetPropertiesByStatusByImageUrlAsync(string imageUrl)
    {
        try
        {
            var properties = await GetPropertiesByImageUrlAsync(imageUrl);
            var breakdown = new Dictionary<string, List<Property>>
            {
                { "Available", new List<Property>() },
                { "InUse", new List<Property>() },
                { "UnderMaintenance", new List<Property>() },
                { "Damaged", new List<Property>() }
            };

            foreach (var property in properties)
            {
                switch (property.Status)
                {
                    case PropertyStatus.Available:
                        breakdown["Available"].Add(property);
                        break;
                    case PropertyStatus.InUse:
                        breakdown["InUse"].Add(property);
                        break;
                    case PropertyStatus.UnderMaintenance:
                        breakdown["UnderMaintenance"].Add(property);
                        break;
                    case PropertyStatus.Damaged:
                        breakdown["Damaged"].Add(property);
                        break;
                }
            }

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting properties by status by image URL: {ImageUrl}", imageUrl);
            return new Dictionary<string, List<Property>>();
        }
    }

    public async Task<Dictionary<string, List<Property>>> GetPropertiesByStatusByPropertyCodeAsync(string propertyCode)
    {
        try
        {
            // Get all properties with the same base property code
            var allProperties = await GetAllPropertiesAsync();
            var matchingProperties = allProperties
                .Where(p => !string.IsNullOrEmpty(p.PropertyCode) && 
                           (p.PropertyCode == propertyCode || 
                            p.PropertyCode.StartsWith(propertyCode + "-", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var breakdown = new Dictionary<string, List<Property>>
            {
                { "Available", new List<Property>() },
                { "InUse", new List<Property>() },
                { "UnderMaintenance", new List<Property>() },
                { "Damaged", new List<Property>() }
            };

            foreach (var property in matchingProperties)
            {
                switch (property.Status)
                {
                    case PropertyStatus.Available:
                        breakdown["Available"].Add(property);
                        break;
                    case PropertyStatus.InUse:
                        breakdown["InUse"].Add(property);
                        break;
                    case PropertyStatus.UnderMaintenance:
                        breakdown["UnderMaintenance"].Add(property);
                        break;
                    case PropertyStatus.Damaged:
                        breakdown["Damaged"].Add(property);
                        break;
                }
            }

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting properties by status by property code: {PropertyCode}", propertyCode);
            return new Dictionary<string, List<Property>>();
        }
    }

    public async Task<Dictionary<string, int>> GetQuantityBreakdownByPropertyCodeAsync(string propertyCode)
    {
        try
        {
            // Get all properties with the same base property code (e.g., PROP-001, PROP-001-001, etc.)
            var allProperties = await GetAllPropertiesAsync();
            var matchingProperties = allProperties
                .Where(p => !string.IsNullOrEmpty(p.PropertyCode) && 
                           (p.PropertyCode == propertyCode || 
                            p.PropertyCode.StartsWith(propertyCode + "-", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var breakdown = new Dictionary<string, int>
            {
                { "Total", 0 },
                { "Available", 0 },
                { "InUse", 0 },
                { "UnderMaintenance", 0 },
                { "Damaged", 0 }
            };

            foreach (var property in matchingProperties)
            {
                breakdown["Total"] += property.Quantity;
                
                switch (property.Status)
                {
                    case PropertyStatus.Available:
                        breakdown["Available"] += property.Quantity;
                        break;
                    case PropertyStatus.InUse:
                        breakdown["InUse"] += property.Quantity;
                        break;
                    case PropertyStatus.UnderMaintenance:
                        breakdown["UnderMaintenance"] += property.Quantity;
                        break;
                    case PropertyStatus.Damaged:
                        breakdown["Damaged"] += property.Quantity;
                        break;
                }
            }

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quantity breakdown by property code: {PropertyCode}", propertyCode);
            return new Dictionary<string, int>();
        }
    }

    public async Task<string> CreatePropertyAsync(Property property)
    {
        try
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property), "Property cannot be null");
            }

            // Ensure required fields are set
            if (string.IsNullOrWhiteSpace(property.PropertyCode))
            {
                throw new ArgumentException("PropertyCode is required", nameof(property));
            }

            if (string.IsNullOrWhiteSpace(property.PropertyName))
            {
                throw new ArgumentException("PropertyName is required", nameof(property));
            }

            if (string.IsNullOrWhiteSpace(property.Category))
            {
                throw new ArgumentException("Category is required", nameof(property));
            }

            if (string.IsNullOrWhiteSpace(property.Location))
            {
                throw new ArgumentException("Location is required", nameof(property));
            }

            var docRef = _db.Collection("properties").Document();
            property.Id = docRef.Id;
            
            // Ensure LastUpdated is set and in UTC
            if (property.LastUpdated == default(DateTime))
            {
                property.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                // Convert to UTC if not already
                if (property.LastUpdated.Kind != DateTimeKind.Utc)
                {
                    property.LastUpdated = property.LastUpdated.ToUniversalTime();
                }
            }
            
            // Ensure DateReceived is in UTC if it has a value
            if (property.DateReceived.HasValue && property.DateReceived.Value.Kind != DateTimeKind.Utc)
            {
                property.DateReceived = property.DateReceived.Value.ToUniversalTime();
            }
            
            _logger.LogInformation("Creating property: Code={Code}, Name={Name}, Category={Category}, LastUpdated={LastUpdated}", 
                property.PropertyCode, property.PropertyName, property.Category, property.LastUpdated);
            
            await docRef.SetAsync(property);
            
            _logger.LogInformation("Property created successfully: Id={Id}, Code={Code}", docRef.Id, property.PropertyCode);
            
            return docRef.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating property: Code={Code}, Error={Error}", 
                property?.PropertyCode ?? "Unknown", ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            throw;
        }
    }

    public async Task<bool> UpdatePropertyAsync(string id, Property property)
    {
        try
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property), "Property cannot be null");
            }

            // Ensure LastUpdated is in UTC
            if (property.LastUpdated.Kind != DateTimeKind.Utc)
            {
                property.LastUpdated = property.LastUpdated.ToUniversalTime();
            }
            else if (property.LastUpdated == default(DateTime))
            {
                property.LastUpdated = DateTime.UtcNow;
            }

            // Ensure DateReceived is in UTC if it has a value
            if (property.DateReceived.HasValue && property.DateReceived.Value.Kind != DateTimeKind.Utc)
            {
                property.DateReceived = property.DateReceived.Value.ToUniversalTime();
            }

            await _db.Collection("properties").Document(id).SetAsync(property, SetOptions.MergeAll);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property: {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeletePropertyAsync(string id)
    {
        try
        {
            await _db.Collection("properties").Document(id).DeleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting property: {Id}", id);
            return false;
        }
    }

    public async Task<List<Property>> GetPropertiesEditedByUserAsync(string userEmail)
    {
        try
        {
            var snapshot = await _db.Collection("properties").GetSnapshotAsync();
            var properties = new List<Property>();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var property = doc.ConvertTo<Property>();
                    property.Id = doc.Id;
                    
                    // Check if UpdatedBy matches user email (case-insensitive)
                    if (!string.IsNullOrEmpty(property.UpdatedBy) && 
                        property.UpdatedBy.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        properties.Add(property);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting property document {DocId}", doc.Id);
                    continue;
                }
            }
            
            // Sort by LastUpdated descending (most recent first)
            return properties.OrderByDescending(p => p.LastUpdated).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting properties edited by user: {Email}", userEmail);
            return new List<Property>();
        }
    }

    public async Task<bool> PropertyCodeExistsAsync(string propertyCode, string? excludeId = null)
    {
        try
        {
            var query = _db.Collection("properties").WhereEqualTo("PropertyCode", propertyCode);
            var snapshot = await query.GetSnapshotAsync();
            
            if (snapshot.Count > 0)
            {
                if (excludeId != null)
                {
                    return snapshot.Documents.Any(d => d.Id != excludeId);
                }
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking property code: {Code}", propertyCode);
            return false;
        }
    }

    // Account Request operations
    public async Task<string> CreateAccountRequestAsync(AccountRequest request)
    {
        try
        {
            // Ensure RequestedAt is in UTC
            if (request.RequestedAt == default(DateTime))
            {
                request.RequestedAt = DateTime.UtcNow;
            }
            else if (request.RequestedAt.Kind != DateTimeKind.Utc)
            {
                request.RequestedAt = request.RequestedAt.ToUniversalTime();
            }
            
            // Ensure Status is set
            if (request.Status == default(AccountRequestStatus))
            {
                request.Status = AccountRequestStatus.Pending;
            }
            
            // Normalize email
            request.Email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            
            var docRef = _db.Collection("accountRequests").Document();
            request.Id = docRef.Id;
            
            _logger.LogInformation("Creating account request: Id={Id}, Email={Email}, Name={Name}, Status={Status}, RequestedAt={RequestedAt}", 
                docRef.Id, request.Email, request.FullName, request.Status, request.RequestedAt);
            
            await docRef.SetAsync(request);
            
            _logger.LogInformation("Account request created successfully: Id={Id}, Email={Email}", docRef.Id, request.Email);
            return docRef.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account request: {Email}, Error: {Error}", request.Email, ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            throw;
        }
    }

    public async Task<List<AccountRequest>> GetPendingAccountRequestsAsync()
    {
        try
        {
            // Get all account requests and filter in memory to handle enum storage variations
            var query = _db.Collection("accountRequests")
                .OrderBy("RequestedAt");
            
            var snapshot = await query.GetSnapshotAsync();
            var requests = new List<AccountRequest>();
            
            _logger.LogInformation("Found {Count} total account requests in database", snapshot.Count);
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var docData = doc.ToDictionary();
                    var request = doc.ConvertTo<AccountRequest>();
                    request.Id = doc.Id;
                    
                    // Check status - handle both string and enum storage
                    var statusMatches = false;
                    if (docData.ContainsKey("Status"))
                    {
                        var statusValue = docData["Status"];
                        
                        // Handle string storage
                        if (statusValue is string statusStr)
                        {
                            statusMatches = statusStr.Equals(AccountRequestStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase);
                            _logger.LogDebug("Account request {Id} - Status as string: '{Status}', Matches: {Matches}", 
                                doc.Id, statusStr, statusMatches);
                        }
                        // Handle enum storage (if stored as int)
                        else if (statusValue is long statusInt)
                        {
                            statusMatches = (int)statusInt == (int)AccountRequestStatus.Pending;
                            _logger.LogDebug("Account request {Id} - Status as int: {Status}, Matches: {Matches}", 
                                doc.Id, statusInt, statusMatches);
                        }
                        // Handle enum from ConvertTo
                        else
                        {
                            statusMatches = request.Status == AccountRequestStatus.Pending;
                            _logger.LogDebug("Account request {Id} - Status from ConvertTo: {Status}, Matches: {Matches}", 
                                doc.Id, request.Status, statusMatches);
                        }
                    }
                    else
                    {
                        // If no Status field, default to Pending (for backward compatibility)
                        statusMatches = request.Status == AccountRequestStatus.Pending;
                        _logger.LogDebug("Account request {Id} - No Status field, using ConvertTo: {Status}, Matches: {Matches}", 
                            doc.Id, request.Status, statusMatches);
                    }
                    
                    if (statusMatches)
                    {
                        requests.Add(request);
                        _logger.LogInformation("Added pending account request: {Id}, Email: {Email}, Name: {Name}", 
                            request.Id, request.Email, request.FullName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing account request document {DocId}", doc.Id);
                    continue;
                }
            }
            
            _logger.LogInformation("Returning {Count} pending account requests", requests.Count);
            return requests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending account requests");
            return new List<AccountRequest>();
        }
    }

    public async Task<List<AccountRequest>> GetAllAccountRequestsAsync(int? limit = null)
    {
        try
        {
            var query = _db.Collection("accountRequests")
                .OrderByDescending("RequestedAt");
            
            if (limit.HasValue && limit.Value > 0)
            {
                query = query.Limit(limit.Value);
            }
            
            var snapshot = await query.GetSnapshotAsync();
            var requests = new List<AccountRequest>();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var request = doc.ConvertTo<AccountRequest>();
                    request.Id = doc.Id;
                    requests.Add(request);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting account request document {DocId}", doc.Id);
                    continue;
                }
            }
            
            return requests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all account requests");
            return new List<AccountRequest>();
        }
    }

    public async Task<AccountRequest?> GetAccountRequestByIdAsync(string id)
    {
        try
        {
            var doc = await _db.Collection("accountRequests").Document(id).GetSnapshotAsync();
            if (doc.Exists)
            {
                var request = doc.ConvertTo<AccountRequest>();
                request.Id = doc.Id;
                return request;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account request: {Id}", id);
            return null;
        }
    }

    public async Task<bool> UpdateAccountRequestAsync(string id, AccountRequest request)
    {
        try
        {
            await _db.Collection("accountRequests").Document(id).SetAsync(request, SetOptions.MergeAll);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account request: {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteAccountRequestAsync(string id)
    {
        try
        {
            await _db.Collection("accountRequests").Document(id).DeleteAsync();
            _logger.LogInformation("Account request deleted successfully: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account request: {Id}", id);
            throw;
        }
    }

    public async Task<bool> AccountRequestExistsAsync(string email)
    {
        try
        {
            var query = _db.Collection("accountRequests")
                .WhereEqualTo("Email", email)
                .WhereEqualTo("Status", AccountRequestStatus.Pending.ToString())
                .Limit(1);
            
            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking account request: {Email}", email);
            return false;
        }
    }

    public async Task<(int Approved, int Rejected, int Pending)> GetAccountRequestCountsAsync()
    {
        try
        {
            var query = _db.Collection("accountRequests");
            var snapshot = await query.GetSnapshotAsync();
            
            int approved = 0;
            int rejected = 0;
            int pending = 0;
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var docData = doc.ToDictionary();
                    AccountRequestStatus status = AccountRequestStatus.Pending;
                    
                    if (docData.ContainsKey("Status"))
                    {
                        var statusValue = docData["Status"];
                        
                        if (statusValue is string statusStr)
                        {
                            if (Enum.TryParse<AccountRequestStatus>(statusStr, true, out var parsedStatus))
                            {
                                status = parsedStatus;
                            }
                        }
                        else if (statusValue is long statusInt)
                        {
                            status = (AccountRequestStatus)(int)statusInt;
                        }
                        else
                        {
                            var request = doc.ConvertTo<AccountRequest>();
                            status = request.Status;
                        }
                    }
                    else
                    {
                        var request = doc.ConvertTo<AccountRequest>();
                        status = request.Status;
                    }
                    
                    switch (status)
                    {
                        case AccountRequestStatus.Approved:
                            approved++;
                            break;
                        case AccountRequestStatus.Rejected:
                            rejected++;
                            break;
                        case AccountRequestStatus.Pending:
                            pending++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing account request document {DocId} for counts", doc.Id);
                    continue;
                }
            }
            
            return (approved, rejected, pending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account request counts");
            return (0, 0, 0);
        }
    }

    // OTP operations
    public async Task<string> CreateOtpVerificationAsync(OtpVerification otp)
    {
        try
        {
            var docRef = _db.Collection("otpVerifications").Document();
            otp.Id = docRef.Id;
            
            // Ensure OTP code is stored as STRING explicitly (not as number)
            // Prefix with "OTP_" to force Firestore to treat it as string
            var otpCodeString = $"OTP_{otp.OtpCode}";
            
            var otpData = new Dictionary<string, object>
            {
                { "Id", otp.Id },
                { "Email", otp.Email.ToLowerInvariant() }, // Normalize email
                { "OtpCode", otpCodeString }, // Store with prefix to ensure string type
                { "OtpCodeRaw", otp.OtpCode }, // Also store raw for backward compatibility
                { "FullName", otp.FullName ?? string.Empty },
                { "PasswordHash", otp.PasswordHash ?? string.Empty },
                { "CreatedAt", Timestamp.FromDateTime(otp.CreatedAt.ToUniversalTime()) },
                { "ExpiresAt", Timestamp.FromDateTime(otp.ExpiresAt.ToUniversalTime()) },
                { "IsUsed", otp.IsUsed }
            };
            
            await docRef.SetAsync(otpData);
            
            _logger.LogInformation("✓ OTP stored in Firestore - DocId: {DocId}, Email: {Email}, OTP: {OtpCode} (Stored as: {StoredCode})", 
                docRef.Id, otp.Email, otp.OtpCode, otpCodeString);
            
            return docRef.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating OTP verification: {Email}", otp.Email);
            throw;
        }
    }

    public async Task<OtpVerification?> GetOtpVerificationAsync(string email, string otpCode)
    {
        try
        {
            // Normalize input - extract only digits
            var inputOtpCode = otpCode?.Trim() ?? string.Empty;
            inputOtpCode = new string(inputOtpCode.Where(char.IsDigit).ToArray());
            
            // Ensure exactly 6 digits
            if (inputOtpCode.Length == 0)
            {
                _logger.LogWarning("Empty OTP code provided");
                return null;
            }
            
            if (inputOtpCode.Length < 6)
            {
                inputOtpCode = inputOtpCode.PadLeft(6, '0');
            }
            else if (inputOtpCode.Length > 6)
            {
                inputOtpCode = inputOtpCode.Substring(0, 6);
            }
            
            var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
            
            _logger.LogInformation("=== OTP VERIFICATION START ===");
            _logger.LogInformation("Searching for - Email: '{Email}' (Normalized: '{NormalizedEmail}'), OTP: '{OriginalOtp}' (Normalized: '{NormalizedOtp}', Length: {Length})", 
                email, normalizedEmail, otpCode, inputOtpCode, inputOtpCode.Length);
            
            var now = DateTime.UtcNow;
            
            // SIMPLER APPROACH: Get all unused OTPs and check manually
            var query = _db.Collection("otpVerifications")
                .WhereEqualTo("IsUsed", false)
                .OrderByDescending("CreatedAt")
                .Limit(200); // Get more records to be safe
            
            var snapshot = await query.GetSnapshotAsync();
            _logger.LogInformation("Found {Count} total unused OTP records in database", snapshot.Count);
            
            // Check each document
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var data = doc.ToDictionary();
                    
                    // Get email and normalize
                    var storedEmail = string.Empty;
                    if (data.ContainsKey("Email"))
                    {
                        var emailValue = data["Email"];
                        storedEmail = emailValue?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
                    }
                    
                    // Skip if email doesn't match (case-insensitive)
                    if (!string.Equals(storedEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    // Get OTP code - handle all possible Firestore types
                    string? storedOtpCode = null;
                    
                    // Try OtpCodeRaw first (new format)
                    if (data.ContainsKey("OtpCodeRaw"))
                    {
                        var otpValue = data["OtpCodeRaw"];
                        if (otpValue is string str)
                        {
                            storedOtpCode = str;
                        }
                        else if (otpValue != null)
                        {
                            storedOtpCode = otpValue.ToString()?.Trim() ?? string.Empty;
                        }
                    }
                    
                    // Fallback to OtpCode (may have OTP_ prefix or be a number)
                    if (string.IsNullOrEmpty(storedOtpCode) && data.ContainsKey("OtpCode"))
                    {
                        var otpValue = data["OtpCode"];
                        
                        if (otpValue is string str)
                        {
                            // Remove OTP_ prefix if present
                            storedOtpCode = str.Replace("OTP_", "");
                        }
                        else if (otpValue is long lng)
                        {
                            storedOtpCode = lng.ToString("D6");
                        }
                        else if (otpValue is int num)
                        {
                            storedOtpCode = num.ToString("D6");
                        }
                        else if (otpValue is System.Int64 int64)
                        {
                            storedOtpCode = int64.ToString("D6");
                        }
                        else if (otpValue != null)
                        {
                            storedOtpCode = otpValue.ToString()?.Trim() ?? string.Empty;
                        }
                    }
                    
                    if (string.IsNullOrEmpty(storedOtpCode))
                    {
                        _logger.LogWarning("OTP code is null or empty in document {DocId}", doc.Id);
                        continue;
                    }
                    
                    // Normalize stored OTP - extract only digits
                    storedOtpCode = new string(storedOtpCode.Where(char.IsDigit).ToArray());
                    
                    // Ensure exactly 6 digits
                    if (storedOtpCode.Length < 6)
                    {
                        storedOtpCode = storedOtpCode.PadLeft(6, '0');
                    }
                    else if (storedOtpCode.Length > 6)
                    {
                        storedOtpCode = storedOtpCode.Substring(0, 6);
                    }
                    
                    // Get expiration
                    DateTime expiresAt = DateTime.MinValue;
                    if (data.ContainsKey("ExpiresAt"))
                    {
                        var expValue = data["ExpiresAt"];
                        if (expValue is Timestamp expTs)
                        {
                            expiresAt = expTs.ToDateTime();
                        }
                        else if (expValue is DateTime dt)
                        {
                            expiresAt = dt;
                        }
                    }
                    
                    // Get IsUsed
                    var isUsed = false;
                    if (data.ContainsKey("IsUsed"))
                    {
                        var usedValue = data["IsUsed"];
                        if (usedValue is bool used)
                        {
                            isUsed = used;
                        }
                    }
                    
                    if (isUsed)
                    {
                        _logger.LogDebug("Skipping used OTP - DocId: {DocId}", doc.Id);
                        continue;
                    }
                    
                    _logger.LogInformation("Checking OTP - DocId: {DocId}, Email: '{Email}', Stored Code: '{StoredCode}' (Length: {Length}), Input Code: '{InputCode}' (Length: {InputLength}), ExpiresAt: {ExpiresAt}", 
                        doc.Id, storedEmail, storedOtpCode, storedOtpCode.Length, inputOtpCode, inputOtpCode.Length, expiresAt);
                    
                    // SIMPLE STRING COMPARISON - both should be 6-digit strings now
                    var otpMatches = string.Equals(storedOtpCode, inputOtpCode, StringComparison.Ordinal);
                    
                    _logger.LogInformation("OTP Comparison Result - Match: {Match} | Stored: '{StoredCode}' ({StoredType}) == Input: '{InputCode}' ({InputType})", 
                        otpMatches, storedOtpCode, storedOtpCode.GetType().Name, inputOtpCode, inputOtpCode.GetType().Name);
                    
                    if (otpMatches)
                    {
                        // Check expiration
                        if (expiresAt > now)
                        {
                            _logger.LogInformation("✓✓✓ OTP MATCHED AND VALID! ✓✓✓");
                            _logger.LogInformation("DocId: {DocId}, Email: {Email}, Code: {Code}, ExpiresAt: {ExpiresAt}, Now: {Now}", 
                                doc.Id, storedEmail, storedOtpCode, expiresAt, now);
                            
                            // Create OtpVerification object
                            var otp = new OtpVerification
                            {
                                Id = doc.Id,
                                Email = storedEmail,
                                OtpCode = storedOtpCode,
                                ExpiresAt = expiresAt,
                                IsUsed = false
                            };
                            
                            // Try to get other fields if available
                            if (data.ContainsKey("FullName"))
                            {
                                otp.FullName = data["FullName"]?.ToString();
                            }
                            if (data.ContainsKey("PasswordHash"))
                            {
                                otp.PasswordHash = data["PasswordHash"]?.ToString();
                            }
                            if (data.ContainsKey("CreatedAt"))
                            {
                                var createdValue = data["CreatedAt"];
                                if (createdValue is Timestamp createdTs)
                                {
                                    otp.CreatedAt = createdTs.ToDateTime();
                                }
                            }
                            
                            return otp;
                        }
                        else
                        {
                            var diffMinutes = (now - expiresAt).TotalMinutes;
                            _logger.LogWarning("OTP matched but EXPIRED. ExpiresAt: {ExpiresAt}, Now: {Now}, Expired {Diff} minutes ago", 
                                expiresAt, now, diffMinutes);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("OTP codes DON'T MATCH - Stored: '{StoredCode}' != Input: '{InputCode}'", 
                            storedOtpCode, inputOtpCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing OTP document {DocId}: {Error}", doc.Id, ex.Message);
                    continue;
                }
            }
            
            _logger.LogWarning("=== OTP VERIFICATION FAILED ===");
            _logger.LogWarning("No matching valid OTP found for Email: '{Email}', OTP: '{OtpCode}'", email, otpCode);
            _logger.LogWarning("Searched through {Count} unused OTP records", snapshot.Count);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting OTP verification: Email={Email}, OTP={OtpCode}, Error: {Error}", email, otpCode, ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            return null;
        }
    }

    public async Task<bool> MarkOtpAsUsedAsync(string id)
    {
        try
        {
            await _db.Collection("otpVerifications").Document(id).UpdateAsync("IsUsed", true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking OTP as used: {Id}", id);
            return false;
        }
    }

    // Get overdue properties (properties with ReturnDate in the past and Status = InUse)
    public async Task<List<Property>> GetOverduePropertiesAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var snapshot = await _db.Collection("properties").GetSnapshotAsync();
            var overdueProperties = new List<Property>();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var property = doc.ConvertTo<Property>();
                    property.Id = doc.Id;
                    
                    // Check if property is overdue:
                    // 1. Status must be InUse
                    // 2. ReturnDate must be set and in the past
                    // 3. BorrowerName must be set
                    if (property.Status == PropertyStatus.InUse &&
                        property.ReturnDate.HasValue &&
                        property.ReturnDate.Value.ToUniversalTime() < now &&
                        !string.IsNullOrWhiteSpace(property.BorrowerName))
                    {
                        overdueProperties.Add(property);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting property document {DocId}", doc.Id);
                    continue;
                }
            }
            
            return overdueProperties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue properties");
            return new List<Property>();
        }
    }

    // Get all individual borrowed properties (Status = InUse with BorrowerName)
    public async Task<List<Property>> GetBorrowedPropertiesAsync()
    {
        try
        {
            var snapshot = await _db.Collection("properties").GetSnapshotAsync();
            var borrowedProperties = new List<Property>();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var property = doc.ConvertTo<Property>();
                    property.Id = doc.Id;
                    
                    // Check if property is borrowed:
                    // 1. Status must be InUse
                    // 2. BorrowerName must be set
                    if (property.Status == PropertyStatus.InUse &&
                        !string.IsNullOrWhiteSpace(property.BorrowerName))
                    {
                        borrowedProperties.Add(property);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting property document {DocId}", doc.Id);
                    continue;
                }
            }
            
            return borrowedProperties.OrderByDescending(p => p.BorrowedDate ?? p.LastUpdated).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting borrowed properties");
            return new List<Property>();
        }
    }

    public async Task<List<Property>> GetBorrowingHistoryByUserAsync(string userEmail)
    {
        try
        {
            var snapshot = await _db.Collection("properties").GetSnapshotAsync();
            var borrowingHistory = new List<Property>();
            
            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var property = doc.ConvertTo<Property>();
                    property.Id = doc.Id;
                    
                    // Get all properties that have been borrowed by this user (current or past)
                    // Check if UpdatedBy matches the user email (case-insensitive)
                    if (!string.IsNullOrWhiteSpace(property.BorrowerName) &&
                        !string.IsNullOrWhiteSpace(property.UpdatedBy) &&
                        property.UpdatedBy.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        borrowingHistory.Add(property);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting property document {DocId}", doc.Id);
                    continue;
                }
            }
            
            // Sort by BorrowedDate descending (most recent first), or LastUpdated if BorrowedDate is null
            return borrowingHistory.OrderByDescending(p => p.BorrowedDate ?? p.LastUpdated).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting borrowing history for user: {Email}", userEmail);
            return new List<Property>();
        }
    }

    // Mark overdue notification as sent for a property
    public async Task<bool> MarkOverdueNotificationSentAsync(string propertyId)
    {
        try
        {
            await _db.Collection("properties").Document(propertyId).UpdateAsync("OverdueNotificationSent", true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking overdue notification as sent: {Id}", propertyId);
            return false;
        }
    }
}

