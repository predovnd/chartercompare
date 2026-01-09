using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Requester;
using CharterCompare.Application.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Requester;

public class GetRequesterRequestsHandler : IRequestHandler<GetRequesterRequestsQuery, GetRequesterRequestsResponse>
{
    private readonly IStorage _storage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetRequesterRequestsHandler> _logger;

    public GetRequesterRequestsHandler(IStorage storage, IHttpContextAccessor httpContextAccessor, ILogger<GetRequesterRequestsHandler> logger)
    {
        _storage = storage;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<GetRequesterRequestsResponse> Handle(GetRequesterRequestsQuery request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var requesterIdClaim = httpContext?.User.FindFirst("RequesterId")?.Value;
        
        if (string.IsNullOrEmpty(requesterIdClaim) || !int.TryParse(requesterIdClaim, out var requesterId))
        {
            return new GetRequesterRequestsResponse();
        }

        var requests = await _storage.GetRequesterCharterRequestsAsync(requesterId, cancellationToken);
        
        var requestDtos = requests.Select(r => new RequesterRequestDto
        {
            Id = r.Id,
            SessionId = r.SessionId,
            RequestData = MapCharterRequest(r.RequestData),
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt,
            Quotes = r.Quotes.Select(q => new QuoteDto
            {
                Id = q.Id,
                OperatorName = q.Provider.Name,
                OperatorEmail = q.Provider.Email,
                Price = q.Price,
                Currency = q.Currency,
                Notes = q.Notes,
                Status = q.Status.ToString(),
                CreatedAt = q.CreatedAt
            }).ToList()
        }).ToList();

        return new GetRequesterRequestsResponse
        {
            Requests = requestDtos
        };
    }

    private CharterRequestDto MapCharterRequest(Domain.Entities.CharterRequest request)
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
