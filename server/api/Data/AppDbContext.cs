using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data;

/// <summary>
/// Database context (PostgreSQL on Neon).
///   Roles  — fixed lookup table: (1, Admin), (2, User)
///   Users  — accounts; Users.RoleId → Roles.Id (many users, one role each)
/// Future chatbot tables (Conversations, ChatMessages) go here too, with a
/// cascade-delete foreign key to User so deleting an account removes everything.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(role =>
        {
            role.HasKey(r => r.Id);
            role.Property(r => r.Id).ValueGeneratedNever(); // ids are fixed, not auto-numbered
            role.Property(r => r.Name).HasMaxLength(20).IsRequired();
            role.HasIndex(r => r.Name).IsUnique();

            // Seed data: the migration inserts these two rows.
            role.HasData(
                new Role { Id = UserRole.Admin, Name = nameof(UserRole.Admin) },
                new Role { Id = UserRole.User, Name = nameof(UserRole.User) });
        });

        modelBuilder.Entity<User>(user =>
        {
            user.HasKey(u => u.Id);
            user.HasIndex(u => u.Email).IsUnique();

            user.Property(u => u.Email).HasMaxLength(180).IsRequired();
            user.Property(u => u.FullName).HasMaxLength(120).IsRequired();
            user.Property(u => u.PasswordHash).IsRequired();
            user.Property(u => u.PasswordResetTokenHash).HasMaxLength(64);

            // Many users → one role. Restrict: a role that still has users can't be deleted.
            user.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
