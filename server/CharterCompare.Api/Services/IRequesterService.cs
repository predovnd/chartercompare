using CharterCompare.Api.Models;

namespace CharterCompare.Api.Services;

public interface IRequesterService
{
    Task<Requester?> GetRequesterByEmailAsync(string email);
    Task<Requester?> GetRequesterByExternalIdAsync(string externalId, string provider);
    Task<Requester> CreateRequesterAsync(string email, string name, string externalId, string externalProvider, string? passwordHash = null, string? phone = null);
    Task<bool> VerifyPasswordAsync(Requester requester, string password);
    Task<List<CharterRequestRecord>> GetRequesterRequestsAsync(int requesterId);
    Task<CharterRequestRecord?> GetRequestByIdAsync(int requestId, int requesterId);
}
