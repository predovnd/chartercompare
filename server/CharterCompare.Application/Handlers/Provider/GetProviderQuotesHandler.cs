using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Provider;
using CharterCompare.Application.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Provider;

public class GetProviderQuotesHandler : IRequestHandler<GetProviderQuotesQuery, GetProviderQuotesResponse>
{
    private readonly IStorage _storage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetProviderQuotesHandler> _logger;

    public GetProviderQuotesHandler(IStorage storage, IHttpContextAccessor httpContextAccessor, ILogger<GetProviderQuotesHandler> logger)
    {
        _storage = storage;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<GetProviderQuotesResponse> Handle(GetProviderQuotesQuery request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var providerIdClaim = httpContext?.User.FindFirst("ProviderId")?.Value;
        
        if (string.IsNullOrEmpty(providerIdClaim) || !int.TryParse(providerIdClaim, out var providerId))
        {
            return new GetProviderQuotesResponse();
        }

        var quotes = await _storage.GetQuotesByProviderIdAsync(providerId, cancellationToken);
        
        var quoteDtos = quotes.Select(q => new ProviderQuoteDto
        {
            Id = q.Id,
            RequestId = q.CharterRequestId,
            Price = q.Price,
            Currency = q.Currency,
            Notes = q.Notes,
            Status = q.Status.ToString(),
            CreatedAt = q.CreatedAt,
            Request = MapCharterRequest(q.CharterRequest)
        }).OrderByDescending(q => q.CreatedAt).ToList();

        return new GetProviderQuotesResponse
        {
            Quotes = quoteDtos
        };
    }

    private ProviderRequestDto? MapCharterRequest(Domain.Entities.CharterRequestRecord? request)
    {
        if (request == null) return null;
        
        return new ProviderRequestDto
        {
            Id = request.Id,
            SessionId = request.SessionId,
            RequestData = MapCharterRequestData(request.RequestData),
            Status = request.Status.ToString(),
            CreatedAt = request.CreatedAt,
            QuoteCount = request.Quotes.Count
        };
    }

    private CharterRequestDto MapCharterRequestData(Domain.Entities.CharterRequest request)
    {
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
