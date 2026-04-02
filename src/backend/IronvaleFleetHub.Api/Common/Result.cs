namespace IronvaleFleetHub.Api.Common;

public class Result<T>
{
    public T? Value { get; init; }
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    public static Result<T> Success(T value) => new() { Value = value, IsSuccess = true };
    public static Result<T> Failure(string error) => new() { Error = error, IsSuccess = false };
}
