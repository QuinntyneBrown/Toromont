namespace IronvaleFleetHub.Api.Services;

public interface IUserBlacklist
{
    void Add(Guid userId);
    void Remove(Guid userId);
    bool IsBlacklisted(Guid userId);
}
