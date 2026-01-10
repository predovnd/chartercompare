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
        int? requesterId = null;
        
        // Try RequesterId claim first (for legacy requesters)
        var requesterIdClaim = httpContext?.User.FindFirst("RequesterId")?.Value;
        if (!string.IsNullOrEmpty(requesterIdClaim) && int.TryParse(requesterIdClaim, out var parsedRequesterId))
        {
            requesterId = parsedRequesterId;
            _logger.LogInformation("Found RequesterId from RequesterId claim: {RequesterId}", requesterId);
        }
        
        // Also try UserId claim and verify user is a requester (for unified user model)
        if (requesterId == null)
        {
            var userIdClaim = httpContext?.User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
            {
                // Verify this user is a requester
                var user = await _storage.GetUserByIdAsync(parsedUserId, cancellationToken);
                if (user != null && user.Role == Domain.Enums.UserRole.Requester)
                {
                    requesterId = parsedUserId;
                    _logger.LogInformation("Found RequesterId from UserId claim (user is requester): {RequesterId}", requesterId);
                }
                else
                {
                    _logger.LogWarning("UserId {UserId} is not a requester (Role: {Role})", parsedUserId, user?.Role);
                }
            }
        }
        
        if (requesterId == null)
        {
            _logger.LogWarning("Could not determine RequesterId from claims. User authenticated: {IsAuthenticated}", 
                httpContext?.User?.Identity?.IsAuthenticated ?? false);
            return new GetRequesterRequestsResponse();
        }

        _logger.LogInformation("Fetching requests for RequesterId: {RequesterId}", requesterId);
        var requests = await _storage.GetRequesterCharterRequestsAsync(requesterId.Value, cancellationToken);
        _logger.LogInformation("Found {Count} requests for RequesterId: {RequesterId}", requests.Count, requesterId);
        
        var requestDtos = requests.Select(r => new RequesterRequestDto
        {
            Id = r.Id,
            SessionId = r.SessionId,
            RequestData = MapCharterRequest(r.RequestData),
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt,
            Quotes = r.Quotes
                .Select(q => new QuoteDto
                {
                    Id = q.Id,
                    OperatorName = q.Provider.Name,
                    OperatorEmail = q.Provider.Email,
                    Price = q.Price,
                    Currency = q.Currency,
                    Notes = q.Notes,
                    Status = q.Status.ToString(),
                    CreatedAt = q.CreatedAt
                })
                .OrderBy(q => q.Price) // Sort by price (cheapest first)
                .ToList()
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
