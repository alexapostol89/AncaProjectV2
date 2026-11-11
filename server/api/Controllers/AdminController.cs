using api.DTOs.Admin;
using api.DTOs.Auth;
using api.DTOs.Common;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

/// <summary>User management for the admin panel. Every action requires the Admin role.</summary>
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
    [ProducesResponseType<PagedResult<UserDto>>(StatusCodes.Status200OK)]
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

    [HttpPut("users/{id:guid}/role")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetRole(Guid id, [FromBody] UpdateUserRoleRequest request)
    {
        var result = await _userAdminService.SetRoleAsync(CurrentUserId, id, request.Role);
        return result.Succeeded ? Ok(result.Value) : ErrorResult(result.Error!);
    }

    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var result = await _userAdminService.DeleteUserAsync(CurrentUserId, id);
        return result.Succeeded ? NoContent() : ErrorResult(result.Error!);
    }
}
