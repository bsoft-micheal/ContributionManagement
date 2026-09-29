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

        try
        {
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS members ALTER COLUMN role_id DROP NOT NULL;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS module VARCHAR(100) NULL;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS statuses DROP CONSTRAINT IF EXISTS statuses_status_name_key;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS statuses DROP CONSTRAINT IF EXISTS statuses_status_name_unique;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS statuses DROP CONSTRAINT IF EXISTS uq_statuses_status_name;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS attachment TEXT NULL;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS support_tickets ALTER COLUMN attachment TYPE TEXT;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS support_tickets ALTER COLUMN subject DROP NOT NULL;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS support_tickets ALTER COLUMN assigned_to DROP NOT NULL;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS file_name TEXT NULL;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS expenses ALTER COLUMN file_name TYPE TEXT;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS expenses ALTER COLUMN approved_by DROP NOT NULL;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS payment_modes ADD COLUMN IF NOT EXISTS is_cash BOOLEAN DEFAULT FALSE;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS payment_modes ADD COLUMN IF NOT EXISTS supports_qr BOOLEAN DEFAULT TRUE;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS payment_modes ADD COLUMN IF NOT EXISTS payment_type VARCHAR(50) DEFAULT 'Digital';", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("UPDATE payment_modes SET is_cash = TRUE, supports_qr = FALSE, payment_type = 'Cash' WHERE LOWER(TRIM(payment_mode_name)) = 'cash';", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("UPDATE payment_modes SET is_cash = FALSE, supports_qr = TRUE, payment_type = 'Split' WHERE LOWER(TRIM(payment_mode_name)) LIKE '%split%';", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("UPDATE payment_modes SET is_cash = FALSE, supports_qr = TRUE, payment_type = 'Digital' WHERE LOWER(TRIM(payment_mode_name)) NOT IN ('cash') AND LOWER(TRIM(payment_mode_name)) NOT LIKE '%split%';", cancellationToken);
        }
        catch { }
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
