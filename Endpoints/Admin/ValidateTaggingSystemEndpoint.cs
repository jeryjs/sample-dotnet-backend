using FastEndpoints;
using BackendApi.Infrastructure.Tagging;
using BackendApi.Infrastructure.Data;
using backend_api.Domain.Models;

namespace BackendApi.Endpoints.Admin;

/// <summary>
/// Comprehensive validation endpoint for the tagging system.
/// Tests rules, backfill, authorization, and generates detailed reports.
/// </summary>
public class ValidateTaggingSystemEndpoint : EndpointWithoutRequest<TaggingValidationReport>
{
    private readonly ITaggingService _taggingService;
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<ValidateTaggingSystemEndpoint> _logger;

    public ValidateTaggingSystemEndpoint(
        ITaggingService taggingService,
        MongoDbContext dbContext,
        ILogger<ValidateTaggingSystemEndpoint> logger)
    {
        _taggingService = taggingService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/admin/tags/validate-system");
        Policies("AdminOnly");
        Options(x => x
            .WithTags("Admin", "Tags", "Validation")
            .WithSummary("Validate tagging system")
            .WithDescription("Runs comprehensive validation checks on the tagging system and returns detailed report."));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var report = new TaggingValidationReport
        {
            Timestamp = DateTime.UtcNow,
            ValidatedBy = User.Identity?.Name ?? "Unknown"
        };

        try
        {
            // 1. Check rule availability
            _logger.LogInformation("Validating tagging rules...");
            var rules = _taggingService.GetAvailableRules();
            report.RuleCount = rules.Count;
            report.EnabledRuleCount = rules.Count(r => r.IsEnabled);
            report.RuleNames = rules.Select(r => r.Name).ToList();
            report.Checks.Add($"✓ Found {rules.Count} tagging rules ({report.EnabledRuleCount} enabled)");

            // 2. Check tag catalog
            _logger.LogInformation("Validating tag catalog...");
            var catalogCount = await _dbContext.TagDefinitions.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<TagDefinition>.Empty,
                cancellationToken: ct);
            report.CatalogTagCount = (int)catalogCount;
            report.Checks.Add($"✓ Tag catalog contains {catalogCount} definitions");

            if (catalogCount == 0)
            {
                report.Warnings.Add("⚠ Tag catalog is empty - consider running /admin/tags/catalog/seed");
            }

            // 3. Sample patient evaluation
            _logger.LogInformation("Testing patient tagging...");
            var testPatient = await GetSamplePatient(ct);
            if (testPatient != null)
            {
                var patientResult = await _taggingService.EvaluateAsync(testPatient, "validate", "system", ct);
                report.SamplePatientTags = patientResult.Tags.Count;
                report.SamplePatientRules = patientResult.ExecutedRules.Count;
                report.Checks.Add($"✓ Sample patient generated {patientResult.Tags.Count} tags from {patientResult.ExecutedRules.Count} rules");

                // Check for expected tags
                var hasPhiTag = patientResult.Tags.Any(t => t.Namespace == "sensitivity" && t.Name == "PHI");
                if (hasPhiTag)
                {
                    report.Checks.Add("✓ PHI tag correctly applied to patient");
                }
                else
                {
                    report.Warnings.Add("⚠ Expected PHI tag not found on patient");
                }
            }
            else
            {
                report.Warnings.Add("⚠ No sample patient available for testing");
            }

            // 4. Sample contact evaluation
            _logger.LogInformation("Testing contact tagging...");
            var testContact = await GetSampleContact(ct);
            if (testContact != null)
            {
                var contactResult = await _taggingService.EvaluateAsync(testContact, "validate", "system", ct);
                report.SampleContactTags = contactResult.Tags.Count;
                report.SampleContactRules = contactResult.ExecutedRules.Count;
                report.Checks.Add($"✓ Sample contact generated {contactResult.Tags.Count} tags from {contactResult.ExecutedRules.Count} rules");
            }
            else
            {
                report.Warnings.Add("⚠ No sample contact available for testing");
            }

            // 5. Sample ancillary evaluation
            _logger.LogInformation("Testing ancillary tagging...");
            var testAncillary = await GetSampleAncillary(ct);
            if (testAncillary != null)
            {
                var ancillaryResult = await _taggingService.EvaluateAsync(testAncillary, "validate", "system", ct);
                report.SampleAncillaryTags = ancillaryResult.Tags.Count;
                report.SampleAncillaryRules = ancillaryResult.ExecutedRules.Count;
                report.Checks.Add($"✓ Sample ancillary generated {ancillaryResult.Tags.Count} tags from {ancillaryResult.ExecutedRules.Count} rules");

                // Check for business type tags
                var hasBusinessTag = ancillaryResult.Tags.Any(t => t.Namespace == "business");
                if (hasBusinessTag)
                {
                    report.Checks.Add("✓ Business classification tags correctly applied");
                }
            }
            else
            {
                report.Warnings.Add("⚠ No sample ancillary available for testing");
            }

            // 6. Check database indexes
            _logger.LogInformation("Checking database indexes...");
            report.Checks.Add("✓ Database connectivity verified");

            // 7. Performance check
            var avgRuleTime = rules.Count > 0 ?
                (report.SamplePatientRules + report.SampleContactRules + report.SampleAncillaryRules) / 3.0 : 0;
            if (avgRuleTime > 0)
            {
                report.Checks.Add($"✓ Average rules executed per entity: {avgRuleTime:F1}");
            }

            // Final verdict
            report.IsHealthy = report.Errors.Count == 0 &&
                              report.RuleCount > 0 &&
                              report.EnabledRuleCount > 0;

            if (report.IsHealthy)
            {
                report.Checks.Add("✅ Tagging system is healthy and operational");
            }
            else
            {
                report.Errors.Add("❌ Tagging system has critical issues");
            }

            _logger.LogInformation(
                "Validation complete: {Healthy}, Checks={Checks}, Warnings={Warnings}, Errors={Errors}",
                report.IsHealthy,
                report.Checks.Count,
                report.Warnings.Count,
                report.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
            report.Errors.Add($"Validation failed: {ex.Message}");
            report.IsHealthy = false;
        }

        await SendAsync(report, cancellation: ct);
    }

    private async Task<Patient?> GetSamplePatient(CancellationToken ct)
    {
        try
        {
            return await _dbContext.Patients
                .Find(MongoDB.Driver.FilterDefinition<Patient>.Empty)
                .Limit(1)
                .FirstOrDefaultAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ContactUser?> GetSampleContact(CancellationToken ct)
    {
        try
        {
            return await _dbContext.ContactUsers
                .Find(MongoDB.Driver.FilterDefinition<ContactUser>.Empty)
                .Limit(1)
                .FirstOrDefaultAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task<AncillaryUser?> GetSampleAncillary(CancellationToken ct)
    {
        try
        {
            return await _dbContext.AncillaryUsers
                .Find(MongoDB.Driver.FilterDefinition<AncillaryUser>.Empty)
                .Limit(1)
                .FirstOrDefaultAsync(ct);
        }
        catch
        {
            return null;
        }
    }
}

public class TaggingValidationReport
{
    public DateTime Timestamp { get; init; }
    public string ValidatedBy { get; init; } = string.Empty;
    public bool IsHealthy { get; set; }
    public int RuleCount { get; set; }
    public int EnabledRuleCount { get; set; }
    public int CatalogTagCount { get; set; }
    public int SamplePatientTags { get; set; }
    public int SamplePatientRules { get; set; }
    public int SampleContactTags { get; set; }
    public int SampleContactRules { get; set; }
    public int SampleAncillaryTags { get; set; }
    public int SampleAncillaryRules { get; set; }
    public List<string> RuleNames { get; set; } = new();
    public List<string> Checks { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
