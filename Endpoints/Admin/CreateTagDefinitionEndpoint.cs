using FastEndpoints;
using BackendApi.Infrastructure.Data;
using backend_api.Domain.Models;
using MongoDB.Driver;

namespace BackendApi.Endpoints.Admin;

/// <summary>
/// Endpoint to create a new tag definition in the catalog.
/// </summary>
public class CreateTagDefinitionEndpoint : Endpoint<CreateTagDefinitionRequest, TagDefinition>
{
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<CreateTagDefinitionEndpoint> _logger;

    public CreateTagDefinitionEndpoint(
        MongoDbContext dbContext,
        ILogger<CreateTagDefinitionEndpoint> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/admin/tags/catalog");
        Policies("AdminOnly");
        Options(x => x
            .WithTags("Admin", "Tags", "Catalog")
            .WithSummary("Create tag definition")
            .WithDescription("Creates a new tag definition in the catalog."));
    }

    public override async Task HandleAsync(CreateTagDefinitionRequest req, CancellationToken ct)
    {
        try
        {
            var tagDef = new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Namespace = req.Namespace,
                Name = req.Name,
                DisplayName = req.DisplayName,
                Description = req.Description,
                Category = req.Category,
                IsSensitive = req.IsSensitive,
                AllowedRoles = req.AllowedRoles ?? Array.Empty<string>(),
                AllowedTaggers = req.AllowedTaggers ?? Array.Empty<string>(),
                IsDeprecated = false,
                IsAutomatic = req.IsAutomatic,
                IsMutable = req.IsMutable,
                ValuePattern = req.ValuePattern,
                ExampleValues = req.ExampleValues,
                Icon = req.Icon,
                Color = req.Color,
                RetentionDays = req.RetentionDays,
                RelatedTags = req.RelatedTags ?? Array.Empty<string>(),
                DocumentationUrl = req.DocumentationUrl,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            await _dbContext.TagDefinitions.InsertOneAsync(tagDef, cancellationToken: ct);

            _logger.LogInformation(
                "Created tag definition: {Namespace}:{Name} by {User}",
                tagDef.Namespace,
                tagDef.Name,
                User.Identity?.Name);

            await SendAsync(tagDef, 201, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            await SendErrorsAsync(409, ct);
            AddError($"Tag definition '{req.Namespace}:{req.Name}' already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tag definition");
            await SendErrorsAsync(500, ct);
            AddError($"Failed to create tag definition: {ex.Message}");
        }
    }
}

public class CreateTagDefinitionRequest
{
    public required string Namespace { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public bool IsSensitive { get; init; }
    public IReadOnlyCollection<string>? AllowedRoles { get; init; }
    public IReadOnlyCollection<string>? AllowedTaggers { get; init; }
    public bool IsAutomatic { get; init; }
    public bool IsMutable { get; init; } = true;
    public string? ValuePattern { get; init; }
    public IReadOnlyCollection<string>? ExampleValues { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public int? RetentionDays { get; init; }
    public IReadOnlyCollection<string>? RelatedTags { get; init; }
    public string? DocumentationUrl { get; init; }
}
