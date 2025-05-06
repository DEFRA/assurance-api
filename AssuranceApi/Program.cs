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
using System.Text;
using System.Net;
using System.Net.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Extensions.Logging;
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
   
   // Create HTTP client handler with proxy support
   HttpClientHandler CreateProxyEnabledHandler()
   {
       // Use the same proxy configuration method as in Proxy.cs
       var proxyUri = System.Environment.GetEnvironmentVariable("CDP_HTTPS_PROXY");
       var handler = new HttpClientHandler();
       
       if (!string.IsNullOrEmpty(proxyUri))
       {
           logger.LogInformation("Configuring proxy for Azure AD connections: {ProxyUri}", 
               proxyUri.Contains('@') ? proxyUri.Substring(0, proxyUri.IndexOf('@')) + "***@" + proxyUri.Substring(proxyUri.IndexOf('@')+1) : proxyUri);
           
           var proxy = new WebProxy
           {
               BypassProxyOnLocal = true
           };
           
           var uri = new UriBuilder(proxyUri);
           
           // Set credentials if available
           var username = uri.UserName;
           var password = uri.Password;
           if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
           {
               logger.LogInformation("Using proxy with authentication");
               proxy.Credentials = new NetworkCredential(username, password);
           }
           
           // Remove credentials from URI for logging safety
           uri.UserName = "";
           uri.Password = "";
           proxy.Address = uri.Uri;
           
           handler.Proxy = proxy;
           handler.UseProxy = true;
       }
       else
       {
           logger.LogWarning("CDP_HTTPS_PROXY is not set. Azure AD connections will not use a proxy.");
       }
       
       // Add certificate validation callback for debugging
       handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
       {
           if (errors != System.Net.Security.SslPolicyErrors.None)
           {
               logger.LogWarning("SSL certificate validation errors when contacting Azure AD: {Errors}", errors);
           }
           return true; // Accept all certificates in this handler
       };
       
       return handler;
   }

   _builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           options.Authority = authority;
           
           // Configure backchannel HTTP client with proper proxy settings
           options.BackchannelHttpHandler = CreateProxyEnabledHandler();
           options.BackchannelTimeout = TimeSpan.FromSeconds(30); // Reduce from default 60s
           
           logger.LogInformation("Configured JWT bearer authentication with proxy support");
           
           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidateLifetime = true,
               ValidateIssuerSigningKey = true,
               ValidIssuer = authority,
               ValidAudiences = validAudiences,
               RequireSignedTokens = true,
               ClockSkew = TimeSpan.FromMinutes(5)
           };
           
           // Add event handlers for better diagnostics
           options.Events = new JwtBearerEvents
           {
               OnAuthenticationFailed = context =>
               {
                   logger.LogError("Authentication failed: {ErrorMessage}", context.Exception.Message);
                   
                   if (context.Exception is SecurityTokenSignatureKeyNotFoundException)
                   {
                       logger.LogError("Security token signature key not found. Check if the API can access Azure AD metadata at: {MetadataUrl}", metadataUrl);
                       logger.LogError("Ensure the CDP_HTTPS_PROXY environment variable is correctly set if a proxy is required");
                   }
                   else if (context.Exception is SecurityTokenInvalidSignatureException)
                   {
                       logger.LogError("Invalid token signature. The signing key might have changed or the token was tampered with.");
                   }
                   
                   return Task.CompletedTask;
               },
               OnTokenValidated = context =>
               {
                   logger.LogInformation("Token successfully validated for subject: {Subject}", 
                       context.Principal?.Identity?.Name ?? "unknown");
                   return Task.CompletedTask;
               },
               OnChallenge = context =>
               {
                   logger.LogWarning("Token challenge: {Error}, {ErrorDescription}", 
                       context.Error, context.ErrorDescription);
                   return Task.CompletedTask;
               },
               OnMessageReceived = context =>
               {
                   logger.LogDebug("JWT bearer token received and will be validated");
                   return Task.CompletedTask;
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

