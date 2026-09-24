namespace StudyFlow.Application.Documents.Interfaces;

public sealed class DocumentProcessingException(string code, string message, bool retryable, Exception? innerException = null) : Exception(message, innerException)
{
    public string Code { get; } = code;
    public bool Retryable { get; } = retryable;
}
