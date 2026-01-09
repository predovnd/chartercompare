using CharterCompare.Application.MediatR;
using CharterCompare.Application.Requests.Admin;
using CharterCompare.Application.Storage;
using Microsoft.Extensions.Logging;

namespace CharterCompare.Application.Handlers.Admin;

public class DeleteOperatorCoverageHandler : IRequestHandler<DeleteOperatorCoverageCommand, DeleteOperatorCoverageResponse>
{
    private readonly IStorage _storage;
    private readonly ILogger<DeleteOperatorCoverageHandler> _logger;

    public DeleteOperatorCoverageHandler(IStorage storage, ILogger<DeleteOperatorCoverageHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<DeleteOperatorCoverageResponse> Handle(DeleteOperatorCoverageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var coverage = await _storage.GetOperatorCoverageByIdAsync(request.CoverageId, cancellationToken);
            if (coverage == null)
            {
                return new DeleteOperatorCoverageResponse
                {
                    Success = false,
                    Error = "Coverage not found"
                };
            }

            await _storage.DeleteOperatorCoverageAsync(request.CoverageId, cancellationToken);
            _logger.LogInformation("Deleted coverage {CoverageId} for operator {OperatorId}", request.CoverageId, coverage.OperatorId);

            return new DeleteOperatorCoverageResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting operator coverage: {Error}", ex.Message);
            return new DeleteOperatorCoverageResponse
            {
                Success = false,
                Error = $"An error occurred: {ex.Message}"
            };
        }
    }
}
