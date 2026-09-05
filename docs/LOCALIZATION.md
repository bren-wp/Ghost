# Ghost FTP Localization Architecture

The current **Ghost FTP 0.1.1 Beta** line includes the shared dependency-free localization layer developed during the preserved internal-development history. Public patch releases do not remove or reduce localization coverage.

## Primary language and fallback

**English (`en`) is the primary language, default language and final fallback.**

A missing technical string must never crash Ghost FTP or leave a blank primary action. When a localized value is unavailable, the English value is returned.

Unknown or malformed stored language codes normalize to English.

## Supported languages

Ghost FTP 0.1.1 Beta validates 29 selectable languages:

| Code | Language |
|---|---|
| en | English |
| hr | Croatian / Hrvatski |
| de | German / Deutsch |
| fr | French / Français |
| es | Spanish / Español |
| it | Italian / Italiano |
| pt | Portuguese / Português |
| nl | Dutch / Nederlands |
| pl | Polish / Polski |
| cs | Czech / Čeština |
| sk | Slovak / Slovenčina |
| sl | Slovenian / Slovenščina |
| hu | Hungarian / Magyar |
| ro | Romanian / Română |
| bg | Bulgarian / Български |
| el | Greek / Ελληνικά |
| tr | Turkish / Türkçe |
| uk | Ukrainian / Українська |
| ru | Russian / Русский |
| sr | Serbian / Srpski |
| bs | Bosnian / Bosanski |
| sv | Swedish / Svenska |
| da | Danish / Dansk |
| no | Norwegian / Norsk |
| fi | Finnish / Suomi |
| ja | Japanese / 日本語 |
| ko | Korean / 한국어 |
| zh-CN | Chinese, Simplified / 简体中文 |
| zh-TW | Chinese, Traditional / 繁體中文 |

## Source layout

Localization is implemented in C# under `GhostFTP.Design` so the application and installer use one language model.

- `GhostLocalization.cs` — language list, English source strings, core application translations, normalization and fallback.
- `GhostSetupLocalization.cs` — compact Setup wizard translation catalog for the guided installation/uninstall flow.

No RESX runtime package, translation framework, JavaScript bundle, web service or remote localization dependency is required.

## Application coverage

The shared application catalog covers core user-facing navigation and workflow terms such as:

- Settings / About;
- Add / Edit / Remove;
- Connect / Disconnect;
- Upload / Download;
- Refresh / New folder / Rename / Delete;
- Host / Port / Security / Username / Password;
- Language / Appearance;
- Files / Local / Remote / Transfers;
- Saved servers / Quick connect;
- status and privacy labels;
- common installer labels reused by the app;
- transfer queue primary actions.

Long technical diagnostics or uncommon error details may intentionally fall back to English. This prevents low-quality partial translations from changing protocol meaning.

## Beta and stable version labels

The active public `0.x.y` line is Beta. Product version surfaces may append **Beta** to the numeric version while keeping product and publisher proper names unchanged.

Localization must not translate or mutate:

- the product proper name **Ghost FTP**;
- the publisher proper name **BRENDIGO LTD**;
- numeric semantic versions such as `0.1.1` or `1.0.0`;
- protocol tokens such as FTP, FTPS, TLS, EPSV, PASV, `NOOP`, `PWD`, `CWD` and `SIZE` where translation would alter their technical meaning.

When Ghost FTP reaches the explicit stable **1.0.0** gate, Beta labeling is removed from version-oriented product surfaces. The supported language catalog remains independent of that release-channel transition.

## Setup coverage

The Setup-specific catalog validates translations for:

- Welcome;
- License Agreement;
- I accept the license terms;
- Back;
- Next;
- Install options;
- Ready to install;
- Finish;
- Client language;
- language-selection instruction.

The governing license itself remains English. The installer can localize navigation around the license, but it does not alter the legal text.

## Runtime behavior

The desktop client reads `LanguageCode` from local settings at startup and configures `GhostLocalization` before creating the main UI.

Setup chooses the current language before rendering its wizard. Selecting another language rebuilds the small Setup view immediately using the shared localization state.

When Setup installs or updates Ghost FTP, the selected Setup language is written as the initial client language while preserving unrelated valid settings.

The selected language does not determine whether a build is Beta or stable. Release status comes only from the version/release-channel contract documented in `docs/VERSIONING.md`.

## Privacy guarantee

Localization is fully offline.

Ghost FTP does not:

- send text to an online translation provider;
- download language packs at runtime;
- contact ghostftp.com to detect locale;
- send the selected language to BRENDIGO LTD;
- use localization analytics.

The selected language is local configuration only.

## CI validation

`GhostFTP.UiSmoke` runs on a Windows STA thread and validates:

- exactly 29 expected language entries for the current Beta line;
- unique language codes;
- English is the first/default language;
- core application coverage for every language;
- Setup wizard coverage for every language;
- non-empty core labels;
- Croatian application and Setup translations as a concrete non-English regression check;
- unknown language codes fall back to English;
- editable WPF controls continue to work while localization is loaded.

Release packaging is blocked if the localization smoke test fails.

## Adding or changing a language

A localization change should:

1. use a stable BCP-47-style code compatible with the current normalization rules;
2. add a native display name and English display name;
3. provide the required core application translation set;
4. provide all Setup wizard strings;
5. preserve placeholders and product/legal names exactly where they are semantically significant;
6. keep **Ghost FTP** as the product name and **BRENDIGO LTD** as the legal publisher where those proper names are shown;
7. preserve Beta/stable version meaning without translating numeric versions;
8. pass the Windows WPF localization smoke test.

Do not add a language to the selector with an empty translation set merely to increase the language count.

## Historical note

The localization architecture was originally developed and documented under the preserved internal 1.x development numbering. Those historical release notes remain in `docs/releases/` for engineering traceability. The active public release sequence started at 0.1.0 Beta and now continues with 0.1.1 Beta; the localization implementation and tests are retained.

## Translation quality rule

Security, protocol and legal meaning take precedence over literal translation. When a translation is uncertain, use the English fallback rather than shipping a misleading security or FTP instruction.
