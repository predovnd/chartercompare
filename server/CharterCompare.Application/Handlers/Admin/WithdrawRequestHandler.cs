using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using CharterCompare.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class WithdrawRequestHandler : IRequestHandler<WithdrawRequestCommand, WithdrawRequestResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<WithdrawRequestHandler> _logger;

    public WithdrawRequestHandler(IStorage storage, ILogger<WithdrawRequestHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<WithdrawRequestResponse> Handle(WithdrawRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var charterRequest = await _storage.GetCharterRequestByIdAsync(request.RequestId, cancellationToken);
            if (charterRequest == null)
            {
                return new WithdrawRequestResponse
                {
                    Success = false,
                    Error = "Request not found"
                };
            }

            // Check if request can be withdrawn (not already completed or cancelled)
            if (charterRequest.Status == RequestStatus.Completed)
            {
                return new WithdrawRequestResponse
                {
                    Success = false,
                    Error = "Cannot withdraw a completed request"
                };
            }

            if (charterRequest.Status == RequestStatus.Cancelled)
            {
                return new WithdrawRequestResponse
                {
                    Success = false,
                    Error = "Request is already withdrawn/cancelled"
                };
            }

            // Change status to Cancelled
            charterRequest.Status = RequestStatus.Cancelled;
            await _storage.UpdateCharterRequestAsync(charterRequest, cancellationToken);

            _logger.LogInformation("Request {RequestId} withdrawn by admin", request.RequestId);

            return new WithdrawRequestResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error withdrawing request: {Error}", ex.Message);
            return new WithdrawRequestResponse
            {
                Success = false,
                Error = $"An error occurred: {ex.Message}"
            };
        }
    }
}
