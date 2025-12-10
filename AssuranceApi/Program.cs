using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Asp.Versioning;
using AssuranceApi.Data;
using AssuranceApi.Data.ChangeHistory;
using AssuranceApi.Data.Models;
using AssuranceApi.Profession.Models;
using AssuranceApi.Project.Models;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.Utils;
using AssuranceApi.Utils.Http;
using AssuranceApi.Utils.Logging;
using AssuranceApi.Utils.Mongo;
using AssuranceApi.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;

var app = CreateWebApplication(args);
await app.RunAsync();

[ExcludeFromCodeCoverage]
static WebApplication CreateWebApplication(string[] args)
{
    var _builder = WebApplication.CreateBuilder(args);

    ConfigureWebApplication(_builder);

    return BuildWebApplication(_builder);
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

    ConfigureServices(_builder);

    ConfigureControllers(_builder);

    ConfigureApiDocumentation(_builder, logger);

    _builder.Services.AddHttpClient();

    // calls outside the platform should be done using the named 'proxy' http client.
    _builder.Services.AddHttpProxyClient(logger);

    _builder.Services.AddValidatorsFromAssemblyContaining<Program>();
}

[ExcludeFromCodeCoverage]
static WebApplication BuildWebApplication(WebApplicationBuilder _builder)
{
    var app = _builder.Build();

    app.UseSwagger();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerUI();
    }

    app.UseRouting();

    // Add CORS middleware - must be before auth middleware
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");

    app.MapControllers();

    return app;
}

[ExcludeFromCodeCoverage]
static void ConfigureControllers(WebApplicationBuilder _builder)
{
    _builder.Services.AddControllers();

    _builder.Services.AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ApiVersionReader = new QueryStringApiVersionReader("v");
    });
}

static void ConfigureServices(WebApplicationBuilder _builder)
{
    _builder.Services.AddSingleton<IProfessionPersistence, ProfessionPersistence>();
    _builder.Services.AddSingleton<IProfessionHistoryPersistence, ProfessionHistoryPersistence>();
    _builder.Services.AddSingleton<IProjectPersistence, ProjectPersistence>();
    _builder.Services.AddSingleton<IProjectHistoryPersistence, ProjectHistoryPersistence>();
    _builder.Services.AddSingleton<IValidator<ProfessionModel>, ProfessionModelValidator>();
    _builder.Services.AddSingleton<IServiceStandardPersistence, ServiceStandardPersistence>();
    _builder.Services.AddSingleton<IServiceStandardHistoryPersistence, ServiceStandardHistoryPersistence>();
    _builder.Services.AddSingleton<IProjectStandardsPersistence, ProjectStandardsPersistence>();
    _builder.Services.AddSingleton<IProjectStandardsHistoryPersistence, ProjectStandardsHistoryPersistence>();
    _builder.Services.AddSingleton<IDeliveryPartnerPersistence, DeliveryPartnerPersistence>();
    _builder.Services.AddSingleton<IProjectDeliveryPartnerPersistence, ProjectDeliveryPartnerPersistence>();
    _builder.Services.AddSingleton<IDeliveryGroupPersistence, DeliveryGroupPersistence>();
    _builder.Services.AddSingleton<IHistoryPersistence<DeliveryGroupChanges>, DeliveryGroupHistoryPersistence>();
    _builder.Services.AddSingleton<IInsightsPersistence, InsightsPersistence>();

    _builder.Services.AddScoped<IValidator<ServiceStandardModel>, ServiceStandardValidator>();
    _builder.Services.AddScoped<IValidator<ProjectModel>, ProjectValidator>();
    _builder.Services.AddScoped<IValidator<DeliveryPartnerModel>, DeliveryPartnerValidator>();
    _builder.Services.AddScoped<IValidator<ProjectDeliveryPartnerModel>, ProjectDeliveryPartnerModelValidator>();
    _builder.Services.AddScoped<IValidator<DeliveryGroupModel>, DeliveryGroupValidator>();

    _builder.Services.AddScoped<AssuranceApi.Project.Handlers.CreateAssessmentHandler>();
    _builder.Services.AddScoped<AssuranceApi.Project.Helpers.StandardsSummaryHelper>();

    _builder.Services.AddHealthChecks();
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

static void ConfigureApiDocumentation(WebApplicationBuilder builder, Logger logger)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "Defra Digital Assurance API",
            Description = "These are the core APIs that underpin the Defra Digital Assurance application.",
            TermsOfService = new Uri("https://defra.gov.uk"),
            Contact = new OpenApiContact
            {
                Name = "Example Contact",
                Url = new Uri("https://defra.gov.uk")
            },
            License = new OpenApiLicense
            {
                Name = "Example License",
                Url = new Uri("https://defra.gov.uk")
            }
        });

        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename), true);
    });
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
        return [clientId, $"api://{clientId}", $"api://{clientId}/access_as_user"];
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class Program
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    protected Program() { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
