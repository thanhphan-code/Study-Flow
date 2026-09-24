namespace StudyFlow.Application.Common.Models;

public enum ErrorType { Validation, Unauthorized, Conflict, NotFound }

public sealed record Error(string Code, string Message, ErrorType Type);

public sealed class Result<T>
{
    private Result(T? value, Error? error) { Value = value; Error = error; }
    public bool IsSuccess => Error is null;
    public T? Value { get; }
    public Error? Error { get; }
    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(string code, string message, ErrorType type) => new(default, new Error(code, message, type));
}
