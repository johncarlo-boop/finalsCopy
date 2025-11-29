using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace PropertyInventory.Models;

[FirestoreData]
public class Property
{
    [FirestoreProperty]
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Property Code")]
    [FirestoreProperty]
    public string PropertyCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Property Name")]
    [FirestoreProperty]
    public string PropertyName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Category")]
    [FirestoreProperty]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [FirestoreProperty]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Location")]
    [FirestoreProperty]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Status")]
    [FirestoreProperty]
    public PropertyStatus Status { get; set; } = PropertyStatus.Available;

    [Display(Name = "Quantity")]
    [FirestoreProperty]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Date Received")]
    [DataType(DataType.Date)]
    [FirestoreProperty]
    public DateTime? DateReceived { get; set; }

    [Display(Name = "Tag Number")]
    [FirestoreProperty]
    public string? SerialNumber { get; set; }

    [Display(Name = "Image URL")]
    [DataType(DataType.ImageUrl)]
    [FirestoreProperty]
    public string? ImageUrl { get; set; }

    [Display(Name = "Last Updated")]
    [FirestoreProperty]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [Display(Name = "Updated By")]
    [FirestoreProperty]
    public string? UpdatedBy { get; set; }

    [Display(Name = "Remarks")]
    [FirestoreProperty]
    public string? Remarks { get; set; }

    [Display(Name = "Borrower Name")]
    [FirestoreProperty]
    public string? BorrowerName { get; set; }

    [Display(Name = "Borrowed Date & Time")]
    [FirestoreProperty]
    public DateTime? BorrowedDate { get; set; }

    [Display(Name = "Return Date & Time")]
    [DataType(DataType.DateTime)]
    [FirestoreProperty]
    public DateTime? ReturnDate { get; set; }

    [Display(Name = "Overdue Notification Sent")]
    [FirestoreProperty]
    public bool OverdueNotificationSent { get; set; } = false;
}

public enum PropertyStatus
{
    Available,
    InUse,
    UnderMaintenance,
    Damaged
}
