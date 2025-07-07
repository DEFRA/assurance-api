using System.Diagnostics.CodeAnalysis;
using System.Net;
using Serilog.Core;

namespace AssuranceApi.Utils.Http;

/// <summary>
/// Provides utility methods for configuring and using an HTTP client with a proxy.
/// </summary>
public static class Proxy
{
    /// <summary>
    /// The name of the HTTP client configured to use the platform's outbound proxy.
    /// </summary>
    public const string ProxyClient = "proxy";

    /// <summary>
    /// Adds a preconfigured HTTP client that uses the platform's outbound proxy to the service collection.
    /// </summary>
    /// <param name="services">The service collection to which the HTTP client will be added.</param>
    /// <param name="logger">The logger used for logging proxy-related information.</param>
    [ExcludeFromCodeCoverage]
    public static void AddHttpProxyClient(this IServiceCollection services, Logger logger)
    {
        services
            .AddHttpClient(ProxyClient)
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return ConfigurePrimaryHttpMessageHandler(logger);
            });
    }

    /// <summary>
    /// Configures the primary HTTP message handler for the HTTP client.
    /// </summary>
    /// <param name="logger">The logger used for logging proxy-related information.</param>
    /// <returns>A configured <see cref="HttpClientHandler"/> instance.</returns>
    [ExcludeFromCodeCoverage]
    public static HttpClientHandler ConfigurePrimaryHttpMessageHandler(Logger logger)
    {
        var proxyUri = Environment.GetEnvironmentVariable("CDP_HTTPS_PROXY");
        return CreateHttpClientHandler(proxyUri, logger);
    }

    /// <summary>
    /// Creates an <see cref="HttpClientHandler"/> configured with the specified proxy URI.
    /// </summary>
    /// <param name="proxyUri">The URI of the proxy server.</param>
    /// <param name="logger">The logger used for logging proxy-related information.</param>
    /// <returns>A configured <see cref="HttpClientHandler"/> instance.</returns>
    public static HttpClientHandler CreateHttpClientHandler(string? proxyUri, Logger logger)
    {
        var proxy = CreateProxy(proxyUri, logger);
        return new HttpClientHandler { Proxy = proxy, UseProxy = proxyUri != null };
    }

    /// <summary>
    /// Creates a <see cref="WebProxy"/> instance configured with the specified proxy URI.
    /// </summary>
    /// <param name="proxyUri">The URI of the proxy server.</param>
    /// <param name="logger">The logger used for logging proxy-related information.</param>
    /// <returns>A configured <see cref="WebProxy"/> instance.</returns>
    public static WebProxy CreateProxy(string? proxyUri, Logger logger)
    {
        var proxy = new WebProxy { BypassProxyOnLocal = true };
        if (proxyUri != null)
        {
            ConfigureProxy(proxy, proxyUri, logger);
        }
        else
        {
            logger.Warning("CDP_HTTP_PROXY is NOT set, proxy client will be disabled");
        }
        return proxy;
    }

    /// <summary>
    /// Configures the specified <see cref="WebProxy"/> with the given proxy URI.
    /// </summary>
    /// <param name="proxy">The <see cref="WebProxy"/> to configure.</param>
    /// <param name="proxyUri">The URI of the proxy server.</param>
    /// <param name="logger">The logger used for logging proxy-related information.</param>
    public static void ConfigureProxy(WebProxy proxy, string proxyUri, Logger logger)
    {
        logger.Debug("Creating proxy http client");
        var uri = new UriBuilder(proxyUri);

        var credentials = GetCredentialsFromUri(uri);
        if (credentials != null)
        {
            logger.Debug("Setting proxy credentials");
            proxy.Credentials = credentials;
        }

        // Remove credentials from URI to so they don't get logged.
        uri.UserName = "";
        uri.Password = "";
        proxy.Address = uri.Uri;
    }

    /// <summary>
    /// Extracts credentials from the specified URI.
    /// </summary>
    /// <param name="uri">The URI containing the credentials.</param>
    /// <returns>A <see cref="NetworkCredential"/> instance if credentials are found; otherwise, null.</returns>
    private static NetworkCredential? GetCredentialsFromUri(UriBuilder uri)
    {
        var username = uri.UserName;
        var password = uri.Password;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;
        return new NetworkCredential(username, password);
    }
}
