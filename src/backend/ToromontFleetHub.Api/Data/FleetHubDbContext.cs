using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Models;

namespace ToromontFleetHub.Api.Data;

public class FleetHubDbContext : DbContext
{
    public FleetHubDbContext(DbContextOptions<FleetHubDbContext> options) : base(options) { }

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
    public DbSet<OrganizationPricing> OrganizationPricings => Set<OrganizationPricing>();
}
