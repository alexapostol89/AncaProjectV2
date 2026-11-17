namespace api.Models;

/// <summary>
/// The two fixed roles. The numbers are the Ids of the rows in the Roles table
/// (seeded by the migration), so Users.RoleId = 1 means Admin, 2 means User.
/// In attributes use nameof: [Authorize(Roles = nameof(UserRole.Admin))].
/// </summary>
public enum UserRole
{
    Admin = 1,
    User = 2
}

/// <summary>Claim names written into our JWTs.</summary>
public static class AppClaimTypes
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
}
