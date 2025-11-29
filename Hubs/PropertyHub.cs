using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PropertyInventory.Hubs;

[Authorize(Roles = "Admin")]
public class PropertyHub : Hub
{
    public async Task PropertyUpdated(string propertyCode, string action)
    {
        await Clients.All.SendAsync("PropertyUpdated", propertyCode, action);
    }

    public async Task PropertyCreated(string propertyCode)
    {
        await Clients.All.SendAsync("PropertyCreated", propertyCode);
    }

    public async Task PropertyDeleted(string propertyCode)
    {
        await Clients.All.SendAsync("PropertyDeleted", propertyCode);
    }
}