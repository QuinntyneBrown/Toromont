namespace ToromontFleetHub.Api.Services;

public interface INotificationDispatchService
{
    Task DispatchAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? entityType = null,
        Guid? entityId = null,
        CancellationToken ct = default);
}
