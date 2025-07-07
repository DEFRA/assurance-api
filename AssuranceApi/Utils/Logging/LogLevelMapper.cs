using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;

namespace AssuranceApi.Utils.Logging;

/// <summary>
/// Maps log levels from the C# default 'Information', 'Debug', etc., to the Node.js style 'info', 'debug', etc.
/// </summary>
[ExcludeFromCodeCoverage]
public class LogLevelMapper : ILogEventEnricher
{
    /// <summary>
    /// Enriches the log event by mapping its level to a Node.js style log level and adding it as a property.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">The factory to create log event properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var logLevel = logEvent.Level switch
        {
            LogEventLevel.Information => "info",
            LogEventLevel.Debug => "debug",
            LogEventLevel.Error => "error",
            LogEventLevel.Fatal => "fatal",
            LogEventLevel.Warning => "warn",
            _ => "all",
        };

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("log.level", logLevel));
    }
}
