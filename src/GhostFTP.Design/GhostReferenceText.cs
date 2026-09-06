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
        ["Exit"] = "Exit",
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
        ["ConnectionDiagnostics"] = "Connection diagnostics",
        ["ConnectionLog"] = "Connection Log",
        ["LocalSessionActivity"] = "local session activity · credentials never logged",
        ["Clear"] = "Clear",
        ["ExplicitFtpsRecommended"] = "FTPS Explicit recommended",
        ["TlsFirst"] = "TLS first",
        ["CredentialsLocal"] = "Credentials stay local to this desktop session unless explicitly saved in Site Manager.",
        ["KeepInTab"] = "Keep in this tab",
        ["SessionOnly"] = "Session only · not saved to disk",
        ["Server"] = "Server",
        ["Items"] = "items",
        ["Selected"] = "selected",
        ["Folder"] = "Folder",
        ["File"] = "File",
        ["NoTelemetry"] = "No telemetry · No tracking",
        ["DoubleClickLocal"] = "Double-click folder or file to open",
        ["DoubleClickRemote"] = "Double-click folder · double-click file downloads",
        ["ResizeConnection"] = "Drag to resize Connection Log and Quick Connect · double-click to reset",
        ["ResizeTransfers"] = "Drag to resize the Transfers queue · double-click to reset",
        ["ResizePanes"] = "Drag to resize Local and Remote panes · double-click to reset",
        ["ResizeSidebar"] = "Drag to resize the server sidebar · double-click to reset",
        ["ConnectionStatusDiagnostics"] = "Connection status · click for local diagnostics",
        ["SavedSites"] = "Saved sites",
        ["ManageSavedSites"] = "Manage FTP and FTPS connection profiles on this device.",
        ["NewSite"] = "New site",
        ["General"] = "General",
        ["Advanced"] = "Advanced",
        ["SiteName"] = "Site name",
        ["HostUrl"] = "Host / IP / URL",
        ["HostHint"] = "Hostname or IP address; do not include ftp://.",
        ["DefaultRemotePath"] = "Default remote path",
        ["ServerRootHint"] = "Use / for the server root.",
        ["PassiveConnections"] = "Passive data connections",
        ["PassiveDescription"] = "Ghost FTP prefers EPSV and safely falls back to PASV. PASV host redirection is not trusted; data channels stay on the authenticated control host.",
        ["TimeoutsRetries"] = "Timeouts and retries",
        ["TimeoutsDescription"] = "Connection timeout, transfer retry, keepalive and concurrent-transfer limits are controlled centrally in Settings so behavior stays predictable across saved sites.",
        ["SelectSavedSite"] = "Select a saved site to edit its connection details.",
        ["EditSavedSite"] = "Edit this site and save it locally, or connect immediately.",
        ["DemoLocked"] = "Ghost FTP Demo is a built-in local profile and cannot be modified."
    };

    private static readonly Dictionary<string, string> Croatian = new(StringComparer.Ordinal)
    {
        ["FileMenu"] = "Datoteka",
        ["ViewMenu"] = "Prikaz",
        ["TransfersMenu"] = "Prijenosi",
        ["SitesMenu"] = "Poslužitelji",
        ["ToolsMenu"] = "Alati",
        ["HelpMenu"] = "Pomoć",
        ["Exit"] = "Izlaz",
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
        ["ConnectionDiagnostics"] = "Dijagnostika veze",
        ["ConnectionLog"] = "Zapis veze",
        ["LocalSessionActivity"] = "lokalna aktivnost sesije · vjerodajnice se nikada ne zapisuju",
        ["Clear"] = "Očisti",
        ["ExplicitFtpsRecommended"] = "Preporučuje se FTPS Explicit",
        ["TlsFirst"] = "TLS prioritet",
        ["CredentialsLocal"] = "Vjerodajnice ostaju lokalne u ovoj desktop sesiji osim ako ih izričito ne spremite u Upravitelju poslužitelja.",
        ["KeepInTab"] = "Zadrži u ovoj kartici",
        ["SessionOnly"] = "Samo ova sesija · ne sprema se na disk",
        ["Server"] = "Poslužitelj",
        ["Items"] = "stavki",
        ["Selected"] = "odabrano",
        ["Folder"] = "Mapa",
        ["File"] = "Datoteka",
        ["NoTelemetry"] = "Bez telemetrije · Bez praćenja",
        ["DoubleClickLocal"] = "Dvoklik na mapu ili datoteku za otvaranje",
        ["DoubleClickRemote"] = "Dvoklik na mapu · dvoklik na datoteku preuzima",
        ["ResizeConnection"] = "Povucite za promjenu veličine Zapisa veze i Brzog povezivanja · dvoklik vraća zadano",
        ["ResizeTransfers"] = "Povucite za promjenu veličine reda prijenosa · dvoklik vraća zadano",
        ["ResizePanes"] = "Povucite za promjenu veličine lokalnog i udaljenog prikaza · dvoklik vraća zadano",
        ["ResizeSidebar"] = "Povucite za promjenu širine bočne trake poslužitelja · dvoklik vraća zadano",
        ["ConnectionStatusDiagnostics"] = "Status veze · klik za lokalnu dijagnostiku",
        ["SavedSites"] = "Spremljeni poslužitelji",
        ["ManageSavedSites"] = "Upravljajte FTP i FTPS profilima veze spremljenima na ovom uređaju.",
        ["NewSite"] = "Novi poslužitelj",
        ["General"] = "Općenito",
        ["Advanced"] = "Napredno",
        ["SiteName"] = "Naziv poslužitelja",
        ["HostUrl"] = "Host / IP / URL",
        ["HostHint"] = "Naziv hosta ili IP adresa; nemojte uključiti ftp://.",
        ["DefaultRemotePath"] = "Zadana udaljena putanja",
        ["ServerRootHint"] = "Koristite / za korijen poslužitelja.",
        ["PassiveConnections"] = "Pasivne podatkovne veze",
        ["PassiveDescription"] = "Ghost FTP preferira EPSV i sigurno prelazi na PASV. PASV preusmjeravanje hosta nije pouzdano; podatkovni kanali ostaju na autentificiranom hostu kontrolne veze.",
        ["TimeoutsRetries"] = "Vremenska ograničenja i ponovni pokušaji",
        ["TimeoutsDescription"] = "Vremenska ograničenja veze, ponovni pokušaji prijenosa, keepalive i ograničenja paralelnih prijenosa upravljaju se centralno u Postavkama radi predvidljivog ponašanja svih spremljenih poslužitelja.",
        ["SelectSavedSite"] = "Odaberite spremljeni poslužitelj za uređivanje podataka veze.",
        ["EditSavedSite"] = "Uredite poslužitelj i spremite ga lokalno ili se odmah povežite.",
        ["DemoLocked"] = "Ghost FTP Demo je ugrađeni lokalni profil i ne može se mijenjati."
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
