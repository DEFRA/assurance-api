using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Serilog.Core;

namespace AssuranceApi.Utils;

/// <summary>
/// Provides utility methods for managing the trust store, including adding custom certificates.
/// </summary>
[ExcludeFromCodeCoverage]
public static class TrustStore
{
    /// <summary>
    /// Adds custom certificates to the trust store from environment variables.
    /// </summary>
    /// <param name="_">The service collection (not used).</param>
    /// <param name="logger">The logger instance for logging information.</param>
    internal static void AddCustomTrustStore(this IServiceCollection _, Logger logger)
    {
        logger.Information("Loading Certificates into Trust store");
        var certificates = GetCertificates(logger);
        AddCertificates(certificates);
    }

    /// <summary>
    /// Retrieves certificates from environment variables that are base64 encoded.
    /// </summary>
    /// <param name="logger">The logger instance for logging information.</param>
    /// <returns>A list of decoded certificate strings.</returns>
    private static List<string> GetCertificates(Logger logger)
    {
        return Environment
            .GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Where(entry =>
                entry.Key.ToString()!.StartsWith("TRUSTSTORE")
                && IsBase64String(entry.Value!.ToString() ?? "")
            )
            .Select(entry =>
            {
                var data = Convert.FromBase64String(entry.Value!.ToString() ?? "");
                logger.Information($"{entry.Key} certificate decoded");
                return Encoding.UTF8.GetString(data);
            })
            .ToList();
    }

    /// <summary>
    /// Adds the provided certificates to the current user's trust store.
    /// </summary>
    /// <param name="certificates">A collection of certificate strings to add.</param>
    private static void AddCertificates(IReadOnlyCollection<string> certificates)
    {
        if (certificates.Count == 0)
            return; // to stop trust store access denied issues on Macs
        var x509Certificate2S = certificates.Select(cert => new X509Certificate2(
            Encoding.ASCII.GetBytes(cert)
        ));
        var certificateCollection = new X509Certificate2Collection();

        foreach (var certificate2 in x509Certificate2S)
        {
            certificateCollection.Add(certificate2);
        }

        var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        try
        {
            store.Open(OpenFlags.ReadWrite);
            store.AddRange(certificateCollection);
        }
        catch (Exception ex)
        {
            throw new FileLoadException("Root certificate import failed: " + ex.Message, ex);
        }
        finally
        {
            store.Close();
        }
    }

    /// <summary>
    /// Checks if a given string is a valid Base64 encoded string.
    /// </summary>
    /// <param name="str">The string to validate.</param>
    /// <returns>True if the string is Base64 encoded; otherwise, false.</returns>
    private static bool IsBase64String(string str)
    {
        var buffer = new Span<byte>(new byte[str.Length]);
        return Convert.TryFromBase64String(str, buffer, out _);
    }
}
