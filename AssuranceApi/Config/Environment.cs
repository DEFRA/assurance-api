namespace AssuranceApi.Config;

/// <summary>
/// Provides environment-related utility methods and constants for the Assurance API.
/// </summary>
public static class Environment
{
    private const string DEFAULT_DATABASE_NAME = "assurance-api";

    /// <summary>
    /// Determines if the application is running in development mode.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance.</param>
    /// <returns>True if the application is not in production mode; otherwise, false.</returns>
    public static bool IsDevMode(this WebApplicationBuilder builder)
    {
        return !builder.Environment.IsProduction();
    }

    /// <summary>
    /// Retrieves the MongoDB connection string from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The MongoDB connection string.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Mongo:DatabaseUri configuration is not set.</exception>
    public static string GetMongoConnectionString(IConfiguration configuration)
    {
        return configuration.GetValue<string>("Mongo:DatabaseUri")
            ?? throw new InvalidOperationException("Mongo:DatabaseUri configuration is not set");
    }

    /// <summary>
    /// Retrieves the MongoDB database name from the configuration or uses the default value.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The MongoDB database name.</returns>
    public static string GetMongoDatabaseName(IConfiguration configuration)
    {
        return configuration.GetValue<string>("Mongo:DatabaseName") ?? DEFAULT_DATABASE_NAME;
    }
}
