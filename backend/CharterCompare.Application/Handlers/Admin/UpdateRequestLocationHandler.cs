using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class UpdateRequestLocationHandler : IRequestHandler<UpdateRequestLocationCommand, UpdateRequestLocationResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<UpdateRequestLocationHandler> _logger;

    public UpdateRequestLocationHandler(IStorage storage, ILogger<UpdateRequestLocationHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<UpdateRequestLocationResponse> Handle(UpdateRequestLocationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var charterRequest = await _storage.GetCharterRequestByIdAsync(request.RequestId, cancellationToken);
            if (charterRequest == null)
            {
                return new UpdateRequestLocationResponse
                {
                    Success = false,
                    Error = "Request not found"
                };
            }

            // Update the location based on type
            if (request.LocationType.ToLower() == "pickup")
            {
                charterRequest.RequestData.Trip.PickupLocation.RawInput = request.LocationName.Trim();
                charterRequest.RequestData.Trip.PickupLocation.ResolvedName = request.LocationName.Trim();
                if (request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    charterRequest.RequestData.Trip.PickupLocation.Lat = request.Latitude.Value;
                    charterRequest.RequestData.Trip.PickupLocation.Lng = request.Longitude.Value;
                    charterRequest.RequestData.Trip.PickupLocation.Confidence = "high";
                }
            }
            else if (request.LocationType.ToLower() == "destination")
            {
                charterRequest.RequestData.Trip.Destination.RawInput = request.LocationName.Trim();
                charterRequest.RequestData.Trip.Destination.ResolvedName = request.LocationName.Trim();
                if (request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    charterRequest.RequestData.Trip.Destination.Lat = request.Latitude.Value;
                    charterRequest.RequestData.Trip.Destination.Lng = request.Longitude.Value;
                    charterRequest.RequestData.Trip.Destination.Confidence = "high";
                }
            }
            else
            {
                return new UpdateRequestLocationResponse
                {
                    Success = false,
                    Error = "Invalid location type. Must be 'pickup' or 'destination'"
                };
            }

            await _storage.UpdateCharterRequestAsync(charterRequest, cancellationToken);

            return new UpdateRequestLocationResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating request location: {Error}", ex.Message);
            return new UpdateRequestLocationResponse
            {
                Success = false,
                Error = $"An error occurred: {ex.Message}"
            };
        }
    }
}
