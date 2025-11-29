using PropertyInventory.Models;

namespace PropertyInventory.Data;

/// <summary>
/// Seed Data - Seeds 23 properties to database
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Seed 23 properties if database is empty
        if (!context.Properties.Any())
        {
            var properties = new List<Property>
            {
                // IT Equipment
                new Property { PropertyCode = "IT-001", PropertyName = "Desktop Computer", Category = "IT Equipment", Description = "Dell OptiPlex 7090 Desktop", Location = "Computer Lab 1", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 1, 15), SerialNumber = "DL-2023-001", LastUpdated = DateTime.Now, Remarks = "Assigned to Computer Lab" },
                new Property { PropertyCode = "IT-002", PropertyName = "Laptop Computer", Category = "IT Equipment", Description = "HP EliteBook 840 G8", Location = "IT Office", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 2, 10), SerialNumber = "HP-2023-002", LastUpdated = DateTime.Now, Remarks = "For IT staff use" },
                new Property { PropertyCode = "IT-003", PropertyName = "Printer", Category = "IT Equipment", Description = "Canon PIXMA G3010", Location = "Administration Office", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 3, 5), SerialNumber = "CN-2023-003", LastUpdated = DateTime.Now, Remarks = "Color printer" },
                new Property { PropertyCode = "IT-004", PropertyName = "Scanner", Category = "IT Equipment", Description = "Epson Perfection V39", Location = "Registrar Office", Status = PropertyStatus.Available, Quantity = 1, DateReceived = new DateTime(2023, 4, 12), SerialNumber = "EP-2023-004", LastUpdated = DateTime.Now, Remarks = "Document scanner" },
                new Property { PropertyCode = "IT-005", PropertyName = "Network Switch", Category = "IT Equipment", Description = "TP-Link 24-Port Switch", Location = "Server Room", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 5, 20), SerialNumber = "TP-2023-005", LastUpdated = DateTime.Now, Remarks = "Network infrastructure" },
                
                // Audio-Visual Equipment
                new Property { PropertyCode = "AV-001", PropertyName = "Projector", Category = "Audio-Visual Equipment", Description = "Epson EB-X41 Projector 3600 Lumens", Location = "Main Auditorium", Status = PropertyStatus.Available, Quantity = 1, DateReceived = new DateTime(2023, 3, 20), SerialNumber = "EP-2023-101", LastUpdated = DateTime.Now, Remarks = "Available for events" },
                new Property { PropertyCode = "AV-002", PropertyName = "Sound System", Category = "Audio-Visual Equipment", Description = "Bose L1 Pro32 Portable PA System", Location = "Main Auditorium", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 6, 15), SerialNumber = "BS-2023-102", LastUpdated = DateTime.Now, Remarks = "For events and ceremonies" },
                new Property { PropertyCode = "AV-003", PropertyName = "LED TV", Category = "Audio-Visual Equipment", Description = "Samsung 55-inch Smart TV", Location = "Conference Room", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 7, 8), SerialNumber = "SM-2023-103", LastUpdated = DateTime.Now, Remarks = "For presentations" },
                new Property { PropertyCode = "AV-004", PropertyName = "Document Camera", Category = "Audio-Visual Equipment", Description = "ELMO TT-12RX Document Camera", Location = "Classroom 201", Status = PropertyStatus.Available, Quantity = 1, DateReceived = new DateTime(2023, 8, 22), SerialNumber = "EL-2023-104", LastUpdated = DateTime.Now, Remarks = "For teaching" },
                
                // Furniture
                new Property { PropertyCode = "FUR-001", PropertyName = "Office Chair", Category = "Furniture", Description = "Ergonomic Office Chair Black", Location = "Faculty Room", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 5, 10), LastUpdated = DateTime.Now, Remarks = "Chair 1" },
                new Property { PropertyCode = "FUR-002", PropertyName = "Office Chair", Category = "Furniture", Description = "Ergonomic Office Chair Black", Location = "Faculty Room", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 5, 10), LastUpdated = DateTime.Now, Remarks = "Chair 2" },
                new Property { PropertyCode = "FUR-003", PropertyName = "Office Chair", Category = "Furniture", Description = "Ergonomic Office Chair Black", Location = "Faculty Room", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 5, 10), LastUpdated = DateTime.Now, Remarks = "Chair 3" },
                new Property { PropertyCode = "FUR-004", PropertyName = "Office Chair", Category = "Furniture", Description = "Ergonomic Office Chair Black", Location = "Faculty Room", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 5, 10), LastUpdated = DateTime.Now, Remarks = "Chair 4" },
                new Property { PropertyCode = "FUR-005", PropertyName = "Office Chair", Category = "Furniture", Description = "Ergonomic Office Chair Black", Location = "Faculty Room", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 5, 10), LastUpdated = DateTime.Now, Remarks = "Chair 5" },
                new Property { PropertyCode = "FUR-006", PropertyName = "Office Desk", Category = "Furniture", Description = "Executive Office Desk 120cm", Location = "Administration Office", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 4, 5), LastUpdated = DateTime.Now, Remarks = "Wooden desk" },
                new Property { PropertyCode = "FUR-007", PropertyName = "Filing Cabinet", Category = "Furniture", Description = "4-Drawer Filing Cabinet", Location = "Registrar Office", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 3, 18), LastUpdated = DateTime.Now, Remarks = "Metal filing cabinet" },
                new Property { PropertyCode = "FUR-008", PropertyName = "Bookshelf", Category = "Furniture", Description = "5-Tier Bookshelf", Location = "Library", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 2, 25), LastUpdated = DateTime.Now, Remarks = "Wooden bookshelf" },
                new Property { PropertyCode = "FUR-009", PropertyName = "Conference Table", Category = "Furniture", Description = "Large Conference Table 3m", Location = "Conference Room", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 6, 10), LastUpdated = DateTime.Now, Remarks = "Seats 12 people" },
                new Property { PropertyCode = "FUR-010", PropertyName = "Student Desk", Category = "Furniture", Description = "Standard Student Desk", Location = "Classroom 101", Status = PropertyStatus.InUse, Quantity = 30, DateReceived = new DateTime(2023, 1, 20), LastUpdated = DateTime.Now, Remarks = "Set of 30 desks" },
                
                // Laboratory Equipment
                new Property { PropertyCode = "LAB-001", PropertyName = "Microscope", Category = "Laboratory Equipment", Description = "Binocular Microscope 1000x", Location = "Science Lab", Status = PropertyStatus.Available, Quantity = 1, DateReceived = new DateTime(2023, 9, 5), SerialNumber = "MS-2023-201", LastUpdated = DateTime.Now, Remarks = "For biology classes" },
                new Property { PropertyCode = "LAB-002", PropertyName = "Laboratory Balance", Category = "Laboratory Equipment", Description = "Digital Analytical Balance", Location = "Chemistry Lab", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 8, 15), SerialNumber = "BL-2023-202", LastUpdated = DateTime.Now, Remarks = "Precision weighing" },
                new Property { PropertyCode = "LAB-003", PropertyName = "Bunsen Burner", Category = "Laboratory Equipment", Description = "Laboratory Bunsen Burner Set", Location = "Chemistry Lab", Status = PropertyStatus.Available, Quantity = 10, DateReceived = new DateTime(2023, 7, 20), LastUpdated = DateTime.Now, Remarks = "Set of 10 burners" },
                
                // Maintenance Equipment
                new Property { PropertyCode = "MNT-001", PropertyName = "Vacuum Cleaner", Category = "Maintenance Equipment", Description = "Industrial Vacuum Cleaner", Location = "Maintenance Office", Status = PropertyStatus.Available, Quantity = 1, DateReceived = new DateTime(2023, 5, 30), SerialNumber = "VC-2023-301", LastUpdated = DateTime.Now, Remarks = "For cleaning" },
                new Property { PropertyCode = "MNT-002", PropertyName = "Ladder", Category = "Maintenance Equipment", Description = "Aluminum Extension Ladder 3m", Location = "Maintenance Office", Status = PropertyStatus.Available, Quantity = 1, DateReceived = new DateTime(2023, 4, 12), LastUpdated = DateTime.Now, Remarks = "For repairs" },
                new Property { PropertyCode = "MNT-003", PropertyName = "Power Drill", Category = "Maintenance Equipment", Description = "Cordless Power Drill Set", Location = "Maintenance Office", Status = PropertyStatus.InUse, Quantity = 1, DateReceived = new DateTime(2023, 6, 8), SerialNumber = "PD-2023-302", LastUpdated = DateTime.Now, Remarks = "For maintenance work" }
            };

            context.Properties.AddRange(properties);
            await context.SaveChangesAsync();
        }
    }
}
