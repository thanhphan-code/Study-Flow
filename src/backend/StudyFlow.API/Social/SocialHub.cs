using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StudyFlow.Application.Social;

namespace StudyFlow.API.Social;

[Authorize]
public sealed class SocialHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "social:" + Context.User!.FindFirst("sub")!.Value);
        await base.OnConnectedAsync();
    }
}
public sealed class SocialNotifier(IHubContext<SocialHub> hub, ILogger<SocialNotifier> logger) : ISocialNotifier
{
    public async Task ChangedAsync(Guid userId, CancellationToken ct)
    {
        try { await hub.Clients.Group("social:" + userId).SendAsync("SocialChanged", cancellationToken: ct); }
        catch (Exception e) { logger.LogWarning(e, "Social notification delivery failed; clients will refresh via polling"); }
    }
}
