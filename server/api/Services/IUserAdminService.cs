using api.DTOs.Admin;
using api.DTOs.Auth;
using api.DTOs.Common;
using api.Services.Results;

namespace api.Services;

/// <summary>User management for the admin panel. Only reachable through Admin-only endpoints.</summary>
public interface IUserAdminService
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default);

    Task<ServiceResult<UserDto>> GetUserAsync(Guid userId);

    /// <summary>Changes a user's role. An admin can't demote themselves.</summary>
    Task<ServiceResult<UserDto>> SetRoleAsync(Guid actingAdminId, Guid userId, string role);

    /// <summary>Deletes another user's account. Admins delete their own account via /api/auth.</summary>
    Task<ServiceResult> DeleteUserAsync(Guid actingAdminId, Guid userId);
}
