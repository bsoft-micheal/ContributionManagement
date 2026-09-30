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
            await _context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_primary BOOLEAN DEFAULT FALSE;
                ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS is_secondary BOOLEAN DEFAULT FALSE;
                ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS enable_multiple_roles BOOLEAN DEFAULT FALSE;
                ALTER TABLE IF EXISTS user_roles ADD COLUMN IF NOT EXISTS is_primary BOOLEAN DEFAULT FALSE;
                ALTER TABLE IF EXISTS user_roles ADD COLUMN IF NOT EXISTS is_secondary BOOLEAN DEFAULT FALSE;
            ", cancellationToken);
        }
        catch
        {
            // Non-fatal if schema already contains the columns or permissions restrict DDL
        }
    }
}
