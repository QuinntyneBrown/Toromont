namespace IronvaleFleetHub.Api.IntegrationTests.Infrastructure;

internal static class TestSeedData
{
    public static readonly Guid Org1Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    public static readonly Guid AdminUserId = Guid.Parse("b1b2c3d4-0001-0000-0000-000000000001");
    public static readonly Guid TechnicianUserId = Guid.Parse("b1b2c3d4-0003-0000-0000-000000000003");
    public static readonly Guid Org1EquipmentId = Guid.Parse("c1b2c3d4-0001-0000-0000-000000000001");
    public static readonly Guid Org2EquipmentId = Guid.Parse("c1b2c3d4-0007-0000-0000-000000000001");

    public const string AdminObjectId = "dev-admin-001";
}
