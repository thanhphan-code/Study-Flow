namespace StudyFlow.Domain.Enums;

public enum LearningAttemptType { Recall, MultipleChoice, TrueFalse, Application, SimilarQuestion }
public enum LearningAttemptResult { Correct, Close, Wrong }
public enum RecallDirection { Forward, Reverse }
public enum LearningCardOutcome { InProgress, Completed, NeedsReview }
public enum RecommendedNextAction { LearnNew, ReviewNow, Strengthen, KeepFresh }
