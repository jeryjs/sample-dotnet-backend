using FastEndpoints;
using BackendApi.Infrastructure.Data;
using backend_api.Domain.Models;
using MongoDB.Driver;

namespace BackendApi.Endpoints.Admin;

/// <summary>
/// Endpoint to list all tag definitions in the catalog.
/// </summary>
public class ListTagDefinitionsEndpoint : Endpoint<ListTagDefinitionsRequest, ListTagDefinitionsResponse>
{
    private readonly MongoDbContext _dbContext;

    public ListTagDefinitionsEndpoint(MongoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/admin/tags/catalog");
        Policies("DefaultAccess");
        Options(x => x
            .WithTags("Admin", "Tags", "Catalog")
            .WithSummary("List tag definitions")
            .WithDescription("Returns all tag definitions from the catalog with optional filtering."));
    }

    public override async Task HandleAsync(ListTagDefinitionsRequest req, CancellationToken ct)
    {
        var filterBuilder = Builders<TagDefinition>.Filter;
        var filters = new List<FilterDefinition<TagDefinition>>();

        if (!string.IsNullOrWhiteSpace(req.Namespace))
        {
            filters.Add(filterBuilder.Eq(t => t.Namespace, req.Namespace));
        }

        if (!string.IsNullOrWhiteSpace(req.Category))
        {
            filters.Add(filterBuilder.Eq(t => t.Category, req.Category));
        }

        if (req.IsSensitive.HasValue)
        {
            filters.Add(filterBuilder.Eq(t => t.IsSensitive, req.IsSensitive.Value));
        }

        if (req.IsDeprecated.HasValue)
        {
            filters.Add(filterBuilder.Eq(t => t.IsDeprecated, req.IsDeprecated.Value));
        }

        var filter = filters.Count > 0
            ? filterBuilder.And(filters)
            : FilterDefinition<TagDefinition>.Empty;

        var tags = await _dbContext.TagDefinitions
            .Find(filter)
            .SortBy(t => t.Namespace)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);

        var response = new ListTagDefinitionsResponse
        {
            Total = tags.Count,
            Tags = tags
        };

        await SendAsync(response, cancellation: ct);
    }
}

public class ListTagDefinitionsRequest
{
    public string? Namespace { get; init; }
    public string? Category { get; init; }
    public bool? IsSensitive { get; init; }
    public bool? IsDeprecated { get; init; }
}

public class ListTagDefinitionsResponse
{
    public int Total { get; init; }
    public List<TagDefinition> Tags { get; init; } = new();
}
