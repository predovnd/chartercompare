using CharterCompare.Application.Storage;
using CharterCompare.Domain.Entities;
using CharterCompare.Domain.Enums;
using CharterCompare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OperatorCoverage = CharterCompare.Domain.Entities.OperatorCoverage;

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

    // User operations
    public async Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Quotes)
            .Include(u => u.Requests)
            .Include(u => u.Attributes)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Attributes)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetUserByExternalIdAsync(string externalId, string provider, CancellationToken cancellationToken = default)
    {
        if (provider == "Internal" && int.TryParse(externalId, out var userId))
        {
            return await _dbContext.Users
                .Include(u => u.Attributes)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }
        return await _dbContext.Users
            .Include(u => u.Attributes)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId && u.ExternalProvider == provider, cancellationToken);
    }

    public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Quotes)
            .Include(u => u.Requests)
            .Include(u => u.Attributes)
            .Include(u => u.OperatorCoverages)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<User>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Quotes)
            .Include(u => u.Requests)
            .Include(u => u.Attributes)
            .Where(u => u.Role == role)
            .ToListAsync(cancellationToken);
    }

    // Convenience methods for backward compatibility
    public async Task<User?> GetOperatorByIdAsync(int operatorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Quotes)
            .Include(u => u.Attributes)
            .Where(u => u.Id == operatorId && (u.Role == UserRole.Operator || u.Role == UserRole.Admin))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetOperatorByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Attributes)
            .Where(u => u.Email == email && (u.Role == UserRole.Operator || u.Role == UserRole.Admin))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetRequesterByIdAsync(int requesterId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Requests)
            .Include(u => u.Attributes)
            .Where(u => u.Id == requesterId && u.Role == UserRole.Requester)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetRequesterByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.Attributes)
            .Where(u => u.Email == email && u.Role == UserRole.Requester)
            .FirstOrDefaultAsync(cancellationToken);
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
            .Where(r => r.Status == RequestStatus.Published)
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

    // UserAttribute operations
    public async Task<List<UserAttribute>> GetUserAttributesAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAttributes
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddUserAttributeAsync(int userId, UserAttributeType attributeType, CancellationToken cancellationToken = default)
    {
        // Check if attribute already exists
        var exists = await _dbContext.UserAttributes
            .AnyAsync(a => a.UserId == userId && a.AttributeType == attributeType, cancellationToken);
        
        if (!exists)
        {
            var attribute = new UserAttribute
            {
                UserId = userId,
                AttributeType = attributeType,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.UserAttributes.Add(attribute);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveUserAttributeAsync(int userId, UserAttributeType attributeType, CancellationToken cancellationToken = default)
    {
        var attribute = await _dbContext.UserAttributes
            .FirstOrDefaultAsync(a => a.UserId == userId && a.AttributeType == attributeType, cancellationToken);
        
        if (attribute != null)
        {
            _dbContext.UserAttributes.Remove(attribute);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetUserAttributesAsync(int userId, List<UserAttributeType> attributeTypes, CancellationToken cancellationToken = default)
    {
        // Get existing attributes
        var existingAttributes = await _dbContext.UserAttributes
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        // Remove attributes that are not in the new list
        var attributesToRemove = existingAttributes
            .Where(a => !attributeTypes.Contains(a.AttributeType))
            .ToList();
        
        foreach (var attr in attributesToRemove)
        {
            _dbContext.UserAttributes.Remove(attr);
        }

        // Add new attributes that don't exist
        var existingTypes = existingAttributes.Select(a => a.AttributeType).ToList();
        var attributesToAdd = attributeTypes
            .Where(t => !existingTypes.Contains(t))
            .Select(t => new UserAttribute
            {
                UserId = userId,
                AttributeType = t,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        _dbContext.UserAttributes.AddRange(attributesToAdd);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Save changes
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // OperatorCoverage operations
    public async Task<OperatorCoverage?> GetOperatorCoverageByIdAsync(int coverageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperatorCoverages
            .Include(c => c.Operator)
            .FirstOrDefaultAsync(c => c.Id == coverageId, cancellationToken);
    }

    public async Task<List<OperatorCoverage>> GetOperatorCoveragesByOperatorIdAsync(int operatorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperatorCoverages
            .Where(c => c.OperatorId == operatorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperatorCoverage> CreateOperatorCoverageAsync(OperatorCoverage coverage, CancellationToken cancellationToken = default)
    {
        _dbContext.OperatorCoverages.Add(coverage);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return coverage;
    }

    public async Task UpdateOperatorCoverageAsync(OperatorCoverage coverage, CancellationToken cancellationToken = default)
    {
        coverage.UpdatedAt = DateTime.UtcNow;
        _dbContext.OperatorCoverages.Update(coverage);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteOperatorCoverageAsync(int coverageId, CancellationToken cancellationToken = default)
    {
        var coverage = await _dbContext.OperatorCoverages.FindAsync(new object[] { coverageId }, cancellationToken);
        if (coverage != null)
        {
            _dbContext.OperatorCoverages.Remove(coverage);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
