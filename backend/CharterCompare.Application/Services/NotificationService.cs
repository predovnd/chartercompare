using CharterCompare.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration? _configuration;

    public NotificationService(ILogger<NotificationService> logger, IEmailService emailService, IConfiguration? configuration = null)
    {
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task NotifyRequestSubmittedAsync(CharterRequestRecord request, string quoteLink, CancellationToken cancellationToken = default)
    {
        var email = GetRequestEmail(request);
        var phone = GetRequestPhone(request);
        
        _logger.LogInformation(
            "Request submitted: {RequestId}. Email: {Email}, Phone: {Phone}. Quote link: {QuoteLink}",
            request.Id, email, phone, quoteLink
        );

        if (!string.IsNullOrEmpty(email))
        {
            var subject = "Your charter bus request has been received";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #3b82f6; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9fafb; padding: 30px; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; background-color: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #6b7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>CharterCompare</h1>
        </div>
        <div class=""content"">
            <h2>Thank you for your request!</h2>
            <p>We've received your charter bus request and will send you quotes from operators within 24 hours.</p>
            <p>You can view your request and quotes anytime using the link below:</p>
            <p style=""text-align: center;"">
                <a href=""{quoteLink}"" class=""button"">View Your Quotes</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #3b82f6;"">{quoteLink}</p>
            <p>We'll notify you as quotes come in, and you'll have 24 hours to receive and compare all available options.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

            var plainBody = $@"Thank you for your request!

We've received your charter bus request and will send you quotes from operators within 24 hours.

View your request and quotes here: {quoteLink}

We'll notify you as quotes come in, and you'll have 24 hours to receive and compare all available options.

This is an automated message. Please do not reply to this email.";

            try
            {
                await _emailService.SendEmailAsync(email, subject, htmlBody, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send request submission email to {Email}", email);
                // Fallback to plain text if HTML fails
                try
                {
                    await _emailService.SendEmailAsync(email, subject, plainBody, isHtml: false);
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Failed to send plain text email to {Email}", email);
                }
            }
        }

        if (!string.IsNullOrEmpty(phone))
        {
            // Example SMS:
            // "Your charter bus request has been received! View quotes: [link]"
            _logger.LogInformation("Would send SMS to {Phone} about request submission with link: {QuoteLink}", phone, quoteLink);
        }

        await Task.CompletedTask;
    }

    public async Task NotifyFirstQuoteReceivedAsync(CharterRequestRecord request, Quote quote, CancellationToken cancellationToken = default)
    {
        var email = GetRequestEmail(request);
        var phone = GetRequestPhone(request);
        
        _logger.LogInformation(
            "First quote received for request {RequestId}. Email: {Email}, Phone: {Phone}. Quote: ${Price} {Currency} from {ProviderName}",
            request.Id, email, phone, quote.Price, quote.Currency, quote.Provider?.Name ?? "Unknown"
        );

        // TODO: Implement actual email/SMS sending
        // For now, we'll just log. In production, you would:
        // 1. Send email via SMTP/SendGrid/Azure Communication Services
        // 2. Send SMS via Twilio/Azure Communication Services
        // 3. Store notification preferences
        
        if (!string.IsNullOrEmpty(email))
        {
            var frontendUrl = _configuration?["Frontend:Url"] ?? "http://localhost:5173";
            var quoteLink = $"{frontendUrl}/quotes/{request.SessionId}";
            
            var subject = "You've received your first quote!";
            var body = $@"Great news! You've received your first quote for your charter bus request.

Quote Details:
- Provider: {quote.Provider?.Name ?? "Unknown"}
- Price: ${quote.Price} {quote.Currency}
{(!string.IsNullOrEmpty(quote.Notes) ? $"- Notes: {quote.Notes}\n" : "")}
More quotes may be coming in the next 24 hours. View all quotes: {quoteLink}";

            try
            {
                await _emailService.SendEmailAsync(email, subject, body, isHtml: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send first quote notification email to {Email}", email);
            }
        }

        if (!string.IsNullOrEmpty(phone))
        {
            // Example SMS:
            // "You've received your first quote! More may be coming. View quotes: [link]"
            _logger.LogInformation("Would send SMS to {Phone} about first quote", phone);
        }

        await Task.CompletedTask;
    }

    public async Task NotifyNewQuoteReceivedAsync(CharterRequestRecord request, Quote quote, CancellationToken cancellationToken = default)
    {
        var email = GetRequestEmail(request);
        var phone = GetRequestPhone(request);
        var quoteCount = request.Quotes.Count;
        var hoursRemaining = request.QuoteDeadline.HasValue 
            ? Math.Max(0, (int)(request.QuoteDeadline.Value - DateTime.UtcNow).TotalHours)
            : 0;

        _logger.LogInformation(
            "New quote received for request {RequestId}. Total quotes: {QuoteCount}. Hours remaining: {HoursRemaining}",
            request.Id, quoteCount, hoursRemaining
        );

        // Throttle notifications - only send if it's been at least 1 hour since last notification
        // or if we're within 2 hours of deadline
        // TODO: Implement notification throttling logic
        
        if (!string.IsNullOrEmpty(email))
        {
            var frontendUrl = _configuration?["Frontend:Url"] ?? "http://localhost:5173";
            var quoteLink = $"{frontendUrl}/quotes/{request.SessionId}";
            
            var subject = $"You've received {quoteCount} quote{(quoteCount != 1 ? "s" : "")} so far";
            var body = $@"Great news! You've received {quoteCount} quote{(quoteCount != 1 ? "s" : "")} for your charter bus request.

{hoursRemaining} hour{(hoursRemaining != 1 ? "s" : "")} remaining to receive more quotes.

View all quotes: {quoteLink}";

            try
            {
                await _emailService.SendEmailAsync(email, subject, body, isHtml: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send new quote notification email to {Email}", email);
            }
        }

        await Task.CompletedTask;
    }

    public async Task NotifyQuoteDeadlineReachedAsync(CharterRequestRecord request, CancellationToken cancellationToken = default)
    {
        var email = GetRequestEmail(request);
        var phone = GetRequestPhone(request);
        var quoteCount = request.Quotes.Count;

        _logger.LogInformation(
            "Quote deadline reached for request {RequestId}. Total quotes: {QuoteCount}",
            request.Id, quoteCount
        );

        if (!string.IsNullOrEmpty(email))
        {
            var frontendUrl = _configuration?["Frontend:Url"] ?? "http://localhost:5173";
            var quoteLink = $"{frontendUrl}/quotes/{request.SessionId}";
            
            var subject = "All quotes are in! Compare your options now";
            var body = $@"The quote collection period has ended. You've received {quoteCount} quote{(quoteCount != 1 ? "s" : "")} for your charter bus request.

Compare and choose the best option: {quoteLink}";

            try
            {
                await _emailService.SendEmailAsync(email, subject, body, isHtml: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send deadline notification email to {Email}", email);
            }
        }

        if (!string.IsNullOrEmpty(phone))
        {
            // Example SMS:
            // "All quotes are in! You have {QuoteCount} options. Compare now: [link]"
            _logger.LogInformation("Would send SMS to {Phone} about deadline reached ({QuoteCount} quotes)", phone, quoteCount);
        }

        await Task.CompletedTask;
    }

    private string? GetRequestEmail(CharterRequestRecord request)
    {
        // Try authenticated requester first
        if (request.Requester != null && !string.IsNullOrEmpty(request.Requester.Email))
        {
            return request.Requester.Email;
        }

        // Use email stored directly on request record (for anonymous users)
        if (!string.IsNullOrEmpty(request.Email))
        {
            return request.Email;
        }

        // Fall back to email from request data (legacy support)
        return request.RequestData?.Customer?.Email;
    }

    private string? GetRequestPhone(CharterRequestRecord request)
    {
        // Try authenticated requester first
        if (request.Requester != null && !string.IsNullOrEmpty(request.Requester.Phone))
        {
            return request.Requester.Phone;
        }

        // Fall back to phone from request data (for anonymous users)
        return request.RequestData?.Customer?.Phone;
    }

    public async Task NotifyOperatorsRequestPublishedAsync(CharterRequestRecord request, List<User> operators, CancellationToken cancellationToken = default)
    {
        if (operators == null || !operators.Any())
        {
            _logger.LogInformation("No operators to notify for published request {RequestId}", request.Id);
            return;
        }

        var pickup = request.RequestData?.Trip?.PickupLocation;
        var destination = request.RequestData?.Trip?.Destination;
        var passengerCount = request.RequestData?.Trip?.PassengerCount ?? 0;
        var date = request.RequestData?.Trip?.Date?.RawInput ?? "Date not specified";
        
        var frontendUrl = _configuration?["Frontend:Url"] ?? "http://localhost:5173";
        var requestLink = $"{frontendUrl}/provider/dashboard"; // Link to operator dashboard where they can see the request

        var subject = $"New Charter Request Available - {passengerCount} passengers";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #3b82f6; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9fafb; padding: 30px; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; background-color: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .details {{ background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .detail-row {{ margin: 10px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #6b7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>New Charter Request Available</h1>
        </div>
        <div class=""content"">
            <h2>A new charter request has been published</h2>
            <div class=""details"">
                <div class=""detail-row""><strong>Passengers:</strong> {passengerCount}</div>
                <div class=""detail-row""><strong>Date:</strong> {date}</div>
                <div class=""detail-row""><strong>Pickup:</strong> {pickup?.ResolvedName ?? pickup?.RawInput ?? "Not specified"}</div>
                <div class=""detail-row""><strong>Destination:</strong> {destination?.ResolvedName ?? destination?.RawInput ?? "Not specified"}</div>
            </div>
            <p>You have 24 hours to submit a quote for this request.</p>
            <p style=""text-align: center;"">
                <a href=""{requestLink}"" class=""button"">View Request & Submit Quote</a>
            </p>
            <p style=""font-size: 12px; color: #6b7280;"">Log in to your operator dashboard to view full details and submit your quote.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        var plainBody = $@"New Charter Request Available

A new charter request has been published and is available for quoting.

Request Details:
- Passengers: {passengerCount}
- Date: {date}
- Pickup: {pickup?.ResolvedName ?? pickup?.RawInput ?? "Not specified"}
- Destination: {destination?.ResolvedName ?? destination?.RawInput ?? "Not specified"}

You have 24 hours to submit a quote for this request.

View the request and submit your quote: {requestLink}

Log in to your operator dashboard to view full details and submit your quote.

This is an automated message. Please do not reply to this email.";

        var notifiedCount = 0;
        var failedCount = 0;

        foreach (var operatorUser in operators.Where(o => o.IsActive && !string.IsNullOrEmpty(o.Email)))
        {
            try
            {
                await _emailService.SendEmailAsync(operatorUser.Email, subject, htmlBody, isHtml: true);
                notifiedCount++;
                _logger.LogInformation("Notified operator {OperatorEmail} about published request {RequestId}", operatorUser.Email, request.Id);
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex, "Failed to send notification email to operator {OperatorEmail} for request {RequestId}", operatorUser.Email, request.Id);
                
                // Try plain text fallback
                try
                {
                    await _emailService.SendEmailAsync(operatorUser.Email, subject, plainBody, isHtml: false);
                    notifiedCount++;
                    failedCount--;
                    _logger.LogInformation("Sent plain text notification to operator {OperatorEmail} for request {RequestId}", operatorUser.Email, request.Id);
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Failed to send plain text notification to operator {OperatorEmail} for request {RequestId}", operatorUser.Email, request.Id);
                }
            }
        }

        _logger.LogInformation("Notified {NotifiedCount} operators about published request {RequestId}. {FailedCount} failed.", 
            notifiedCount, request.Id, failedCount);
    }
}
