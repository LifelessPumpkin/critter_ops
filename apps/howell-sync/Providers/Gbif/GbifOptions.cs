using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace HowellSync.Providers.Gbif;

public sealed class GbifOptions
{
    public string BaseUrl { get; set; } = "https://api.gbif.org/v1/";
    public string UserAgent { get; set; } = "";
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaximumConcurrentRequests { get; set; } = 1;
    public TimeSpan MinimumRequestInterval { get; set; } = TimeSpan.FromSeconds(1);
    public int MaximumRequestsPerRun { get; set; } = 100;
    public int MaximumRetries { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromSeconds(60);
    public int PageSize { get; set; } = 20;
    public int DefaultImportLimit { get; set; } = 20;
    // Explicit v1 compatibility strategy, NOT GBIF's preferred current taxonomy (COL XR).
    public string DatasetIdentifier { get; set; } = "d7dddbf4-2cf0-4f39-9b2a-bb099caae36c";

    public static GbifOptions Read(IConfigurationSection section)
    {
        var options = new GbifOptions();
        section.Bind(options);
        options.Validate();
        return options;
    }

    private static bool HasContact(string userAgent)
    {
        // Validate contact syntax without a network lookup; ownership/reachability belongs to the operator.
        foreach (Match match in Regex.Matches(userAgent, @"https://[^\s)]+|[^\s()]+@[^\s()]+"))
        {
            if (Uri.TryCreate(match.Value, UriKind.Absolute, out var url) && url.Scheme == "https" &&
                url.HostNameType == UriHostNameType.Dns && url.Host.Contains('.') && string.IsNullOrEmpty(url.UserInfo)) return true;
            if (MailAddress.TryCreate(match.Value, out var mail) && mail.Host.Contains('.')) return true;
        }
        return false;
    }

    public void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !BaseUrl.EndsWith('/'))
            throw new ArgumentException("GBIF BaseUrl must be an HTTPS base URL ending in /.");
        using var request = new HttpRequestMessage();
        if (UserAgent.Length > 500 || !UserAgent.Contains("CritterOps", StringComparison.OrdinalIgnoreCase) ||
            !HasContact(UserAgent) || UserAgent.Contains("example.org", StringComparison.OrdinalIgnoreCase) ||
            !request.Headers.UserAgent.TryParseAdd(UserAgent))
            throw new ArgumentException("GBIF UserAgent must identify CritterOps and a real configured project URL or contact.");
        if (RequestTimeout <= TimeSpan.Zero || RequestTimeout > TimeSpan.FromMinutes(5) ||
            MaximumConcurrentRequests is < 1 or > 8 || MinimumRequestInterval < TimeSpan.Zero || MinimumRequestInterval > TimeSpan.FromMinutes(1) ||
            MaximumRequestsPerRun is < 1 or > 10000 || MaximumRetries is < 0 or > 10 || InitialRetryDelay <= TimeSpan.Zero ||
            MaximumRetryDelay < InitialRetryDelay || MaximumRetryDelay > TimeSpan.FromMinutes(5) ||
            PageSize is < 1 or > 100 || DefaultImportLimit is < 1 or > 1000 || !Guid.TryParse(DatasetIdentifier, out _))
            throw new ArgumentException("Invalid GBIF timeout, request bounds, pagination, or dataset configuration.");
        DatasetIdentifier = Guid.Parse(DatasetIdentifier).ToString();
        if (DatasetIdentifier == "7ddf754f-d193-4cc9-b351-99906754a03b")
            throw new ArgumentException("COL XR identifiers are not supported by this v1 usage adapter; use a compatible v1 checklist dataset.");
    }
}
