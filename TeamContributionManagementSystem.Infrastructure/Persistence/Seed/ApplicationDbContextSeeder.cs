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

                -- user_roles
                ALTER TABLE IF EXISTS user_roles ADD COLUMN IF NOT EXISTS is_primary BOOLEAN DEFAULT FALSE;
                ALTER TABLE IF EXISTS user_roles ADD COLUMN IF NOT EXISTS is_secondary BOOLEAN DEFAULT FALSE;

                -- payment_transactions
                ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS member_name VARCHAR(150) NOT NULL DEFAULT '';
                ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS event_name VARCHAR(200) NOT NULL DEFAULT '';
                ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS txn_number VARCHAR(50) NOT NULL DEFAULT '';

                -- support_tickets
                ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS member_name VARCHAR(150) NOT NULL DEFAULT '';
                ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS member_id VARCHAR(100) NULL;
                ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS ticket_type VARCHAR(100) NOT NULL DEFAULT '';
                ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS priority VARCHAR(50) NOT NULL DEFAULT '';
                ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS status VARCHAR(50) NOT NULL DEFAULT '';
            ", cancellationToken);
        }
        catch
        {
            // Non-fatal if schema already contains the columns or permissions restrict DDL
        }
    }
}
