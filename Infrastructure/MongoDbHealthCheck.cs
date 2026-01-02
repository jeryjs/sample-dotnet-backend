using BackendApi.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BackendApi.Infrastructure;

/// <summary>
/// Health check for MongoDB connection.
/// </summary>
public class MongoDbHealthCheck : IHealthCheck
{
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<MongoDbHealthCheck> _logger;

    public MongoDbHealthCheck(
        MongoDbContext dbContext,
        ILogger<MongoDbHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _dbContext.PingAsync(cancellationToken);
            
            if (!isConnected)
            {
                return HealthCheckResult.Unhealthy("MongoDB ping failed");
            }

            // Get collection counts
            var patientCount = await _dbContext.Patients.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<backend_api.Domain.Models.Patient>.Empty,
                cancellationToken: cancellationToken);

            var ancillaryCount = await _dbContext.AncillaryUsers.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<backend_api.Domain.Models.AncillaryUser>.Empty,
                cancellationToken: cancellationToken);

            var contactCount = await _dbContext.ContactUsers.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<backend_api.Domain.Models.ContactUser>.Empty,
                cancellationToken: cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "patients", patientCount },
                { "ancillaryUsers", ancillaryCount },
                { "contactUsers", contactCount },
                { "database", _dbContext.Database.DatabaseNamespace.DatabaseName }
            };

            return HealthCheckResult.Healthy("MongoDB is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB health check failed");
            return HealthCheckResult.Unhealthy("MongoDB health check failed", ex);
        }
    }
}
