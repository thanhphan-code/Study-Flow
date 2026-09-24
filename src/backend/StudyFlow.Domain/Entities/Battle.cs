using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class BattleRoom : BaseEntity
{
    public Guid HostUserId { get; set; }
    public Guid StudySetId { get; set; }
    public string Name { get; set; } = "";
    public string JoinCode { get; set; } = "";
    public string Status { get; set; } = "LobbyOpen";
    public string Mode { get; set; } = "Classic";
    public string Difficulty { get; set; } = "Balanced";
    public string LeaderboardMode { get; set; } = "BetweenRounds";
    public int MaxPlayers { get; set; }
    public int QuestionCount { get; set; }
    public int? DefaultTimeLimitSeconds { get; set; }
    public int CurrentQuestionIndex { get; set; } = -1;
    public DateTimeOffset? QuestionStartedAt { get; set; }
    public DateTimeOffset? QuestionEndedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class BattleParticipant : BaseEntity
{
    public Guid? ReviewStudySetId { get; set; }
    public Guid BattleRoomId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTimeOffset LastConnectedAt { get; set; }
    public int Score { get; set; }
    public int Combo { get; set; }
}

public sealed class BattleQuestion : BaseEntity
{
    public Guid? SourceQuestionId { get; set; }
    public Guid BattleRoomId { get; set; }
    public Guid? FlashcardId { get; set; }
    public string QuestionType { get; set; } = "MultipleChoice";
    public string Prompt { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public string? AcceptedAnswers { get; set; }
    public string? Explanation { get; set; }
    public string OptionsJson { get; set; } = "[]";
    public string LeftItemsJson { get; set; } = "[]";
    public string Difficulty { get; set; } = "Medium";
    public int OrderIndex { get; set; }
    public int? TimeLimitSeconds { get; set; }
    public Guid? SourceReferenceId { get; set; }
    public string? SourceJson { get; set; }
}

public sealed class BattleAnswer : BaseEntity
{
    public Guid? LearningFlashcardId { get; set; }
    public Guid BattleRoomId { get; set; }
    public Guid BattleQuestionId { get; set; }
    public Guid UserId { get; set; }
    public string SubmittedAnswer { get; set; } = "";
    public string Evaluation { get; set; } = "Wrong";
    public bool IsCorrect { get; set; }
    public int ResponseTimeMs { get; set; }
    public int ScoreEarned { get; set; }
    public bool TimedOut { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}
