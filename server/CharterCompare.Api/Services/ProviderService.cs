using CharterCompare.Api.Data;
using CharterCompare.Api.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace CharterCompare.Api.Services;

public class ProviderService : IProviderService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(ApplicationDbContext dbContext, ILogger<ProviderService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Provider?> GetProviderByExternalIdAsync(string externalId, string provider)
    {
        if (provider == "Internal" && int.TryParse(externalId, out var providerId))
        {
            return await _dbContext.Providers.FindAsync(providerId);
        }
        return await _dbContext.Providers
            .FirstOrDefaultAsync(p => p.ExternalId == externalId && p.ExternalProvider == provider);
    }

    public async Task<Provider?> GetProviderByEmailAsync(string email)
    {
        return await _dbContext.Providers
            .FirstOrDefaultAsync(p => p.Email == email);
    }

    public async Task<Provider> CreateProviderAsync(string email, string name, string externalId, string externalProvider, string? companyName = null, string? passwordHash = null)
    {
        var provider = new Provider
        {
            Email = email,
            Name = name,
            CompanyName = companyName,
            ExternalId = externalId,
            ExternalProvider = externalProvider,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.Providers.Add(provider);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created new provider: {Email}", email);
        return provider;
    }

    public async Task<bool> VerifyPasswordAsync(Provider provider, string password)
    {
        if (string.IsNullOrEmpty(provider.PasswordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, provider.PasswordHash);
    }

    public async Task<List<CharterRequestRecord>> GetOpenRequestsAsync()
    {
        return await _dbContext.CharterRequests
            .Where(r => r.Status == RequestStatus.Published)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<CharterRequestRecord?> GetRequestByIdAsync(int requestId)
    {
        return await _dbContext.CharterRequests
            .Include(r => r.Quotes)
            .ThenInclude(q => q.Provider)
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }

    public async Task<Quote> SubmitQuoteAsync(int providerId, int requestId, decimal price, string currency, string? notes)
    {
        var quote = new Quote
        {
            ProviderId = providerId,
            CharterRequestId = requestId,
            Price = price,
            Currency = currency,
            Notes = notes,
            Status = QuoteStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Quotes.Add(quote);

        // Update request status if first quote
        var request = await _dbContext.CharterRequests.FindAsync(requestId);
        if (request != null && request.Status == RequestStatus.Published)
        {
            request.Status = RequestStatus.QuotesReceived;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Quote submitted by provider {ProviderId} for request {RequestId}", providerId, requestId);
        return quote;
    }

    public async Task<List<Quote>> GetProviderQuotesAsync(int providerId)
    {
        return await _dbContext.Quotes
            .Include(q => q.CharterRequest)
            .Where(q => q.ProviderId == providerId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }
}
