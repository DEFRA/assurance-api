using AssuranceApi.Example.Endpoints;
using AssuranceApi.Example.Services;
using AssuranceApi.ServiceStandard.Endpoints;
using AssuranceApi.ServiceStandard.Services;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.ServiceStandard.Validators;
using AssuranceApi.Project.Endpoints;
using AssuranceApi.Project.Services;
using AssuranceApi.Project.Models;
using AssuranceApi.Project.Validators;
using AssuranceApi.Utils;
using AssuranceApi.Utils.Http;
using AssuranceApi.Utils.Logging;
using AssuranceApi.Utils.Mongo;
using AssuranceApi.Config;
using FluentValidation;
using Serilog;
using Serilog.Core;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Text;
using System.Net.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;
//-------- Configure the WebApplication builder------------------//

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

   // Configure Authentication
   ConfigureAuthentication(_builder);
   
   // Add CORS support
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
static Logger ConfigureLogging(WebApplicationBuilder _builder)
{
   _builder.Logging.ClearProviders();
   var logger = new LoggerConfiguration()
       .ReadFrom.Configuration(_builder.Configuration)
       .Enrich.With<LogLevelMapper>()
       .Enrich.WithProperty("service.version", System.Environment.GetEnvironmentVariable("SERVICE_VERSION"))
       .CreateLogger();
   _builder.Logging.AddSerilog(logger);
   logger.Information("Starting application");
   return logger;
}

[ExcludeFromCodeCoverage]
static void ConfigureMongoDb(WebApplicationBuilder _builder)
{
   _builder.Services.AddSingleton<IMongoDbClientFactory>(sp => {
       var logger = sp.GetRequiredService<ILogger<Program>>();
       var connectionString = AssuranceApi.Config.Environment.GetMongoConnectionString(_builder.Configuration);
       var databaseName = AssuranceApi.Config.Environment.GetMongoDatabaseName(_builder.Configuration);
       
       logger.LogInformation("Configuring MongoDB with connection string: {ConnectionString}, database: {DatabaseName}", 
           connectionString, databaseName);

       return new MongoDbClientFactory(connectionString, databaseName);
   });
}

[ExcludeFromCodeCoverage]
static void ConfigureEndpoints(WebApplicationBuilder _builder)
{
   // our Example service, remove before deploying!
   _builder.Services.AddSingleton<IExamplePersistence, ExamplePersistence>();
   _builder.Services.AddSingleton<IServiceStandardPersistence, ServiceStandardPersistence>();
   _builder.Services.AddSingleton<IProjectPersistence, ProjectPersistence>();
   _builder.Services.AddSingleton<IStandardHistoryPersistence, StandardHistoryPersistence>();
   _builder.Services.AddSingleton<IProjectHistoryPersistence, ProjectHistoryPersistence>();
   _builder.Services.AddSingleton<IServiceStandardHistoryPersistence, ServiceStandardHistoryPersistence>();

   _builder.Services.AddScoped<IValidator<ServiceStandardModel>, ServiceStandardValidator>();
   _builder.Services.AddScoped<IValidator<ProjectModel>, ProjectValidator>();

   _builder.Services.AddHealthChecks();
}

[ExcludeFromCodeCoverage]
static void ConfigureAuthentication(WebApplicationBuilder _builder)
{
   using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
   var logger = loggerFactory.CreateLogger<Program>();
   
   // Try to get config from various sources
   var tenantId = _builder.Configuration["Azure:TenantId"] ?? 
       _builder.Configuration["AZURE:TENANTID"] ??
       System.Environment.GetEnvironmentVariable("AZURE__TENANTID");
   var clientId = _builder.Configuration["Azure:ClientId"] ?? 
       _builder.Configuration["AZURE:CLIENTID"] ??
       System.Environment.GetEnvironmentVariable("AZURE__CLIENTID");

   if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId))
   {
       logger.LogWarning("Azure AD configuration is missing. Authentication will be disabled.");
       return;
   }

   var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0/";
   logger.LogInformation("Configuring Azure AD authentication with Authority: {Authority}", authority);
   logger.LogInformation("Using ClientId: {ClientId}", clientId);
   logger.LogInformation("Using TenantId: {TenantId}", tenantId);
   
   // Log the audiences that will be accepted
   var validAudiences = new[]
   {
       clientId,
       $"api://{clientId}",
       $"api://{clientId}/access_as_user"
   };
   
   logger.LogInformation("Configured valid audiences:");
   foreach (var audience in validAudiences)
   {
       logger.LogInformation("  - {Audience}", audience);
   }
   
   // Log the OpenID Connect metadata URL
   var metadataUrl = $"{authority}.well-known/openid-configuration";
   logger.LogInformation("Will retrieve OpenID configuration from: {MetadataUrl}", metadataUrl);
   
   // Log the expected JWKS URI - this is where the signing keys are typically fetched
   var expectedJwksUri = $"{authority}.well-known/jwks.json";
   logger.LogInformation("Expected location of signing keys (JWKS URI): {JwksUri}", expectedJwksUri);
   
   // Try to fetch the actual JWKS URI from the discovery document
   string actualJwksUri = null;
   try
   {
       using var httpClient = new HttpClient();
       var response = httpClient.GetStringAsync(metadataUrl).GetAwaiter().GetResult();
       
       // Parse the JSON to get the jwks_uri
       var jsonOptions = new System.Text.Json.JsonDocumentOptions
       {
           AllowTrailingCommas = true
       };
       
       using var document = System.Text.Json.JsonDocument.Parse(response, jsonOptions);
       if (document.RootElement.TryGetProperty("jwks_uri", out var jwksUriElement))
       {
           actualJwksUri = jwksUriElement.GetString();
           logger.LogInformation("Actual JWKS URI from discovery document: {JwksUri}", actualJwksUri);
           
           // Test connectivity to the JWKS URI
           TestJwksUriConnectivity(actualJwksUri, logger);
       }
       else
       {
           logger.LogWarning("Could not find jwks_uri in discovery document");
       }
   }
   catch (Exception ex)
   {
       logger.LogError("Failed to fetch or parse discovery document: {Error}", ex.Message);
   }

   _builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           options.Authority = authority;
           
           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidateLifetime = true,
               ValidateIssuerSigningKey = true,
               ValidIssuer = authority,
               ValidAudiences = validAudiences,
               RequireSignedTokens = true,
               // Add clock skew tolerance to handle server time differences
               ClockSkew = TimeSpan.FromMinutes(5)
           };
           
           options.Events = new JwtBearerEvents
           {
               OnAuthenticationFailed = context =>
               {
                   logger.LogError("Authentication failed: {ErrorMessage}", context.Exception.Message);
                   if (context.Exception is SecurityTokenSignatureKeyNotFoundException)
                   {
                       logger.LogError("Security token signature key not found. Check if you can access metadata URL: {MetadataUrl}", metadataUrl);
                       
                       // Try to get the actual JWKS URI used by the token handler
                       string jwksUriUsed = "";
                       try
                       {
                           using var httpClient = new HttpClient();
                           var response = httpClient.GetStringAsync(metadataUrl).GetAwaiter().GetResult();
                           using var document = System.Text.Json.JsonDocument.Parse(response);
                           jwksUriUsed = document.RootElement.GetProperty("jwks_uri").GetString();
                           logger.LogError("Verify connectivity to JWKS URI: {JwksUri}", jwksUriUsed);
                       }
                       catch (Exception ex)
                       {
                           logger.LogError("Failed to determine JWKS URI: {Error}", ex.Message);
                       }
                   }
                   else if (context.Exception is SecurityTokenInvalidSignatureException)
                   {
                       logger.LogError("Invalid token signature. The signing key might have changed or the token was tampered with.");
                   }
                   return Task.CompletedTask;
               },
               OnTokenValidated = context =>
               {
                   logger.LogInformation("Token successfully validated for subject: {Subject}", context.Principal?.Identity?.Name ?? "unknown");
                   if (context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtToken)
                   {
                       logger.LogDebug("Token details - Issuer: {Issuer}, Audience: {Audience}, ValidFrom: {ValidFrom}, ValidTo: {ValidTo}",
                           jwtToken.Issuer, jwtToken.Audiences.FirstOrDefault(), jwtToken.ValidFrom, jwtToken.ValidTo);
                   }
                   return Task.CompletedTask;
               },
               OnChallenge = context =>
               {
                   logger.LogWarning("Token challenge: {Error}, Error Description: {ErrorDescription}", 
                       context.Error, context.ErrorDescription);
                   return Task.CompletedTask;
               },
               OnMessageReceived = context =>
               {
                   logger.LogDebug("JWT bearer token received and will be validated");
                   return Task.CompletedTask;
               }
           };
           
           // Log metadata retrieval
           options.BackchannelHttpHandler = new HttpClientHandler
           {
               ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
               {
                   if (errors != System.Net.Security.SslPolicyErrors.None)
                   {
                       logger.LogWarning("SSL certificate validation errors when contacting Azure AD: {Errors}", errors);
                   }
                   return true; // Still accept the certificate to prevent blocking in dev environments
               }
           };
       });

   _builder.Services.AddAuthorization(options =>
   {
       options.AddPolicy("RequireAuthenticated", policy =>
           policy.RequireAuthenticatedUser());
   });
}

[ExcludeFromCodeCoverage]
static WebApplication BuildWebApplication(WebApplicationBuilder _builder)
{
   var app = _builder.Build();

   app.UseRouting();
   
   // Add CORS middleware - must be before auth middleware
   app.UseCors();
   
   // Add authentication and authorization middleware
   app.UseAuthentication();
   app.UseAuthorization();
   
   app.MapHealthChecks("/health");

   app.UseServiceStandardEndpoints();
   app.UseProjectEndpoints();

   return app;
}

static void TestJwksUriConnectivity(string jwksUri, Microsoft.Extensions.Logging.ILogger logger)
{
    try
    {
        using var httpClient = new HttpClient();
        var response = httpClient.GetAsync(jwksUri).GetAwaiter().GetResult();
        
        if (response.IsSuccessStatusCode)
        {
            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            logger.LogInformation("Successfully connected to JWKS URI. Status: {StatusCode}", response.StatusCode);
            
            // Parse and log the keys information
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(content);
                if (document.RootElement.TryGetProperty("keys", out var keysElement) && keysElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    int keyCount = keysElement.GetArrayLength();
                    logger.LogInformation("Found {KeyCount} signing keys in JWKS document", keyCount);
                    
                    // Log key IDs
                    for (int i = 0; i < keyCount; i++)
                    {
                        var key = keysElement[i];
                        if (key.TryGetProperty("kid", out var kidElement))
                        {
                            string kid = kidElement.GetString();
                            logger.LogInformation("Key {Index}: ID = {KeyId}", i+1, kid);
                        }
                    }
                }
                else
                {
                    logger.LogWarning("No 'keys' array found in JWKS document");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to parse JWKS content: {Error}", ex.Message);
            }
        }
        else
        {
            logger.LogError("Failed to connect to JWKS URI. Status: {StatusCode}", response.StatusCode);
        }
    }
    catch (Exception ex)
    {
        logger.LogError("Exception when testing connectivity to JWKS URI: {Error}", ex.Message);
    }
}

