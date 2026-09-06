# Ghost FTP Localization Architecture

The current **Ghost FTP 0.1.5 Beta** line includes a shared dependency-free localization layer used by Windows, Linux and Windows Setup.

## Primary language and fallback

**English (`en`) is the primary language, default language and final fallback.**

A missing technical string must never crash Ghost FTP or leave a blank primary action. When a localized value is unavailable, the English value is returned. Unknown or malformed stored language codes normalize to English.

## 29 selectable languages

Ghost FTP exposes **29 selectable languages** from local application resources. The catalog includes English, Croatian and major European/Asian languages represented in `GhostLocalization.SupportedLanguages`.

Source audit verifies the catalog count remains 29 for the active release contract.

## Offline-only localization

Ghost FTP does not use an online translation API. UI text, Setup strings and technical fallback strings are resolved locally. Connection details, filenames, server information or credentials are not sent to a translation service.

## Shared localization layer

`GhostLocalization` owns supported-language metadata, current language selection, normalization of stored/requested codes, English source strings, localized override dictionaries and formatting helpers.

Both Windows and Linux use the same `GhostLocalization.CurrentLanguageCode` state and supported-language list.

## Reference shell copy

`GhostReferenceText` contains workstation-specific shared text for menu names, Connection Log, Site Manager, Quick Connect helper copy, session-only/local privacy wording, search and sidebar concepts.

English is authoritative. Croatian has explicit reference-shell overrides. Other languages safely fall back to English where a dedicated override is not present.

## Transfer-management copy

`GhostTransferText` isolates queue-management labels such as Pause queue, Resume queue, Retry failed, selective cleanup and paused/active state. English remains authoritative; Croatian has explicit overrides and other configured languages receive guaranteed English fallback for missing transfer keys.

0.1.5 makes Pause queue / Resume queue more visible in the Windows Transfers header without creating a second localization source or changing the fallback contract.

## Windows Setup localization

Setup uses `GhostLocalization` plus `GhostSetupLocalization` for wizard-specific labels. Language can be selected on Welcome and is saved locally for the installed client. Setup language resolution remains entirely local.

## Technical/security strings

Security-sensitive behavior is not inferred from translated labels. Protocol/security mode is represented by typed `FtpSecurityMode` values and validated independently of UI language.

Plain FTP warnings, TLS validation, malformed-reply rejection, passive-mode validation, parser limits and destructive confirmations remain functional even when a label falls back to English.

## Linux font/input considerations

The Linux X11/XWayland renderer initializes a locale-aware Xlib font set and Unicode-capable fallback patterns. It draws UTF-8 strings through `Xutf8DrawString` while consuming the same language codes/resources as Windows.

## Windows text rendering

Windows uses WPF/Segoe UI Variable/Segoe UI fallback through the shared design layer. Text wrapping is enabled on explanatory copy and dialogs where longer localized strings may require extra height.

## Setup/layout resilience

The Windows application and Setup are resizable. Layouts use flexible grid columns, wrapping where appropriate and compact controls to reduce clipping risk for languages with longer text.

0.1.5 additionally persists/bounds more workstation splitter state. Localization must not depend on a specific saved splitter size; labels still need to degrade cleanly at the supported minimum layout.

## Adding or improving a translation

When a language override is extended:

1. keep the key identical to the English source key;
2. preserve placeholders/format semantics;
3. do not alter protocol/security behavior in translation code;
4. verify compact and normal widths;
5. preserve English fallback;
6. avoid online translation dependencies;
7. run WPF input/localization smoke tests and Linux compile/runtime smoke tests.

## Release verification

Source audit verifies English default code, representative language entries, the expected 29-language count and Setup consumption of the shared list. Windows UI smoke tests verify editable controls/localization behavior; Linux CI builds/runs the native renderer with the same language layer.

Authentic Windows capture is also inspected after meaningful layout changes so longer copy does not cause visible overlap/clipping in the canonical shell.

## Privacy

Language selection is a local preference. Ghost FTP does not upload language choice as analytics or use it for advertising/profile segmentation.
