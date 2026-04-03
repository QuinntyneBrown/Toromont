namespace IronvaleFleetHub.Api.Services;

public interface IEmailChannel
{
    Task<DeliveryAttemptResult> SendAsync(EmailDeliveryRequest request, CancellationToken ct = default);
}

public interface ISmsChannel
{
    Task<DeliveryAttemptResult> SendAsync(SmsDeliveryRequest request, CancellationToken ct = default);
}

public sealed record EmailDeliveryRequest(
    string To,
    string Subject,
    string HtmlBody,
    string TextBody);

public sealed record SmsDeliveryRequest(
    string To,
    string Message);

public sealed record DeliveryAttemptResult(
    bool Success,
    string Channel,
    string Target,
    string? Error = null);
