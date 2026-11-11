namespace api.Models;

/// <summary>
/// A row in the Roles table. The table is fixed: it always contains exactly
/// (1, "Admin") and (2, "User"), inserted by the migration (see AppDbContext).
/// </summary>
public class Role
{
    public UserRole Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>All users that have this role (one role → many users).</summary>
    public ICollection<User> Users { get; set; } = [];
}
