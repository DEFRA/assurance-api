using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace AssuranceApi.IntegrationTests;

public class TestApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private MongoDbContainer? _mongoDbContainer;
    private readonly Lazy<Task> _mongoDbContainerInitializer;

    public TestApplicationFactory()
    {
        _mongoDbContainerInitializer = new Lazy<Task>(InitializeMongoDbAsync);
    }

    private async Task InitializeMongoDbAsync()
    {
        _mongoDbContainer = new MongoDbBuilder()
            .WithImage("mongo:6.0")
            .Build();
            
        await _mongoDbContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure MongoDB container is started before configuration
        _mongoDbContainerInitializer.Value.GetAwaiter().GetResult();

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override MongoDB configuration for testing
            var testConfiguration = new Dictionary<string, string?>
            {
                ["Mongo:DatabaseUri"] = _mongoDbContainer!.GetConnectionString(),
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
        if (disposing && _mongoDbContainer != null)
        {
            _mongoDbContainer.DisposeAsync().AsTask().Wait();
        }
        base.Dispose(disposing);
    }

    public async ValueTask DisposeAsync()
    {
        if (_mongoDbContainer != null)
        {
            await _mongoDbContainer.DisposeAsync();
        }
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    public string GetMongoConnectionString()
    {
        _mongoDbContainerInitializer.Value.GetAwaiter().GetResult();
        return _mongoDbContainer?.GetConnectionString() ?? throw new InvalidOperationException("MongoDB container not initialized");
    }

    /// <summary>
    /// Clears all collections in the test database for test isolation
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        await _mongoDbContainerInitializer.Value;
        if (_mongoDbContainer == null) return;

        var client = new MongoClient(_mongoDbContainer.GetConnectionString());
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