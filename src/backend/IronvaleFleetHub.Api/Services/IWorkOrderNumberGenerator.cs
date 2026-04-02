namespace IronvaleFleetHub.Api.Services;

public interface IWorkOrderNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}
