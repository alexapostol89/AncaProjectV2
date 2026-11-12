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

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default)
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
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.RoleId == UserRole.Admin ? nameof(UserRole.Admin) : nameof(UserRole.User),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                PendingBookings = _db.Bookings.Count(b => b.UserId == u.Id && b.Status == BookingStatus.Pending),
                ApprovedBookings = _db.Bookings.Count(b => b.UserId == u.Id && b.Status == BookingStatus.Approved)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserDto>
        {
            Items = page,
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

    private static ServiceResult<T> NotFound<T>() =>
        ServiceResult<T>.Fail(ServiceErrorType.NotFound, "Utilizatorul nu a fost găsit.", "not_found");
}
