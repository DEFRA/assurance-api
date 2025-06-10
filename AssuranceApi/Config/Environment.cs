namespace AssuranceApi.Config;

public static class Environment
{
    private const string DEFAULT_DATABASE_NAME = "assurance-api";

    public static bool IsDevMode(this WebApplicationBuilder builder)
    {
        return !builder.Environment.IsProduction();
    }

    public static string GetMongoConnectionString(IConfiguration configuration)
    {
        return configuration.GetValue<string>("Mongo:DatabaseUri")
            ?? throw new InvalidOperationException("Mongo:DatabaseUri configuration is not set");
    }

    public static string GetMongoDatabaseName(IConfiguration configuration)
    {
        return configuration.GetValue<string>("Mongo:DatabaseName") ?? DEFAULT_DATABASE_NAME;
    }
}
