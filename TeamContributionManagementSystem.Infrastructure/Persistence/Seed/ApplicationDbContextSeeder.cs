using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Auth;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Infrastructure.Persistence.Seed;

public class ApplicationDbContextSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration? _configuration;
    private readonly ILogger<ApplicationDbContextSeeder>? _logger;

    public ApplicationDbContextSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration? configuration = null,
        ILogger<ApplicationDbContextSeeder>? logger = null)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    private static class SeedDefaults
    {
        public const string AdminEmail = "admin@gmail.com";
        public const string FallbackAdminEmail = "daniel@example.com";
        public const string AdminFullName = "System Admin";
        public const string AdminPassword = "Password@123";

        public const string MemberEmail = "member@gmail.com";
        public const string MemberFullName = "Default Member";
        public const string MemberPassword = "Password@123";

        public const int MinimumNavigationMenuCount = 89;

        public const string NavigationMenusScript = "seed_navigation_menus.sql";
        public const string RoleRightsScript = "seed_role_rights.sql";
    }

    private static class ConfigKeys
    {
        public const string AdminEmail = "SeedData:Admin:Email";
        public const string AdminFallbackEmail = "SeedData:Admin:FallbackEmail";
        public const string AdminFullName = "SeedData:Admin:FullName";
        public const string AdminPassword = "SeedData:Admin:Password";

        public const string MemberEmail = "SeedData:Member:Email";
        public const string MemberFullName = "SeedData:Member:FullName";
        public const string MemberPassword = "SeedData:Member:Password";
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var initScriptPath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Scripts", "init.sql");
            if (!File.Exists(initScriptPath))
            {
                initScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "TeamContributionManagementSystem.Infrastructure", "Persistence", "Scripts", "init.sql");
            }
            if (File.Exists(initScriptPath))
            {
                var initSql = await File.ReadAllTextAsync(initScriptPath, cancellationToken);
                await _context.Database.ExecuteSqlRawAsync(initSql, cancellationToken);
            }

            await ExecuteScriptIfExistsAsync(SeedDefaults.NavigationMenusScript, cancellationToken);
            await ExecuteScriptIfExistsAsync(SeedDefaults.RoleRightsScript, cancellationToken);

            try
            {
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS users ALTER COLUMN username DROP NOT NULL; ALTER TABLE IF EXISTS users ALTER COLUMN password_hash DROP NOT NULL;", cancellationToken);
            }
            catch { /* Non-fatal */ }

            // 4. Ensure default roles exist
            var adminRole = await EnsureRoleAsync(CommonRoles.Admin, cancellationToken);
            var memberRole = await EnsureRoleAsync(CommonRoles.Member, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Ensure default admin user exists
            await EnsureAdminUserAsync(adminRole.RoleId, cancellationToken);

            // 6. Ensure default member user exists
            await EnsureMemberUserAsync(memberRole.RoleId, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Non-fatal if schema already contains columns or permissions restrict DDL
            _logger?.LogWarning(ex, "Non-fatal error occurred during database seeding / schema synchronization: {Message}", ex.Message);
        }
    }

    private async Task<Role> EnsureRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName, cancellationToken);
        if (role == null)
        {
            role = new Role
            {
                RoleId = Guid.NewGuid(),
                RoleName = roleName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Roles.AddAsync(role, cancellationToken);
        }
        return role;
    }

    private async Task EnsureAdminUserAsync(Guid adminRoleId, CancellationToken cancellationToken)
    {
        var adminEmail = _configuration?[ConfigKeys.AdminEmail] ?? SeedDefaults.AdminEmail;
        var fallbackEmail = _configuration?[ConfigKeys.AdminFallbackEmail] ?? SeedDefaults.FallbackAdminEmail;
        var adminFullName = _configuration?[ConfigKeys.AdminFullName] ?? SeedDefaults.AdminFullName;
        var adminPassword = _configuration?[ConfigKeys.AdminPassword] ?? SeedDefaults.AdminPassword;

        var adminUser = await _context.Users.FirstOrDefaultAsync(
            u => u.Email == adminEmail || (!string.IsNullOrEmpty(fallbackEmail) && u.Email == fallbackEmail),
            cancellationToken);

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserId = Guid.NewGuid(),
                FullName = adminFullName,
                Email = adminEmail,
                PasswordHash = _passwordHasher.HashPassword(adminPassword),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(adminUser, cancellationToken);
            await _context.UserRoles.AddAsync(new AppUserRole
            {
                UserId = adminUser.UserId,
                RoleId = adminRoleId,
                IsPrimary = true
            }, cancellationToken);
        }
    }

    private async Task EnsureMemberUserAsync(Guid memberRoleId, CancellationToken cancellationToken)
    {
        var memberEmail = _configuration?[ConfigKeys.MemberEmail] ?? SeedDefaults.MemberEmail;
        var memberFullName = _configuration?[ConfigKeys.MemberFullName] ?? SeedDefaults.MemberFullName;
        var memberPassword = _configuration?[ConfigKeys.MemberPassword] ?? SeedDefaults.MemberPassword;

        var memberUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == memberEmail, cancellationToken);
        if (memberUser == null)
        {
            memberUser = new AppUser
            {
                UserId = Guid.NewGuid(),
                FullName = memberFullName,
                Email = memberEmail,
                PasswordHash = _passwordHasher.HashPassword(memberPassword),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(memberUser, cancellationToken);
            await _context.UserRoles.AddAsync(new AppUserRole
            {
                UserId = memberUser.UserId,
                RoleId = memberRoleId,
                IsPrimary = true
            }, cancellationToken);
        }
    }

    private async Task ExecuteScriptIfExistsAsync(string scriptFileName, CancellationToken cancellationToken)
    {
        var scriptPath = ResolveScriptPath(scriptFileName);
        if (scriptPath != null && File.Exists(scriptPath))
        {
            var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
        }
    }

    private static string? ResolveScriptPath(string scriptFileName)
    {
        // 1. Check relative to base directory
        var path = Path.Combine(AppContext.BaseDirectory, "Persistence", "Scripts", scriptFileName);
        if (File.Exists(path)) return path;

        // 2. Check relative to project source directory
        path = Path.Combine(Directory.GetCurrentDirectory(), "..", "TeamContributionManagementSystem.Infrastructure", "Persistence", "Scripts", scriptFileName);
        if (File.Exists(path)) return path;

        // 3. Check directly in CurrentDirectory Persistence/Scripts
        path = Path.Combine(Directory.GetCurrentDirectory(), "Persistence", "Scripts", scriptFileName);
        if (File.Exists(path)) return path;

        return null;
    }
}
