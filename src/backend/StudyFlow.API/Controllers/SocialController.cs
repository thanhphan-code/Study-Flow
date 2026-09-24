using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudyFlow.Application.Social;

namespace StudyFlow.API.Controllers;

[ApiController, Authorize, Route("api/social"), EnableRateLimiting("social")]
public sealed class SocialController(ISocialService social, IConfiguration configuration) : ControllerBase
{
    private Guid Me => Guid.Parse(User.FindFirst("sub")!.Value);
    private async Task<IActionResult> Read(Task<object> task) => Ok(await task);
    private async Task<IActionResult> Write(Func<Task<object>> action, CancellationToken ct) => Ok(await social.WriteAsync(Me, action, ct));
    [HttpGet("profiles/{username}")] public Task<IActionResult> Profile(string username, CancellationToken ct) => Read(social.ProfileAsync(Me, username, ct));
    [HttpPut("profile")] public Task<IActionResult> Profile(ProfileRequest request, CancellationToken ct) => Write(() => social.UpdateProfileAsync(Me, request, ct), ct);
    [HttpPost("people/{id:guid}/{actionName}")] public Task<IActionResult> Relationship(Guid id, string actionName, CancellationToken ct) => Write(() => social.RelationshipAsync(Me, id, actionName, ct), ct);
    [HttpGet("people")] public Task<IActionResult> People(CancellationToken ct, string kind = "search", int page = 1, string? q = null) => Read(social.PeopleAsync(Me, kind, page, q, ct));
    [HttpGet("sets")] public Task<IActionResult> Explore(CancellationToken ct, string tab = "discover", string? q = null, string sort = "trending", int page = 1, Guid? authorId = null) => Read(social.ExploreAsync(Me, tab, q, sort, page, authorId, ct));
    [HttpGet("saved/unavailable")] public Task<IActionResult> Unavailable(CancellationToken ct, int page = 1) => Read(social.UnavailableSavesAsync(Me, page, ct));
    [HttpGet("sets/{id:guid}")] public Task<IActionResult> Set(Guid id, CancellationToken ct) => Read(social.SetAsync(Me, id, ct));
    [HttpPost("sets/{id:guid}/answer")] public Task<IActionResult> Answer(Guid id, PublicAnswerRequest request, CancellationToken ct) => Write(() => social.AnswerAsync(Me, id, request, ct), ct);
    [HttpGet("sets/{id:guid}/cards/{cardId:guid}/image")] public async Task<IActionResult> Image(Guid id, Guid cardId, CancellationToken ct)
    {
        var image = await social.ImageAsync(Me, id, cardId, ct);
        Response.Headers.CacheControl = "no-store";
        return File(image.Content, image.ContentType);
    }
    [HttpPut("sets/{id:guid}/publication")] public Task<IActionResult> Publish(Guid id, PublicationRequest request, CancellationToken ct) => Write(() => social.PublishAsync(Me, id, request, ct), ct);
    [HttpPost("sets/{id:guid}/remix")] public Task<IActionResult> Remix(Guid id, CancellationToken ct) => Write(() => social.RemixAsync(Me, id, ct), ct);
    [HttpPut("reactions/{id:guid}/{kind}")] public Task<IActionResult> React(Guid id, string kind, CancellationToken ct) => Write(() => social.ReactAsync(Me, id, kind, true, ct), ct);
    [HttpDelete("reactions/{id:guid}/{kind}")] public Task<IActionResult> Unreact(Guid id, string kind, CancellationToken ct) => Write(() => social.ReactAsync(Me, id, kind, false, ct), ct);
    [HttpGet("sets/{id:guid}/comments")] public Task<IActionResult> Comments(Guid id, CancellationToken ct, int page = 1) => Read(social.CommentsAsync(Me, id, page, ct));
    [HttpPost("sets/{id:guid}/comments")] public Task<IActionResult> Comment(Guid id, CommentRequest request, CancellationToken ct) => Write(() => social.CommentAsync(Me, id, request, ct), ct);
    [HttpPut("comments/{id:guid}")] public Task<IActionResult> EditComment(Guid id, CommentRequest request, CancellationToken ct) => Write(() => social.EditCommentAsync(Me, id, request.Content, ct), ct);
    [HttpDelete("comments/{id:guid}")] public Task<IActionResult> DeleteComment(Guid id, CancellationToken ct) => Write(() => social.EditCommentAsync(Me, id, null, ct), ct);
    [HttpGet("notifications")] public Task<IActionResult> Notifications(CancellationToken ct, int page = 1) => Read(social.NotificationsAsync(Me, page, ct));
    [HttpPost("notifications/read-all")] public Task<IActionResult> ReadAll(CancellationToken ct) => Write(() => social.ReadNotificationsAsync(Me, null, ct), ct);
    [HttpPost("notifications/{id:guid}/read")] public Task<IActionResult> ReadOne(Guid id, CancellationToken ct) => Write(() => social.ReadNotificationsAsync(Me, id, ct), ct);
    [HttpGet("conversations")] public Task<IActionResult> Conversations(CancellationToken ct, int page = 1) => Read(social.ConversationsAsync(Me, page, ct));
    [HttpPost("conversations/direct/{id:guid}")] public Task<IActionResult> Direct(Guid id, CancellationToken ct) => Write(() => social.DirectAsync(Me, id, ct), ct);
    [HttpGet("conversations/{id:guid}/messages")] public Task<IActionResult> Messages(Guid id, CancellationToken ct, int page = 1) => Read(social.MessagesAsync(Me, id, page, ct));
    [HttpPost("conversations/{id:guid}/messages")] public Task<IActionResult> Send(Guid id, MessageRequest request, CancellationToken ct) => Write(() => social.SendAsync(Me, id, request, ct), ct);
    [HttpPost("conversations/{id:guid}/read")] public Task<IActionResult> ReadConversation(Guid id, CancellationToken ct) => Write(() => social.ReadConversationAsync(Me, id, ct), ct);
    [HttpPost("reports")] public Task<IActionResult> Report(ReportRequest request, CancellationToken ct) => Write(() => social.ReportAsync(Me, request, ct), ct);
    private void Admin() { if (User.IsInRole("Admin")) return; if (!(configuration.GetSection("Social:AdminUserIds").Get<string[]>() ?? []).Contains(Me.ToString(), StringComparer.OrdinalIgnoreCase)) throw new SocialException(403, "Chỉ quản trị viên có quyền thực hiện."); }
    [HttpGet("moderation/reports")] public Task<IActionResult> Reports(CancellationToken ct, int page = 1) { Admin(); return Read(social.ReportsAsync(page, ct)); }
    [HttpPost("moderation/reports/{id:guid}")] public Task<IActionResult> Moderate(Guid id, ModerationRequest request, CancellationToken ct) { Admin(); return Write(() => social.ModerateAsync(id, request.Action, ct), ct); }
}
