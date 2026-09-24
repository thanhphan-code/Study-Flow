namespace StudyFlow.Infrastructure.Documents;

public sealed class DocumentProcessingOptions
{
    public const string SectionName = "DocumentProcessing";
    public int MaxAttempts { get; init; } = 3;
    public int ProcessingTimeoutMinutes { get; init; } = 5;
    public int PollIntervalMilliseconds { get; init; } = 1000;
    public int BatchSize { get; init; } = 5;
}
