using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using MongoDB.Driver;

namespace AssuranceApi.IntegrationTests;

public class TestApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private MongoDbRunner? _mongoDbRunner;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Start MongoDB instance if not already started
            if (_mongoDbRunner == null)
            {
                _mongoDbRunner = MongoDbRunner.Start();
            }

            // Override MongoDB configuration for testing
            var testConfiguration = new Dictionary<string, string?>
            {
                ["Mongo:DatabaseUri"] = _mongoDbRunner.ConnectionString,
                ["Mongo:DatabaseName"] = "test-assurance-api",
                ["Azure:TenantId"] = "test-tenant-id",
                ["Azure:ClientId"] = "test-client-id"
            };

            config.AddInMemoryCollection(testConfiguration);
        });

        builder.ConfigureServices(services =>
        {
            // Add authorization services for testing
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireClaim("role", "admin"));
            });
            services.AddAuthentication("Test")
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", options => { });
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mongoDbRunner?.Dispose();
        }
        base.Dispose(disposing);
    }

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public string GetMongoConnectionString()
    {
        return _mongoDbRunner?.ConnectionString ?? throw new InvalidOperationException("MongoDB runner not initialized");
    }

    /// <summary>
    /// Clears all collections in the test database for test isolation
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        if (_mongoDbRunner == null) return;

        var client = new MongoClient(_mongoDbRunner.ConnectionString);
        var database = client.GetDatabase("test-assurance-api");
        
        // Get all collection names and drop them
        var collectionNames = await database.ListCollectionNamesAsync();
        await collectionNames.ForEachAsync(async name =>
        {
            await database.DropCollectionAsync(name);
        });
    }

    /// <summary>
    /// Creates an authenticated client (admin user)
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        return client;
    }

    /// <summary>
    /// Creates an unauthenticated client (no authentication)
    /// </summary>
    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        // Explicitly ensure no authorization header
        client.DefaultRequestHeaders.Remove("Authorization");
        return client;
    }
} 