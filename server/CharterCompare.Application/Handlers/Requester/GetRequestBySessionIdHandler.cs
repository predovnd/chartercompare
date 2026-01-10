using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Requester;
using CharterCompare.Application.Storage;
using CharterCompare.Application.Requests.Provider;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Requester;

public class GetRequestBySessionIdHandler : IRequestHandler<GetRequestBySessionIdQuery, GetRequestBySessionIdResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<GetRequestBySessionIdHandler> _logger;

    public GetRequestBySessionIdHandler(IStorage storage, ILogger<GetRequestBySessionIdHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<GetRequestBySessionIdResponse> Handle(GetRequestBySessionIdQuery request, CancellationToken cancellationToken)
    {
        var allRequests = await _storage.GetAllCharterRequestsAsync(cancellationToken);
        var charterRequest = allRequests.FirstOrDefault(r => r.SessionId == request.SessionId);

        if (charterRequest == null)
        {
            return new GetRequestBySessionIdResponse();
        }

        var hoursRemaining = 0;
        var isDeadlinePassed = false;
        if (charterRequest.QuoteDeadline.HasValue)
        {
            var timeRemaining = charterRequest.QuoteDeadline.Value - DateTime.UtcNow;
            hoursRemaining = Math.Max(0, (int)timeRemaining.TotalHours);
            isDeadlinePassed = timeRemaining <= TimeSpan.Zero;
        }

        var requestDto = new RequestBySessionIdDto
        {
            Id = charterRequest.Id,
            SessionId = charterRequest.SessionId,
            RequestData = MapCharterRequest(charterRequest.RequestData),
            Status = charterRequest.Status.ToString(),
            CreatedAt = charterRequest.CreatedAt,
            QuoteDeadline = charterRequest.QuoteDeadline,
            QuoteCount = charterRequest.Quotes.Count,
            IsDeadlinePassed = isDeadlinePassed,
            HoursRemaining = hoursRemaining,
            Quotes = charterRequest.Quotes.Select(q => new RequesterQuoteDto
            {
                Id = q.Id,
                ProviderName = q.Provider?.Name ?? "Unknown",
                Price = q.Price,
                Currency = q.Currency,
                Notes = q.Notes,
                Status = q.Status.ToString(),
                CreatedAt = q.CreatedAt
            }).OrderBy(q => q.Price).ToList()
        };

        return new GetRequestBySessionIdResponse
        {
            Request = requestDto
        };
    }

    private Requests.Provider.CharterRequestDto MapCharterRequest(Domain.Entities.CharterRequest request)
    {
        return new Requests.Provider.CharterRequestDto
        {
            Customer = new Requests.Provider.CustomerInfoDto
            {
                FirstName = request.Customer.FirstName,
                LastName = request.Customer.LastName,
                Phone = request.Customer.Phone,
                Email = request.Customer.Email
            },
            Trip = new Requests.Provider.TripInfoDto
            {
                Type = request.Trip.Type,
                PassengerCount = request.Trip.PassengerCount,
                Date = new Requests.Provider.DateInfoDto
                {
                    RawInput = request.Trip.Date.RawInput,
                    ResolvedDate = request.Trip.Date.ResolvedDate,
                    Confidence = request.Trip.Date.Confidence
                },
                PickupLocation = new Requests.Provider.LocationInfoDto
                {
                    RawInput = request.Trip.PickupLocation.RawInput,
                    ResolvedName = request.Trip.PickupLocation.ResolvedName,
                    Suburb = request.Trip.PickupLocation.Suburb,
                    State = request.Trip.PickupLocation.State,
                    Lat = request.Trip.PickupLocation.Lat,
                    Lng = request.Trip.PickupLocation.Lng,
                    Confidence = request.Trip.PickupLocation.Confidence
                },
                Destination = new Requests.Provider.LocationInfoDto
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
                Timing = new Requests.Provider.TimingInfoDto
                {
                    RawInput = request.Trip.Timing.RawInput,
                    PickupTime = request.Trip.Timing.PickupTime,
                    ReturnTime = request.Trip.Timing.ReturnTime
                },
                SpecialRequirements = request.Trip.SpecialRequirements
            },
            Meta = new Requests.Provider.RequestMetaDto
            {
                Source = request.Meta.Source,
                CreatedAt = request.Meta.CreatedAt
            }
        };
    }
}
