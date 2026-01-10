using CharterCompare.Domain.Entities;

namespace CharterCompare.Application.Services;

public interface INotificationService
{
    Task NotifyRequestSubmittedAsync(CharterRequestRecord request, string quoteLink, CancellationToken cancellationToken = default);
    Task NotifyFirstQuoteReceivedAsync(CharterRequestRecord request, Quote quote, CancellationToken cancellationToken = default);
    Task NotifyNewQuoteReceivedAsync(CharterRequestRecord request, Quote quote, CancellationToken cancellationToken = default);
    Task NotifyQuoteDeadlineReachedAsync(CharterRequestRecord request, CancellationToken cancellationToken = default);
    Task NotifyOperatorsRequestPublishedAsync(CharterRequestRecord request, List<User> operators, CancellationToken cancellationToken = default);
}
