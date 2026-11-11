using System.ComponentModel.DataAnnotations;

namespace api.DTOs.Admin;

/// <summary>Query string for GET /api/admin/users.</summary>
public class UserListQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>Matches email or full name (case-insensitive).</summary>
    [StringLength(100)]
    public string? Search { get; set; }
}

public class UpdateUserRoleRequest
{
    /// <summary>"Admin" or "User".</summary>
    [Required(ErrorMessage = "Rolul este obligatoriu.")]
    [RegularExpression("^(Admin|User)$", ErrorMessage = "Rolul trebuie să fie Admin sau User.")]
    public string Role { get; set; } = string.Empty;
}
