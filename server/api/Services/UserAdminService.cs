using api.Data;
using api.DTOs.Admin;
using api.DTOs.Auth;
using api.DTOs.Common;
using api.Models;
using api.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class UserAdminService : IUserAdminService
{
    private readonly AppDbContext _db;

    public UserAdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        var users = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            users = users.Where(u => u.Email.Contains(search) || u.FullName.ToLower().Contains(search));
        }

        var totalCount = await users.CountAsync(cancellationToken);

        var page = await users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = page.Select(UserDto.FromUser).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ServiceResult<UserDto>> GetUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user is null ? NotFound<UserDto>() : ServiceResult<UserDto>.Ok(UserDto.FromUser(user));
    }

    public async Task<ServiceResult<UserDto>> SetRoleAsync(Guid actingAdminId, Guid userId, string role)
    {
        var newRole = Enum.Parse<UserRole>(role); // already validated by UpdateUserRoleRequest

        if (userId == actingAdminId && newRole != UserRole.Admin)
        {
            return ServiceResult<UserDto>.Fail(ServiceErrorType.Conflict,
                "Nu îți poți elimina propriul rol de administrator.", "cannot_demote_self");
        }

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return NotFound<UserDto>();
        }

        // Takes effect at the user's next login (their current token still has the old role).
        user.RoleId = newRole;
        await _db.SaveChangesAsync();
        return ServiceResult<UserDto>.Ok(UserDto.FromUser(user));
    }

    public async Task<ServiceResult> DeleteUserAsync(Guid actingAdminId, Guid userId)
    {
        if (userId == actingAdminId)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict,
                "Nu îți poți șterge propriul cont din panoul de administrare.", "cannot_delete_self");
        }

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return NotFound<UserDto>();
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private static ServiceResult<T> NotFound<T>() =>
        ServiceResult<T>.Fail(ServiceErrorType.NotFound, "Utilizatorul nu a fost găsit.", "not_found");
}
