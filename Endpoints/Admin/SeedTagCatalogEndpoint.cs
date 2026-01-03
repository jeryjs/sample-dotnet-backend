using FastEndpoints;
using BackendApi.Infrastructure.Data;
using backend_api.Domain.Models;
using MongoDB.Driver;

namespace BackendApi.Endpoints.Admin;

/// <summary>
/// Endpoint to seed the tag catalog with initial tag definitions.
/// Creates common tags for PHI, PII, business types, etc.
/// </summary>
public class SeedTagCatalogEndpoint : EndpointWithoutRequest<SeedCatalogResponse>
{
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<SeedTagCatalogEndpoint> _logger;

    public SeedTagCatalogEndpoint(
        MongoDbContext dbContext,
        ILogger<SeedTagCatalogEndpoint> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/admin/tags/catalog/seed");
        Policies("AdminOnly");
        Options(x => x
            .WithTags("Admin", "Tags", "Catalog")
            .WithSummary("Seed tag catalog")
            .WithDescription("Seeds the tag catalog with standard tag definitions for PHI, PII, business types, etc."));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var createdCount = 0;
        var skippedCount = 0;
        var errors = new List<string>();

        var standardTags = GetStandardTagDefinitions();

        foreach (var tagDef in standardTags)
        {
            try
            {
                await _dbContext.TagDefinitions.InsertOneAsync(tagDef, cancellationToken: ct);
                createdCount++;
                _logger.LogInformation("Created tag definition: {Identifier}", tagDef.ToIdentifier());
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                skippedCount++;
                _logger.LogDebug("Tag definition already exists: {Identifier}", tagDef.ToIdentifier());
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to create {tagDef.ToIdentifier()}: {ex.Message}");
                _logger.LogError(ex, "Failed to create tag definition: {Identifier}", tagDef.ToIdentifier());
            }
        }

        var response = new SeedCatalogResponse
        {
            TotalDefinitions = standardTags.Count,
            Created = createdCount,
            Skipped = skippedCount,
            Errors = errors
        };

        await SendAsync(response, cancellation: ct);
    }

    private List<TagDefinition> GetStandardTagDefinitions()
    {
        var createdBy = User.Identity?.Name ?? "system";
        var now = DateTime.UtcNow;

        return new List<TagDefinition>
        {
            // Sensitivity tags
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "sensitivity",
                Name = "PHI",
                DisplayName = "Protected Health Information",
                Description = "Contains protected health information as defined by HIPAA",
                Category = "Data Classification",
                IsSensitive = true,
                AllowedRoles = new[] { "Admin", "Clinician", "PHI-Reader" },
                IsAutomatic = true,
                IsMutable = false,
                Icon = "🏥",
                Color = "#DC2626",
                RetentionDays = 2555, // 7 years
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "sensitivity",
                Name = "PII",
                DisplayName = "Personally Identifiable Information",
                Description = "Contains personally identifiable information (names, emails, phone, etc.)",
                Category = "Data Classification",
                IsSensitive = true,
                AllowedRoles = new[] { "Admin", "User", "PII-Reader" },
                IsAutomatic = true,
                IsMutable = false,
                Icon = "👤",
                Color = "#EA580C",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "sensitivity",
                Name = "clinical-data",
                DisplayName = "Clinical Data",
                Description = "Contains clinical care management information",
                Category = "Data Classification",
                IsSensitive = true,
                AllowedRoles = new[] { "Admin", "Clinician" },
                IsAutomatic = true,
                IsMutable = false,
                Icon = "📋",
                Color = "#DC2626",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "sensitivity",
                Name = "diagnosis-data",
                DisplayName = "Diagnosis Data",
                Description = "Contains diagnosis and medical condition information",
                Category = "Data Classification",
                IsSensitive = true,
                AllowedRoles = new[] { "Admin", "Clinician" },
                IsAutomatic = true,
                IsMutable = false,
                Icon = "🩺",
                Color = "#DC2626",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },

            // Business classification tags
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "business",
                Name = "home-health",
                DisplayName = "Home Health Agency",
                Description = "Entity is a home health agency",
                Category = "Business Intelligence",
                IsSensitive = false,
                IsAutomatic = true,
                Icon = "🏠",
                Color = "#2563EB",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "business",
                Name = "hospice",
                DisplayName = "Hospice Provider",
                Description = "Entity is a hospice care provider",
                Category = "Business Intelligence",
                IsSensitive = false,
                IsAutomatic = true,
                Icon = "🕊️",
                Color = "#7C3AED",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "business",
                Name = "has-npi",
                DisplayName = "Has NPI Number",
                Description = "Entity has a valid National Provider Identifier",
                Category = "Business Intelligence",
                IsSensitive = false,
                IsAutomatic = true,
                Icon = "🔢",
                Color = "#059669",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },

            // Lifecycle tags
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "lifecycle",
                Name = "freemium",
                DisplayName = "Freemium Tier",
                Description = "Entity is on freemium service tier",
                Category = "Business Intelligence",
                IsSensitive = false,
                IsAutomatic = true,
                Icon = "💫",
                Color = "#10B981",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "lifecycle",
                Name = "premium",
                DisplayName = "Premium Tier",
                Description = "Entity is on premium service tier",
                Category = "Business Intelligence",
                IsSensitive = false,
                IsAutomatic = true,
                Icon = "⭐",
                Color = "#F59E0B",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },

            // Quality tags
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "quality",
                Name = "suspect",
                DisplayName = "Suspect Data Quality",
                Description = "Data quality issues detected (placeholders, test data, incomplete)",
                Category = "Data Quality",
                IsSensitive = false,
                IsAutomatic = true,
                Icon = "⚠️",
                Color = "#DC2626",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "quality",
                Name = "verified",
                DisplayName = "Verified Data",
                Description = "No obvious data quality issues detected",
                Category = "Data Quality",
                IsSensitive = false,
                IsAutomatic = true,
                Icon = "✅",
                Color = "#10B981",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },

            // Access/ownership tags
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "access",
                Name = "owner",
                DisplayName = "Owner Assignment",
                Description = "Identifies the owner/responsible party for this entity",
                Category = "Access Control",
                IsSensitive = false,
                IsAutomatic = true,
                ValuePattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
                ExampleValues = new[] { "user@example.com" },
                Icon = "👤",
                Color = "#6366F1",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            },

            // Retention tags
            new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = "retention",
                Name = "7y",
                DisplayName = "7 Year Retention",
                Description = "Must be retained for 7 years per HIPAA requirements",
                Category = "Compliance",
                IsSensitive = false,
                IsAutomatic = true,
                IsMutable = false,
                RetentionDays = 2555,
                Icon = "📅",
                Color = "#6366F1",
                CreatedBy = createdBy,
                CreatedAt = now,
                Version = 1
            }
        };
    }
}

public class SeedCatalogResponse
{
    public int TotalDefinitions { get; init; }
    public int Created { get; init; }
    public int Skipped { get; init; }
    public List<string> Errors { get; init; } = new();
}
