using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class LearningDecision : BaseEntity
{
    private LearningDecision() { }
    private LearningDecision(Guid learningAttemptId, decimal masteryBefore, decimal masteryAfter, decimal weaknessBefore, decimal weaknessAfter, int intervalBefore, int intervalAfter, DateTimeOffset? nextReviewBefore, DateTimeOffset? nextReviewAfter, string actionBefore, string actionAfter, string statePolicyVersion, string schedulerVersion, string decisionTrace, DateTimeOffset now)
    {
        LearningAttemptId = learningAttemptId; MasteryBefore = masteryBefore; MasteryAfter = masteryAfter; WeaknessBefore = weaknessBefore; WeaknessAfter = weaknessAfter; IntervalBefore = intervalBefore; IntervalAfter = intervalAfter; NextReviewBefore = nextReviewBefore; NextReviewAfter = nextReviewAfter; ActionBefore = actionBefore; ActionAfter = actionAfter; StatePolicyVersion = statePolicyVersion; SchedulerVersion = schedulerVersion; DecisionTrace = decisionTrace; CreatedAt = now; UpdatedAt = now;
    }
    public Guid LearningAttemptId { get; private init; }
    public decimal MasteryBefore { get; private init; }
    public decimal MasteryAfter { get; private init; }
    public decimal WeaknessBefore { get; private init; }
    public decimal WeaknessAfter { get; private init; }
    public int IntervalBefore { get; private init; }
    public int IntervalAfter { get; private init; }
    public DateTimeOffset? NextReviewBefore { get; private init; }
    public DateTimeOffset? NextReviewAfter { get; private init; }
    public string ActionBefore { get; private init; } = string.Empty;
    public string ActionAfter { get; private init; } = string.Empty;
    public string StatePolicyVersion { get; private init; } = string.Empty;
    public string SchedulerVersion { get; private init; } = string.Empty;
    public string DecisionTrace { get; private init; } = "{}";
    public static LearningDecision Create(Guid attemptId, decimal masteryBefore, decimal masteryAfter, decimal weaknessBefore, decimal weaknessAfter, int intervalBefore, int intervalAfter, DateTimeOffset? nextReviewBefore, DateTimeOffset? nextReviewAfter, string actionBefore, string actionAfter, string policyVersion, string schedulerVersion, string trace, DateTimeOffset now) => new(attemptId, masteryBefore, masteryAfter, weaknessBefore, weaknessAfter, intervalBefore, intervalAfter, nextReviewBefore, nextReviewAfter, actionBefore, actionAfter, policyVersion, schedulerVersion, trace, now);
}
