using System.Collections.Concurrent;

namespace IronvaleFleetHub.Api.Services;

public class InMemoryUserBlacklist : IUserBlacklist
{
    private readonly ConcurrentDictionary<Guid, DateTime> _blacklist = new();

    public void Add(Guid userId) =>
        _blacklist[userId] = DateTime.UtcNow;

    public void Remove(Guid userId) =>
        _blacklist.TryRemove(userId, out _);

    public bool IsBlacklisted(Guid userId) =>
        _blacklist.TryGetValue(userId, out var addedAt)
        && DateTime.UtcNow - addedAt < TimeSpan.FromMinutes(30);
}
