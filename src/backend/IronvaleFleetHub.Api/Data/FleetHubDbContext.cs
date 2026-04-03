using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Models;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Data;

public partial class FleetHubDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public FleetHubDbContext(DbContextOptions<FleetHubDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Returns the current organization ID from the tenant context, or Guid.Empty when unresolved.
    /// EF Core captures this property reference in query filter expressions so that
    /// each request evaluates the filter against the current scoped tenant.
    /// </summary>
    public Guid CurrentOrganizationId => _tenantContext?.OrganizationId ?? Guid.Empty;

    /// <summary>
    /// True when a tenant has been resolved for the current request.
    /// When false, all tenant-filtered queries return zero rows (fail-closed).
    /// </summary>
    public bool TenantResolved => CurrentOrganizationId != Guid.Empty;

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderHistory> WorkOrderHistories => Set<WorkOrderHistory>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<PartsOrder> PartsOrders => Set<PartsOrder>();
    public DbSet<OrderLineItem> OrderLineItems => Set<OrderLineItem>();
    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AIPrediction> AIPredictions => Set<AIPrediction>();
    public DbSet<AnomalyDetection> AnomalyDetections => Set<AnomalyDetection>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<EquipmentModelThreshold> EquipmentModelThresholds => Set<EquipmentModelThreshold>();
    public DbSet<AlertThreshold> AlertThresholds => Set<AlertThreshold>();
    public DbSet<TelemetryDeadLetterEntry> TelemetryDeadLetterEntries => Set<TelemetryDeadLetterEntry>();
    public DbSet<UserOrganization> UserOrganizations => Set<UserOrganization>();
    public DbSet<OrganizationPricing> OrganizationPricings => Set<OrganizationPricing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Organization ---
        modelBuilder.Entity<Organization>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContactEmail).HasMaxLength(200);
        });

        // --- User ---
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EntraObjectId).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Role).HasMaxLength(50);
            e.HasIndex(x => x.EntraObjectId);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- UserInvitation ---
        modelBuilder.Entity<UserInvitation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.Role).HasMaxLength(50);
            e.Property(x => x.Token).HasMaxLength(500);
            e.HasIndex(x => x.Token).IsUnique();
        });

        // --- Equipment ---
        modelBuilder.Entity<Equipment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Make).HasMaxLength(100);
            e.Property(x => x.Model).HasMaxLength(100);
            e.Property(x => x.SerialNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Category).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Location).HasMaxLength(500);
            e.Property(x => x.GpsDeviceId).HasMaxLength(100);
            e.HasIndex(x => new { x.OrganizationId, x.SerialNumber }).IsUnique();
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- WorkOrder ---
        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.WorkOrderNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.ServiceType).HasMaxLength(50);
            e.Property(x => x.Priority).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.EquipmentId);
            e.HasIndex(x => x.AssignedToUserId);
            e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedTo).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- WorkOrderHistory ---
        modelBuilder.Entity<WorkOrderHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PreviousStatus).HasMaxLength(20);
            e.Property(x => x.NewStatus).HasMaxLength(20);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasOne(x => x.WorkOrder).WithMany(w => w.History).HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ChangedBy).WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- Part ---
        modelBuilder.Entity<Part>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PartNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Availability).HasMaxLength(20);
            e.Property(x => x.CompatibleModels).HasMaxLength(500);
            e.HasIndex(x => x.PartNumber).IsUnique();
        });

        // --- CartItem ---
        modelBuilder.Entity<CartItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.UserId);
        });

        // --- PartsOrder ---
        modelBuilder.Entity<PartsOrder>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- OrderLineItem ---
        modelBuilder.Entity<OrderLineItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PartNumber).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.HasOne(x => x.Order).WithMany(o => o.LineItems).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        // --- TelemetryEvent ---
        modelBuilder.Entity<TelemetryEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(50);
            e.HasIndex(x => new { x.EquipmentId, x.Timestamp });
            e.HasIndex(x => x.OrganizationId);
        });

        // --- Alert ---
        modelBuilder.Entity<Alert>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AlertType).HasMaxLength(100);
            e.Property(x => x.Severity).HasMaxLength(20);
            e.Property(x => x.Message).HasMaxLength(1000);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasIndex(x => new { x.OrganizationId, x.Status, x.Severity });
            e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- AIPrediction ---
        modelBuilder.Entity<AIPrediction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Component).HasMaxLength(200);
            e.Property(x => x.RecommendedAction).HasMaxLength(1000);
            e.Property(x => x.Timeframe).HasMaxLength(100);
            e.Property(x => x.Priority).HasMaxLength(20);
            e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- AnomalyDetection ---
        modelBuilder.Entity<AnomalyDetection>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AnomalyType).HasMaxLength(100);
            e.Property(x => x.MetricName).HasMaxLength(100);
            e.Property(x => x.Severity).HasMaxLength(20);
            e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- Notification ---
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(100);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Message).HasMaxLength(2000);
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        });

        // --- NotificationPreference ---
        modelBuilder.Entity<NotificationPreference>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.NotificationType).HasMaxLength(100);
            e.HasIndex(x => x.UserId);
        });

        // --- EquipmentModelThreshold ---
        modelBuilder.Entity<EquipmentModelThreshold>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EquipmentModel).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.EquipmentModel).IsUnique();
        });

        // --- UserOrganization ---
        modelBuilder.Entity<UserOrganization>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.OrganizationId }).IsUnique();
        });

        // --- OrganizationPricing ---
        modelBuilder.Entity<OrganizationPricing>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.HasIndex(x => new { x.OrganizationId, x.PartId }).IsUnique();
        });

        // --- AlertThreshold ---
        modelBuilder.Entity<AlertThreshold>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MetricName).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.OrganizationId);
        });

        // --- TelemetryDeadLetterEntry ---
        modelBuilder.Entity<TelemetryDeadLetterEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OriginalPayload).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(2000).IsRequired();
        });

        // --- Global query filters for multi-tenancy ---
        if (_tenantContext != null && _tenantContext.OrganizationId != Guid.Empty)
        {
            var orgId = _tenantContext.OrganizationId;

            modelBuilder.Entity<Equipment>().HasQueryFilter(x => x.OrganizationId == orgId);
            modelBuilder.Entity<WorkOrder>().HasQueryFilter(x => x.OrganizationId == orgId);
            modelBuilder.Entity<Alert>().HasQueryFilter(x => x.OrganizationId == orgId);
            modelBuilder.Entity<AIPrediction>().HasQueryFilter(x => x.OrganizationId == orgId);
            modelBuilder.Entity<AnomalyDetection>().HasQueryFilter(x => x.OrganizationId == orgId);
            modelBuilder.Entity<Notification>().HasQueryFilter(x => x.OrganizationId == orgId);
            modelBuilder.Entity<PartsOrder>().HasQueryFilter(x => x.OrganizationId == orgId);
            modelBuilder.Entity<User>().HasQueryFilter(x => x.OrganizationId == orgId);
            modelBuilder.Entity<UserInvitation>().HasQueryFilter(x => x.OrganizationId == orgId);
            modelBuilder.Entity<NotificationPreference>().HasQueryFilter(x =>
                Users.Any(u => u.Id == x.UserId && u.OrganizationId == orgId));
            modelBuilder.Entity<CartItem>().HasQueryFilter(x =>
                Users.Any(u => u.Id == x.UserId && u.OrganizationId == orgId));
            modelBuilder.Entity<TelemetryEvent>().HasQueryFilter(x => x.OrganizationId == orgId);
            modelBuilder.Entity<AlertThreshold>().HasQueryFilter(x => x.OrganizationId == orgId);
        }
    }
}
