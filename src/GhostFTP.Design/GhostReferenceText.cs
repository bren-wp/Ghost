namespace GhostFTP.Design;

/// <summary>
/// Localized copy used by the shared reference desktop shell.
/// English is the authoritative fallback. Reference-shell copy that is not translated here
/// deliberately falls back to English rather than performing any online lookup.
/// </summary>
public static class GhostReferenceText
{
    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["FileMenu"] = "File",
        ["ViewMenu"] = "View",
        ["TransfersMenu"] = "Transfers",
        ["SitesMenu"] = "Sites",
        ["ToolsMenu"] = "Tools",
        ["HelpMenu"] = "Help",
        ["PrivateFileClient"] = "PRIVATE FILE CLIENT",
        ["Tagline"] = "Private file transfers, simply.",
        ["Home"] = "Home",
        ["ThisTab"] = "This tab",
        ["NoSavedConnection"] = "No saved connection in this tab.",
        ["FavoritesInTab"] = "Favorites in this tab",
        ["RecentInTab"] = "Recent connections in this tab",
        ["AccountNotRequired"] = "Account not required",
        ["PrivacyDescription"] = "Connection data stays in local memory and local profile storage. Nothing is sent to a Ghost FTP account.",
        ["SearchRemote"] = "Search remote files…",
        ["SiteManager"] = "Site Manager",
        ["Diagnostics"] = "Diagnostics",
        ["ConnectionLog"] = "Connection Log",
        ["LocalSessionActivity"] = "local session activity · credentials never logged",
        ["Clear"] = "Clear",
        ["ExplicitFtpsRecommended"] = "FTPS Explicit recommended",
        ["CredentialsLocal"] = "Credentials stay local to this desktop session unless explicitly saved in Site Manager.",
        ["Server"] = "Server",
        ["Items"] = "items",
        ["Folder"] = "Folder",
        ["File"] = "File",
        ["NoTelemetry"] = "No telemetry · No tracking"
    };

    private static readonly Dictionary<string, string> Croatian = new(StringComparer.Ordinal)
    {
        ["FileMenu"] = "Datoteka",
        ["ViewMenu"] = "Prikaz",
        ["TransfersMenu"] = "Prijenosi",
        ["SitesMenu"] = "Poslužitelji",
        ["ToolsMenu"] = "Alati",
        ["HelpMenu"] = "Pomoć",
        ["PrivateFileClient"] = "PRIVATNI FTP KLIJENT",
        ["Tagline"] = "Privatni prijenosi datoteka, jednostavno.",
        ["Home"] = "Početna",
        ["ThisTab"] = "Ova kartica",
        ["NoSavedConnection"] = "Nema spremljene veze u ovoj kartici.",
        ["FavoritesInTab"] = "Favoriti u ovoj kartici",
        ["RecentInTab"] = "Nedavne veze u ovoj kartici",
        ["AccountNotRequired"] = "Račun nije potreban",
        ["PrivacyDescription"] = "Podaci veze ostaju samo u lokalnoj memoriji i lokalnoj pohrani profila. Ništa se ne šalje na Ghost FTP račun.",
        ["SearchRemote"] = "Pretraži udaljene datoteke…",
        ["SiteManager"] = "Upravitelj poslužitelja",
        ["Diagnostics"] = "Dijagnostika",
        ["ConnectionLog"] = "Zapis veze",
        ["LocalSessionActivity"] = "lokalna aktivnost sesije · vjerodajnice se nikada ne zapisuju",
        ["Clear"] = "Očisti",
        ["ExplicitFtpsRecommended"] = "Preporučuje se FTPS Explicit",
        ["CredentialsLocal"] = "Vjerodajnice ostaju lokalne u ovoj desktop sesiji osim ako ih izričito ne spremite u Upravitelju poslužitelja.",
        ["Server"] = "Poslužitelj",
        ["Items"] = "stavki",
        ["Folder"] = "Mapa",
        ["File"] = "Datoteka",
        ["NoTelemetry"] = "Bez telemetrije · Bez praćenja"
    };

    public static string T(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var language = GhostLocalization.CurrentLanguageCode;
        if (string.Equals(language, "hr", StringComparison.OrdinalIgnoreCase)
            && Croatian.TryGetValue(key, out var croatian)
            && !string.IsNullOrWhiteSpace(croatian))
        {
            return croatian;
        }

        return English.TryGetValue(key, out var english) ? english : key;
    }
}
