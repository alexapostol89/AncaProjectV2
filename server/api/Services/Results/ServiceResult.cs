namespace api.Services.Results;

public enum ServiceErrorType
{
    Validation,   // 400
    Unauthorized, // 401
    NotFound,     // 404
    Conflict      // 409
}

/// <summary>
/// A failure a service reports to its controller. Services don't know about HTTP;
/// ApiControllerBase.ErrorResult turns the Type into a status code.
/// Code is a stable value the frontend can check (e.g. "email_taken"),
/// Message is the Romanian text to show the user.
/// </summary>
public record ServiceError(ServiceErrorType Type, string Message, string Code);

/// <summary>Outcome of a service call: either success, or a ServiceError.</summary>
public class ServiceResult
{
    protected ServiceResult(ServiceError? error) => Error = error;

    public ServiceError? Error { get; }

    public bool Succeeded => Error is null;

    public static ServiceResult Ok() => new(null);

    public static ServiceResult Fail(ServiceErrorType type, string message, string code) =>
        new(new ServiceError(type, message, code));
}

/// <summary>Same as ServiceResult, but carries a value on success.</summary>
public class ServiceResult<T> : ServiceResult
{
    private ServiceResult(T? value, ServiceError? error) : base(error) => Value = value;

    /// <summary>Only set when Succeeded is true.</summary>
    public T? Value { get; }

    public static ServiceResult<T> Ok(T value) => new(value, null);

    public static new ServiceResult<T> Fail(ServiceErrorType type, string message, string code) =>
        new(default, new ServiceError(type, message, code));
}
