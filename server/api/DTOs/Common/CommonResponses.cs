namespace api.DTOs.Common;

/// <summary>Simple { message } body — same shape the contact endpoint already returns.</summary>
public class MessageResponse
{
    public MessageResponse(string message) => Message = message;

    public string Message { get; }
}

/// <summary>
/// Error body returned by services (see ApiControllerBase.ErrorResult). Code is a
/// stable value the frontend can switch on (e.g. "email_taken"), Message is the
/// Romanian text to show the user.
/// </summary>
public class ErrorResponse
{
    public string Message { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }
}
