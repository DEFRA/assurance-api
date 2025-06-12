using System.Diagnostics.CodeAnalysis;
using AssuranceApi.Profession.Endpoints;
using AssuranceApi.Profession.Services;
using AssuranceApi.Project.Endpoints;
using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using AssuranceApi.Project.Validators;
using AssuranceApi.ServiceStandard.Endpoints;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.ServiceStandard.Services;
using AssuranceApi.ServiceStandard.Validators;
using AssuranceApi.Utils;
using AssuranceApi.Utils.Http;
using AssuranceApi.Utils.Logging;
using AssuranceApi.Utils.Mongo;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;


var app = CreateWebApplication(args);
await app.RunAsync();

[ExcludeFromCodeCoverage]
static WebApplication CreateWebApplication(string[] args)
{
    var _builder = WebApplication.CreateBuilder(args);

    ConfigureWebApplication(_builder);

    var _app = BuildWebApplication(_builder);

    return _app;
}

[ExcludeFromCodeCoverage]
static void ConfigureWebApplication(WebApplicationBuilder _builder)
{
    _builder.Configuration.AddEnvironmentVariables();

    var logger = ConfigureLogging(_builder);

    // Load certificates into Trust Store - Note must happen before Mongo and Http client connections
    _builder.Services.AddCustomTrustStore(logger);

    ConfigureAuthentication(_builder, logger);

    _builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("Authorization");
        });
    });

    ConfigureMongoDb(_builder);

    ConfigureEndpoints(_builder);

    _builder.Services.AddHttpClient();

    // calls outside the platform should be done using the named 'proxy' http client.
    _builder.Services.AddHttpProxyClient(logger);

    _builder.Services.AddValidatorsFromAssemblyContaining<Program>();
}

[ExcludeFromCodeCoverage]
static WebApplication BuildWebApplication(WebApplicationBuilder _builder)
{
    var app = _builder.Build();

    app.UseRouting();

    // Add CORS middleware - must be before auth middleware
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");

    app.UseServiceStandardEndpoints();
    app.UseProjectEndpoints();
    app.UseProfessionEndpoints();

    return app;
}

[ExcludeFromCodeCoverage]
static Logger ConfigureLogging(WebApplicationBuilder _builder)
{
    _builder.Logging.ClearProviders();
    var logger = new LoggerConfiguration()
        .ReadFrom.Configuration(_builder.Configuration)
        .Enrich.With<LogLevelMapper>()
        .Enrich.WithProperty(
            "service.version",
            System.Environment.GetEnvironmentVariable("SERVICE_VERSION")
        )
        .CreateLogger();
    _builder.Logging.AddSerilog(logger);
    logger.Information("Starting application");
    return logger;
}

[ExcludeFromCodeCoverage]
static void ConfigureMongoDb(WebApplicationBuilder _builder)
{
    _builder.Services.AddSingleton<IMongoDbClientFactory>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        var connectionString = AssuranceApi.Config.Environment.GetMongoConnectionString(
            _builder.Configuration
        );
        var databaseName = AssuranceApi.Config.Environment.GetMongoDatabaseName(
            _builder.Configuration
        );

        logger.LogInformation(
            "Configuring MongoDB with connection string: {ConnectionString}, database: {DatabaseName}",
            connectionString,
            databaseName
        );

        return new MongoDbClientFactory(connectionString, databaseName);
    });
}

[ExcludeFromCodeCoverage]
static void ConfigureEndpoints(WebApplicationBuilder _builder)
{
    _builder.Services.AddSingleton<IServiceStandardPersistence, ServiceStandardPersistence>();
    _builder.Services.AddSingleton<IServiceStandardHistoryPersistence, ServiceStandardHistoryPersistence>();

    _builder.Services.AddSingleton<IProfessionPersistence, ProfessionPersistence>();
    _builder.Services.AddSingleton<IProfessionHistoryPersistence, ProfessionHistoryPersistence>();

    _builder.Services.AddSingleton<IProjectPersistence, ProjectPersistence>();
    _builder.Services.AddSingleton<IProjectHistoryPersistence, ProjectHistoryPersistence>();

    _builder.Services.AddSingleton<IProjectStandardsPersistence, ProjectStandardsPersistence>();
    _builder.Services.AddSingleton<IProjectStandardsHistoryPersistence, ProjectStandardsHistoryPersistence>();

    _builder.Services.AddScoped<IValidator<ServiceStandardModel>, ServiceStandardValidator>();
    _builder.Services.AddScoped<IValidator<ProjectModel>, ProjectValidator>();

    _builder.Services.AddScoped<AssuranceApi.Project.Handlers.CreateAssessmentHandler>();
    _builder.Services.AddScoped<AssuranceApi.Project.Helpers.StandardsSummaryHelper>();

    _builder.Services.AddHealthChecks();
}

[ExcludeFromCodeCoverage]
static void ConfigureAuthentication(WebApplicationBuilder _builder, Logger logger)
{
    logger.Information("Configuring Azure AD authentication");

    string? tenantId = GetTenantIdFromConfiguration(_builder);
    string? clientId = GetClientIdFromConfiguration(_builder);

    if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId))
    {
        logger.Warning("Azure AD configuration is missing. Authentication will be disabled.");
        return;
    }

    var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0/";

    var validAudiences = GetValidAudiencesFOrTokenValidation(clientId);

    _builder
        .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;

            // Use existing proxy configuration from Utils.Http.Proxy
            var proxyUri = System.Environment.GetEnvironmentVariable("CDP_HTTPS_PROXY");
            var handler = Proxy.CreateHttpClientHandler(proxyUri, logger);

            options.BackchannelHttpHandler = handler;
            options.BackchannelTimeout = TimeSpan.FromSeconds(30); // Reduce from default 60s

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = authority,
                ValidAudiences = validAudiences,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromMinutes(5),
            };
        });

    static string? GetTenantIdFromConfiguration(WebApplicationBuilder _builder)
    {
        return _builder.Configuration["Azure:TenantId"]
            ?? _builder.Configuration["AZURE:TENANTID"]
            ?? System.Environment.GetEnvironmentVariable("AZURE__TENANTID");
    }

    static string? GetClientIdFromConfiguration(WebApplicationBuilder _builder)
    {
        return _builder.Configuration["Azure:ClientId"]
            ?? _builder.Configuration["AZURE:CLIENTID"]
            ?? System.Environment.GetEnvironmentVariable("AZURE__CLIENTID");
    }

    static string[] GetValidAudiencesFOrTokenValidation(string clientId)
    {
        return
            [
                clientId,
                $"api://{clientId}",
                $"api://{clientId}/access_as_user",
            ];
    }

    _builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser());

        // Add admin role policy
        options.AddPolicy(
            "RequireAdmin",
            policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(
                        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                        "Admin",
                        "admin"
                    )
        );
    });
}

// Make Program class accessible for integration testing
public partial class Program
{
    protected Program() { }
}