using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Provider;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CharterCompare.Application.Handlers.Provider;

public class GetProviderRequestsHandler : IRequestHandler<GetProviderRequestsQuery, GetProviderRequestsResponse>
{
    private readonly IStorage _storage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetProviderRequestsHandler> _logger;

    public GetProviderRequestsHandler(IStorage storage, IHttpContextAccessor httpContextAccessor, ILogger<GetProviderRequestsHandler> logger)
    {
        _storage = storage;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<GetProviderRequestsResponse> Handle(GetProviderRequestsQuery request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var providerIdClaim = httpContext?.User.FindFirst("ProviderId")?.Value;
        
        if (string.IsNullOrEmpty(providerIdClaim) || !int.TryParse(providerIdClaim, out var providerId))
        {
            return new GetProviderRequestsResponse();
        }

        var requests = await _storage.GetOpenCharterRequestsAsync(cancellationToken);
        
        var requestDtos = requests.Select(r => new ProviderRequestDto
        {
            Id = r.Id,
            SessionId = r.SessionId,
            RequestData = MapCharterRequest(r.RequestData),
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt,
            QuoteCount = r.Quotes.Count
        }).ToList();

        return new GetProviderRequestsResponse
        {
            Requests = requestDtos
        };
    }

    private CharterRequestDto MapCharterRequest(Domain.Entities.CharterRequest request)
    {
        // Simple mapping - in production, use AutoMapper or similar
        return new CharterRequestDto
        {
            Customer = new CustomerInfoDto
            {
                FirstName = request.Customer.FirstName,
                LastName = request.Customer.LastName,
                Phone = request.Customer.Phone,
                Email = request.Customer.Email
            },
            Trip = new TripInfoDto
            {
                Type = request.Trip.Type,
                PassengerCount = request.Trip.PassengerCount,
                Date = new DateInfoDto
                {
                    RawInput = request.Trip.Date.RawInput,
                    ResolvedDate = request.Trip.Date.ResolvedDate,
                    Confidence = request.Trip.Date.Confidence
                },
                PickupLocation = new LocationInfoDto
                {
                    RawInput = request.Trip.PickupLocation.RawInput,
                    ResolvedName = request.Trip.PickupLocation.ResolvedName,
                    Suburb = request.Trip.PickupLocation.Suburb,
                    State = request.Trip.PickupLocation.State,
                    Lat = request.Trip.PickupLocation.Lat,
                    Lng = request.Trip.PickupLocation.Lng,
                    Confidence = request.Trip.PickupLocation.Confidence
                },
                Destination = new LocationInfoDto
                {
                    RawInput = request.Trip.Destination.RawInput,
                    ResolvedName = request.Trip.Destination.ResolvedName,
                    Suburb = request.Trip.Destination.Suburb,
                    State = request.Trip.Destination.State,
                    Lat = request.Trip.Destination.Lat,
                    Lng = request.Trip.Destination.Lng,
                    Confidence = request.Trip.Destination.Confidence
                },
                TripFormat = request.Trip.TripFormat,
                Timing = new TimingInfoDto
                {
                    RawInput = request.Trip.Timing.RawInput,
                    PickupTime = request.Trip.Timing.PickupTime,
                    ReturnTime = request.Trip.Timing.ReturnTime
                },
                SpecialRequirements = request.Trip.SpecialRequirements
            },
            Meta = new RequestMetaDto
            {
                Source = request.Meta.Source,
                CreatedAt = request.Meta.CreatedAt
            }
        };
    }
}
