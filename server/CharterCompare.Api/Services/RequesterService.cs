using CharterCompare.Infrastructure.Data;
using CharterCompare.Api.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace CharterCompare.Api.Services;

public class RequesterService : IRequesterService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RequesterService> _logger;

    public RequesterService(ApplicationDbContext dbContext, ILogger<RequesterService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Requester?> GetRequesterByEmailAsync(string email)
    {
        return await _dbContext.Requesters
            .FirstOrDefaultAsync(r => r.Email == email);
    }

    public async Task<Requester?> GetRequesterByExternalIdAsync(string externalId, string provider)
    {
        if (provider == "Internal" && int.TryParse(externalId, out var requesterId))
        {
            return await _dbContext.Requesters.FindAsync(requesterId);
        }
        return await _dbContext.Requesters
            .FirstOrDefaultAsync(r => r.ExternalId == externalId && r.ExternalProvider == provider);
    }

    public async Task<Requester> CreateRequesterAsync(string email, string name, string externalId, string externalProvider, string? passwordHash = null, string? phone = null)
    {
        var requester = new Requester
        {
            Email = email,
            Name = name,
            Phone = phone,
            ExternalId = externalId,
            ExternalProvider = externalProvider,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.Requesters.Add(requester);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created new requester: {Email}", email);
        return requester;
    }

    public async Task<bool> VerifyPasswordAsync(Requester requester, string password)
    {
        if (string.IsNullOrEmpty(requester.PasswordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, requester.PasswordHash);
    }

    public async Task<List<CharterRequestRecord>> GetRequesterRequestsAsync(int requesterId)
    {
        return await _dbContext.CharterRequests
            .Include(r => r.Quotes)
            .ThenInclude(q => q.Provider)
            .Where(r => r.RequesterId == requesterId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<CharterRequestRecord?> GetRequestByIdAsync(int requestId, int requesterId)
    {
        return await _dbContext.CharterRequests
            .Include(r => r.Quotes)
            .ThenInclude(q => q.Provider)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.RequesterId == requesterId);
    }
}
