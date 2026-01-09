using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUsername;
    private readonly string? _smtpPassword;
    private readonly string? _fromEmail;
    private readonly string? _fromName;
    private readonly bool _enableSsl;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        _smtpHost = _configuration["Email:Smtp:Host"];
        if (int.TryParse(_configuration["Email:Smtp:Port"], out var port))
        {
            _smtpPort = port;
        }
        else
        {
            _smtpPort = 587;
        }
        _smtpUsername = _configuration["Email:Smtp:Username"];
        _smtpPassword = _configuration["Email:Smtp:Password"];
        _fromEmail = _configuration["Email:From:Address"] ?? _smtpUsername;
        _fromName = _configuration["Email:From:Name"] ?? "CharterCompare";
        if (bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var enableSsl))
        {
            _enableSsl = enableSsl;
        }
        else
        {
            _enableSsl = true;
        }
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_smtpHost) || string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
        {
            _logger.LogWarning("Email configuration is incomplete. Email not sent to {To}. Subject: {Subject}", to, subject);
            _logger.LogInformation("Email would have been sent to {To} with subject: {Subject}\nBody:\n{Body}", to, subject, body);
            return;
        }

        if (string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("Cannot send email: recipient address is empty");
            return;
        }

        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_fromEmail!, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(to);

            await client.SendMailAsync(message, cancellationToken);
            
            _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
            throw;
        }
    }
}
