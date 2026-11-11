namespace api.Configuration;

/// <summary>
/// First admin account, created at startup if it doesn't exist yet.
/// Public registration can never create admins — this is the only way in.
/// Set via user-secrets (dev) or Fly secrets (prod):
///   Seed__AdminEmail, Seed__AdminPassword, Seed__AdminFullName
/// The password is only used when the account is first created; you can
/// remove the secret afterwards.
/// </summary>
public class SeedSettings
{
    public const string SectionName = "Seed";

    public string? AdminEmail { get; set; }

    public string? AdminPassword { get; set; }

    public string AdminFullName { get; set; } = "Administrator";
}
