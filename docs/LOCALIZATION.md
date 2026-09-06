# Ghost FTP Localization Architecture

The current **Ghost FTP 0.1.2 Beta** line ships a shared dependency-free localization layer used by Windows, Linux and Windows Setup. Localization is entirely local; Ghost FTP does not contact an online translation service.

## Primary language and fallback

**English (`en`) is the primary language**, default language and authoritative final fallback.

A missing technical string must not crash Ghost FTP, leave a blank primary action or trigger a network lookup. When a locale does not define a value, the English source value is returned.

Unknown/malformed stored language codes normalize to English. The product can therefore recover from a damaged/unsupported language setting without external services.

## 29 selectable languages

Ghost FTP 0.1.2 contains **29 selectable languages** in `GhostLocalization.SupportedLanguages`:

| Code | Native name | English name |
| --- | --- | --- |
| `en` | English | English |
| `hr` | Hrvatski | Croatian |
| `de` | Deutsch | German |
| `fr` | Français | French |
| `es` | Español | Spanish |
| `it` | Italiano | Italian |
| `pt` | Português | Portuguese |
| `nl` | Nederlands | Dutch |
| `pl` | Polski | Polish |
| `cs` | Čeština | Czech |
| `sk` | Slovenčina | Slovak |
| `sl` | Slovenščina | Slovenian |
| `hu` | Magyar | Hungarian |
| `ro` | Română | Romanian |
| `bg` | Български | Bulgarian |
| `el` | Ελληνικά | Greek |
| `tr` | Türkçe | Turkish |
| `uk` | Українська | Ukrainian |
| `ru` | Русский | Russian |
| `sr` | Srpski | Serbian |
| `bs` | Bosanski | Bosnian |
| `sv` | Svenska | Swedish |
| `da` | Dansk | Danish |
| `no` | Norsk | Norwegian |
| `fi` | Suomi | Finnish |
| `ja` | 日本語 | Japanese |
| `ko` | 한국어 | Korean |
| `zh-CN` | 简体中文 | Chinese (Simplified) |
| `zh-TW` | 繁體中文 | Chinese (Traditional) |

The release source audit counts the local catalog and fails if the 0.1.2 release contract no longer exposes all 29 entries.

## Shared implementation

`src/GhostFTP.Design/GhostLocalization.cs` is the authoritative general UI localization catalog. It provides:

- `DefaultLanguageCode = "en"`;
- normalized language-code selection;
- local dictionaries/overrides;
- English fallback;
- `SupportedLanguages` metadata used by platform selectors.

`src/GhostFTP.Design/GhostReferenceText.cs` contains copy used specifically by the reference workstation shell. English is authoritative there too; Croatian has dedicated reference-shell overrides and other languages safely fall back to English for copy that has not yet received a dedicated override.

`src/GhostFTP.Setup/GhostSetupLocalization.cs` provides Setup-specific copy while relying on the same selected language identity.

## Windows behavior

Windows loads the stored language from local application settings and calls `GhostLocalization.SetLanguage` before/while building user-facing surfaces.

The top language control opens Settings rather than embedding a second independent language system. On compact windows the optional top overlay can hide to protect toolbar layout; language selection remains available in Settings.

The Windows UI smoke test exercises editable controls and localization behavior so a locale change cannot silently make text input unusable.

## Linux behavior

Linux reads `--lang` when explicitly supplied, otherwise uses locally stored `LanguageCode`. It normalizes through `GhostLocalization`, stores the normalized code, and derives its language-selector index from the same `SupportedLanguages` list used by Windows/Setup.

Renderer-specific text uses the shared localization/reference-text helpers. No online lookup path exists.

## Setup behavior

Windows Setup exposes `GhostLocalization.SupportedLanguages` through its language ComboBox. A language change rebuilds the wizard only after the selection event has unwound, preventing detached/reused WPF controls from being attached to two logical parents.

The user's Setup language preference is stored locally. Setup remains usable if a locale has partial coverage because English fallback is always available.

## Adding or improving a translation

For a normal product string:

1. define/confirm the English key/value;
2. add the locale override in `GhostLocalization`;
3. preserve the exact semantic meaning of security/privacy text rather than translating word-for-word if that weakens clarity;
4. keep button labels short enough for dense desktop toolbars;
5. test the Windows compact layout and Linux renderer with longer text;
6. keep English fallback intact.

For reference-shell-only copy, update `GhostReferenceText` as appropriate. For Setup-only copy, update `GhostSetupLocalization`.

## Translation quality rules

- Do not translate protocol literals such as `AUTH TLS`, `PBSZ 0`, `PROT P`, `TYPE I`, FTP/FTPS, SHA-256 or product/file names.
- Preserve warnings about plaintext FTP accurately.
- Preserve the meaning of local-only/session-only credential behavior.
- Avoid labels that are significantly longer than necessary in the primary workstation toolbar.
- Keep punctuation and casing natural to the target language.
- Never introduce a translation SDK, CDN or remote localization dependency.

## Encoding

Source files are UTF-8. UI code must remain Unicode-safe for Latin, Cyrillic, Greek, CJK and other scripts present in the catalog.

Windows uses Segoe UI/Segoe UI Variable fallbacks provided by the operating system. Linux uses its native X11 font-set path; missing glyph support is an environment/font configuration issue rather than a reason to download fonts at runtime.

## Privacy

Language choice is local settings data. It is not sent to BRENDIGO LTD or a telemetry service. Ghost FTP does not download language packs or report which language the user selected.

## Release gate

A release must keep:

- English default/fallback;
- the 29-language local catalog;
- Windows/Linux/Setup use of the same supported-language metadata;
- no external localization dependency;
- documentation synchronized with the active version.
