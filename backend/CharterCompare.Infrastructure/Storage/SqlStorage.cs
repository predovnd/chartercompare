using CharterCompare.Application.Storage;
using CharterCompare.Domain.Entities;
using CharterCompare.Domain.Enums;
using CharterCompare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Infrastructure.Storage;

public class SqlStorage : IStorage
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SqlStorage> _logger;

    public SqlStorage(ApplicationDbContext dbContext, ILogger<SqlStorage> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Operator operations
    public async Task<Operator?> GetOperatorByIdAsync(int operatorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Providers
            .Include(o => o.Quotes)
            .FirstOrDefaultAsync(o => o.Id == operatorId, cancellationToken);
    }

    public async Task<Operator?> GetOperatorByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Providers
            .FirstOrDefaultAsync(o => o.Email == email, cancellationToken);
    }

    public async Task<Operator?> GetOperatorByExternalIdAsync(string externalId, string provider, CancellationToken cancellationToken = default)
    {
        if (provider == "Internal" && int.TryParse(externalId, out var operatorId))
        {
            return await _dbContext.Providers.FindAsync(new object[] { operatorId }, cancellationToken);
        }
        return await _dbContext.Providers
            .FirstOrDefaultAsync(o => o.ExternalId == externalId && o.ExternalProvider == provider, cancellationToken);
    }

    public async Task<Operator> CreateOperatorAsync(Operator operatorEntity, CancellationToken cancellationToken = default)
    {
        _dbContext.Providers.Add(operatorEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return operatorEntity;
    }

    public async Task UpdateOperatorAsync(Operator operatorEntity, CancellationToken cancellationToken = default)
    {
        _dbContext.Providers.Update(operatorEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Operator>> GetAllOperatorsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Providers
            .Include(o => o.Quotes)
            .ToListAsync(cancellationToken);
    }

    // Requester operations
    public async Task<Requester?> GetRequesterByIdAsync(int requesterId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Requesters
            .Include(r => r.Requests)
            .FirstOrDefaultAsync(r => r.Id == requesterId, cancellationToken);
    }

    public async Task<Requester?> GetRequesterByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Requesters
            .FirstOrDefaultAsync(r => r.Email == email, cancellationToken);
    }

    public async Task<Requester?> GetRequesterByExternalIdAsync(string externalId, string provider, CancellationToken cancellationToken = default)
    {
        if (provider == "Internal" && int.TryParse(externalId, out var requesterId))
        {
            return await _dbContext.Requesters.FindAsync(new object[] { requesterId }, cancellationToken);
        }
        return await _dbContext.Requesters
            .FirstOrDefaultAsync(r => r.ExternalId == externalId && r.ExternalProvider == provider, cancellationToken);
    }

    public async Task<Requester> CreateRequesterAsync(Requester requester, CancellationToken cancellationToken = default)
    {
        _dbContext.Requesters.Add(requester);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return requester;
    }

    public async Task UpdateRequesterAsync(Requester requester, CancellationToken cancellationToken = default)
    {
        _dbContext.Requesters.Update(requester);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Requester>> GetAllRequestersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Requesters
            .Include(r => r.Requests)
            .ToListAsync(cancellationToken);
    }

    // CharterRequestRecord operations
    public async Task<CharterRequestRecord?> GetCharterRequestByIdAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CharterRequests
            .Include(r => r.Quotes)
                .ThenInclude(q => q.Provider)
            .Include(r => r.Requester)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
    }

    public async Task<CharterRequestRecord> CreateCharterRequestAsync(CharterRequestRecord request, CancellationToken cancellationToken = default)
    {
        _dbContext.CharterRequests.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task UpdateCharterRequestAsync(CharterRequestRecord request, CancellationToken cancellationToken = default)
    {
        _dbContext.CharterRequests.Update(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<CharterRequestRecord>> GetAllCharterRequestsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CharterRequests
            .Include(r => r.Quotes)
                .ThenInclude(q => q.Provider)
            .Include(r => r.Requester)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CharterRequestRecord>> GetOpenCharterRequestsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CharterRequests
            .Include(r => r.Quotes)
            .Where(r => r.Status == RequestStatus.Open)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CharterRequestRecord>> GetRequesterCharterRequestsAsync(int requesterId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CharterRequests
            .Include(r => r.Quotes)
                .ThenInclude(q => q.Provider)
            .Where(r => r.RequesterId == requesterId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    // Quote operations
    public async Task<Quote?> GetQuoteByIdAsync(int quoteId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Quotes
            .Include(q => q.Provider)
            .Include(q => q.CharterRequest)
            .FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);
    }

    public async Task<Quote> CreateQuoteAsync(Quote quote, CancellationToken cancellationToken = default)
    {
        _dbContext.Quotes.Add(quote);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return quote;
    }

    public async Task UpdateQuoteAsync(Quote quote, CancellationToken cancellationToken = default)
    {
        _dbContext.Quotes.Update(quote);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Quote>> GetQuotesByRequestIdAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Quotes
            .Include(q => q.Provider)
            .Where(q => q.CharterRequestId == requestId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Quote>> GetQuotesByProviderIdAsync(int providerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Quotes
            .Include(q => q.CharterRequest)
            .Where(q => q.ProviderId == providerId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    // Save changes
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
