// ============================================================================
// Dependency Injection and EF Core DbContext Registration Guide / Snippets
// ============================================================================

/*
1. In your DbContext (e.g. ApplicationDbContext.cs):
---------------------------------------------------
using TeamContributionManagementSystem.Domain.Entities;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<PaymentQRSettings> PaymentQRSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PaymentQRSettings>(entity =>
        {
            entity.ToTable("PaymentQRSettings");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EventType).IsUnique();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ReceiverName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UPIId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.QRCodeMode).IsRequired().HasMaxLength(50).HasDefaultValue("generated");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });
    }
}

2. In Program.cs (or DependencyInjection.cs / ServiceRegistration.cs):
---------------------------------------------------------------------
using TeamContributionManagementSystem.Application.Interfaces;
using TeamContributionManagementSystem.Infrastructure.Repositories;

// Register Repository containing the data isolation & upsert logic:
builder.Services.AddScoped<IPaymentQRSettingsRepository, PaymentQRSettingsRepository>();
*/
