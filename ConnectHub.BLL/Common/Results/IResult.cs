namespace ConnectHub.BLL.Common.Results;

public enum ErrorType
{
    Failure = 0,
    NotFound = 1,
    Validation = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}

/// <summary>
/// Common contract representing the outcome of a business operation.
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure => !IsSuccess;
    string? ErrorMessage { get; }
    ErrorType ErrorType { get; }
    IDictionary<string, string[]>? ValidationErrors { get; }
}
