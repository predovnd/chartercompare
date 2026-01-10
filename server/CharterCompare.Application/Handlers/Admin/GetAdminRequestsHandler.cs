using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class GetAdminRequestsHandler : IRequestHandler<GetAdminRequestsQuery, GetAdminRequestsResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<GetAdminRequestsHandler> _logger;

    public GetAdminRequestsHandler(IStorage storage, ILogger<GetAdminRequestsHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<GetAdminRequestsResponse> Handle(GetAdminRequestsQuery request, CancellationToken cancellationToken)
    {
        var requests = await _storage.GetAllCharterRequestsAsync(cancellationToken);
        
        var requestDtos = requests.Select(r => new AdminRequestDto
        {
            Id = r.Id,
            SessionId = r.SessionId,
            RequestData = MapCharterRequest(r.RequestData),
            RawJsonPayload = r.RawJsonPayload,
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt,
            RequesterId = r.RequesterId,
            RequesterEmail = r.Requester?.Email ?? r.Email, // Use stored email if no authenticated requester
            RequesterName = r.Requester?.Name,
            QuoteCount = r.Quotes.Count,
            HasLowConfidence = (r.RequestData.Trip.PickupLocation.Confidence == "low" || 
                               r.RequestData.Trip.Destination.Confidence == "low"),
            Quotes = r.Quotes.Select(q => new AdminQuoteDto
            {
                Id = q.Id,
                ProviderName = q.Provider?.Name ?? "Unknown",
                ProviderEmail = q.Provider?.Email ?? "",
                Price = q.Price,
                Currency = q.Currency,
                Notes = q.Notes,
                Status = q.Status.ToString(),
                CreatedAt = q.CreatedAt
            }).ToList()
        }).ToList();

        return new GetAdminRequestsResponse
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
