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

        public const string MinMenuCount = "SeedData:NavigationMenu:MinimumCount";
    }

    private const string SchemaSynchronizationSql = @"
        -- contributions reminder tracking
        ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS last_reminder_sent_at TIMESTAMPTZ NULL;
        ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS reminder_count INT NOT NULL DEFAULT 0;

        -- system_settings
        ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS allowed_multiple_event BOOLEAN NOT NULL DEFAULT FALSE;

        -- budget_calculations
        ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS category VARCHAR(100) NULL;
        ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS event_type_id UUID NULL;

        -- roles
        ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS default_contribution_amount NUMERIC(12,2) NOT NULL DEFAULT 0;

        -- users
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_primary BOOLEAN DEFAULT FALSE;
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_secondary BOOLEAN DEFAULT FALSE;
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS enable_multiple_roles BOOLEAN DEFAULT FALSE;
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS phone VARCHAR(20) DEFAULT '';
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS gender VARCHAR(20) DEFAULT 'Male';
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS work_type VARCHAR(20) DEFAULT 'Office';
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS work_type_id UUID NULL;
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS date_of_birth TIMESTAMPTZ DEFAULT CURRENT_DATE;
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS joining_date TIMESTAMPTZ DEFAULT CURRENT_DATE;
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_exited BOOLEAN DEFAULT FALSE;
        ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_first_login BOOLEAN DEFAULT TRUE;
        ALTER TABLE IF EXISTS users ALTER COLUMN profile_image TYPE TEXT;
    ";

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

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Ensure required schema columns and migrations exist
            await _context.Database.ExecuteSqlRawAsync(SchemaSynchronizationSql, cancellationToken);

            // 2. Ensure navigation menus are seeded
            var minMenuCount = _configuration?.GetValue(ConfigKeys.MinMenuCount, SeedDefaults.MinimumNavigationMenuCount)
                ?? SeedDefaults.MinimumNavigationMenuCount;

            var menuCount = await _context.NavigationMenus.CountAsync(cancellationToken);
            if (menuCount < minMenuCount)
            {
                await ExecuteScriptIfExistsAsync(SeedDefaults.NavigationMenusScript, cancellationToken);
            }

            // 3. Ensure role rights are seeded / synced for all roles
            await ExecuteScriptIfExistsAsync(SeedDefaults.RoleRightsScript, cancellationToken);

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
