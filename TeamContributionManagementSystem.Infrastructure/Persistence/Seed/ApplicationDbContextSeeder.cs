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
                try
                {
                    var initSql = await File.ReadAllTextAsync(initScriptPath, cancellationToken);
                    var conn = _context.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open)
                    {
                        await conn.OpenAsync(cancellationToken);
                    }
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = initSql;
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to run init.sql via raw command: {Message}", ex.Message);
                }
            }

            await ExecuteScriptIfExistsAsync(SeedDefaults.NavigationMenusScript, cancellationToken);
            await ExecuteScriptIfExistsAsync(SeedDefaults.RoleRightsScript, cancellationToken);

            try
            {
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS users ALTER COLUMN username DROP NOT NULL; ALTER TABLE IF EXISTS users ALTER COLUMN password_hash DROP NOT NULL;", cancellationToken);
                await _context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS ix_users_username_lower ON users (LOWER(username)) WHERE is_deleted = false AND username IS NOT NULL AND username <> '';", cancellationToken);
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS gallery_photos ALTER COLUMN title TYPE VARCHAR(250);", cancellationToken);
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS enable_celebrant_selection BOOLEAN NOT NULL DEFAULT FALSE;", cancellationToken);
                await ExecuteScriptIfExistsAsync("add_enable_celebrant_selection_to_events.sql", cancellationToken);

                // Ensure birthday_reminder_logs table exists
                await _context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS birthday_reminder_logs (
    reminder_log_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    reminder_type VARCHAR(50) NOT NULL DEFAULT 'Scheduled',
    celebration_date TIMESTAMPTZ NOT NULL,
    celebrant_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    recipient_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    recipient_email VARCHAR(150) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Sent',
    sent_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    error_message VARCHAR(500) NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_birthday_reminder_logs_dedup
ON birthday_reminder_logs (celebration_date, celebrant_user_id, recipient_user_id, reminder_type);

CREATE UNIQUE INDEX IF NOT EXISTS ix_events_unique_event_type_month_year
ON events (event_type_id, EXTRACT(YEAR FROM event_date), EXTRACT(MONTH FROM event_date))
WHERE is_deleted = false;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'expenses' AND column_name = 'expense_number') THEN
        ALTER TABLE expenses RENAME COLUMN expense_number TO expense_no;
    END IF;
END $$;
ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS expense_no VARCHAR(30);
CREATE UNIQUE INDEX IF NOT EXISTS ix_expenses_expense_no
ON expenses (expense_no)
WHERE is_deleted = false AND expense_no IS NOT NULL AND expense_no <> '';

CREATE TABLE IF NOT EXISTS expense_sequences (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    year INT NOT NULL,
    month INT NOT NULL,
    last_sequence INT NOT NULL DEFAULT 0,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_expense_sequences_year_month ON expense_sequences (year, month);

CREATE TABLE IF NOT EXISTS exit_clearances (
    clearance_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    member_id UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    event_id UUID NOT NULL REFERENCES events(event_id) ON DELETE RESTRICT,
    status VARCHAR(50) NOT NULL DEFAULT 'PendingClearance',
    planned_exit_date TIMESTAMPTZ NULL,
    notes TEXT NULL,
    approval_remarks TEXT NULL,
    approved_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    approved_at TIMESTAMPTZ NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_exit_clearances_member_event
ON exit_clearances (member_id, event_id)
WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_exit_clearances_member_id ON exit_clearances(member_id);
CREATE INDEX IF NOT EXISTS ix_exit_clearances_event_id ON exit_clearances(event_id);
CREATE INDEX IF NOT EXISTS ix_exit_clearances_status ON exit_clearances(status);

CREATE TABLE IF NOT EXISTS exit_clearance_histories (
    history_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    clearance_id UUID NOT NULL REFERENCES exit_clearances(clearance_id) ON DELETE RESTRICT,
    member_id UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    event_id UUID NOT NULL REFERENCES events(event_id) ON DELETE RESTRICT,
    member_name_snapshot VARCHAR(200) NOT NULL,
    member_code_snapshot VARCHAR(100) NULL,
    event_name_snapshot VARCHAR(200) NOT NULL,
    joining_date TIMESTAMPTZ NULL,
    exit_approval_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    approved_by UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    approved_by_name VARCHAR(200) NOT NULL,
    total_obligations NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_verified_paid NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_approved_adjustments NUMERIC(12,2) NOT NULL DEFAULT 0,
    outstanding_at_approval NUMERIC(12,2) NOT NULL DEFAULT 0,
    payment_verification_summary TEXT NULL,
    approval_remarks TEXT NULL,
    snapshot_json TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS ix_exit_clearance_histories_member_id ON exit_clearance_histories(member_id);
CREATE INDEX IF NOT EXISTS ix_exit_clearance_histories_event_id ON exit_clearance_histories(event_id);
CREATE INDEX IF NOT EXISTS ix_exit_clearance_histories_clearance_id ON exit_clearance_histories(clearance_id);
", cancellationToken);
            }
            catch { /* Non-fatal */ }

            // 4. Ensure default roles exist
            var adminRole = await EnsureRoleAsync(CommonRoles.Admin, cancellationToken);
            var memberRole = await EnsureRoleAsync(CommonRoles.Member, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Clean up demo users from users and user_roles
            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM user_roles WHERE user_id IN (SELECT user_id FROM users WHERE LOWER(email) IN ('admin@gmail.com', 'member@gmail.com'));
                    DELETE FROM users WHERE LOWER(email) IN ('admin@gmail.com', 'member@gmail.com');
                ", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Non-fatal error removing seed demo users: {Message}", ex.Message);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // 7. Safe backfill for legacy expense records to populate budget_amount, extra_amount, and additional_expenses columns
            try
            {
                var legacyExpenses = await _context.Expenses
                    .Where(e => e.BudgetAmount == 0 && e.ExtraAmount == 0)
                    .ToListAsync(cancellationToken);

                if (legacyExpenses.Count > 0)
                {
                    bool anyModified = false;
                    foreach (var leg in legacyExpenses)
                    {
                        ExpenseMetadataHelper.UnpackDescription(
                            leg.Description,
                            leg.Amount,
                            out var cleanDesc,
                            out var budgetAmt,
                            out var extraAmt,
                            out var addRows);

                        leg.BudgetAmount = budgetAmt;
                        leg.ExtraAmount = extraAmt;
                        leg.Description = cleanDesc;
                        if (addRows != null && addRows.Count > 0)
                        {
                            leg.AdditionalExpenses = System.Text.Json.JsonSerializer.Serialize(addRows);
                        }
                        anyModified = true;
                    }

                    if (anyModified)
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                        _logger?.LogInformation("Successfully backfilled {Count} legacy expense records to use budget_amount, extra_amount, and additional_expenses columns.", legacyExpenses.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Non-fatal error backfilling legacy expenses: {Message}", ex.Message);
            }

            // 8. Safe backfill for legacy expense records without an expense_number
            try
            {
                var expensesWithoutNumber = await _context.Expenses
                    .Where(e => string.IsNullOrEmpty(e.ExpenseNo))
                    .OrderBy(e => e.CreatedAt ?? e.ExpenseDate)
                    .ToListAsync(cancellationToken);

                if (expensesWithoutNumber.Count > 0)
                {
                    var grouped = expensesWithoutNumber.GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month });
                    foreach (var group in grouped)
                    {
                        int y = group.Key.Year;
                        int m = group.Key.Month;
                        var seq = await _context.ExpenseSequences.FirstOrDefaultAsync(s => s.Year == y && s.Month == m, cancellationToken);
                        int currentSeq = seq?.LastSequence ?? 0;

                        foreach (var exp in group)
                        {
                            currentSeq++;
                            exp.ExpenseNo = $"EXP{y:D4}{m:D2}{currentSeq:D4}";
                        }

                        if (seq == null)
                        {
                            _context.ExpenseSequences.Add(new ExpenseSequence
                            {
                                Id = Guid.NewGuid(),
                                Year = y,
                                Month = m,
                                LastSequence = currentSeq,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            seq.LastSequence = currentSeq;
                            seq.UpdatedAt = DateTime.UtcNow;
                            _context.ExpenseSequences.Update(seq);
                        }
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    _logger?.LogInformation("Successfully backfilled {Count} expenses with generated expense numbers.", expensesWithoutNumber.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Non-fatal error backfilling expense numbers: {Message}", ex.Message);
            }
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

    private async Task ExecuteScriptIfExistsAsync(string scriptFileName, CancellationToken cancellationToken)
    {
        var scriptPath = ResolveScriptPath(scriptFileName);
        if (scriptPath != null && File.Exists(scriptPath))
        {
            var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                await _context.Database.ExecuteSqlRawAsync(sql, Array.Empty<object>(), cancellationToken);
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
