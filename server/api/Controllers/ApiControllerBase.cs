using System.Security.Claims;
using api.DTOs.Common;
using api.Models;
using api.Services.Results;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

/// <summary>Shared helpers for controllers that call services returning ServiceResult.</summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id of the logged-in user, read from the JWT "sub" claim. Only use inside [Authorize] actions.</summary>
    protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue(AppClaimTypes.Subject)!);

    /// <summary>Turns a ServiceError into an HTTP response with an { message, code } body.</summary>
    protected IActionResult ErrorResult(ServiceError error)
    {
        var status = error.Type switch
        {
            ServiceErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(status, new ErrorResponse { Message = error.Message, Code = error.Code });
    }
}
