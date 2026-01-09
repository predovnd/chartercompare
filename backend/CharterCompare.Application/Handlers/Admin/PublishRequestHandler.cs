using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class PublishRequestHandler : IRequestHandler<PublishRequestCommand, PublishRequestResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<PublishRequestHandler> _logger;

    public PublishRequestHandler(IStorage storage, ILogger<PublishRequestHandler> logger)
    {
        _storage = storage;
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
            charterRequest.Status = RequestStatus.Published;
            await _storage.UpdateCharterRequestAsync(charterRequest, cancellationToken);

            _logger.LogInformation("Request {RequestId} published by admin", request.RequestId);

            // TODO: Determine matching operators and notify them (to be implemented later)

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
