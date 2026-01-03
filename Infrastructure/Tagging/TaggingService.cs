using backend_api.Domain.Common;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Data;
using BackendApi.Infrastructure.Tagging.Rules;
using MongoDB.Driver;
using System.Diagnostics;

namespace BackendApi.Infrastructure.Tagging;

/// <summary>
/// Production implementation of tagging service with comprehensive rule execution and backfill support.
/// </summary>
public sealed class TaggingService : ITaggingService
{
    private readonly ILogger<TaggingService> _logger;
    private readonly MongoDbContext _dbContext;
    private readonly IReadOnlyList<ITaggingRule> _rules;
    private readonly IMongoCollection<TagDefinition>? _tagCatalog;

    public TaggingService(
        ILogger<TaggingService> logger,
        MongoDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;

        // Initialize all tagging rules
        _rules = new List<ITaggingRule>
        {
            // Priority 1-10: Fundamental classification
            new PatientPhiRule(),
            new ContactPiiRule(),
            new AncillaryPiiRule(),

            // Priority 10-20: Business classification
            new AncillaryBusinessTypeRule(),
            new LifecycleStageRule(),
            new OwnershipRule(),

            // Priority 20-50: Derived & contextual
            new PatientStatusRule(),

            // Priority 50+: Quality & validation
            new DataQualityRule()
        }
        .OrderBy(r => r.Priority)
        .ToList();

        try
        {
            _tagCatalog = _dbContext.Database.GetCollection<TagDefinition>("tag_catalog");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize tag catalog collection");
        }

        _logger.LogInformation(
            "TaggingService initialized with {RuleCount} rules: {RuleNames}",
            _rules.Count,
            string.Join(", ", _rules.Select(r => r.Name)));
    }

    public async Task<TaggingResult> EvaluateAsync(
        object entity,
        string operation = "evaluate",
        string? performedBy = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var context = CreateContext(entity, operation, performedBy);
            var results = new List<TaggingResult>();

            // Execute all applicable rules
            foreach (var rule in _rules.Where(r => r.IsEnabled && r.AppliesTo(context)))
            {
                try
                {
                    var ruleResult = await rule.EvaluateAsync(context, cancellationToken);
                    results.Add(ruleResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rule {RuleName} failed during evaluation", rule.Name);
                    results.Add(TaggingResult.Failed($"Rule '{rule.Name}' threw exception: {ex.Message}"));
                }
            }

            // Merge all results
            var mergedResult = TaggingResult.Merge(results.ToArray());

            // Deduplicate tags by identifier
            var uniqueTags = mergedResult.Tags
                .GroupBy(t => t.ToIdentifier())
                .Select(g => g.OrderByDescending(t => t.Confidence ?? 1.0).First())
                .ToList();

            sw.Stop();

            _logger.LogInformation(
                "Evaluated {EntityType} with {RuleCount} rules in {Duration}ms, produced {TagCount} unique tags",
                context.EntityType,
                mergedResult.ExecutedRules.Count,
                sw.ElapsedMilliseconds,
                uniqueTags.Count);

            return mergedResult with
            {
                Tags = uniqueTags,
                Diagnostics = mergedResult.Diagnostics.Append(
                    $"Evaluation completed in {sw.ElapsedMilliseconds}ms").ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TaggingService.EvaluateAsync failed");
            return TaggingResult.Failed($"Tagging evaluation failed: {ex.Message}");
        }
    }

    public async Task<T> ApplyTagsAsync<T>(
        T entity,
        string operation = "apply",
        string? performedBy = null,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
        where T : class
    {
        // Evaluate to get new tags
        var result = await EvaluateAsync(entity, operation, performedBy, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Tag evaluation failed for entity {EntityType}, errors: {Errors}",
                entity.GetType().Name,
                string.Join("; ", result.Errors));
            return entity;
        }

        if (dryRun)
        {
            _logger.LogInformation(
                "DRY RUN: Would apply {TagCount} tags to {EntityType}",
                result.Tags.Count,
                entity.GetType().Name);
            return entity;
        }

        // Get existing tags
        var existingTags = GetExistingTags(entity);

        // Merge: keep existing tags that aren't expired, add new tags
        var validExistingTags = existingTags
            .Where(t => t.IsValid())
            .ToList();

        // Remove tags that are being replaced by new versions
        var existingIdentifiers = validExistingTags.Select(t => t.ToIdentifier()).ToHashSet();
        var newIdentifiers = result.Tags.Select(t => t.ToIdentifier()).ToHashSet();

        // Keep existing tags that aren't being updated
        var tagsToKeep = validExistingTags
            .Where(t => !newIdentifiers.Contains(t.ToIdentifier()))
            .ToList();

        // Combine with new tags
        var finalTags = tagsToKeep.Concat(result.Tags).ToList();

        // Update entity with new tags
        var updatedEntity = SetTags(entity, finalTags);

        _logger.LogInformation(
            "Applied {NewTagCount} new tags to {EntityType}, total tags: {TotalCount}",
            result.Tags.Count,
            entity.GetType().Name,
            finalTags.Count);

        return updatedEntity;
    }

    public async Task<BackfillResult> BackfillCollectionAsync(
        string collectionName,
        bool dryRun = true,
        int batchSize = 100,
        string? performedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BackfillResult
        {
            CollectionName = collectionName,
            IsDryRun = dryRun,
            StartedAt = DateTime.UtcNow
        };

        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting backfill for collection '{Collection}' (DryRun={DryRun}, BatchSize={BatchSize})",
                collectionName,
                dryRun,
                batchSize);

            var (totalCount, processedCount, tagStats) = collectionName.ToLowerInvariant() switch
            {
                "patients" => await BackfillEntitiesAsync<Patient>(
                    _dbContext.Patients,
                    dryRun,
                    batchSize,
                    performedBy,
                    cancellationToken),
                "contactusers" or "contacts" => await BackfillEntitiesAsync<ContactUser>(
                    _dbContext.ContactUsers,
                    dryRun,
                    batchSize,
                    performedBy,
                    cancellationToken),
                "ancillaryusers" or "ancillaries" => await BackfillEntitiesAsync<AncillaryUser>(
                    _dbContext.AncillaryUsers,
                    dryRun,
                    batchSize,
                    performedBy,
                    cancellationToken),
                _ => throw new ArgumentException($"Unknown collection: {collectionName}", nameof(collectionName))
            };

            sw.Stop();

            result = result with
            {
                TotalEntities = totalCount,
                ProcessedEntities = processedCount,
                SuccessfullyTagged = processedCount,
                TotalTagsAdded = tagStats.TotalTags,
                TagsByNamespace = tagStats.ByNamespace,
                TagsByName = tagStats.ByName,
                Duration = sw.Elapsed,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Backfill completed: {Processed}/{Total} entities, {TagCount} tags added in {Duration}ms",
                processedCount,
                totalCount,
                tagStats.TotalTags,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backfill failed for collection '{Collection}'", collectionName);
            result = result with
            {
                Errors = result.Errors.Append($"Backfill failed: {ex.Message}").ToList(),
                CompletedAt = DateTime.UtcNow,
                Duration = sw.Elapsed
            };
        }

        return result;
    }

    private async Task<(int Total, int Processed, TagStats Stats)> BackfillEntitiesAsync<T>(
        IMongoCollection<T> collection,
        bool dryRun,
        int batchSize,
        string? performedBy,
        CancellationToken cancellationToken)
        where T : class
    {
        var totalCount = (int)await collection.CountDocumentsAsync(
            FilterDefinition<T>.Empty,
            cancellationToken: cancellationToken);

        var processedCount = 0;
        var stats = new TagStats();

        var cursor = await collection.Find(FilterDefinition<T>.Empty)
            .ToCursorAsync(cancellationToken);

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            var batch = cursor.Current.ToList();

            foreach (var entity in batch)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var taggedEntity = await ApplyTagsAsync(
                        entity,
                        "backfill",
                        performedBy,
                        dryRun,
                        cancellationToken);

                    if (!dryRun)
                    {
                        // Update in database
                        var entityId = GetEntityId(taggedEntity);
                        if (entityId != null)
                        {
                            var filter = Builders<T>.Filter.Eq("_id", entityId);
                            await collection.ReplaceOneAsync(filter, taggedEntity, cancellationToken: cancellationToken);
                        }
                    }

                    // Collect statistics
                    var tags = GetExistingTags(taggedEntity);
                    foreach (var tag in tags)
                    {
                        stats.TotalTags++;
                        stats.ByNamespace.TryGetValue(tag.Namespace, out var nsCount);
                        stats.ByNamespace[tag.Namespace] = nsCount + 1;

                        stats.ByName.TryGetValue(tag.Name, out var nameCount);
                        stats.ByName[tag.Name] = nameCount + 1;
                    }

                    processedCount++;

                    if (processedCount % batchSize == 0)
                    {
                        _logger.LogInformation(
                            "Backfill progress: {Processed}/{Total} ({Percentage:F1}%)",
                            processedCount,
                            totalCount,
                            (processedCount * 100.0 / totalCount));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to tag entity in backfill");
                }
            }
        }

        return (totalCount, processedCount, stats);
    }

    public async Task<TagStatistics> GetTagStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new TagStatistics();

        try
        {
            // This would be a complex aggregation across all collections
            // For now, return empty statistics (can be implemented as needed)
            _logger.LogInformation("Getting tag statistics (not yet fully implemented)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tag statistics");
        }

        return stats;
    }

    public async Task<ValidationResult> ValidateTagsAsync(
        IEnumerable<Tag> tags,
        CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult { IsValid = true };

        if (_tagCatalog == null)
        {
            result = result with
            {
                Warnings = result.Warnings.Append("Tag catalog not available, skipping validation").ToList()
            };
            return result;
        }

        try
        {
            // Load catalog definitions
            var catalogTags = await _tagCatalog
                .Find(FilterDefinition<TagDefinition>.Empty)
                .ToListAsync(cancellationToken);

            var catalogLookup = catalogTags.ToDictionary(
                t => t.ToIdentifier(),
                StringComparer.OrdinalIgnoreCase);

            foreach (var tag in tags)
            {
                var identifier = tag.ToIdentifier();

                if (!catalogLookup.TryGetValue(identifier, out var definition))
                {
                    result = result with
                    {
                        IsValid = false,
                        UnknownTags = result.UnknownTags.Append(tag).ToList(),
                        Warnings = result.Warnings.Append($"Tag '{identifier}' not found in catalog").ToList()
                    };
                }
                else if (definition.IsDeprecated)
                {
                    result = result with
                    {
                        DeprecatedTags = result.DeprecatedTags.Append(tag).ToList(),
                        Warnings = result.Warnings.Append(
                            $"Tag '{identifier}' is deprecated. Use '{definition.ReplacedBy}' instead.").ToList()
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tag validation failed");
            result = result with
            {
                IsValid = false,
                Errors = result.Errors.Append($"Validation error: {ex.Message}").ToList()
            };
        }

        return result;
    }

    public IReadOnlyCollection<ITaggingRule> GetAvailableRules() => _rules.ToList();

    public Task<T> CleanupExpiredTagsAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class
    {
        var existingTags = GetExistingTags(entity);
        var validTags = existingTags.Where(t => t.IsValid()).ToList();

        if (validTags.Count < existingTags.Count)
        {
            _logger.LogInformation(
                "Removed {ExpiredCount} expired tags from {EntityType}",
                existingTags.Count - validTags.Count,
                entity.GetType().Name);
            return Task.FromResult(SetTags(entity, validTags));
        }

        return Task.FromResult(entity);
    }

    // Helper methods

    private TaggingContext CreateContext(object entity, string operation, string? performedBy)
    {
        var entityType = entity.GetType();
        var existingTags = GetExistingTags(entity);

        return new TaggingContext
        {
            Entity = entity,
            EntityType = entityType.FullName ?? entityType.Name,
            EntityId = GetEntityId(entity)?.ToString(),
            Operation = operation,
            PerformedBy = performedBy,
            ExistingTags = existingTags
        };
    }

    private static IReadOnlyCollection<Tag> GetExistingTags(object entity)
    {
        return entity switch
        {
            Patient p => p.Tags,
            ContactUser c => c.Tags,
            AncillaryUser a => a.Tags,
            _ => Array.Empty<Tag>()
        };
    }

    private static T SetTags<T>(T entity, IReadOnlyCollection<Tag> tags) where T : class
    {
        return entity switch
        {
            Patient p => (p with { Tags = tags }) as T ?? entity,
            ContactUser c => (c with { Tags = tags }) as T ?? entity,
            AncillaryUser a => (a with { Tags = tags }) as T ?? entity,
            _ => entity
        };
    }

    private static object? GetEntityId(object entity)
    {
        return entity switch
        {
            Patient p => p.Id,
            ContactUser c => c.Id,
            AncillaryUser a => a.Id,
            _ => null
        };
    }

    private sealed class TagStats
    {
        public int TotalTags { get; set; }
        public Dictionary<string, int> ByNamespace { get; } = new();
        public Dictionary<string, int> ByName { get; } = new();
    }
}
