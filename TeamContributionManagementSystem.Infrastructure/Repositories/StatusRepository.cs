using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.DTOs.Statuses;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class StatusRepository : IStatusRepository
{
    private readonly ApplicationDbContext _context;
    private static bool _migrationEnsured = false;
    private static readonly SemaphoreSlim _columnLock = new(1, 1);

    public StatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task EnsureCommonStatusesMigrationAsync(CancellationToken cancellationToken = default)
    {
        if (_migrationEnsured) return;
        await _columnLock.WaitAsync(cancellationToken);
        try
        {
            if (_migrationEnsured) return;
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    @"DO $$
                      BEGIN
                          -- 1. Deduplicate & map FK references to canonical status_id
                          WITH canonical AS (
                              SELECT LOWER(TRIM(status_name)) as clean_name, MIN(status_id) as canonical_id
                              FROM statuses
                              WHERE is_deleted = false
                              GROUP BY LOWER(TRIM(status_name))
                          ),
                          duplicates AS (
                              SELECT s.status_id, c.canonical_id
                              FROM statuses s
                              JOIN canonical c ON LOWER(TRIM(s.status_name)) = c.clean_name
                              WHERE s.status_id <> c.canonical_id
                          )
                          UPDATE contributions cont
                          SET status_id = d.canonical_id
                          FROM duplicates d
                          WHERE cont.status_id = d.status_id;

                          WITH canonical AS (
                              SELECT LOWER(TRIM(status_name)) as clean_name, MIN(status_id) as canonical_id
                              FROM statuses
                              WHERE is_deleted = false
                              GROUP BY LOWER(TRIM(status_name))
                          ),
                          duplicates AS (
                              SELECT s.status_id, c.canonical_id
                              FROM statuses s
                              JOIN canonical c ON LOWER(TRIM(s.status_name)) = c.clean_name
                              WHERE s.status_id <> c.canonical_id
                          )
                          UPDATE payment_transactions pt
                          SET status_id = d.canonical_id
                          FROM duplicates d
                          WHERE pt.status_id = d.status_id;

                          WITH canonical AS (
                              SELECT LOWER(TRIM(status_name)) as clean_name, MIN(status_id) as canonical_id
                              FROM statuses
                              WHERE is_deleted = false
                              GROUP BY LOWER(TRIM(status_name))
                          ),
                          duplicates AS (
                              SELECT s.status_id, c.canonical_id
                              FROM statuses s
                              JOIN canonical c ON LOWER(TRIM(s.status_name)) = c.clean_name
                              WHERE s.status_id <> c.canonical_id
                          )
                          UPDATE support_tickets st
                          SET status_id = d.canonical_id
                          FROM duplicates d
                          WHERE st.status_id = d.status_id;

                          -- 2. Delete duplicate status rows
                          WITH canonical AS (
                              SELECT LOWER(TRIM(status_name)) as clean_name, MIN(status_id) as canonical_id
                              FROM statuses
                              WHERE is_deleted = false
                              GROUP BY LOWER(TRIM(status_name))
                          )
                          DELETE FROM statuses s
                          USING canonical c
                          WHERE LOWER(TRIM(s.status_name)) = c.clean_name AND s.status_id <> c.canonical_id;

                          -- 3. Insert default common statuses if missing
                          INSERT INTO statuses (status_id, status_name, is_active, is_deleted, created_at, created_on)
                          SELECT gen_random_uuid(), 'Paid', true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                          WHERE NOT EXISTS (SELECT 1 FROM statuses WHERE LOWER(TRIM(status_name)) = 'paid' AND is_deleted = false);

                          INSERT INTO statuses (status_id, status_name, is_active, is_deleted, created_at, created_on)
                          SELECT gen_random_uuid(), 'Pending', true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                          WHERE NOT EXISTS (SELECT 1 FROM statuses WHERE LOWER(TRIM(status_name)) = 'pending' AND is_deleted = false);

                          INSERT INTO statuses (status_id, status_name, is_active, is_deleted, created_at, created_on)
                          SELECT gen_random_uuid(), 'Verified', true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                          WHERE NOT EXISTS (SELECT 1 FROM statuses WHERE LOWER(TRIM(status_name)) = 'verified' AND is_deleted = false);

                          INSERT INTO statuses (status_id, status_name, is_active, is_deleted, created_at, created_on)
                          SELECT gen_random_uuid(), 'Closed', true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                          WHERE NOT EXISTS (SELECT 1 FROM statuses WHERE LOWER(TRIM(status_name)) = 'closed' AND is_deleted = false);

                          INSERT INTO statuses (status_id, status_name, is_active, is_deleted, created_at, created_on)
                          SELECT gen_random_uuid(), 'Active', true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                          WHERE NOT EXISTS (SELECT 1 FROM statuses WHERE LOWER(TRIM(status_name)) = 'active' AND is_deleted = false);

                          INSERT INTO statuses (status_id, status_name, is_active, is_deleted, created_at, created_on)
                          SELECT gen_random_uuid(), 'Inactive', true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                          WHERE NOT EXISTS (SELECT 1 FROM statuses WHERE LOWER(TRIM(status_name)) = 'inactive' AND is_deleted = false);

                          INSERT INTO statuses (status_id, status_name, is_active, is_deleted, created_at, created_on)
                          SELECT gen_random_uuid(), 'Approved', true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                          WHERE NOT EXISTS (SELECT 1 FROM statuses WHERE LOWER(TRIM(status_name)) = 'approved' AND is_deleted = false);

                          INSERT INTO statuses (status_id, status_name, is_active, is_deleted, created_at, created_on)
                          SELECT gen_random_uuid(), 'Rejected', true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                          WHERE NOT EXISTS (SELECT 1 FROM statuses WHERE LOWER(TRIM(status_name)) = 'rejected' AND is_deleted = false);

                          -- 4. Drop legacy module column and constraints if present
                          ALTER TABLE IF EXISTS statuses DROP CONSTRAINT IF EXISTS statuses_status_name_key;
                          ALTER TABLE IF EXISTS statuses DROP CONSTRAINT IF EXISTS statuses_status_name_unique;
                          ALTER TABLE IF EXISTS statuses DROP CONSTRAINT IF EXISTS uq_statuses_status_name;
                          ALTER TABLE IF EXISTS statuses DROP CONSTRAINT IF EXISTS uq_statuses_status_name_module;
                          ALTER TABLE IF EXISTS statuses DROP COLUMN IF EXISTS module;

                      END $$;",
                    cancellationToken);
            }
            catch
            {
                // ignore if already executed or during concurrent operations
            }
            _migrationEnsured = true;
        }
        finally
        {
            _columnLock.Release();
        }
    }

    public async Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        await EnsureCommonStatusesMigrationAsync(cancellationToken);

        var query = _context.Statuses
            .Where(x => !x.IsDeleted)
            .AsNoTracking();

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        var usedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var ticketStatuses = await _context.SupportTickets
            .Where(x => !x.IsDeleted && x.StatusItem != null)
            .Select(x => x.StatusItem!.StatusName.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in ticketStatuses) usedStatuses.Add(s);

        var expenseStatuses = await _context.Expenses
            .Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.Status))
            .Select(x => x.Status.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in expenseStatuses) usedStatuses.Add(s);

        var txnStatuses = await _context.PaymentTransactions
            .Where(x => !x.IsDeleted && x.StatusItem != null)
            .Select(x => x.StatusItem!.StatusName.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in txnStatuses) usedStatuses.Add(s);

        var eventStatuses = await _context.Events
            .Where(x => !x.IsDeleted)
            .Select(x => x.Status.ToString().ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in eventStatuses) usedStatuses.Add(s);

        var contributionStatuses = await _context.Contributions
            .Where(x => !x.IsDeleted && x.StatusItem != null)
            .Select(x => x.StatusItem!.StatusName.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in contributionStatuses) usedStatuses.Add(s);

        var users = await _context.Users
            .AsNoTracking()
            .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
            .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

        var items = await query
            .OrderBy(x => x.StatusName)
            .Select(x => new StatusDto
            {
                StatusId = x.StatusId,
                StatusName = x.StatusName,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                CreatedAt = x.CreatedAt,
                CreatedOn = x.CreatedOn,
                ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                ModifiedOn = x.ModifiedOn
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            var cleanName = item.StatusName.Trim().ToLower();
            item.IsReferred = usedStatuses.Contains(cleanName);

            if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
            {
                item.CreatedBy = cName;
            }
            if (!string.IsNullOrWhiteSpace(item.ModifiedBy) && Guid.TryParse(item.ModifiedBy, out var mGuid) && users.TryGetValue(mGuid, out var mName))
            {
                item.ModifiedBy = mName;
            }
        }

        return items;
    }

    public Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly, string? module, CancellationToken cancellationToken)
    {
        return GetAllAsync(activeOnly, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetAllModuleAsync(CancellationToken cancellationToken = default)
    {
        return new List<string>();
    }

    public async Task<IReadOnlyCollection<string>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllModuleAsync(cancellationToken);
    }

    public async Task<Status?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureCommonStatusesMigrationAsync(cancellationToken);
        return await _context.Statuses
            .FirstOrDefaultAsync(x => x.StatusId == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Status?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await EnsureCommonStatusesMigrationAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(name)) return null;

        return await _context.Statuses
            .FirstOrDefaultAsync(x => x.StatusName.ToLower() == name.Trim().ToLower() && !x.IsDeleted, cancellationToken);
    }

    public async Task<Status?> GetByNameAndModuleAsync(string name, string? module, CancellationToken cancellationToken = default)
    {
        return await GetByNameAsync(name, cancellationToken);
    }

    public async Task<bool> IsInUseAsync(string statusName, CancellationToken cancellationToken = default)
    {
        await EnsureCommonStatusesMigrationAsync(cancellationToken);
        var cleanName = statusName.Trim().ToLower();

        // Support tickets
        var inSupport = await _context.SupportTickets.AnyAsync(x => !x.IsDeleted && x.StatusItem != null && x.StatusItem.StatusName.ToLower() == cleanName, cancellationToken);
        if (inSupport) return true;

        // Expenses
        var inExpenses = await _context.Expenses.AnyAsync(x => !x.IsDeleted && x.Status.ToLower() == cleanName, cancellationToken);
        if (inExpenses) return true;

        // Payment Transactions
        var inTxns = await _context.PaymentTransactions.AnyAsync(x => !x.IsDeleted && x.StatusItem != null && x.StatusItem.StatusName.ToLower() == cleanName, cancellationToken);
        if (inTxns) return true;

        // Events
        if (Enum.TryParse<TeamContributionManagementSystem.Domain.Enums.EventStatus>(cleanName, true, out var eventStatus))
        {
            var inEvents = await _context.Events.AnyAsync(x => !x.IsDeleted && x.Status == eventStatus, cancellationToken);
            if (inEvents) return true;
        }

        // Contributions
        var inContributions = await _context.Contributions.AnyAsync(x => !x.IsDeleted && x.StatusItem != null && x.StatusItem.StatusName.ToLower() == cleanName, cancellationToken);
        if (inContributions) return true;

        return false;
    }

    public async Task AddAsync(Status status, CancellationToken cancellationToken = default)
    {
        await EnsureCommonStatusesMigrationAsync(cancellationToken);
        await _context.Statuses.AddAsync(status, cancellationToken);
    }

    public void Update(Status status)
    {
        _context.Statuses.Update(status);
    }

    public void Delete(Status status)
    {
        status.IsDeleted = true;
        _context.Statuses.Update(status);
    }
}
