using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.Interfaces.Auth;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Infrastructure.Persistence.Seed;

public class ApplicationDbContextSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ApplicationDbContextSeeder(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Database will be created if it does not already exist
        await _context.Database.EnsureCreatedAsync(cancellationToken);

        // Self-healing DB update for password reset OTP columns in Postgres
        await _context.Database.ExecuteSqlRawAsync("ALTER TABLE users ADD COLUMN IF NOT EXISTS password_reset_otp VARCHAR(10) NULL;");
        await _context.Database.ExecuteSqlRawAsync("ALTER TABLE users ADD COLUMN IF NOT EXISTS password_reset_otp_expiry TIMESTAMP WITH TIME ZONE NULL;");
        await _context.Database.ExecuteSqlRawAsync("ALTER TABLE event_types ADD COLUMN IF NOT EXISTS base_amount DECIMAL(12, 2) NOT NULL DEFAULT 0;");
        await _context.Database.ExecuteSqlRawAsync("UPDATE event_types SET base_amount = 500 WHERE LOWER(event_type_name) LIKE '%birthday%' AND (base_amount = 0 OR base_amount IS NULL);");

        if (!await _context.Users.AnyAsync(cancellationToken))
        {
            var adminUser = new AppUser
            {
                UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
                Username = "admin",
                Email = "admin@teamcontribution.local",
                FullName = "System Administrator",
                PasswordHash = _passwordHasher.HashPassword("Admin@123"),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };
            await _context.Users.AddAsync(adminUser, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Seeding default Role Rights
        var existingRoleRights = await _context.RoleRights.ToListAsync(cancellationToken);
        var existingKeySet = existingRoleRights
            .Select(x => $"{x.Role}|{x.Module.Trim()}|{x.SubModule.Trim()}|{x.Page.Trim()}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var defaultPages = new[]
        {
            (Module: "Dashboard", SubModule: "Analytics", Page: "Dashboard"),
            (Module: "Members", SubModule: "Directory", Page: "Members"),
            (Module: "Events", SubModule: "Registry", Page: "Events"),
            (Module: "Events", SubModule: "Calendar", Page: "Calendar"),
            (Module: "Contributions", SubModule: "Ledger", Page: "Contributions"),
            (Module: "Contributions", SubModule: "Calculation", Page: "Calculation"),
            (Module: "Support Data", SubModule: "Categories", Page: "Event Types"),
            (Module: "Support Data", SubModule: "Clearance", Page: "Exit Process"),
            (Module: "Support Data", SubModule: "Admin", Page: "User Rights"),
            (Module: "Support Data", SubModule: "Admin", Page: "Users"),
            (Module: "Support Data", SubModule: "Admin", Page: "Roles"),
            (Module: "Reports", SubModule: "Analytics", Page: "Event Audit"),
            (Module: "Reports", SubModule: "Analytics", Page: "Member Velocity"),
            (Module: "Reports", SubModule: "Analytics", Page: "Pending Dues"),
            (Module: "Reports", SubModule: "Analytics", Page: "Member Category Paid")
        };

        var roleRightsList = new List<RoleRight>();

        foreach (var role in Enum.GetValues<UserRole>())
        {
            foreach (var page in defaultPages)
            {
                var key = $"{role}|{page.Module.Trim()}|{page.SubModule.Trim()}|{page.Page.Trim()}";
                if (existingKeySet.Contains(key))
                {
                    continue;
                }

                string access = "readWrite"; // default for Admin / Manager

                if (role == UserRole.User || role == UserRole.Member)
                {
                    if (page.Page == "Event Types" || page.Page == "Exit Process" || page.Page == "User Rights" || page.Page == "Users")
                    {
                        access = "deny";
                    }
                    else
                    {
                        access = "readOnly";
                    }
                }

                roleRightsList.Add(new RoleRight
                {
                    RoleRightId = Guid.NewGuid(),
                    Role = role,
                    Module = page.Module,
                    SubModule = page.SubModule,
                    Page = page.Page,
                    Access = access
                });
                existingKeySet.Add(key);
            }
        }

        if (roleRightsList.Count > 0)
        {
            await _context.RoleRights.AddRangeAsync(roleRightsList, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
