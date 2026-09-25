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
        // Ensure gpay.png is present in output wwwroot directory
        try
        {
            var targetDir = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var targetFile = Path.Combine(targetDir, "gpay.png");
            if (!File.Exists(targetFile))
            {
                var candidateSources = new[]
                {
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot", "gpay.png")),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TeamContributionManagementSystem.API", "wwwroot", "gpay.png")),
                    @"d:\ContributionManagement\backend\ContributionManagement\TeamContributionManagementSystem.API\wwwroot\gpay.png"
                };
                var source = candidateSources.FirstOrDefault(File.Exists);
                if (source != null)
                {
                    Directory.CreateDirectory(targetDir);
                    File.Copy(source, targetFile, true);
                }
            }
        }
        catch { }

        // Database will be created if it does not already exist
        await _context.Database.EnsureCreatedAsync(cancellationToken);

        // Self-healing DB update for password reset OTP columns in Postgres
        await _context.Database.ExecuteSqlRawAsync("ALTER TABLE users ADD COLUMN IF NOT EXISTS password_reset_otp VARCHAR(10) NULL;");
        await _context.Database.ExecuteSqlRawAsync("ALTER TABLE users ADD COLUMN IF NOT EXISTS password_reset_otp_expiry TIMESTAMP WITH TIME ZONE NULL;");
        await _context.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS events ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS support_tickets ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS system_settings ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS payment_transactions ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS gallery_photos ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS members ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS roles ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS event_types ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS contributions ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
            ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
            ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS role_rights ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS ticket_types ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;

            ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
            ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
            ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS created_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS modified_by VARCHAR(150) NULL;
            ALTER TABLE IF EXISTS work_types ADD COLUMN IF NOT EXISTS modified_on TIMESTAMP WITH TIME ZONE NULL;
        ");

        await _context.Database.ExecuteSqlRawAsync("ALTER TABLE event_types ADD COLUMN IF NOT EXISTS base_amount DECIMAL(12, 2) NOT NULL DEFAULT 0;");
        await _context.Database.ExecuteSqlRawAsync("ALTER TABLE contributions ADD COLUMN IF NOT EXISTS cash_amount NUMERIC(12, 2) NULL;");
        await _context.Database.ExecuteSqlRawAsync("ALTER TABLE contributions ADD COLUMN IF NOT EXISTS upi_amount NUMERIC(12, 2) NULL;");
        await _context.Database.ExecuteSqlRawAsync("UPDATE event_types SET base_amount = 500 WHERE LOWER(event_type_name) LIKE '%birthday%' AND (base_amount = 0 OR base_amount IS NULL);");
        await _context.Database.ExecuteSqlRawAsync("ALTER TABLE members ADD COLUMN IF NOT EXISTS member_type VARCHAR(20) NOT NULL DEFAULT 'Office';");
        try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE gallery_photos ALTER COLUMN image_url TYPE TEXT;"); } catch { }
        try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE payment_transactions ALTER COLUMN screenshot TYPE TEXT;"); } catch { }
        try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE support_tickets ADD COLUMN IF NOT EXISTS attachment TEXT;"); } catch { }
        try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE support_tickets ALTER COLUMN attachment TYPE TEXT;"); } catch { }
        // Self-healing: Ensure new tables exist in PostgreSQL
        await _context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS expenses (
                expense_id UUID PRIMARY KEY,
                event_name VARCHAR(200) NOT NULL,
                category VARCHAR(100) NOT NULL,
                amount NUMERIC(12,2) NOT NULL,
                expense_date TIMESTAMP WITH TIME ZONE NOT NULL,
                status VARCHAR(50) NOT NULL,
                submitted_by VARCHAR(150) NOT NULL,
                approved_by VARCHAR(150) NULL,
                description VARCHAR(1000) NOT NULL DEFAULT '',
                file_name VARCHAR(500) NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_by VARCHAR(150) NULL,
                created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modified_by VARCHAR(150) NULL,
                modified_on TIMESTAMP WITH TIME ZONE NULL
            );

            CREATE TABLE IF NOT EXISTS support_tickets (
                ticket_id UUID PRIMARY KEY,
                ticket_no VARCHAR(50) NOT NULL UNIQUE,
                member_name VARCHAR(150) NOT NULL,
                member_id VARCHAR(100) NULL,
                related_event VARCHAR(200) NULL,
                ticket_type VARCHAR(100) NOT NULL,
                subject VARCHAR(300) NOT NULL,
                description VARCHAR(2000) NOT NULL,
                status VARCHAR(50) NOT NULL,
                priority VARCHAR(50) NOT NULL,
                assigned_to VARCHAR(150) NULL,
                ref_no VARCHAR(100) NULL,
                utr VARCHAR(100) NULL,
                attachment TEXT NULL,
                resolution_notes VARCHAR(2000) NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_by VARCHAR(150) NULL,
                created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modified_by VARCHAR(150) NULL,
                modified_on TIMESTAMP WITH TIME ZONE NULL
            );

            CREATE TABLE IF NOT EXISTS system_settings (
                setting_id UUID PRIMARY KEY,
                setting_key VARCHAR(100) NOT NULL UNIQUE,
                setting_value TEXT NOT NULL,
                category VARCHAR(100) NOT NULL,
                description VARCHAR(500) NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_by VARCHAR(150) NULL,
                created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modified_by VARCHAR(150) NULL,
                modified_on TIMESTAMP WITH TIME ZONE NULL
            );

            CREATE TABLE IF NOT EXISTS payment_transactions (
                transaction_id UUID PRIMARY KEY,
                txn_number VARCHAR(50) NOT NULL UNIQUE,
                member_name VARCHAR(150) NOT NULL,
                event_name VARCHAR(200) NOT NULL,
                amount NUMERIC(12,2) NOT NULL,
                payment_date TIMESTAMP WITH TIME ZONE NOT NULL,
                payment_mode VARCHAR(50) NOT NULL,
                utr VARCHAR(100) NULL,
                status VARCHAR(50) NOT NULL,
                verified_by VARCHAR(150) NULL,
                verified_on TIMESTAMP WITH TIME ZONE NULL,
                notes VARCHAR(1000) NULL,
                screenshot VARCHAR(500) NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_by VARCHAR(150) NULL,
                created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modified_by VARCHAR(150) NULL,
                modified_on TIMESTAMP WITH TIME ZONE NULL
            );

            CREATE TABLE IF NOT EXISTS gallery_photos (
                photo_id UUID PRIMARY KEY,
                title VARCHAR(200) NOT NULL,
                event_name VARCHAR(200) NOT NULL,
                category VARCHAR(100) NOT NULL,
                image_url TEXT NOT NULL,
                taken_date TIMESTAMP WITH TIME ZONE NOT NULL,
                description VARCHAR(1000) NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_by VARCHAR(150) NULL,
                created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modified_by VARCHAR(150) NULL,
                modified_on TIMESTAMP WITH TIME ZONE NULL
            );

            CREATE TABLE IF NOT EXISTS budget_calculations (
                budget_calculation_id UUID PRIMARY KEY,
                expense_item VARCHAR(150) NOT NULL,
                rate NUMERIC(12,2) NOT NULL DEFAULT 0,
                category VARCHAR(100) NULL DEFAULT 'Birthday',
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_by VARCHAR(150) NULL,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modified_by VARCHAR(150) NULL,
                modified_on TIMESTAMP WITH TIME ZONE NULL
            );

            CREATE TABLE IF NOT EXISTS ticket_types (
                ticket_type_id UUID PRIMARY KEY,
                type_name VARCHAR(150) NOT NULL UNIQUE,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_by VARCHAR(150) NULL,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modified_by VARCHAR(150) NULL,
                modified_on TIMESTAMP WITH TIME ZONE NULL
            );

            CREATE TABLE IF NOT EXISTS statuses (
                status_id UUID PRIMARY KEY,
                status_name VARCHAR(100) NOT NULL UNIQUE,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_by VARCHAR(150) NULL,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modified_by VARCHAR(150) NULL,
                modified_on TIMESTAMP WITH TIME ZONE NULL
            );

            CREATE TABLE IF NOT EXISTS work_types (
                work_type_id UUID PRIMARY KEY,
                work_type_name VARCHAR(100) NOT NULL UNIQUE,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_by VARCHAR(150) NULL,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                created_on TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modified_by VARCHAR(150) NULL,
                modified_on TIMESTAMP WITH TIME ZONE NULL
            );
        ");

        try { await _context.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS ticket_types DROP COLUMN IF EXISTS description;"); } catch { }

        // Seed initial Budget Calculations if empty
        try
        {
            if (!await _context.BudgetCalculations.AnyAsync(cancellationToken))
            {
                var defaultBudgetItems = new List<BudgetCalculation>
                {
                    new BudgetCalculation
                    {
                        BudgetCalculationId = Guid.NewGuid(),
                        ExpenseItem = "½ kg Cake",
                        Rate = 300,
                        Category = "Birthday",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    },
                    new BudgetCalculation
                    {
                        BudgetCalculationId = Guid.NewGuid(),
                        ExpenseItem = "Chicken Roll / Puffs",
                        Rate = 20,
                        Category = "Birthday",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    },
                    new BudgetCalculation
                    {
                        BudgetCalculationId = Guid.NewGuid(),
                        ExpenseItem = "Birthday Gift",
                        Rate = 1000,
                        Category = "Birthday",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    }
                };
                await _context.BudgetCalculations.AddRangeAsync(defaultBudgetItems, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch { }

        // Seed initial Ticket Types if empty
        try
        {
            if (!await _context.TicketTypes.AnyAsync(cancellationToken))
            {
                var defaultTicketTypes = new List<TicketType>
                {
                    new TicketType
                    {
                        TicketTypeId = Guid.NewGuid(),
                        TypeName = "Payment Issue",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    },
                    new TicketType
                    {
                        TicketTypeId = Guid.NewGuid(),
                        TypeName = "Event Clarification",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    },
                    new TicketType
                    {
                        TicketTypeId = Guid.NewGuid(),
                        TypeName = "Application Issue",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    },
                    new TicketType
                    {
                        TicketTypeId = Guid.NewGuid(),
                        TypeName = "Feedback",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    },
                    new TicketType
                    {
                        TicketTypeId = Guid.NewGuid(),
                        TypeName = "Other",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    }
                };
                await _context.TicketTypes.AddRangeAsync(defaultTicketTypes, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch { }

        // Seed initial Statuses if empty
        try
        {
            if (!await _context.Statuses.AnyAsync(cancellationToken))
            {
                var defaultStatuses = new List<Status>
                {
                    new Status
                    {
                        StatusId = Guid.NewGuid(),
                        StatusName = "Open",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    },
                    new Status
                    {
                        StatusId = Guid.NewGuid(),
                        StatusName = "In Progress",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    },
                    new Status
                    {
                        StatusId = Guid.NewGuid(),
                        StatusName = "Resolved",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    },
                    new Status
                    {
                        StatusId = Guid.NewGuid(),
                        StatusName = "Closed",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    }
                };
                await _context.Statuses.AddRangeAsync(defaultStatuses, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch { }

        // Seed initial Work Types if empty
        try
        {
            if (!await _context.WorkTypes.AnyAsync(cancellationToken))
            {
                var defaultWorkTypes = new List<WorkType>
                {
                    new WorkType
                    {
                        WorkTypeId = Guid.NewGuid(),
                        WorkTypeName = "Office",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    },
                    new WorkType
                    {
                        WorkTypeId = Guid.NewGuid(),
                        WorkTypeName = "WFH",
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    }
                };
                await _context.WorkTypes.AddRangeAsync(defaultWorkTypes, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch { }

        // Clean up previously seeded dummy expenses if any exist
        var dummyExpenses = await _context.Expenses
            .Where(x => (x.CreatedBy == "System" || x.CreatedBy == null) && (x.EventName == "Team Dinner" || x.EventName == "Michael Farewell" || x.EventName == "Priya Birthday"))
            .ToListAsync(cancellationToken);
        if (dummyExpenses.Any())
        {
            _context.Expenses.RemoveRange(dummyExpenses);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Seed initial Support Tickets if empty
        if (!await _context.SupportTickets.AnyAsync(cancellationToken))
        {
            var initialTickets = new List<SupportTicket>
            {
                new() { TicketId = Guid.NewGuid(), TicketNo = "TKT-2026-001", MemberName = "Rahul Sharma", MemberId = "MEM-001", RelatedEvent = "Team Dinner", TicketType = "Payment Issue", Subject = "Payment not reflected in my account", Description = "I have made the payment for Team Dinner on 24 Sep 2026, but it is not yet reflected in my account. Please check and confirm.", Status = "Open", Priority = "High", AssignedTo = null, RefNo = "REF-20260924-001", Utr = "HDFC1234567890", Attachment = "payment_screenshot.jpg (245 KB)", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { TicketId = Guid.NewGuid(), TicketNo = "TKT-2026-002", MemberName = "Sneha Iyer", MemberId = "MEM-002", RelatedEvent = "Michael Farewell", TicketType = "Event Clarification", Subject = "Event venue details", Description = "Could you please share the exact hall location and parking instructions for the farewell event?", Status = "In Progress", Priority = "Medium", AssignedTo = "Priya N.", RefNo = "REF-20260923-002", CreatedBy = null, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new() { TicketId = Guid.NewGuid(), TicketNo = "TKT-2026-003", MemberName = "Amit Patel", MemberId = "MEM-003", RelatedEvent = "Priya Birthday", TicketType = "Application Issue", Subject = "Cannot access event page", Description = "Getting an error when attempting to access the birthday event registry tab.", Status = "Resolved", Priority = "Low", AssignedTo = "Rohit S.", RefNo = "REF-20260922-003", ResolutionNotes = "Cache cleared and user right refreshed.", CreatedBy = null, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new() { TicketId = Guid.NewGuid(), TicketNo = "TKT-2026-004", MemberName = "Divya Nair", MemberId = "MEM-004", RelatedEvent = "Team Dinner", TicketType = "Payment Issue", Subject = "Wrong amount deducted", Description = "Two payments were initiated by mistake for the same dinner event contribution.", Status = "In Progress", Priority = "High", AssignedTo = null, RefNo = "REF-20260921-004", Utr = "ICIC9876543210", Attachment = "bank_statement.pdf (512 KB)", CreatedBy = null, CreatedAt = DateTime.UtcNow.AddDays(-3) }
            };
            await _context.SupportTickets.AddRangeAsync(initialTickets, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Seed initial System Settings if empty
        if (!await _context.SystemSettings.AnyAsync(cancellationToken))
        {
            var initialSettings = new List<SystemSetting>
            {
                new() { SettingId = Guid.NewGuid(), SettingKey = "orgName", SettingValue = "Unit 1A Residents Association", Category = "General", Description = "Organization display name" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "defaultCurrency", SettingValue = "INR", Category = "General", Description = "Default currency code" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "timeZone", SettingValue = "Asia/Kolkata", Category = "General", Description = "Timezone" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "fromEmail", SettingValue = "noreply@unit1a.com", Category = "Email", Description = "System sender email" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "fromName", SettingValue = "Unit 1A Management", Category = "Email", Description = "Sender display name" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "smtpHost", SettingValue = "smtp.gmail.com", Category = "Email", Description = "SMTP Server host" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "smtpPort", SettingValue = "587", Category = "Email", Description = "SMTP Server port" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "encryption", SettingValue = "TLS", Category = "Email", Description = "Email encryption standard" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "otpExpiry", SettingValue = "10", Category = "Security", Description = "OTP Expiration in minutes" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "maxRetry", SettingValue = "3", Category = "Security", Description = "Max OTP retry attempts" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "enableOtpLogin", SettingValue = "true", Category = "Security", Description = "Enable OTP based password reset" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "enable2faAdmin", SettingValue = "false", Category = "Security", Description = "Two-Factor Auth for Admin" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "enableEmailNotif", SettingValue = "true", Category = "Notifications", Description = "Global email notifications" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "notifNewMember", SettingValue = "true", Category = "Notifications", Description = "New member alert" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "notifPaymentConfirm", SettingValue = "true", Category = "Notifications", Description = "Payment confirmation alert" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "notifEventReminder", SettingValue = "true", Category = "Notifications", Description = "Event reminder alert" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "notifSupportTicket", SettingValue = "false", Category = "Notifications", Description = "Support ticket updates alert" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "qrReceiverName", SettingValue = "Daniel A", Category = "PaymentQr", Description = "Payment QR Receiver Name" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "qrUpiId", SettingValue = "danielrobertanto604@okicici", Category = "PaymentQr", Description = "Payment UPI ID" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "qrImage", SettingValue = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=upi://pay?pa=danielrobertanto604@okicici&pn=Daniel%20A", Category = "PaymentQr", Description = "QR Image Source" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "enableAuditLogs", SettingValue = "true", Category = "Audit", Description = "Enable audit trail recording" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "logUserLogin", SettingValue = "true", Category = "Audit", Description = "Record user login activities" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "logDataChanges", SettingValue = "true", Category = "Audit", Description = "Record data mutations" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "logConfigChanges", SettingValue = "true", Category = "Audit", Description = "Record configuration modifications" },
                new() { SettingId = Guid.NewGuid(), SettingKey = "retentionPeriod", SettingValue = "365", Category = "Audit", Description = "Audit logs retention in days" }
            };
            await _context.SystemSettings.AddRangeAsync(initialSettings, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Seed initial Payment Transactions if empty
        if (!await _context.PaymentTransactions.AnyAsync(cancellationToken))
        {
            var initialTransactions = new List<PaymentTransaction>
            {
                new() { TransactionId = Guid.NewGuid(), TxnNumber = "TXN001256", MemberName = "Rahul Sharma", EventName = "Team Dinner", Amount = 1000, PaymentDate = DateTime.UtcNow.AddDays(-9), PaymentMode = "GPay", Utr = "UPI1234567890", Status = "Verified", VerifiedBy = null, VerifiedOn = DateTime.UtcNow.AddDays(-9), Notes = "Payment verified. Amount received in bank account.", Screenshot = "payment_screenshot.jpg (320 KB)", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { TransactionId = Guid.NewGuid(), TxnNumber = "TXN001255", MemberName = "Priya Mehta", EventName = "Diwali Celebration", Amount = 2500, PaymentDate = DateTime.UtcNow.AddDays(-11), PaymentMode = "PhonePe", Utr = "PPE9876543210", Status = "Pending", VerifiedBy = null, Notes = "Awaiting statement reconciliation.", Screenshot = "priya_phonepe_receipt.png (280 KB)", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { TransactionId = Guid.NewGuid(), TxnNumber = "TXN001254", MemberName = "Amit Patel", EventName = "Annual Sports", Amount = 1200, PaymentDate = DateTime.UtcNow.AddDays(-13), PaymentMode = "Paytm", Utr = "PAYTM56473829", Status = "Verified", VerifiedBy = "Neha S.", VerifiedOn = DateTime.UtcNow.AddDays(-13), Notes = "Verified against Paytm merchant dashboard.", Screenshot = "paytm_txn_receipt.jpg (190 KB)", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { TransactionId = Guid.NewGuid(), TxnNumber = "TXN001253", MemberName = "Sneha Iyer", EventName = "Team Dinner", Amount = 500, PaymentDate = DateTime.UtcNow.AddDays(-15), PaymentMode = "UPI", Utr = "UPI9988776655", Status = "Failed", VerifiedBy = null, Notes = "Transaction failed at member's bank end.", Screenshot = "failed_txn.png (210 KB)", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { TransactionId = Guid.NewGuid(), TxnNumber = "TXN001252", MemberName = "Vikram Rao", EventName = "Michael Farewell", Amount = 3000, PaymentDate = DateTime.UtcNow.AddDays(-17), PaymentMode = "GPay", Utr = "UPI7766554433", Status = "Verified", VerifiedBy = null, VerifiedOn = DateTime.UtcNow.AddDays(-17), Notes = "Full contribution received.", Screenshot = "gpay_proof.jpg (420 KB)", CreatedBy = null, CreatedAt = DateTime.UtcNow }
            };
            await _context.PaymentTransactions.AddRangeAsync(initialTransactions, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Seed initial Gallery Photos if empty
        if (!await _context.GalleryPhotos.AnyAsync(cancellationToken))
        {
            var initialPhotos = new List<GalleryPhoto>
            {
                new() { PhotoId = Guid.NewGuid(), Title = "Birthday Cake", EventName = "Birthday - Albin Antony", Category = "Cake", ImageUrl = "https://images.unsplash.com/photo-1578985545062-69928b1d9587?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Birthday celebration cake", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Gifts & Wishes", EventName = "Birthday - Albin Antony", Category = "Gifts", ImageUrl = "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Team gifts and wishes", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Snacks & Puffs", EventName = "Birthday - Albin Antony", Category = "Food", ImageUrl = "https://images.unsplash.com/photo-1621996346565-e3d5d6281005?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Evening refreshment snacks", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Decoration", EventName = "Birthday - Albin Antony", Category = "Decoration", ImageUrl = "https://images.unsplash.com/photo-1530103862676-de8c9debad1d?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Room balloons and ribbons", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Team Photo", EventName = "Birthday - Albin Antony", Category = "Team", ImageUrl = "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Group celebration picture", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Celebration Moment", EventName = "Birthday - Albin Antony", Category = "Moments", ImageUrl = "https://images.unsplash.com/photo-1511556532299-8f662fc26c06?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Cake cutting ceremony", CreatedBy = null, CreatedAt = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Event Card", EventName = "Birthday - Albin Antony", Category = "Decoration", ImageUrl = "https://images.unsplash.com/photo-1513151233558-d860c5398176?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Customized invitation greeting", CreatedBy = null, CreatedAt = DateTime.UtcNow }
            };
            await _context.GalleryPhotos.AddRangeAsync(initialPhotos, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Self-healing: Ensure existing non-birthday events have equal share contributions for pending members
        var nonBirthdayEvents = await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Contributions)
            .Where(e => !e.IsDeleted && e.EventType != null && !e.EventType.EventTypeName.ToLower().Contains("birthday"))
            .ToListAsync(cancellationToken);

        bool updatedAny = false;
        foreach (var evt in nonBirthdayEvents)
        {
            var activeContributions = evt.Contributions.Where(c => !c.IsDeleted).ToList();
            var pendingContributions = activeContributions.Where(c => c.PaymentStatus == PaymentStatus.Pending).ToList();
            if (activeContributions.Count > 0 && pendingContributions.Count == activeContributions.Count && evt.BaseAmount > 0)
            {
                decimal equalShare = Math.Round(evt.BaseAmount / activeContributions.Count, 2);
                bool hasUnequal = activeContributions.Any(c => c.Amount != equalShare);
                if (hasUnequal)
                {
                    foreach (var c in activeContributions)
                    {
                        c.Amount = equalShare;
                    }
                    updatedAny = true;
                }
            }
        }
        if (updatedAny)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Clean up any remaining auto-generated "Birthday Celebration - *" events
        var autoGeneratedBirthdayEvents = await _context.Events
            .Include(e => e.Contributions)
            .Include(e => e.Participants)
            .Where(e => e.Description.Contains("Auto-generated birthday contribution event") || e.EventName.StartsWith("Birthday Celebration - "))
            .ToListAsync(cancellationToken);

        if (autoGeneratedBirthdayEvents.Any())
        {
            _context.Events.RemoveRange(autoGeneratedBirthdayEvents);
            await _context.SaveChangesAsync(cancellationToken);
        }

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

    }
}
