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
static WebApplication BuildWebApplication(WebApplicationBuilder _builder)
{
   var app = _builder.Build();

   app.UseRouting();
   app.MapHealthChecks("/health");

   // Example module, remove before deploying!
   app.UseExampleEndpoints();

   app.UseServiceStandardEndpoints();
   app.UseProjectEndpoints();

   return app;
}

