using api.DTOs.Admin;
using api.DTOs.Auth;
using api.DTOs.Common;
using api.Services.Results;

namespace api.Services;

/// <summary>Read-only user overview for the admin panel. Only reachable through Admin-only endpoints.</summary>
public interface IUserAdminService
{
    /// <summary>Paged list, newest first, with each user's session counts.</summary>
    Task<PagedResult<AdminUserDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default);

    Task<ServiceResult<UserDto>> GetUserAsync(Guid userId);
}
