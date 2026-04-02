using System.Collections.Concurrent;

namespace IronvaleFleetHub.Api.Services;

public class InMemoryUserBlacklist : IUserBlacklist
{
    private readonly ConcurrentDictionary<Guid, DateTime> _blacklist = new();

    public void Add(Guid userId) =>
        _blacklist[userId] = DateTime.UtcNow;

    public void Remove(Guid userId) =>
        _blacklist.TryRemove(userId, out _);

    public bool IsBlacklisted(Guid userId)
    {
        if (!_blacklist.TryGetValue(userId, out var addedAt))
            return false;

        if (DateTime.UtcNow - addedAt < TimeSpan.FromMinutes(30))
            return true;

        // Entry has expired — remove it to prevent unbounded growth
        _blacklist.TryRemove(userId, out _);
        return false;
    }
}
