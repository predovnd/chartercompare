using CharterCompare.Domain.Entities;

namespace CharterCompare.Application.Storage;

public interface IStorage
{
    // Operator operations
    Task<Operator?> GetOperatorByIdAsync(int operatorId, CancellationToken cancellationToken = default);
    Task<Operator?> GetOperatorByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Operator?> GetOperatorByExternalIdAsync(string externalId, string provider, CancellationToken cancellationToken = default);
    Task<Operator> CreateOperatorAsync(Operator operatorEntity, CancellationToken cancellationToken = default);
    Task UpdateOperatorAsync(Operator operatorEntity, CancellationToken cancellationToken = default);
    Task<List<Operator>> GetAllOperatorsAsync(CancellationToken cancellationToken = default);

    // Requester operations
    Task<Requester?> GetRequesterByIdAsync(int requesterId, CancellationToken cancellationToken = default);
    Task<Requester?> GetRequesterByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Requester?> GetRequesterByExternalIdAsync(string externalId, string provider, CancellationToken cancellationToken = default);
    Task<Requester> CreateRequesterAsync(Requester requester, CancellationToken cancellationToken = default);
    Task UpdateRequesterAsync(Requester requester, CancellationToken cancellationToken = default);

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
