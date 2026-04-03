using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Data;

public partial class FleetHubDbContext
{
    public DbSet<AiScenarioRecord> AiScenarioRecords => Set<AiScenarioRecord>();
    public DbSet<WorkflowRunRecord> WorkflowRunRecords => Set<WorkflowRunRecord>();
    public DbSet<WorkflowDispatchRecord> WorkflowDispatchRecords => Set<WorkflowDispatchRecord>();
    public DbSet<DevPartsOrderEvent> DevPartsOrderEvents => Set<DevPartsOrderEvent>();
}
