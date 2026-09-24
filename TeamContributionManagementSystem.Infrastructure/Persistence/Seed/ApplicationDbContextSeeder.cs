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

        // Clean up previously seeded dummy expenses if any exist
        var dummyExpenses = await _context.Expenses
            .Where(x => x.CreatedBy == "System" && (x.EventName == "Team Dinner" || x.EventName == "Michael Farewell" || x.EventName == "Priya Birthday"))
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
                new() { TicketId = Guid.NewGuid(), TicketNo = "TKT-2026-001", MemberName = "Rahul Sharma", MemberId = "MEM-001", RelatedEvent = "Team Dinner", TicketType = "Payment Issue", Subject = "Payment not reflected in my account", Description = "I have made the payment for Team Dinner on 24 Sep 2026, but it is not yet reflected in my account. Please check and confirm.", Status = "Open", Priority = "High", AssignedTo = "Admin", RefNo = "REF-20260924-001", Utr = "HDFC1234567890", Attachment = "payment_screenshot.jpg (245 KB)", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { TicketId = Guid.NewGuid(), TicketNo = "TKT-2026-002", MemberName = "Sneha Iyer", MemberId = "MEM-002", RelatedEvent = "Michael Farewell", TicketType = "Event Clarification", Subject = "Event venue details", Description = "Could you please share the exact hall location and parking instructions for the farewell event?", Status = "In Progress", Priority = "Medium", AssignedTo = "Priya N.", RefNo = "REF-20260923-002", CreatedBy = "System", CreatedOn = DateTime.UtcNow.AddDays(-1) },
                new() { TicketId = Guid.NewGuid(), TicketNo = "TKT-2026-003", MemberName = "Amit Patel", MemberId = "MEM-003", RelatedEvent = "Priya Birthday", TicketType = "Application Issue", Subject = "Cannot access event page", Description = "Getting an error when attempting to access the birthday event registry tab.", Status = "Resolved", Priority = "Low", AssignedTo = "Rohit S.", RefNo = "REF-20260922-003", ResolutionNotes = "Cache cleared and user right refreshed.", CreatedBy = "System", CreatedOn = DateTime.UtcNow.AddDays(-2) },
                new() { TicketId = Guid.NewGuid(), TicketNo = "TKT-2026-004", MemberName = "Divya Nair", MemberId = "MEM-004", RelatedEvent = "Team Dinner", TicketType = "Payment Issue", Subject = "Wrong amount deducted", Description = "Two payments were initiated by mistake for the same dinner event contribution.", Status = "In Progress", Priority = "High", AssignedTo = "Admin", RefNo = "REF-20260921-004", Utr = "ICIC9876543210", Attachment = "bank_statement.pdf (512 KB)", CreatedBy = "System", CreatedOn = DateTime.UtcNow.AddDays(-3) }
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
                new() { TransactionId = Guid.NewGuid(), TxnNumber = "TXN001256", MemberName = "Rahul Sharma", EventName = "Team Dinner", Amount = 1000, PaymentDate = DateTime.UtcNow.AddDays(-9), PaymentMode = "GPay", Utr = "UPI1234567890", Status = "Verified", VerifiedBy = "Admin", VerifiedOn = DateTime.UtcNow.AddDays(-9), Notes = "Payment verified. Amount received in bank account.", Screenshot = "payment_screenshot.jpg (320 KB)", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { TransactionId = Guid.NewGuid(), TxnNumber = "TXN001255", MemberName = "Priya Mehta", EventName = "Diwali Celebration", Amount = 2500, PaymentDate = DateTime.UtcNow.AddDays(-11), PaymentMode = "PhonePe", Utr = "PPE9876543210", Status = "Pending", VerifiedBy = "-", Notes = "Awaiting statement reconciliation.", Screenshot = "priya_phonepe_receipt.png (280 KB)", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { TransactionId = Guid.NewGuid(), TxnNumber = "TXN001254", MemberName = "Amit Patel", EventName = "Annual Sports", Amount = 1200, PaymentDate = DateTime.UtcNow.AddDays(-13), PaymentMode = "Paytm", Utr = "PAYTM56473829", Status = "Verified", VerifiedBy = "Neha S.", VerifiedOn = DateTime.UtcNow.AddDays(-13), Notes = "Verified against Paytm merchant dashboard.", Screenshot = "paytm_txn_receipt.jpg (190 KB)", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { TransactionId = Guid.NewGuid(), TxnNumber = "TXN001253", MemberName = "Sneha Iyer", EventName = "Team Dinner", Amount = 500, PaymentDate = DateTime.UtcNow.AddDays(-15), PaymentMode = "UPI", Utr = "UPI9988776655", Status = "Failed", VerifiedBy = "-", Notes = "Transaction failed at member's bank end.", Screenshot = "failed_txn.png (210 KB)", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { TransactionId = Guid.NewGuid(), TxnNumber = "TXN001252", MemberName = "Vikram Rao", EventName = "Michael Farewell", Amount = 3000, PaymentDate = DateTime.UtcNow.AddDays(-17), PaymentMode = "GPay", Utr = "UPI7766554433", Status = "Verified", VerifiedBy = "Admin", VerifiedOn = DateTime.UtcNow.AddDays(-17), Notes = "Full contribution received.", Screenshot = "gpay_proof.jpg (420 KB)", CreatedBy = "System", CreatedOn = DateTime.UtcNow }
            };
            await _context.PaymentTransactions.AddRangeAsync(initialTransactions, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Seed initial Gallery Photos if empty
        if (!await _context.GalleryPhotos.AnyAsync(cancellationToken))
        {
            var initialPhotos = new List<GalleryPhoto>
            {
                new() { PhotoId = Guid.NewGuid(), Title = "Birthday Cake", EventName = "Birthday - Albin Antony", Category = "Cake", ImageUrl = "https://images.unsplash.com/photo-1578985545062-69928b1d9587?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Birthday celebration cake", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Gifts & Wishes", EventName = "Birthday - Albin Antony", Category = "Gifts", ImageUrl = "https://images.unsplash.com/photo-1549465220-1a8b9238cd48?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Team gifts and wishes", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Snacks & Puffs", EventName = "Birthday - Albin Antony", Category = "Food", ImageUrl = "https://images.unsplash.com/photo-1621996346565-e3d5d6281005?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Evening refreshment snacks", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Decoration", EventName = "Birthday - Albin Antony", Category = "Decoration", ImageUrl = "https://images.unsplash.com/photo-1530103862676-de8c9debad1d?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Room balloons and ribbons", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Team Photo", EventName = "Birthday - Albin Antony", Category = "Team", ImageUrl = "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Group celebration picture", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Celebration Moment", EventName = "Birthday - Albin Antony", Category = "Moments", ImageUrl = "https://images.unsplash.com/photo-1511556532299-8f662fc26c06?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Cake cutting ceremony", CreatedBy = "System", CreatedOn = DateTime.UtcNow },
                new() { PhotoId = Guid.NewGuid(), Title = "Event Card", EventName = "Birthday - Albin Antony", Category = "Decoration", ImageUrl = "https://images.unsplash.com/photo-1513151233558-d860c5398176?w=600&auto=format&fit=crop&q=80", TakenDate = DateTime.UtcNow.AddDays(24), Description = "Customized invitation greeting", CreatedBy = "System", CreatedOn = DateTime.UtcNow }
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

        // Seed navigation menus — exact copy of original menu sheet
        if (!await _context.NavigationMenus.AnyAsync(cancellationToken))
        {
            var menus = new List<NavigationMenu>
            {
                // Top-level modules: Module filled, SubModule=null, Activity=null
                new() { FeatureID = 1,  MainModuleID = 1, Module = "Dashboard",      ParentID = 0,  SubModule = null,             Activity = null, RoutingUrl = "#",                        ModuleNO = 1, DisplayOrder = 1,  HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 2,  MainModuleID = 2, Module = "Members",        ParentID = 0,  SubModule = null,             Activity = null, RoutingUrl = "#",                        ModuleNO = 2, DisplayOrder = 2,  HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 3,  MainModuleID = 3, Module = "Events",         ParentID = 0,  SubModule = null,             Activity = null, RoutingUrl = "#",                        ModuleNO = 3, DisplayOrder = 3,  HasSubModule = true,  ShowingUserRight = true },
                new() { FeatureID = 7,  MainModuleID = 4, Module = "Finance",        ParentID = 0,  SubModule = null,             Activity = null, RoutingUrl = "#",                        ModuleNO = 4, DisplayOrder = 7,  HasSubModule = true,  ShowingUserRight = true },
                new() { FeatureID = 12, MainModuleID = 5, Module = "Support Ticket", ParentID = 0,  SubModule = null,             Activity = null, RoutingUrl = "#",                        ModuleNO = 5, DisplayOrder = 12, HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 13, MainModuleID = 6, Module = "Tools",          ParentID = 0,  SubModule = null,             Activity = null, RoutingUrl = "#",                        ModuleNO = 6, DisplayOrder = 14, HasSubModule = true,  ShowingUserRight = true },
                new() { FeatureID = 20, MainModuleID = 7, Module = "Reports",        ParentID = 0,  SubModule = null,             Activity = null, RoutingUrl = "#",                        ModuleNO = 7, DisplayOrder = 21, HasSubModule = false, ShowingUserRight = true },

                // Events children: Module=null, SubModule=display label, Activity=null
                new() { FeatureID = 4,  MainModuleID = 3, Module = null,             ParentID = 3,  SubModule = "Event",          Activity = null, RoutingUrl = "/events",                  ModuleNO = 3, DisplayOrder = 4,  HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 5,  MainModuleID = 3, Module = null,             ParentID = 3,  SubModule = "Calendar",       Activity = null, RoutingUrl = "/calendar",                ModuleNO = 3, DisplayOrder = 5,  HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 6,  MainModuleID = 3, Module = null,             ParentID = 3,  SubModule = "Gallery",        Activity = null, RoutingUrl = "/gallery",                 ModuleNO = 3, DisplayOrder = 6,  HasSubModule = false, ShowingUserRight = true },

                // Finance children: Module=null, SubModule=display label, Activity=null
                new() { FeatureID = 8,  MainModuleID = 4, Module = null,             ParentID = 7,  SubModule = "Contribution",   Activity = null, RoutingUrl = "/contributions",           ModuleNO = 4, DisplayOrder = 8,  HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 9,  MainModuleID = 4, Module = null,             ParentID = 7,  SubModule = "Payment History",Activity = null, RoutingUrl = "/payments",                ModuleNO = 4, DisplayOrder = 9,  HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 10, MainModuleID = 4, Module = null,             ParentID = 7,  SubModule = "Calculation",    Activity = null, RoutingUrl = "/contribution-calculation", ModuleNO = 4, DisplayOrder = 10, HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 11, MainModuleID = 4, Module = null,             ParentID = 7,  SubModule = "Expense",        Activity = null, RoutingUrl = "/expense",                 ModuleNO = 4, DisplayOrder = 11, HasSubModule = false, ShowingUserRight = true },

                // Tools children: Module=null, SubModule=display label, Activity=null
                new() { FeatureID = 14, MainModuleID = 6, Module = null,             ParentID = 13, SubModule = "Roles",          Activity = null, RoutingUrl = "/roles",                   ModuleNO = 6, DisplayOrder = 15, HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 15, MainModuleID = 6, Module = null,             ParentID = 13, SubModule = "Event Types",    Activity = null, RoutingUrl = "/event-types",             ModuleNO = 6, DisplayOrder = 16, HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 16, MainModuleID = 6, Module = null,             ParentID = 13, SubModule = "Exit Process",   Activity = null, RoutingUrl = "/exit-process",            ModuleNO = 6, DisplayOrder = 17, HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 17, MainModuleID = 6, Module = null,             ParentID = 13, SubModule = "User Rights",    Activity = null, RoutingUrl = "/user-rights",             ModuleNO = 6, DisplayOrder = 18, HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 18, MainModuleID = 6, Module = null,             ParentID = 13, SubModule = "Users",          Activity = null, RoutingUrl = "/users",                   ModuleNO = 6, DisplayOrder = 19, HasSubModule = false, ShowingUserRight = true },
                new() { FeatureID = 19, MainModuleID = 6, Module = null,             ParentID = 13, SubModule = "Settings",       Activity = null, RoutingUrl = "/settings",                ModuleNO = 6, DisplayOrder = 20, HasSubModule = false, ShowingUserRight = true },
            };
            await _context.NavigationMenus.AddRangeAsync(menus, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }



        // Seeding default Role Rights
        var existingRoleRights = await _context.RoleRights.ToListAsync(cancellationToken);
        var existingKeySet = existingRoleRights
            .Select(x => $"{x.Role}|{x.Module.Trim()}|{x.SubModule.Trim()}|{x.Page.Trim()}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var defaultPages = new[]
        {
            (Module: "Dashboard",      SubModule: "Analytics",  Page: "Dashboard"),
            (Module: "Members",        SubModule: "Directory",  Page: "Members"),
            (Module: "Events",         SubModule: "Registry",   Page: "Event"),
            (Module: "Events",         SubModule: "Calendar",   Page: "Calendar"),
            (Module: "Events",         SubModule: "Media",      Page: "Gallery"),
            (Module: "Finance",        SubModule: "Ledger",     Page: "Contribution"),
            (Module: "Finance",        SubModule: "Payments",   Page: "Payment History"),
            (Module: "Finance",        SubModule: "Calculation",Page: "Calculation"),
            (Module: "Finance",        SubModule: "Expenses",   Page: "Expense"),
            (Module: "Support Ticket", SubModule: "Helpdesk",   Page: "Support Ticket"),
            (Module: "Tools",          SubModule: "Admin",      Page: "Roles"),
            (Module: "Tools",          SubModule: "Categories", Page: "Event Types"),
            (Module: "Tools",          SubModule: "Clearance",  Page: "Exit Process"),
            (Module: "Tools",          SubModule: "Admin",      Page: "User Rights"),
            (Module: "Tools",          SubModule: "Admin",      Page: "Users"),
            (Module: "Tools",          SubModule: "Config",     Page: "Settings"),
            (Module: "Reports",        SubModule: "Analytics",  Page: "Event Audit"),
            (Module: "Reports",        SubModule: "Analytics",  Page: "Member Velocity"),
            (Module: "Reports",        SubModule: "Analytics",  Page: "Pending Dues"),
            (Module: "Reports",        SubModule: "Analytics",  Page: "Member Category Paid"),
        };

        var roleRightsList = new List<RoleRight>();

        foreach (var role in Enum.GetValues<UserRole>())
        {
            foreach (var page in defaultPages)
            {
                var key = $"{role}|{page.Module.Trim()}|{page.SubModule.Trim()}|{page.Page.Trim()}";
                if (existingKeySet.Contains(key))
                {
                    continue;
                }

                string access = "readWrite"; // default for Admin / Manager

                if (role == UserRole.User || role == UserRole.Member)
                {
                    // Deny sensitive Tools pages for non-admin roles
                    if (page.Module == "Tools" &&
                        (page.Page == "Event Types" || page.Page == "Exit Process" ||
                         page.Page == "User Rights" || page.Page == "Users" || page.Page == "Settings"))
                    {
                        access = "deny";
                    }
                    else
                    {
                        access = "readWrite";
                    }
                }

                roleRightsList.Add(new RoleRight
                {
                    RoleRightId = Guid.NewGuid(),
                    Role = role,
                    Module = page.Module,
                    SubModule = page.SubModule,
                    Page = page.Page,
                    Access = access
                });
                existingKeySet.Add(key);
            }
        }

        if (roleRightsList.Count > 0)
        {
            await _context.RoleRights.AddRangeAsync(roleRightsList, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
