namespace IronvaleFleetHub.Api.Services;

public sealed class CompositeEmailChannel : IEmailChannel
{
    private readonly DevSmtpEmailChannel _smtp;
    private readonly FileDropEmailChannel _fileDrop;
    private readonly ILogger<CompositeEmailChannel> _logger;

    public CompositeEmailChannel(
        DevSmtpEmailChannel smtp,
        FileDropEmailChannel fileDrop,
        ILogger<CompositeEmailChannel> logger)
    {
        _smtp = smtp;
        _fileDrop = fileDrop;
        _logger = logger;
    }

    public async Task<DeliveryAttemptResult> SendAsync(EmailDeliveryRequest request, CancellationToken ct = default)
    {
        var smtpResult = await _smtp.SendAsync(request, ct);
        if (smtpResult.Success)
            return smtpResult;

        _logger.LogInformation("SMTP failed, falling back to file-drop for {Recipient}", request.To);
        return await _fileDrop.SendAsync(request, ct);
    }
}
