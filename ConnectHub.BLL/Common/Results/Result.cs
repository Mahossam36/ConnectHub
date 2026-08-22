namespace ConnectHub.BLL.Common.Results;

public class Result : IResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public ErrorType ErrorType { get; }
    public IDictionary<string, string[]>? ValidationErrors { get; }

    protected Result(bool isSuccess, string? errorMessage = null, ErrorType errorType = ErrorType.Failure, IDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorType = errorType;
        ValidationErrors = validationErrors;
    }

    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error, ErrorType.Failure);
    public static Result NotFound(string error) => new(false, error, ErrorType.NotFound);
    public static Result Conflict(string error) => new(false, error, ErrorType.Conflict);
    public static Result Forbidden(string error = "You do not have permission to perform this action.") => new(false, error, ErrorType.Forbidden);
    public static Result Unauthorized(string error = "Unauthorized access.") => new(false, error, ErrorType.Unauthorized);
    public static Result Validation(IDictionary<string, string[]> errors) => new(false, "One or more validation errors occurred.", ErrorType.Validation, errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, string? errorMessage = null, ErrorType errorType = ErrorType.Failure, IDictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, errorMessage, errorType, validationErrors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true);
    public static new Result<T> Failure(string error) => new(default, false, error, ErrorType.Failure);
    public static new Result<T> NotFound(string error) => new(default, false, error, ErrorType.NotFound);
    public static new Result<T> Conflict(string error) => new(default, false, error, ErrorType.Conflict);
    public static new Result<T> Forbidden(string error = "You do not have permission to perform this action.") => new(default, false, error, ErrorType.Forbidden);
    public static new Result<T> Unauthorized(string error = "Unauthorized access.") => new(default, false, error, ErrorType.Unauthorized);
    public static new Result<T> Validation(IDictionary<string, string[]> errors) => new(default, false, "One or more validation errors occurred.", ErrorType.Validation, errors);

    public static implicit operator Result<T>(T value) => Success(value);
}
