namespace VendingMachine.Domain.Common;

/// <summary>Non-generic result for operations that return no value.</summary>
public sealed class Result
{
    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Error message when the operation failed; null on success.</summary>
    public string? Error { get; }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Creates a failed result with the given error message.</summary>
    public static Result Failure(string error) => new(false, error);

    /// <summary>Creates a successful result carrying a value.</summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>Creates a failed result carrying the given error message.</summary>
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}

/// <summary>Result of an operation that returns a value of type <typeparamref name="T"/>.</summary>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The result value; only valid when <see cref="IsSuccess"/> is true.</summary>
    public T? Value { get; }

    /// <summary>Error message when the operation failed; null on success.</summary>
    public string? Error { get; }

    /// <summary>Creates a successful result carrying a value.</summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>Creates a failed result with the given error message.</summary>
    public static Result<T> Failure(string error) => new(false, default, error);
}
