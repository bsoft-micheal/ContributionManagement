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
        // Database will be created if it does not already exist
        await _context.Database.EnsureCreatedAsync(cancellationToken);

        // Execute init.pgsql / init.sql script to ensure all tables, columns, and indexes are applied from SQL
        await ExecuteInitSqlScriptAsync(cancellationToken);
    }

    private async Task ExecuteInitSqlScriptAsync(CancellationToken cancellationToken)
    {
        try
        {
            var candidatePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Persistence", "Scripts", "init.pgsql"),
                Path.Combine(AppContext.BaseDirectory, "Persistence", "Scripts", "init.sql"),
                Path.Combine(AppContext.BaseDirectory, "Scripts", "init.pgsql"),
                Path.Combine(AppContext.BaseDirectory, "Scripts", "init.sql"),
                Path.Combine(AppContext.BaseDirectory, "init.pgsql"),
                Path.Combine(AppContext.BaseDirectory, "init.sql"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Persistence", "Scripts", "init.pgsql")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Persistence", "Scripts", "init.sql")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TeamContributionManagementSystem.Infrastructure", "Persistence", "Scripts", "init.pgsql")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TeamContributionManagementSystem.Infrastructure", "Persistence", "Scripts", "init.sql")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "TeamContributionManagementSystem.Infrastructure", "Persistence", "Scripts", "init.pgsql")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "TeamContributionManagementSystem.Infrastructure", "Persistence", "Scripts", "init.sql")),
                @"d:\ContributionManagement\backend\ContributionManagement\TeamContributionManagementSystem.Infrastructure\Persistence\Scripts\init.pgsql",
                @"d:\ContributionManagement\backend\ContributionManagement\TeamContributionManagementSystem.Infrastructure\Persistence\Scripts\init.sql"
            };

            var scriptPath = candidatePaths.FirstOrDefault(File.Exists);
            if (scriptPath != null)
            {
                var sqlContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(sqlContent))
                {
                    await _context.Database.ExecuteSqlRawAsync(sqlContent, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApplicationDbContextSeeder] Warning executing init.sql: {ex.Message}");
        }
    }
}
