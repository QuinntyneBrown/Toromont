namespace IronvaleFleetHub.Api.Services;

public sealed class FileDropEmailChannel : IEmailChannel
{
    private readonly DevNotificationDeliveryOptions _options;
    private readonly ILogger<FileDropEmailChannel> _logger;

    public FileDropEmailChannel(DevNotificationDeliveryOptions options, ILogger<FileDropEmailChannel> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<DeliveryAttemptResult> SendAsync(EmailDeliveryRequest request, CancellationToken ct = default)
    {
        try
        {
            var folder = _options.EmailDropFolder ?? "artifacts/dev-emails";
            Directory.CreateDirectory(folder);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            var safeSubject = string.Join("-", request.Subject.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{timestamp}-{safeSubject}.eml";
            var filePath = Path.Combine(folder, fileName);

            var emlContent = $"""
                From: noreply@ironvale.local
                To: {request.To}
                Subject: {request.Subject}
                Date: {DateTime.UtcNow:R}
                MIME-Version: 1.0
                Content-Type: text/html; charset=utf-8

                {request.HtmlBody}
                """;

            await File.WriteAllTextAsync(filePath, emlContent, ct);

            _logger.LogInformation("Dev email written to file: {FilePath}", filePath);
            return new DeliveryAttemptResult(true, "email", "file-drop");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dev file-drop email delivery failed for {Recipient}", request.To);
            return new DeliveryAttemptResult(false, "email", "file-drop", ex.Message);
        }
    }
}
