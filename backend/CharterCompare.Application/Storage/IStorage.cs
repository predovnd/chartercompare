using CharterCompare.Domain.Entities;
using CharterCompare.Domain.Enums;

namespace CharterCompare.Application.Storage;

public interface IStorage
{
    // User operations
    Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetUserByExternalIdAsync(string externalId, string provider, CancellationToken cancellationToken = default);
    Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<List<User>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    
    // UserAttribute operations
    Task<List<UserAttribute>> GetUserAttributesAsync(int userId, CancellationToken cancellationToken = default);
    Task AddUserAttributeAsync(int userId, UserAttributeType attributeType, CancellationToken cancellationToken = default);
    Task RemoveUserAttributeAsync(int userId, UserAttributeType attributeType, CancellationToken cancellationToken = default);
    Task SetUserAttributesAsync(int userId, List<UserAttributeType> attributeTypes, CancellationToken cancellationToken = default);
    
    // Convenience methods for backward compatibility
    Task<User?> GetOperatorByIdAsync(int operatorId, CancellationToken cancellationToken = default);
    Task<User?> GetOperatorByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetRequesterByIdAsync(int requesterId, CancellationToken cancellationToken = default);
    Task<User?> GetRequesterByEmailAsync(string email, CancellationToken cancellationToken = default);

    // CharterRequestRecord operations
    Task<CharterRequestRecord?> GetCharterRequestByIdAsync(int requestId, CancellationToken cancellationToken = default);
    Task<CharterRequestRecord> CreateCharterRequestAsync(CharterRequestRecord request, CancellationToken cancellationToken = default);
    Task UpdateCharterRequestAsync(CharterRequestRecord request, CancellationToken cancellationToken = default);
    Task<List<CharterRequestRecord>> GetAllCharterRequestsAsync(CancellationToken cancellationToken = default);
    Task<List<CharterRequestRecord>> GetOpenCharterRequestsAsync(CancellationToken cancellationToken = default);
    Task<List<CharterRequestRecord>> GetRequesterCharterRequestsAsync(int requesterId, CancellationToken cancellationToken = default);

    // Quote operations
    Task<Quote?> GetQuoteByIdAsync(int quoteId, CancellationToken cancellationToken = default);
    Task<Quote> CreateQuoteAsync(Quote quote, CancellationToken cancellationToken = default);
    Task UpdateQuoteAsync(Quote quote, CancellationToken cancellationToken = default);
    Task<List<Quote>> GetQuotesByRequestIdAsync(int requestId, CancellationToken cancellationToken = default);
    Task<List<Quote>> GetQuotesByProviderIdAsync(int providerId, CancellationToken cancellationToken = default);

    // Save changes
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
