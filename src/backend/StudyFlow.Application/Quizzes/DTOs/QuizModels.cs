using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.Quizzes.DTOs;

public sealed record CreateQuizRequest(string Title, int QuestionCount);
public sealed record ManualQuizOptionRequest(string Text, bool IsCorrect);
public sealed record ManualQuizQuestionRequest(string QuestionText, string? Explanation, IReadOnlyList<ManualQuizOptionRequest> Options);
public sealed record CreateManualQuizRequest(string Title, IReadOnlyList<ManualQuizQuestionRequest> Questions);
public sealed record SubmitQuizAnswerRequest(Guid QuestionId, Guid AnswerOptionId);
public sealed record QuizDto(Guid Id, Guid StudySetId, string Title, int QuestionCount, DateTimeOffset CreatedAt);
public sealed record QuizOptionDto(Guid Id, string Text);
public sealed record QuizQuestionDto(Guid Id, QuestionType Type, string QuestionText, int OrderIndex, IReadOnlyList<QuizOptionDto> Options);
public sealed record QuizAttemptSessionDto(Guid AttemptId, Guid QuizId, string Title, DateTimeOffset StartedAt, IReadOnlyList<QuizQuestionDto> Questions);
public sealed record QuizAnswerAcceptedDto(Guid QuestionId, Guid AnswerOptionId);
public sealed record QuizQuestionResultDto(Guid QuestionId, string QuestionText, Guid? SelectedOptionId, string? SelectedAnswer, Guid CorrectOptionId, string CorrectAnswer, bool IsCorrect, string? Explanation);
public sealed record QuizResultDto(Guid AttemptId, double Score, int CorrectCount, int WrongCount, DateTimeOffset CompletedAt, IReadOnlyList<QuizQuestionResultDto> Questions);
