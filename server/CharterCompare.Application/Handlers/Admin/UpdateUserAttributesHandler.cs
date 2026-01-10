using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class UpdateUserAttributesHandler : IRequestHandler<UpdateUserAttributesCommand, UpdateUserAttributesResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<UpdateUserAttributesHandler> _logger;

    public UpdateUserAttributesHandler(IStorage storage, ILogger<UpdateUserAttributesHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<UpdateUserAttributesResponse> Handle(UpdateUserAttributesCommand request, CancellationToken cancellationToken)
    {
        var user = await _storage.GetUserByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
        {
            return new UpdateUserAttributesResponse
            {
                Success = false,
                Error = "User not found"
            };
        }

        // Validate mutually exclusive attributes: Individual and Business
        var hasIndividual = request.Attributes.Contains(UserAttributeType.Individual);
        var hasBusiness = request.Attributes.Contains(UserAttributeType.Business);
        
        if (hasIndividual && hasBusiness)
        {
            return new UpdateUserAttributesResponse
            {
                Success = false,
                Error = "Individual and Business attributes are mutually exclusive"
            };
        }

        // Validate operator attributes (Bus, Airplane, Train, Boat) - only for operators/admins
        var operatorAttributes = new[] { UserAttributeType.Bus, UserAttributeType.Airplane, UserAttributeType.Train, UserAttributeType.Boat };
        var hasOperatorAttributes = request.Attributes.Any(a => operatorAttributes.Contains(a));
        
        if (hasOperatorAttributes && user.Role == UserRole.Requester)
        {
            return new UpdateUserAttributesResponse
            {
                Success = false,
                Error = "Operator attributes (Bus, Airplane, Train, Boat) can only be assigned to operators or admins"
            };
        }

        // Validate requester attributes (Individual, Business) - only for requesters
        var requesterAttributes = new[] { UserAttributeType.Individual, UserAttributeType.Business };
        var hasRequesterAttributes = request.Attributes.Any(a => requesterAttributes.Contains(a));
        
        if (hasRequesterAttributes && (user.Role == UserRole.Operator || user.Role == UserRole.Admin))
        {
            return new UpdateUserAttributesResponse
            {
                Success = false,
                Error = "Requester attributes (Individual, Business) can only be assigned to requesters"
            };
        }

        // Update attributes
        await _storage.SetUserAttributesAsync(request.UserId, request.Attributes, cancellationToken);

        // If Business attribute is set, update CompanyName if provided
        if (hasBusiness && !string.IsNullOrEmpty(request.CompanyName))
        {
            user.CompanyName = request.CompanyName;
            await _storage.UpdateUserAsync(user, cancellationToken);
        }
        // If Individual attribute is set, clear CompanyName
        else if (hasIndividual)
        {
            user.CompanyName = null;
            await _storage.UpdateUserAsync(user, cancellationToken);
        }

        _logger.LogInformation("Admin updated attributes for user {UserId} to: {Attributes}", 
            request.UserId, string.Join(", ", request.Attributes));

        return new UpdateUserAttributesResponse
        {
            Success = true
        };
    }
}
