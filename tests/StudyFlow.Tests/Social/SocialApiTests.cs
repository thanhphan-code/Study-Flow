using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Domain.Entities;
using StudyFlow.Infrastructure.Persistence;
using StudyFlow.Tests.Auth;
using StudyFlow.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace StudyFlow.Tests.Social;

public sealed class SocialApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private async Task<(HttpClient Client, Guid Id)> Account()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = $"social-{Guid.NewGuid():N}@example.com", password = "StudyFlow!123", displayName = "Social Learner", timeZoneId = "Asia/Ho_Chi_Minh" });
        response.EnsureSuccessStatusCode(); var payload = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.AccessToken);
        return (client, payload.User.Id);
    }
    private async Task<Guid> Set(Guid owner)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
        var subject = Subject.Create(owner, "Private subject", null); var set = StudySet.Create(subject.Id, "Community vocabulary", "Useful words");
        db.Subjects.Add(subject); db.StudySets.Add(set);
        db.Flashcards.Add(Flashcard.Create(set.Id, "Hello", "Xin chào", "A greeting", 0));
        await db.SaveChangesAsync(); return set.Id;
    }
    private static async Task<JsonElement> Json(HttpResponseMessage response) { response.EnsureSuccessStatusCode(); return await response.Content.ReadFromJsonAsync<JsonElement>(); }
    private static Task<HttpResponseMessage> Publish(HttpClient c, Guid id, string visibility, bool allowRemix = true) => c.PutAsJsonAsync($"/api/social/sets/{id}/publication", new { visibility, allowRemix });
    private static async Task Befriend(HttpClient a, Guid aId, HttpClient b, Guid bId)
    {
        (await a.PostAsync($"/api/social/people/{bId}/request", null)).EnsureSuccessStatusCode();
        (await b.PostAsync($"/api/social/people/{aId}/accept", null)).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task Publication_DefaultPrivate_UnlistedHidden_RevocationProtectsSavedAndPrivateEndpoints()
    {
        var (owner, ownerId) = await Account(); var (reader, _) = await Account(); var id = await Set(ownerId);
        Assert.Equal(HttpStatusCode.NotFound, (await reader.GetAsync($"/api/social/sets/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reader.PutAsJsonAsync($"/api/social/sets/{id}/publication", new { visibility = "Public" })).StatusCode);
        (await Publish(owner, id, "Unlisted")).EnsureSuccessStatusCode();
        var visible = await Json(await reader.GetAsync($"/api/social/sets/{id}")); Assert.Single(visible.GetProperty("cards").EnumerateArray());
        var explore = await Json(await reader.GetAsync("/api/social/sets?q=Community")); Assert.DoesNotContain(explore.GetProperty("items").EnumerateArray(), x => x.GetProperty("id").GetGuid() == id);
        (await reader.PutAsync($"/api/social/reactions/{id}/Save", null)).EnsureSuccessStatusCode();
        (await Publish(owner, id, "Private")).EnsureSuccessStatusCode();
        var unavailable = await Json(await reader.GetAsync("/api/social/saved/unavailable"));
        Assert.Contains(unavailable.GetProperty("items").EnumerateArray(), x => x.GetProperty("id").GetGuid() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await reader.GetAsync($"/api/social/sets/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reader.GetAsync($"/api/study-sets/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reader.PostAsync($"/api/social/sets/{id}/remix", null)).StatusCode);
        (await reader.DeleteAsync($"/api/social/reactions/{id}/Save")).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task Friendship_IsSeparateFromFollow_RequestsRequireRecipient_BlockRevokesAccess()
    {
        var (a, aid) = await Account(); var (b, bid) = await Account(); var (c, _) = await Account(); var id = await Set(aid);
        (await Publish(a, id, "FriendsOnly")).EnsureSuccessStatusCode();
        (await b.PostAsync($"/api/social/people/{aid}/follow", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/social/sets/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await b.PostAsync($"/api/social/conversations/direct/{aid}", null)).StatusCode);
        (await b.PostAsync($"/api/social/people/{aid}/request", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync($"/api/social/people/{bid}/accept", null)).StatusCode);
        (await a.PostAsync($"/api/social/people/{bid}/accept", null)).EnsureSuccessStatusCode();
        (await b.GetAsync($"/api/social/sets/{id}")).EnsureSuccessStatusCode();
        (await a.PostAsync($"/api/social/people/{bid}/block", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/social/sets/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/api/social/people/{aid}/follow", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsync($"/api/social/people/{bid}/request", null)).StatusCode);
    }
    [Fact]
    public async Task Messages_AreMemberOnly_SharesCheckRecipient_RevokeOnRead_AndBlock()
    {
        var (a, aid) = await Account(); var (b, bid) = await Account(); var (stranger, _) = await Account();
        await Befriend(a, aid, b, bid);
        var conversation = (await Json(await a.PostAsync($"/api/social/conversations/direct/{bid}", null))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/social/conversations/{conversation}/messages")).StatusCode);
        (await a.PostAsJsonAsync($"/api/social/conversations/{conversation}/messages", new { content = "Hi" })).EnsureSuccessStatusCode();
        var set = await Set(aid);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/social/conversations/{conversation}/messages", new { kind = "StudySetShare", sharedId = set })).StatusCode);
        (await Publish(a, set, "FriendsOnly")).EnsureSuccessStatusCode();
        (await a.PostAsJsonAsync($"/api/social/conversations/{conversation}/messages", new { kind = "StudySetShare", sharedId = set })).EnsureSuccessStatusCode();
        (await Publish(a, set, "Private")).EnsureSuccessStatusCode();
        var messages = await Json(await b.GetAsync($"/api/social/conversations/{conversation}/messages"));
        Assert.All(messages.GetProperty("items").EnumerateArray().Where(m => m.GetProperty("kind").GetString() == "StudySetShare"), m => Assert.Equal(JsonValueKind.Null, m.GetProperty("shared").ValueKind));
        (await b.PostAsync($"/api/social/people/{aid}/block", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/social/conversations/{conversation}/messages", new { content = "Blocked" })).StatusCode);
    }
    [Fact]
    public async Task Reactions_AreIdempotent_RemixKeepsAttributionAndPrivacy()
    {
        var (a, aid) = await Account(); var (b, _) = await Account(); var id = await Set(aid);
        (await Publish(a, id, "Public", false)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await b.PostAsync($"/api/social/sets/{id}/remix", null)).StatusCode);
        var likes = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => b.PutAsync($"/api/social/reactions/{id}/Like", null)));
        Assert.All(likes, x => x.EnsureSuccessStatusCode());
        Assert.Equal(1, (await Json(await b.GetAsync($"/api/social/sets/{id}"))).GetProperty("likes").GetInt32());
        (await Publish(a, id, "Public")).EnsureSuccessStatusCode();
        var copy = (await Json(await b.PostAsync($"/api/social/sets/{id}/remix", null))).GetProperty("studySetId").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/social/sets/{copy}")).StatusCode);
        (await Publish(a, id, "Private")).EnsureSuccessStatusCode();
        var ownCopy = await Json(await b.GetAsync($"/api/social/sets/{copy}"));
        Assert.Equal("Private", ownCopy.GetProperty("visibility").GetString()); Assert.Equal(JsonValueKind.Null, ownCopy.GetProperty("attribution").GetProperty("studySetId").ValueKind);
    }
    [Fact]
    public async Task Comments_LimitReplyDepth_EnforceOwnership_AndSoftDelete()
    {
        var (a, aid) = await Account(); var (b, _) = await Account(); var id = await Set(aid); await Publish(a, id, "Public");
        var first = (await Json(await b.PostAsJsonAsync($"/api/social/sets/{id}/comments", new { content = "Question" }))).GetProperty("id").GetGuid();
        var reply = (await Json(await a.PostAsJsonAsync($"/api/social/sets/{id}/comments", new { content = "Answer", parentCommentId = first }))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync($"/api/social/sets/{id}/comments", new { content = "Nested", parentCommentId = reply })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.DeleteAsync($"/api/social/comments/{first}")).StatusCode);
        (await b.DeleteAsync($"/api/social/comments/{first}")).EnsureSuccessStatusCode();
        var list = await Json(await a.GetAsync($"/api/social/sets/{id}/comments")); Assert.Equal(2, list.GetProperty("items").GetArrayLength());
        Assert.True(list.GetProperty("items")[0].GetProperty("isDeleted").GetBoolean());
    }
    [Fact]
    public async Task Profiles_ValidateUniqueUsername_AndModerationRequiresAdmin()
    {
        var (a, aid) = await Account(); var (b, _) = await Account(); var name = "learner_" + aid.ToString("N")[..10];
        (await a.PutAsJsonAsync("/api/social/profile", new { username = name, displayName = "Learner" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await b.PutAsJsonAsync("/api/social/profile", new { username = name.ToUpperInvariant(), displayName = "Other" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PutAsJsonAsync("/api/social/profile", new { username = "admin", displayName = "Fake" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await a.GetAsync("/api/social/moderation/reports")).StatusCode);
        (await b.PostAsJsonAsync("/api/social/reports", new { targetType = "User", targetId = aid, reason = "Spam" })).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task PublicSources_ExposeOnlyPrivateFlag_AndRemixDoesNotCopyReferences()
    {
        var (owner, ownerId) = await Account(); var (reader, _) = await Account(); var id = await Set(ownerId);
        Guid cardId; var documentId = Guid.NewGuid(); var chunkId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>(); cardId = await db.Flashcards.Where(c => c.StudySetId == id).Select(c => c.Id).SingleAsync();
            db.SourceReferences.Add(SourceReference.Create(documentId, chunkId, GroundedContentType.Flashcard, cardId, 17, null, "Secret section", 0, 15, "hash", "revision", "Private medical report.pdf", "CONFIDENTIAL_SOURCE_TEXT"));
            await db.SaveChangesAsync();
        }
        await Publish(owner, id, "Public");
        var raw = await reader.GetStringAsync($"/api/social/sets/{id}");
        Assert.Contains("hasPrivateSource\":true", raw); Assert.DoesNotContain("CONFIDENTIAL", raw); Assert.DoesNotContain("medical report", raw); Assert.DoesNotContain(documentId.ToString(), raw); Assert.DoesNotContain(chunkId.ToString(), raw);
        Assert.Equal(HttpStatusCode.NotFound, (await reader.GetAsync($"/api/documents/{documentId}")).StatusCode);
        var copyId = (await Json(await reader.PostAsync($"/api/social/sets/{id}/remix", null))).GetProperty("studySetId").GetGuid();
        using var verify = factory.Services.CreateScope(); var context = verify.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
        var copyCards = await context.Flashcards.Where(c => c.StudySetId == copyId).Select(c => c.Id).ToListAsync();
        Assert.False(await context.SourceReferences.AnyAsync(s => copyCards.Contains(s.ContentId)));
    }
    [Fact]
    public async Task PublicStudy_CountsActualAnswersOnce_AndRejectsUnrelatedCard()
    {
        var (a, aid) = await Account(); var (b, _) = await Account(); var id = await Set(aid); var another = await Set(aid);
        await Publish(a, id, "Public");
        var detail = await Json(await b.GetAsync($"/api/social/sets/{id}")); var card = detail.GetProperty("cards")[0].GetProperty("id").GetGuid();
        var second = await Json(await a.GetAsync($"/api/social/sets/{another}")); var privateCard = second.GetProperty("cards")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync($"/api/social/sets/{id}/answer", new { flashcardId = privateCard, answer = "Xin chào" })).StatusCode);
        for (var i = 0; i < 2; i++)
        {
            var answer = await Json(await b.PostAsJsonAsync($"/api/social/sets/{id}/answer", new { flashcardId = card, answer = "Xin chào" }));
            Assert.Equal("Correct", answer.GetProperty("result").GetString());
        }
        var feed = await Json(await b.GetAsync($"/api/social/sets?authorId={aid}"));
        Assert.Equal(1, feed.GetProperty("items").EnumerateArray().Single(s => s.GetProperty("id").GetGuid() == id).GetProperty("learners").GetInt32());
        await Publish(a, id, "Private");
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync($"/api/social/sets/{id}/answer", new { flashcardId = card, answer = "Xin chào" })).StatusCode);
    }
    [Fact]
    public async Task Pagination_CommentsHaveStableBoundaries_NotificationsDeduplicate()
    {
        var (a, aid) = await Account(); var (b, bid) = await Account(); var id = await Set(aid); await Publish(a, id, "Public");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
            db.Comments.AddRange(Enumerable.Range(0, 23).Select(i => new StudySetComment { StudySetId = id, UserId = bid, Content = $"Comment {i}" })); await db.SaveChangesAsync();
        }
        var first = await Json(await a.GetAsync($"/api/social/sets/{id}/comments?page=1")); var second = await Json(await a.GetAsync($"/api/social/sets/{id}/comments?page=2"));
        Assert.Equal(20, first.GetProperty("items").GetArrayLength()); Assert.Equal(3, second.GetProperty("items").GetArrayLength()); Assert.True(first.GetProperty("hasMore").GetBoolean()); Assert.False(second.GetProperty("hasMore").GetBoolean());
        var ids = first.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToHashSet(); Assert.DoesNotContain(second.GetProperty("items").EnumerateArray(), x => ids.Contains(x.GetProperty("id").GetGuid()));
        await b.PutAsync($"/api/social/reactions/{id}/Like", null); await b.DeleteAsync($"/api/social/reactions/{id}/Like"); await b.PutAsync($"/api/social/reactions/{id}/Like", null);
        var notices = await Json(await a.GetAsync("/api/social/notifications")); Assert.Single(notices.GetProperty("items").EnumerateArray(), n => n.GetProperty("kind").GetString() == "Like");
        await a.PostAsync("/api/social/notifications/read-all", null);
        Assert.Equal(0, (await Json(await a.GetAsync("/api/social/notifications"))).GetProperty("unread").GetInt32());
    }
    [Fact]
    public async Task Moderation_RemovesPublishedContent_AndSuspendsExistingSessions()
    {
        var (admin, adminId) = await Account(); var (reported, reportedId) = await Account(); var id = await Set(reportedId); await Publish(reported, id, "Public");
        var config = factory.Services.GetRequiredService<IConfiguration>(); config["Social:AdminUserIds:0"] = adminId.ToString();
        try
        {
            await admin.PostAsJsonAsync("/api/social/reports", new { targetType = "StudySet", targetId = id, reason = "Copyright" });
            var reportList = await Json(await admin.GetAsync("/api/social/moderation/reports"));
            var reportId = reportList.GetProperty("items").EnumerateArray().First(x => x.GetProperty("targetId").GetGuid() == id).GetProperty("id").GetGuid();
            (await admin.PostAsJsonAsync($"/api/social/moderation/reports/{reportId}", new { action = "Remove" })).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/social/sets/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await Publish(reported, id, "Public")).StatusCode);
            await admin.PostAsJsonAsync("/api/social/reports", new { targetType = "User", targetId = reportedId, reason = "Spam" });
            reportList = await Json(await admin.GetAsync("/api/social/moderation/reports")); reportId = reportList.GetProperty("items").EnumerateArray().First(x => x.GetProperty("targetId").GetGuid() == reportedId).GetProperty("id").GetGuid();
            (await admin.PostAsJsonAsync($"/api/social/moderation/reports/{reportId}", new { action = "Suspend" })).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Forbidden, (await reported.GetAsync("/api/subjects")).StatusCode);
            var me = await Json(await admin.GetAsync($"/api/social/profiles/me"));
            Assert.NotEqual(reportedId, me.GetProperty("userId").GetGuid());
            (await reported.PostAsync("/api/auth/logout", null)).EnsureSuccessStatusCode();
            (await admin.PostAsJsonAsync($"/api/social/moderation/reports/{reportId}", new { action = "Restore" })).EnsureSuccessStatusCode();
            (await reported.GetAsync("/api/subjects")).EnsureSuccessStatusCode();
        }
        finally { config["Social:AdminUserIds:0"] = null; }
    }
}
