using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class UpdateUserActiveStatusHandler : IRequestHandler<UpdateUserActiveStatusCommand, UpdateUserActiveStatusResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<UpdateUserActiveStatusHandler> _logger;

    public UpdateUserActiveStatusHandler(IStorage storage, ILogger<UpdateUserActiveStatusHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<UpdateUserActiveStatusResponse> Handle(UpdateUserActiveStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _storage.GetUserByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return new UpdateUserActiveStatusResponse
                {
                    Success = false,
                    Error = "User not found"
                };
            }

            // Admins should always be active
            if (user.Role == UserRole.Admin)
            {
                return new UpdateUserActiveStatusResponse
                {
                    Success = false,
                    Error = "Admin users cannot be deactivated"
                };
            }

            // Update the active status
            user.IsActive = request.IsActive;
            await _storage.UpdateUserAsync(user, cancellationToken);

            _logger.LogInformation("User {UserId} active status updated to {IsActive}", request.UserId, request.IsActive);

            return new UpdateUserActiveStatusResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user active status: {Error}", ex.Message);
            return new UpdateUserActiveStatusResponse
            {
                Success = false,
                Error = $"An error occurred: {ex.Message}"
            };
        }
    }
}
