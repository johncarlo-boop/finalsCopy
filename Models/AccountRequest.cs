using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace PropertyInventory.Models;

[FirestoreData]
public class AccountRequest
{
    [FirestoreProperty]
    public string Id { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;

    [Required]
    [FirestoreProperty]
    public string FullName { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? Position { get; set; }

    [FirestoreProperty]
    public AccountRequestStatus Status { get; set; } = AccountRequestStatus.Pending;

    [FirestoreProperty]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? ReviewedAt { get; set; }

    [FirestoreProperty]
    public string? ReviewedBy { get; set; }

    [FirestoreProperty]
    public string? RejectionReason { get; set; }
}

public enum AccountRequestStatus
{
    Pending,
    Approved,
    Rejected
}





