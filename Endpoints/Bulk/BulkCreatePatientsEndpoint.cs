using FastEndpoints;
using backend_api.Domain.Models;

namespace BackendApi.Endpoints.Bulk;

/// <summary>
/// Request model for bulk creating patients.
/// </summary>
public class BulkCreatePatientsRequest
{
    /// <summary>
    /// Array of patient data to create.
    /// </summary>
    public required List<PatientDto> Patients { get; set; }
}

/// <summary>
/// DTO for patient creation in bulk operations.
/// </summary>
public class PatientDto
{
    /// <summary>
    /// Unique identifier for the patient.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Indicates whether the patient is billable.
    /// </summary>
    public bool? IsBillable { get; set; }

    /// <summary>
    /// Indicates whether the patient is PG billable.
    /// </summary>
    public bool? IsPgBillable { get; set; }

    /// <summary>
    /// Indicates whether the patient is eligible.
    /// </summary>
    public bool? IsEligible { get; set; }

    /// <summary>
    /// Indicates whether the patient is PG eligible.
    /// </summary>
    public bool? IsPgEligible { get; set; }

    /// <summary>
    /// Agency information for the patient.
    /// </summary>
    public required AgencyInfo AgencyInfo { get; set; }
}

/// <summary>
/// Response model for bulk patient creation.
/// </summary>
public class BulkCreatePatientsResponse
{
    /// <summary>
    /// Indicates whether the bulk operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result of the operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Number of patients successfully created.
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// Number of patients that failed to create.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// List of errors that occurred during bulk creation.
    /// </summary>
    public List<BulkOperationError> Errors { get; set; } = new();
}

/// <summary>
/// Represents an error that occurred during a bulk operation.
/// </summary>
public class BulkOperationError
{
    /// <summary>
    /// Index of the item in the array that caused the error.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Identifier of the item that caused the error (if available).
    /// </summary>
    public string? ItemId { get; set; }

    /// <summary>
    /// Error message describing what went wrong.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to bulk create patients.
/// </summary>
public class BulkCreatePatientsEndpoint : Endpoint<BulkCreatePatientsRequest, BulkCreatePatientsResponse>
{
    private readonly ILogger<BulkCreatePatientsEndpoint> _logger;

    public BulkCreatePatientsEndpoint(ILogger<BulkCreatePatientsEndpoint> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/bulk/patients");
        Policies("WriteAccess");
        Options(x => x
            .WithTags("Bulk Operations")
            .WithSummary("Bulk create patients")
            .WithDescription("Creates multiple patients in a single operation. Phase 1: Returns 501 Not Implemented. This endpoint will be implemented in future phases to support batch patient creation.")
            .Produces<BulkCreatePatientsResponse>(201, "application/json")
            .Produces<BulkCreatePatientsResponse>(400, "application/json")
            .Produces<BulkCreatePatientsResponse>(501, "application/json")
            .Produces<BulkCreatePatientsResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(BulkCreatePatientsRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Bulk patient creation attempted with {Count} patients", req.Patients?.Count ?? 0);

            // Validate request
            if (req.Patients == null || req.Patients.Count == 0)
            {
                var validationResponse = new BulkCreatePatientsResponse
                {
                    Success = false,
                    Message = "Request must contain at least one patient.",
                    CreatedCount = 0,
                    ErrorCount = 0
                };

                await SendAsync(validationResponse, 400, ct);
                return;
            }

            // Phase 1: Not implemented yet
            var response = new BulkCreatePatientsResponse
            {
                Success = false,
                Message = "Bulk patient creation not yet implemented. Repository is currently read-only. This feature will be available in future phases.",
                CreatedCount = 0,
                ErrorCount = 0
            };

            _logger.LogWarning("Bulk patient creation attempted but not yet implemented (Phase 1). Request contained {Count} patients", req.Patients.Count);

            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during bulk patient creation attempt");

            var response = new BulkCreatePatientsResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}",
                CreatedCount = 0,
                ErrorCount = 0
            };

            await SendAsync(response, 500, ct);
        }
    }
}
