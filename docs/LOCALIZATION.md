# Ghost FTP Localization Architecture

The current **Ghost FTP 0.1.3 Beta** line includes a shared dependency-free localization layer used by Windows, Linux and Windows Setup.

## Primary language and fallback

**English (`en`) is the primary language, default language and final fallback.**

A missing technical string must never crash Ghost FTP or leave a blank primary action. When a localized value is unavailable, the English value is returned. Unknown or malformed stored language codes normalize to English.

## 29 selectable languages

Ghost FTP exposes **29 selectable languages** from local application resources. The catalog includes English, Croatian and major European/Asian languages represented in `GhostLocalization.SupportedLanguages`.

The source audit verifies that the catalog count remains 29 for the current release contract.

## Offline-only localization

Ghost FTP does not use an online translation API. UI text, setup strings and technical fallback strings are resolved locally. Connection details, filenames, server information or credentials are not sent to a translation service.

## Shared localization layer

`GhostLocalization` owns:

- supported language metadata;
- current language selection;
- normalization of stored/requested language codes;
- English source strings;
- localized override dictionaries;
- formatting helpers.

Both Windows and Linux use the same `GhostLocalization.CurrentLanguageCode` state and the same supported language list.

## Reference shell copy

`GhostReferenceText` contains workstation-specific shared text for the canonical desktop shell, including:

- menu names;
- Connection Log;
- Site Manager;
- Quick Connect helper copy;
- session-only/local privacy wording;
- search and sidebar concepts.

English is authoritative. Croatian has explicit reference-shell overrides. Other languages safely fall back to the main English reference text where a dedicated override is not present.

## 0.1.3 transfer-management copy

`GhostTransferText` isolates the new queue-management strings introduced in 0.1.3:

- Pause queue;
- Resume queue;
- Retry failed;
- Clear completed;
- Clear failed;
- Clear cancelled;
- Queue paused/active state;
- running-transfer pause explanation.

English remains authoritative. Croatian has explicit translations for these new actions. Other configured languages currently use the guaranteed English fallback for any transfer-management key that does not yet have a native override.

This approach is deliberate: a new safety-critical operation must stay understandable rather than displaying an empty or machine-translated label.

## Windows Setup localization

Setup uses `GhostLocalization` plus `GhostSetupLocalization` for wizard-specific labels. The language can be selected on the Welcome step and is saved locally for the installed client.

0.1.3 keeps the Setup language selector local and adds no translation/network dependency.

## Technical/security strings

Security-sensitive behavior is not inferred from translated labels. Protocol/security mode is represented by typed values (`FtpSecurityMode`) and validated independently of UI language.

Plain FTP warning behavior, TLS validation and destructive confirmation remain functional even if a localized label falls back to English.

## Linux font/input considerations

The Linux X11/XWayland renderer initializes a locale-aware Xlib font set and includes Unicode-capable fallback patterns. The renderer draws UTF-8 strings through `Xutf8DrawString`.

The Linux renderer therefore consumes the same language codes and UTF-8 resource strings while remaining independent of a third-party UI toolkit.

## Windows text rendering

Windows uses WPF/Segoe UI Variable/Segoe UI fallback through the shared design layer. Text wrapping is enabled on explanatory copy and dialogs where longer localized strings may require extra height.

## Setup/layout resilience

The Windows application and Setup are resizable. Layouts use flexible grid columns, wrapping where appropriate and compact controls to reduce clipping risk for languages with longer text.

## Adding or improving a translation

When a language override is extended:

1. keep the key identical to the English source key;
2. preserve placeholders/format semantics;
3. do not alter protocol/security behavior in translation code;
4. verify the UI at compact and normal widths;
5. preserve English fallback;
6. avoid online translation dependencies;
7. run WPF input/localization smoke tests and Linux compile/runtime smoke tests.

## Release verification

The source audit verifies:

- English default code;
- representative language entries such as Croatian and Traditional Chinese;
- the expected 29-language catalog count;
- Setup consumption of the shared language list.

The Windows UI smoke test verifies editable controls/localization behavior, and Linux CI builds/runs the native renderer with the shared language layer.

## Privacy

Language selection is local user preference. Ghost FTP does not upload language choice as analytics or use it for advertising/profile segmentation.
