using StudyFlow.Application.Common.Models;

namespace StudyFlow.Application.Battles;

public sealed record CreateBattleRequest(Guid StudySetId, string Name, int QuestionCount = 10, int MaxPlayers = 10,
    int? TimeLimitSeconds = 20, string[]? QuestionTypes = null, string Difficulty = "Balanced",
    string Mode = "Classic", string LeaderboardMode = "BetweenRounds");
public sealed record BattleAnswerRequest(string Answer);
public sealed record BattleOption(string Id, string Text);
public sealed record BattleQuestionDto(Guid Id, string QuestionType, string Prompt, string Difficulty, int Number,
    int? TimeLimitSeconds, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, BattleOption[] Options, BattleOption[] LeftItems);
public sealed record BattleFeedback(Guid QuestionId, string Evaluation, int ScoreEarned, string CorrectAnswer, string? Explanation, object? Source);
public sealed record BattlePlayerDto(Guid UserId, string Name, bool IsHost, string Status, int? Score, int? Rank, int? CorrectCount, double? Accuracy, double? AverageResponseSeconds);
public sealed record BattleReview(Guid QuestionId, string Prompt, string CorrectAnswer, string? Explanation, string Evaluation, object? Source, double RoomAccuracy);
public sealed record BattleRoomDto(Guid Id, Guid StudySetId, Guid HostUserId, string Name, string JoinCode, string Status,
    string Mode, string Difficulty, string LeaderboardMode, int MaxPlayers, int QuestionCount, int? TimeLimitSeconds,
    DateTimeOffset ServerTime, int PlayerCount, bool IsMember, int AnsweredCount, int MyScore, int MyCombo,
    BattlePlayerDto[] Players, BattleQuestionDto? Question, BattleFeedback? MyAnswer, BattleReview[] Reviews, Guid? ReviewStudySetId);
public interface IBattleService
{
    Task<Result<BattleRoomDto>> CreateAsync(Guid userId, CreateBattleRequest request, CancellationToken ct);
    Task<Result<BattleRoomDto>> FindAsync(Guid userId, string code, CancellationToken ct);
    Task<Result<BattleRoomDto>> GetAsync(Guid userId, Guid roomId, CancellationToken ct);
    Task<Result<BattleRoomDto>> ActAsync(Guid userId, Guid roomId, string action, Guid? target, CancellationToken ct);
    Task<Result<BattleRoomDto>> AnswerAsync(Guid userId, Guid roomId, Guid questionId, BattleAnswerRequest request, CancellationToken ct);
    Task TickAsync(CancellationToken ct);
}
public interface IBattleNotifier { Task ChangedAsync(Guid roomId, CancellationToken ct); }
