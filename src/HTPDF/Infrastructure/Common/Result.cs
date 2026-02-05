namespace HTPDF.Infrastructure.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public static Result Success(string message = "") => new(true, message);
    public static Result Failure(string message) => new(false, message);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, string message) : base(isSuccess, message)
    {
        Value = value;
    }

    public static Result<T> Success(T value, string message = "") => new(value, true, message);
    public static new Result<T> Failure(string message) => new(default, false, message);
}
