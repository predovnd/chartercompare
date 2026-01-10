using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using CharterCompare.Application.Services;
using CharterCompare.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class PublishRequestHandler : IRequestHandler<PublishRequestCommand, PublishRequestResponse>
{
    private readonly IStorage _storage;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PublishRequestHandler> _logger;

    public PublishRequestHandler(IStorage storage, INotificationService notificationService, ILogger<PublishRequestHandler> logger)
    {
        _storage = storage;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PublishRequestResponse> Handle(PublishRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var charterRequest = await _storage.GetCharterRequestByIdAsync(request.RequestId, cancellationToken);
            if (charterRequest == null)
            {
                return new PublishRequestResponse
                {
                    Success = false,
                    Error = "Request not found"
                };
            }

            // Validate that locations are geocoded
            var pickup = charterRequest.RequestData.Trip.PickupLocation;
            var destination = charterRequest.RequestData.Trip.Destination;

            if (!pickup.Lat.HasValue || !pickup.Lng.HasValue)
            {
                return new PublishRequestResponse
                {
                    Success = false,
                    Error = "Pickup location must be geocoded before publishing"
                };
            }

            if (!destination.Lat.HasValue || !destination.Lng.HasValue)
            {
                return new PublishRequestResponse
                {
                    Success = false,
                    Error = "Destination location must be geocoded before publishing"
                };
            }

            // Change status to Published
            // Published requests are visible to ALL operators (no filtering yet)
            // Filtering logic will be added later based on operator coverage
            charterRequest.Status = RequestStatus.Published;
            
            // Set quote deadline to 24 hours from now
            charterRequest.QuoteDeadline = DateTime.UtcNow.AddHours(24);
            
            await _storage.UpdateCharterRequestAsync(charterRequest, cancellationToken);

            _logger.LogInformation("Request {RequestId} published by admin - now visible to all operators", request.RequestId);

            // Notify all active operators about the published request
            try
            {
                var allOperators = await _storage.GetUsersByRoleAsync(UserRole.Operator, cancellationToken);
                var activeOperators = allOperators.Where(o => o.IsActive).ToList();
                
                if (activeOperators.Any())
                {
                    _logger.LogInformation("Notifying {Count} active operators about published request {RequestId}", 
                        activeOperators.Count, request.RequestId);
                    
                    await _notificationService.NotifyOperatorsRequestPublishedAsync(charterRequest, activeOperators, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("No active operators found to notify for request {RequestId}", request.RequestId);
                }
            }
            catch (Exception notifyEx)
            {
                _logger.LogError(notifyEx, "Error notifying operators about published request {RequestId}", request.RequestId);
                // Don't fail the publish operation if notification fails
            }

            return new PublishRequestResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing request: {Error}", ex.Message);
            return new PublishRequestResponse
            {
                Success = false,
                Error = $"An error occurred: {ex.Message}"
            };
        }
    }
}
