# Ghost FTP desktop UI parity contract

This document defines the visual contract for Ghost FTP **0.1.0 Beta** across the Windows application, Windows portable build, Windows Setup and the Linux desktop client.

The approved workstation reference is a dense dark desktop FTP workspace. The implementation must preserve the same product identity, spacing system, information hierarchy and color tokens without introducing a web runtime, analytics framework, UI telemetry or a third-party runtime package.

## Canonical reference viewport

The supplied desktop reference is treated as a **1914 × 907 logical-pixel viewport at 96 DPI** for deterministic visual comparison.

The real Windows client therefore exposes a deterministic documentation capture mode at that viewport. CI captures the compiled WPF application itself; the reference-validation path must not substitute an externally generated mockup.

The reference viewport is a comparison target, not a fixed window-size requirement. Normal application windows remain resizable and must retain a usable layout above their documented minimum dimensions.

## Canonical design tokens

The authoritative source is `src/GhostFTP.Design/GhostReferencePalette.cs`.

Reference geometry:

- permanent left rail: **292 px** at the approved desktop reference width;
- application menu: **38 px**;
- global action toolbar: **70 px**;
- normal outer workspace gap: **10 px**;
- normal card radius: **10 px** where the native renderer supports rounded geometry;
- normal field radius: **9 px** where the native renderer supports rounded geometry;
- dense table/list row target: approximately **28–32 px**.

Reference dark palette:

```text
background      #091421
menu            #09131F
sidebar         #091521
toolbar         #0B1826
surface         #091725
surface-2       #0B1A29
surface-3       #0D1D2D
surface-hover   #10263A
text            #E8EEF8
muted           #8FA6C4
subtle          #6884A5
border          #193A53
border-strong   #244D6D
accent          #6464FF
accent-hover    #7474FF
accent-pressed  #5555E8
accent-soft     #222B63
success         #2BCB88
danger          #EF5265
warning         #DCA64A
```

Do not duplicate these values in new Windows or Setup code. Linux native drawing should consume the shared palette instead of maintaining an unrelated color system.

## Workstation hierarchy

At normal desktop width the application shell follows this order:

1. permanent left product/navigation rail;
2. menu row across the application workspace;
3. compact global action toolbar;
4. Connection Log and Quick Connect side by side;
5. Local and Remote file panes side by side;
6. full-width transfer queue;
7. compact local status/privacy state.

The left rail owns product identity, saved-site navigation, local privacy messaging and Settings/About access. Product identity is therefore not repeated as a large block in the action toolbar.

The upper-right area owns language access and global Remote search. Quick Connect remains a local connection form and must not become an account/cloud sign-in surface.

## Menu and toolbar contract

At the reference viewport the top-level menu order is:

```text
File → View → Transfers → Sites → Tools → Help
```

The corresponding Croatian reference order is:

```text
Datoteka → Prikaz → Prijenosi → Poslužitelji → Alati → Pomoć
```

The normal-width global action toolbar keeps the major FTP actions directly visible and uses the same compact icon-over-label treatment as the approved reference. It includes Connect, Disconnect, Upload, Download, Refresh, New Folder, Rename, Delete, Site Manager, Settings and Diagnostics where horizontal space permits.

New Folder, Rename and Delete are real contextual actions rather than decorative controls. Their target follows the active Local/Remote workspace and destructive actions remain disabled when no compatible selection exists.

When width is constrained, lower-priority actions may compact or move out of the visible toolbar while remaining available through menus/context actions. Search must never overlap an action target.

## Reference-shell localization

English remains the product's primary/default language and guaranteed fallback. The core localization catalog continues to provide the existing 29 selectable languages without any online translation service.

Reference-only shell copy is centralized in `src/GhostFTP.Design/GhostReferenceText.cs` so Windows and Linux do not grow unrelated hardcoded wording. The approved Croatian reference receives explicit shell wording for menu/navigation/privacy/search labels. Any reference-shell phrase not translated for the selected locale falls back locally to English; no language choice causes a network lookup.

Localization is a presentation concern only. It must not alter protocol behavior, data retention, telemetry policy or FTP/FTPS security defaults.

## Windows installed application and portable.exe

`GhostFTP.App` is the single Windows desktop UI source. The installed application and `portable.exe` are packaging variants of that same client and therefore must not diverge visually or functionally.

Portable mode may change only local data-path behavior. It must not switch to another renderer, add tracking, use a hosted UI or remove security controls.

The authentic Windows UI capture path remains the primary visual regression artifact. Documentation captures must be produced from the compiled product, never from an AI-generated mockup.

## Windows Setup

Setup is a different workflow and therefore does not display FTP file panes. It must nevertheless use the same Ghost FTP product identity, reference dark palette, typography scale, field/button treatment, density and privacy language.

Setup must remain a local native application. It must not embed a browser, download UI assets at runtime, send installation analytics or require a Ghost FTP account.

## Linux

`GhostFTP.Linux` is a native X11/XWayland renderer sharing the same FTP/FTPS Core and the same reference palette.

At normal desktop width the Linux client must preserve the same workstation hierarchy as Windows: permanent left rail, menu, toolbar, side-by-side log/Quick Connect, Local/Remote panes and Transfers queue.

X11 drawing primitives are not WPF, so antialiasing, font rasterization, native window chrome and exact glyph metrics can differ by Linux desktop/font configuration. Such OS-level raster differences are not an excuse to change layout, product colors, feature placement or privacy behavior.

## Responsive behavior

The approved desktop reference is not a license to make the application fixed-size. Both clients must remain usable when resized.

When width becomes constrained, controls may compact, ellipsize or reflow while preserving the same logical order. File panes and Transfers must retain usable minimum dimensions. Resizing must never expose controls outside the window or make a destructive action overlap an unrelated target.

## Privacy and dependency invariant

Visual parity work must not weaken the project architecture. In particular it must not add:

- application telemetry or analytics;
- crash-upload SDKs;
- advertising/tracking libraries;
- a cloud profile service;
- a hidden update checker;
- embedded web UI frameworks;
- third-party NuGet `PackageReference` dependencies in shipping projects;
- background product requests unrelated to the FTP/FTPS server explicitly selected by the user.

All reference assets and localization remain compiled/local. User connection data, profiles and settings stay on the user's device.

## Validation

A UI change is acceptable only when the normal build/security gates remain green and the authentic Windows capture remains renderable. Linux must continue to pass the real X11/XWayland smoke test under Xvfb and the packaged x64 runtime smoke test.

Source-level parity checks may validate canonical token values and required shell structures, but they do not justify a claim of mathematical pixel identity. A pixel-level claim requires comparing authentic captures produced by the shipping renderer against the approved reference at the same viewport and DPI.
