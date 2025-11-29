using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages;

[Authorize(Roles = "Admin")]
public class AnalyticsModel : PageModel
{
    private readonly FirebaseService _firebaseService;

    public AnalyticsModel(FirebaseService firebaseService)
    {
        _firebaseService = firebaseService;
    }

    public Dictionary<string, int> StatusCounts { get; private set; } = new();
    public Dictionary<string, int> CategoryCounts { get; private set; } = new();
    public Dictionary<string, int> BorrowingTrends { get; private set; } = new();
    public Dictionary<string, int> TopBorrowers { get; private set; } = new();
    public Dictionary<string, int> BorrowingByCategory { get; private set; } = new();
    public int TotalProperties { get; private set; }
    public int ActiveProperties { get; private set; }
    public int UnderMaintenance { get; private set; }
    public int DamagedProperties { get; private set; }
    public int BorrowedProperties { get; private set; }
    public int OverdueProperties { get; private set; }
    public int RecentlyBorrowed7Days { get; private set; }
    public int RecentlyBorrowed30Days { get; private set; }
    public int TotalBorrowingTransactions { get; private set; }

    public async Task OnGetAsync()
    {
        var properties = await _firebaseService.GetAllPropertiesAsync();
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);
        
        TotalProperties = properties.Count;

        // Initialize status counts
        foreach (var statusName in Enum.GetNames(typeof(PropertyStatus)))
        {
            {
                StatusCounts[statusName] = 0;
            }
        }

        // Initialize borrowing trends for last 30 days
        for (int i = 29; i >= 0; i--)
        {
            var date = now.AddDays(-i);
            var dateKey = date.ToString("MM/dd");
            BorrowingTrends[dateKey] = 0;
        }

        foreach (var property in properties)
        {
            var statusKey = property.Status.ToString();
            
            // Process status
            if (true)
            {
                StatusCounts[statusKey] = StatusCounts.TryGetValue(statusKey, out var existingStatusCount)
                    ? existingStatusCount + 1
                    : 1;
            }

            var categoryKey = string.IsNullOrWhiteSpace(property.Category) ? "Uncategorized" : property.Category;
            CategoryCounts[categoryKey] = CategoryCounts.TryGetValue(categoryKey, out var existingCategoryCount)
                ? existingCategoryCount + 1
                : 1;

            // Borrowing analytics
            if (property.Status == PropertyStatus.InUse && 
                !string.IsNullOrWhiteSpace(property.BorrowerName) && 
                property.BorrowedDate.HasValue)
            {
                BorrowedProperties++;
                TotalBorrowingTransactions++;

                // Check if overdue
                if (property.ReturnDate.HasValue && 
                    property.ReturnDate.Value.ToUniversalTime() < now)
                {
                    OverdueProperties++;
                }

                // Recently borrowed (last 7 days)
                if (property.BorrowedDate.Value.ToUniversalTime() >= sevenDaysAgo)
                {
                    RecentlyBorrowed7Days++;
                }

                // Recently borrowed (last 30 days)
                if (property.BorrowedDate.Value.ToUniversalTime() >= thirtyDaysAgo)
                {
                    RecentlyBorrowed30Days++;
                    
                    // Add to borrowing trends
                    var borrowedDate = property.BorrowedDate.Value.ToUniversalTime();
                    var dateKey = borrowedDate.ToString("MM/dd");
                    if (BorrowingTrends.ContainsKey(dateKey))
                    {
                        BorrowingTrends[dateKey]++;
                    }
                }

                // Top borrowers
                var borrowerKey = property.BorrowerName;
                TopBorrowers[borrowerKey] = TopBorrowers.TryGetValue(borrowerKey, out var existingBorrowerCount)
                    ? existingBorrowerCount + 1
                    : 1;

                // Borrowing by category
                var borrowingCategoryKey = string.IsNullOrWhiteSpace(property.Category) ? "Uncategorized" : property.Category;
                BorrowingByCategory[borrowingCategoryKey] = BorrowingByCategory.TryGetValue(borrowingCategoryKey, out var existingCategoryBorrowCount)
                    ? existingCategoryBorrowCount + 1
                    : 1;
            }
        }

        ActiveProperties = StatusCounts.TryGetValue(nameof(PropertyStatus.InUse), out var active) ? active : 0;
        UnderMaintenance = StatusCounts.TryGetValue(nameof(PropertyStatus.UnderMaintenance), out var maintenance) ? maintenance : 0;
        DamagedProperties = StatusCounts.TryGetValue(nameof(PropertyStatus.Damaged), out var damaged) ? damaged : 0;
    }
}


