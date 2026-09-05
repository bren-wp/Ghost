using System.Reflection;

namespace GhostFTP.Design;

public static class GhostProduct
{
    public const string DisplayName = "Ghost FTP";
    public const string ProductName = "GhostFTP";
    public const string Website = "https://ghostftp.com";
    public const string Repository = "https://github.com/bren-wp/Ghost";

    public const string Publisher = "BRENDIGO LTD";
    public const string PublisherWebsite = "https://brendigo.com";
    public const string CompanyNumber = "16545639";
    public const string RegisteredOffice = "71–75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom";
    public const string CopyrightNotice = "Copyright © 2026 BRENDIGO LTD. All rights reserved.";

    public static string InformationalVersion =>
        typeof(GhostProduct).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(GhostProduct).Assembly.GetName().Version?.ToString(3)
        ?? "0.0.0";

    public static bool IsBeta => InformationalVersion.Contains("-beta", StringComparison.OrdinalIgnoreCase);

    public static string ReleaseChannelDisplay => IsBeta ? "Beta" : "Stable";

    public static string PrivacyTagline => IsBeta
        ? "Beta · Private FTP / FTPS workspace"
        : "Private FTP / FTPS workspace";
}
