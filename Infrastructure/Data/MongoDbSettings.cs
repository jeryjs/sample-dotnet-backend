namespace BackendApi.Infrastructure.Data;

/// <summary>
/// Configuration settings for MongoDB connection.
/// </summary>
public class MongoDbSettings
{
    public const string SectionName = "MongoDB";

    /// <summary>
    /// MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// Name of the database.
    /// </summary>
    public string DatabaseName { get; set; } = "WAVBackendAPI";

    /// <summary>
    /// Name of the patients collection.
    /// </summary>
    public string PatientsCollectionName { get; set; } = "patients";

    /// <summary>
    /// Name of the ancillary users collection.
    /// </summary>
    public string AncillaryUsersCollectionName { get; set; } = "ancillary_users";

    /// <summary>
    /// Name of the contact users collection.
    /// </summary>
    public string ContactUsersCollectionName { get; set; } = "contact_users";

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Server selection timeout in seconds.
    /// </summary>
    public int ServerSelectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum connection pool size.
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Minimum connection pool size.
    /// </summary>
    public int MinConnectionPoolSize { get; set; } = 10;

    /// <summary>
    /// Enable retry writes.
    /// </summary>
    public bool RetryWrites { get; set; } = true;

    /// <summary>
    /// Enable retry reads.
    /// </summary>
    public bool RetryReads { get; set; } = true;
}
