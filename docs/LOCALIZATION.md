# Ghost FTP Localization Architecture

The current **Ghost FTP 0.1.7 Beta** line includes a shared dependency-free localization layer used by Windows, Linux and Windows Setup.

## Primary language and fallback

**English (`en`) is the primary language, default language and final fallback.**

A missing technical string must never crash Ghost FTP or leave a blank primary action. When a localized value is unavailable, the English value is returned. Unknown or malformed stored language codes normalize to English.

## 29 selectable languages

Ghost FTP exposes **29 selectable languages** from local application resources. The catalog includes English, Croatian and the other languages represented in `GhostLocalization.SupportedLanguages`.

Source audit verifies that the catalog count remains 29 for the active release contract.

## Offline-only localization

Ghost FTP does not use an online translation API. UI text, Setup strings and technical fallback strings are resolved locally. Connection details, filenames, server information, resume metadata and credentials are never sent to a translation service.

## Shared localization layer

`GhostLocalization` owns supported-language metadata, current language selection, normalization of stored/requested codes, English source strings, localized override dictionaries and formatting helpers.

Windows and Linux consume the same `GhostLocalization.CurrentLanguageCode` state and supported-language list.

## Reference shell copy

`GhostReferenceText` contains workstation-specific shared text for menu names, Connection Log, Site Manager, Quick Connect helper copy, session-only/local privacy wording, search/sidebar concepts and compact workspace guidance.

0.1.7 expands this catalog with local/remote double-click hints, splitter resize/reset guidance, connection-status diagnostics text, TLS-first wording and the principal Site Manager section/description strings.

English is authoritative. Croatian has explicit reference-shell overrides. Other configured languages safely fall back locally to English where a dedicated override is not present. This fallback never performs a network request.

## Transfer-management copy

`GhostTransferText` isolates queue-management labels such as Pause queue, Resume queue, Retry failed, selective cleanup and paused/active state. English remains authoritative; Croatian has explicit overrides and other configured languages receive guaranteed English fallback for missing transfer keys.

0.1.7 retains the visible Windows Pause queue / Resume queue action and the shared queue semantics established in the preceding release line without introducing another localization source.

## Resume-integrity messages

Safe download resume is a Core protocol/filesystem invariant, not a translated UI decision. Host/port/security/path/SIZE/MDTM matching, fail-closed stale-partial cleanup and staged commit behavior are executed independently of selected UI language.

User-facing resume/integrity errors may use the English technical fallback when a dedicated translation is unavailable. A missing translation must never weaken the rule that untrusted staged data is rejected or that an existing destination remains untouched until validation succeeds.

## Windows Setup localization

Setup uses `GhostLocalization` plus `GhostSetupLocalization` for wizard-specific labels. Language can be selected on Welcome and is saved locally for the installed client. Setup language resolution remains entirely local.

## Technical/security strings

Security-sensitive behavior is never inferred from translated labels. FTP security mode is represented by typed `FtpSecurityMode` values and validated independently of UI language.

Plain FTP warnings, TLS validation, malformed-reply rejection, passive-mode validation, parser limits, destructive confirmations and safe resume-integrity enforcement remain functional when a label falls back to English.

## Linux font/input considerations

The native Linux X11/XWayland renderer initializes a locale-aware Xlib font set and Unicode-capable fallback patterns. It draws UTF-8 strings through `Xutf8DrawString` while consuming the same language codes/resources as Windows.

The 0.1.7 window/theme changes do not introduce a second Linux localization source. Appearance and geometry remain local renderer state while labels still resolve through the shared catalog/reference-text fallback.

## Windows text rendering

Windows uses WPF/Segoe UI Variable/Segoe UI fallback through the shared design layer. Explanatory copy and dialogs wrap where longer localized strings require additional height. Native WPF editor templates remain intact for text entry and selection behavior.

## Setup/layout resilience

The Windows application and Setup are resizable. Layouts use flexible grid columns, wrapping and compact controls to reduce clipping risk for languages with longer text. Persisted workstation dimensions are bounded before use; localization must not depend on one saved splitter size.

0.1.7 also gives workspace splitters localized tooltips and visible hover feedback, keeping resize behavior discoverable without permanently adding more text to the main layout.

## Adding or improving a translation

When a language override is extended:

1. keep the key identical to the English source key;
2. preserve placeholders and formatting semantics;
3. do not alter protocol/security behavior in translation code;
4. verify compact and normal widths;
5. preserve English fallback;
6. avoid online translation dependencies;
7. run Windows WPF input/localization smoke tests and Linux compile/runtime smoke tests.

## Release verification

Source audit verifies English default code, representative language entries, the exact 29-language count and Setup consumption of the shared list. Windows UI smoke tests verify editable controls/localization behavior; Linux CI runs the native renderer with the same language layer.

0.1.7 UI smoke additionally verifies required reference-shell English copy, explicit Croatian reference text and local English fallback for configured languages without dedicated reference overrides.

The safe download resume integrity suite continues to run on Windows and Linux. That test is language-independent by design, proving that integrity behavior cannot drift with renderer or localization state.

Authentic Windows capture is inspected after meaningful layout changes so longer copy does not cause visible overlap or clipping in the canonical shell.

## Privacy

Language selection is a local preference. Ghost FTP does not upload language choice as analytics or use it for advertising/profile segmentation.
