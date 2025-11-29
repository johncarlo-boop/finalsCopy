using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace PropertyInventory.Models;

[FirestoreData]
public class OtpVerification
{
    [FirestoreProperty]
    public string Id { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6)]
    [FirestoreProperty]
    public string OtpCode { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime ExpiresAt { get; set; }

    [FirestoreProperty]
    public bool IsUsed { get; set; } = false;

    // Store registration data temporarily
    [FirestoreProperty]
    public string? FullName { get; set; }
    
    [FirestoreProperty]
    public string? PasswordHash { get; set; }
}



