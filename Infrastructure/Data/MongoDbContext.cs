using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using backend_api.Domain.Models;
using Microsoft.Extensions.Options;

namespace BackendApi.Infrastructure.Data;

/// <summary>
/// MongoDB database context for managing collections and connection.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbContext> _logger;
    private readonly MongoDbSettings _settings;

    static MongoDbContext()
    {
        // Configure MongoDB conventions
        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String)
        };
        ConventionRegistry.Register("CustomConventions", pack, t => true);

        // Configure serializers for specific types if needed
        try
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
        }
        catch (BsonSerializationException)
        {
            // Serializer already registered, ignore
        }
    }

    public MongoDbContext(
        IOptions<MongoDbSettings> settings,
        ILogger<MongoDbContext> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        try
        {
            var clientSettings = MongoClientSettings.FromConnectionString(_settings.ConnectionString);
            clientSettings.ConnectTimeout = TimeSpan.FromSeconds(_settings.ConnectionTimeoutSeconds);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(_settings.ServerSelectionTimeoutSeconds);
            clientSettings.MaxConnectionPoolSize = _settings.MaxConnectionPoolSize;
            clientSettings.MinConnectionPoolSize = _settings.MinConnectionPoolSize;
            clientSettings.RetryWrites = _settings.RetryWrites;
            clientSettings.RetryReads = _settings.RetryReads;

            // Add logging for MongoDB commands in development
            clientSettings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(e =>
                {
                    _logger.LogDebug("MongoDB Command: {CommandName} - {Command}", 
                        e.CommandName, e.Command.ToJson());
                });
            };

            var client = new MongoClient(clientSettings);
            _database = client.GetDatabase(_settings.DatabaseName);

            _logger.LogInformation(
                "MongoDB context initialized. Database: {DatabaseName}, Connection: {ConnectionString}",
                _settings.DatabaseName,
                MaskConnectionString(_settings.ConnectionString));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MongoDB context");
            throw;
        }
    }

    /// <summary>
    /// Gets the Patients collection.
    /// </summary>
    public IMongoCollection<Patient> Patients =>
        _database.GetCollection<Patient>(_settings.PatientsCollectionName);

    /// <summary>
    /// Gets the Ancillary Users collection.
    /// </summary>
    public IMongoCollection<AncillaryUser> AncillaryUsers =>
        _database.GetCollection<AncillaryUser>(_settings.AncillaryUsersCollectionName);

    /// <summary>
    /// Gets the Contact Users collection.
    /// </summary>
    public IMongoCollection<ContactUser> ContactUsers =>
        _database.GetCollection<ContactUser>(_settings.ContactUsersCollectionName);

    /// <summary>
    /// Gets the Tag Definitions collection for the tag catalog.
    /// </summary>
    public IMongoCollection<TagDefinition> TagDefinitions =>
        _database.GetCollection<TagDefinition>("tag_catalog");

    /// <summary>
    /// Gets the underlying MongoDB database.
    /// </summary>
    public IMongoDatabase Database => _database;

    /// <summary>
    /// Verifies database connectivity.
    /// </summary>
    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB ping failed");
            return false;
        }
    }

    /// <summary>
    /// Creates indexes for all collections to optimize queries.
    /// </summary>
    public async Task CreateIndexesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Patient indexes
            await Patients.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Patient>(
                    Builders<Patient>.IndexKeys.Ascending(p => p.AgencyInfo.PatientWAVId),
                    new CreateIndexOptions { Name = "idx_patient_wavid", Unique = false }),
                new CreateIndexModel<Patient>(
                    Builders<Patient>.IndexKeys.Ascending(p => p.AgencyInfo.PatientFName),
                    new CreateIndexOptions { Name = "idx_patient_fname" }),
                new CreateIndexModel<Patient>(
                    Builders<Patient>.IndexKeys.Ascending(p => p.AgencyInfo.PatientLName),
                    new CreateIndexOptions { Name = "idx_patient_lname" }),
                new CreateIndexModel<Patient>(
                    Builders<Patient>.IndexKeys.Combine(
                        Builders<Patient>.IndexKeys.Ascending(p => p.AgencyInfo.PatientFName),
                        Builders<Patient>.IndexKeys.Ascending(p => p.AgencyInfo.PatientLName)),
                    new CreateIndexOptions { Name = "idx_patient_fullname" }),
                // Tag indexes for patients
                new CreateIndexModel<Patient>(
                    Builders<Patient>.IndexKeys.Ascending("tags.namespace"),
                    new CreateIndexOptions { Name = "idx_patient_tags_namespace" }),
                new CreateIndexModel<Patient>(
                    Builders<Patient>.IndexKeys.Ascending("tags.name"),
                    new CreateIndexOptions { Name = "idx_patient_tags_name" }),
                new CreateIndexModel<Patient>(
                    Builders<Patient>.IndexKeys.Combine(
                        Builders<Patient>.IndexKeys.Ascending("tags.namespace"),
                        Builders<Patient>.IndexKeys.Ascending("tags.name")),
                    new CreateIndexOptions { Name = "idx_patient_tags_composite" })
            }, cancellationToken);

            // Ancillary User indexes
            await AncillaryUsers.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<AncillaryUser>(
                    Builders<AncillaryUser>.IndexKeys.Ascending(a => a.EntityWavId),
                    new CreateIndexOptions { Name = "idx_ancillary_wavid", Unique = true }),
                new CreateIndexModel<AncillaryUser>(
                    Builders<AncillaryUser>.IndexKeys.Ascending(a => a.Name),
                    new CreateIndexOptions { Name = "idx_ancillary_name" }),
                new CreateIndexModel<AncillaryUser>(
                    Builders<AncillaryUser>.IndexKeys.Ascending(a => a.EntityType),
                    new CreateIndexOptions { Name = "idx_ancillary_entitytype" }),
                new CreateIndexModel<AncillaryUser>(
                    Builders<AncillaryUser>.IndexKeys.Ascending(a => a.Email),
                    new CreateIndexOptions { Name = "idx_ancillary_email" }),
                // Tag indexes for ancillary users
                new CreateIndexModel<AncillaryUser>(
                    Builders<AncillaryUser>.IndexKeys.Ascending("tags.namespace"),
                    new CreateIndexOptions { Name = "idx_ancillary_tags_namespace" }),
                new CreateIndexModel<AncillaryUser>(
                    Builders<AncillaryUser>.IndexKeys.Ascending("tags.name"),
                    new CreateIndexOptions { Name = "idx_ancillary_tags_name" }),
                new CreateIndexModel<AncillaryUser>(
                    Builders<AncillaryUser>.IndexKeys.Combine(
                        Builders<AncillaryUser>.IndexKeys.Ascending("tags.namespace"),
                        Builders<AncillaryUser>.IndexKeys.Ascending("tags.name")),
                    new CreateIndexOptions { Name = "idx_ancillary_tags_composite" })
            }, cancellationToken);

            // Contact User indexes
            await ContactUsers.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<ContactUser>(
                    Builders<ContactUser>.IndexKeys.Ascending(c => c.ContactWavId),
                    new CreateIndexOptions { Name = "idx_contact_wavid", Unique = true }),
                new CreateIndexModel<ContactUser>(
                    Builders<ContactUser>.IndexKeys.Ascending(c => c.Email),
                    new CreateIndexOptions { Name = "idx_contact_email" }),
                new CreateIndexModel<ContactUser>(
                    Builders<ContactUser>.IndexKeys.Ascending(c => c.FirstName),
                    new CreateIndexOptions { Name = "idx_contact_fname" }),
                new CreateIndexModel<ContactUser>(
                    Builders<ContactUser>.IndexKeys.Ascending(c => c.LastName),
                    new CreateIndexOptions { Name = "idx_contact_lname" }),
                new CreateIndexModel<ContactUser>(
                    Builders<ContactUser>.IndexKeys.Ascending(c => c.IsActive),
                    new CreateIndexOptions { Name = "idx_contact_isactive" }),
                // Tag indexes for contact users
                new CreateIndexModel<ContactUser>(
                    Builders<ContactUser>.IndexKeys.Ascending("tags.namespace"),
                    new CreateIndexOptions { Name = "idx_contact_tags_namespace" }),
                new CreateIndexModel<ContactUser>(
                    Builders<ContactUser>.IndexKeys.Ascending("tags.name"),
                    new CreateIndexOptions { Name = "idx_contact_tags_name" }),
                new CreateIndexModel<ContactUser>(
                    Builders<ContactUser>.IndexKeys.Combine(
                        Builders<ContactUser>.IndexKeys.Ascending("tags.namespace"),
                        Builders<ContactUser>.IndexKeys.Ascending("tags.name")),
                    new CreateIndexOptions { Name = "idx_contact_tags_composite" })
            }, cancellationToken);

            // Tag Catalog indexes
            await TagDefinitions.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<TagDefinition>(
                    Builders<TagDefinition>.IndexKeys.Combine(
                        Builders<TagDefinition>.IndexKeys.Ascending(t => t.Namespace),
                        Builders<TagDefinition>.IndexKeys.Ascending(t => t.Name)),
                    new CreateIndexOptions { Name = "idx_tagdef_identifier", Unique = true }),
                new CreateIndexModel<TagDefinition>(
                    Builders<TagDefinition>.IndexKeys.Ascending(t => t.Category),
                    new CreateIndexOptions { Name = "idx_tagdef_category" }),
                new CreateIndexModel<TagDefinition>(
                    Builders<TagDefinition>.IndexKeys.Ascending(t => t.IsDeprecated),
                    new CreateIndexOptions { Name = "idx_tagdef_deprecated" })
            }, cancellationToken);

            _logger.LogInformation("MongoDB indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MongoDB indexes");
            throw;
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        // Mask password in connection string for logging
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        var parts = connectionString.Split('@');
        if (parts.Length <= 1)
            return connectionString;

        var credentials = parts[0].Split("://");
        if (credentials.Length <= 1)
            return connectionString;

        return $"{credentials[0]}://****@{parts[1]}";
    }
}
