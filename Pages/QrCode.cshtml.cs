using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyInventory.Models;
using PropertyInventory.Services;

namespace PropertyInventory.Pages;

[Authorize(Roles = "Admin")]
public class QrCodeModel : PageModel
{
    private readonly FirebaseService _firebaseService;
    private readonly QrCodeService _qrCodeService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QrCodeModel(FirebaseService firebaseService, QrCodeService qrCodeService, IHttpContextAccessor httpContextAccessor)
    {
        _firebaseService = firebaseService;
        _qrCodeService = qrCodeService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var property = await _firebaseService.GetPropertyByIdAsync(id);
        
        if (property == null)
        {
            return NotFound();
        }

        // Generate QR code data - contains property code for mobile scanning
        // Mobile app will scan this and use the property code to fetch property details
        var qrData = property.PropertyCode;

        var qrCodeBytes = _qrCodeService.GenerateQrCode(qrData, pixelsPerModule: 10);
        
        var fileName = $"QRCode-{property.PropertyCode}.png";
        
        // Set headers to force download
        Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
        
        return File(qrCodeBytes, "image/png", fileName);
    }
}

