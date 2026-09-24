using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StudyFlow.Application.Battles;

namespace StudyFlow.API.Battles;

[Authorize]
public sealed class BattleHub(IBattleService battles) : Hub
{
    public async Task Watch(Guid roomId)
    {
        if (!Guid.TryParse(Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)) throw new HubException("Authentication required.");
        var result = await battles.GetAsync(userId, roomId, Context.ConnectionAborted);
        if (!result.IsSuccess || !result.Value!.IsMember) throw new HubException("Room membership required.");
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString(), Context.ConnectionAborted);
        await Clients.Caller.SendAsync("RoomChanged", roomId, Context.ConnectionAborted);
    }
}

public sealed class BattleNotifier(IHubContext<BattleHub> hub, ILogger<BattleNotifier> logger) : IBattleNotifier
{
    public async Task ChangedAsync(Guid roomId, CancellationToken ct)
    {
        // Notifications contain no private state. Every refresh rechecks membership, including kicked clients.
        try { await hub.Clients.Group(roomId.ToString()).SendAsync("RoomChanged", roomId, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogWarning(ex, "Battle notification failed for {RoomId}", roomId); }
    }
}

public sealed class BattleWorker(IServiceScopeFactory scopes, ILogger<BattleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IBattleService>().TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Battle tick failed"); }
        }
    }
}
