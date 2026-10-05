using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.Interfaces.Auth;

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


            var count = await _context.NavigationMenus.CountAsync(cancellationToken);
            if (count < 89)
            {
                var sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Scripts", "seed_navigation_menus.sql");
                if (!File.Exists(sqlScriptPath))
                {
                    sqlScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "TeamContributionManagementSystem.Infrastructure", "Persistence", "Scripts", "seed_navigation_menus.sql");
                }
                if (File.Exists(sqlScriptPath))
                {
                    var sql = await File.ReadAllTextAsync(sqlScriptPath, cancellationToken);
                    await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                }
            }

            // Seed / sync role_rights for all roles
            var rrScriptPath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Scripts", "seed_role_rights.sql");
            if (!File.Exists(rrScriptPath))
            {
                rrScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "TeamContributionManagementSystem.Infrastructure", "Persistence", "Scripts", "seed_role_rights.sql");
            }
            if (File.Exists(rrScriptPath))
            {
                var rrSql = await File.ReadAllTextAsync(rrScriptPath, cancellationToken);
                await _context.Database.ExecuteSqlRawAsync(rrSql, cancellationToken);
            }
            // Seed default roles & users if missing
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin", cancellationToken);
            if (adminRole == null)
            {
                adminRole = new Domain.Entities.Role { RoleId = Guid.NewGuid(), RoleName = "Admin" };
                await _context.Roles.AddAsync(adminRole, cancellationToken);
            }

            var memberRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Member", cancellationToken);
            if (memberRole == null)
            {
                memberRole = new Domain.Entities.Role { RoleId = Guid.NewGuid(), RoleName = "Member" };
                await _context.Roles.AddAsync(memberRole, cancellationToken);
            }
            await _context.SaveChangesAsync(cancellationToken);

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@gmail.com" || u.Email == "daniel@example.com", cancellationToken);
            if (adminUser == null)
            {
                adminUser = new Domain.Entities.AppUser
                {
                    UserId = Guid.NewGuid(),
                    FullName = "System Admin",
                    Email = "admin@gmail.com",
                    PasswordHash = _passwordHasher.HashPassword("Password@123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Users.AddAsync(adminUser, cancellationToken);
                await _context.UserRoles.AddAsync(new Domain.Entities.AppUserRole { UserId = adminUser.UserId, RoleId = adminRole.RoleId, IsPrimary = true }, cancellationToken);
            }

            var memberUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "member@gmail.com", cancellationToken);
            if (memberUser == null)
            {
                memberUser = new Domain.Entities.AppUser
                {
                    UserId = Guid.NewGuid(),
                    FullName = "Default Member",
                    Email = "member@gmail.com",
                    PasswordHash = _passwordHasher.HashPassword("Password@123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Users.AddAsync(memberUser, cancellationToken);
                await _context.UserRoles.AddAsync(new Domain.Entities.AppUserRole { UserId = memberUser.UserId, RoleId = memberRole.RoleId, IsPrimary = true }, cancellationToken);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Non-fatal if schema already contains the columns or permissions restrict DDL
        }
    }
}
