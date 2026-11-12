using api.DTOs.Admin;
using api.DTOs.Auth;
using api.DTOs.Common;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

/// <summary>
/// Read-only user overview for the admin panel. Every action requires the Admin role.
/// Changing roles or deleting accounts is deliberately not exposed: developers do that in the database.
/// </summary>
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : ApiControllerBase
{
    private readonly IUserAdminService _userAdminService;

    public AdminController(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    /// <summary>Paged user list, newest first. Optional ?search= (email or name).</summary>
    [HttpGet("users")]
    [ProducesResponseType<PagedResult<AdminUserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] UserListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _userAdminService.GetUsersAsync(query, cancellationToken));
    }

    [HttpGet("users/{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await _userAdminService.GetUserAsync(id);
        return result.Succeeded ? Ok(result.Value) : ErrorResult(result.Error!);
    }
}
