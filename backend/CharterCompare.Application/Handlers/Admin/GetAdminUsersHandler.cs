using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CharterCompare.Application.Handlers.Admin;

public class GetAdminUsersHandler : IRequestHandler<GetAdminUsersQuery, GetAdminUsersResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<GetAdminUsersHandler> _logger;

    public GetAdminUsersHandler(IStorage storage, ILogger<GetAdminUsersHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<GetAdminUsersResponse> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all users for admin dashboard...");
            var allUsers = await _storage.GetAllUsersAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} users from database", allUsers.Count);
            
            var allRequests = await _storage.GetAllCharterRequestsAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} requests from database", allRequests.Count);
            
            var userDtos = allUsers.Select(u => 
            {
                var coverage = u.Role == Domain.Enums.UserRole.Operator && u.OperatorCoverages?.FirstOrDefault() != null
                    ? new OperatorCoverageDto
                    {
                        Id = u.OperatorCoverages!.First().Id,
                        OperatorId = u.Id,
                        BaseLocationName = u.OperatorCoverages.First().BaseLocationName,
                        Latitude = u.OperatorCoverages.First().Latitude,
                        Longitude = u.OperatorCoverages.First().Longitude,
                        CoverageRadiusKm = u.OperatorCoverages.First().CoverageRadiusKm,
                        MinPassengerCapacity = u.OperatorCoverages.First().MinPassengerCapacity,
                        MaxPassengerCapacity = u.OperatorCoverages.First().MaxPassengerCapacity,
                        IsGeocoded = u.OperatorCoverages.First().IsGeocoded,
                        GeocodingError = u.OperatorCoverages.First().GeocodingError,
                        CreatedAt = u.OperatorCoverages.First().CreatedAt
                    }
                    : null;

                return new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Name = u.Name,
                    CompanyName = u.CompanyName,
                    Phone = u.Phone,
                    ExternalProvider = u.ExternalProvider,
                    IsAdmin = u.IsAdmin,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    QuoteCount = u.Quotes?.Count ?? 0,
                    RequestCount = u.Role == Domain.Enums.UserRole.Requester 
                        ? (u.Requests?.Count ?? 0)
                        : allRequests.Count(r => r.Quotes?.Any(q => q.ProviderId == u.Id) ?? false),
                    UserType = u.Role == Domain.Enums.UserRole.Admin ? "admin" :
                               u.Role == Domain.Enums.UserRole.Operator ? "operator" : "requester",
                    Attributes = u.Attributes?.Select(a => a.AttributeType.ToString()).ToList() ?? new List<string>(),
                    Coverage = coverage
                };
            }).OrderBy(u => u.CreatedAt).ToList();

            _logger.LogInformation("Returning {Count} user DTOs", userDtos.Count);
            return new GetAdminUsersResponse
            {
                Users = userDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for admin dashboard: {Error}", ex.Message);
            throw;
        }
    }
}
