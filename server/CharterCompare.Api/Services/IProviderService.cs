using CharterCompare.Api.Models;

namespace CharterCompare.Api.Services;

public interface IProviderService
{
    Task<Provider?> GetProviderByExternalIdAsync(string externalId, string provider);
    Task<Provider?> GetProviderByEmailAsync(string email);
    Task<Provider> CreateProviderAsync(string email, string name, string externalId, string externalProvider, string? companyName = null, string? passwordHash = null);
    Task<bool> VerifyPasswordAsync(Provider provider, string password);
    Task<List<CharterRequestRecord>> GetOpenRequestsAsync();
    Task<CharterRequestRecord?> GetRequestByIdAsync(int requestId);
    Task<Quote> SubmitQuoteAsync(int providerId, int requestId, decimal price, string currency, string? notes);
    Task<List<Quote>> GetProviderQuotesAsync(int providerId);
}
