using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace PropertyInventory.Models;

[FirestoreData]
public class ApplicationUser
{
    [FirestoreProperty]
    public string Id { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;

    [Required]
    [FirestoreProperty]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [FirestoreProperty]
    public string FullName { get; set; } = string.Empty;

    [FirestoreProperty]
    public bool IsAdmin { get; set; } = true; // All users are admin by default

    [FirestoreProperty]
    public UserType UserType { get; set; } = UserType.Admin; // Admin or MobileUser

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public string? ProfilePicturePath { get; set; }

    [FirestoreProperty]
    public bool RequiresPasswordChange { get; set; } = false;

    [FirestoreProperty]
    public bool IsApproved { get; set; } = true; // For account requests
}

public enum UserType
{
    Admin,
    MobileUser
}

