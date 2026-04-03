using System.Net;
using System.Net.Mail;

namespace IronvaleFleetHub.Api.Services;

public sealed class DevSmtpEmailChannel : IEmailChannel
{
    private readonly DevNotificationDeliveryOptions _options;
    private readonly ILogger<DevSmtpEmailChannel> _logger;

    public DevSmtpEmailChannel(DevNotificationDeliveryOptions options, ILogger<DevSmtpEmailChannel> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<DeliveryAttemptResult> SendAsync(EmailDeliveryRequest request, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient(_options.SmtpHost ?? "localhost", _options.SmtpPort)
            {
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("noreply@ironvale.local", "Ironvale Fleet Hub"),
                Subject = request.Subject,
                Body = request.HtmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(request.To);

            if (!string.IsNullOrEmpty(request.TextBody))
            {
                mailMessage.AlternateViews.Add(
                    AlternateView.CreateAlternateViewFromString(request.TextBody, null, "text/plain"));
            }

            await client.SendMailAsync(mailMessage, ct);

            _logger.LogInformation("Dev email sent via SMTP to {Recipient}: {Subject}", request.To, request.Subject);
            return new DeliveryAttemptResult(true, "email", "smtp");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dev SMTP email delivery failed for {Recipient}", request.To);
            return new DeliveryAttemptResult(false, "email", "smtp", ex.Message);
        }
    }
}
