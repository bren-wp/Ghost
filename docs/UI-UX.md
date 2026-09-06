# Ghost FTP UI / UX Guidelines

This document defines the desktop interaction rules for Ghost FTP **0.1.4 Beta** and the Windows Setup experience. The goal is a clean, information-dense professional FTP workstation rendered in Ghost FTP's own modern visual language.

Ghost FTP is the product. **BRENDIGO LTD** is the developer/publisher shown on legal and publisher surfaces.

## Core UX principles

1. **The transfer target must be obvious.** Local and Remote operations belong with the pane they affect.
2. **Security state must be understandable.** Explicit FTPS is recommended; plaintext FTP warns before use.
3. **Credentials remain visually private.** Password values are masked and never echoed into the connection log.
4. **Desktop space is user-controlled.** Users can resize the main window and important workstation regions.
5. **Primary work remains visible.** Optional controls yield before they overlap connection or transfer actions.
6. **No account wall.** Users connect directly to their own FTP/FTPS server.
7. **No decorative fake functionality.** Canonical screenshots are generated from the compiled application.
8. **Queue state must be truthful.** Pause queue pauses new dispatch; it does not pretend to freeze an active FTP byte stream.
9. **Failure state must be deterministic.** Protocol or lifecycle failure returns the UI to a stable state rather than leaving ambiguous busy controls.

## Workstation hierarchy

The approved desktop information architecture is saved-server sidebar, menu/primary toolbar, Connection Log + Quick Connect, Local + Remote panes, Transfers queue and status area. This hierarchy is the visual contract on Windows and Linux even though WPF and X11 use different native primitives.

## Clean toolbar rule

The global toolbar is reserved for application-level Connect, Disconnect, Upload, Download, Refresh, Site Manager, Settings and Diagnostics where space permits. Contextual New folder, Rename and Delete actions stay inside Local/Remote panes.

## Quick Connect

Host receives flexible width; Port stays compact; Security remains readable; Username/Password stay editable at the supported minimum size. Connect/Disconnect and session-only controls are secondary to credential entry. **Keep in this tab** is session-only and does not imply persistent password storage. Plain FTP approval is explicit for real non-Demo servers.

## Resizing model

Windows supports native resize/minimize/maximize/restore plus draggable sidebar, connection-area, Local/Remote and Transfers splitters. Split ratios and maximized state persist locally.

Linux reacts to X11/XWayland resize and condenses secondary toolbar/queue controls before hiding primary file-transfer operations.

## Compact window behavior

Do not solve compact layout by creating permanent horizontal overflow. Hide optional duplication, ellipsize secondary copy, wrap explanatory dialog text and preserve Local/Remote and transfer actions first.

## Local / Remote panes

Each pane shows title, current path, navigation, contextual actions, filter and file list. Local exposes Upload; Remote exposes Download. Both expose Refresh, New folder, Rename and Delete. Destructive actions use the danger treatment; primary transfers use the accent treatment; disabled commands visibly disable.

## File-list behavior

Lists prioritize Name, Type, Size, Modified and Remote Permissions where available. Folders sort before files. Columns resize with available pane width. Double-click behavior remains predictable: folders navigate, local files use OS behavior and remote files trigger the established download workflow.

## Transfers queue

The queue communicates direction, state, progress, bytes, speed, ETA, retry count, source and destination.

### Pause / resume

**Pause queue** gates queued/retrying dispatch. Running transfers continue. **Resume queue** releases the dispatch gate. This behavior is identical across renderers because both use the same Core queue.

### Retry, cancellation and cleanup

Windows exposes selected/failed retry plus selected/all cancellation and richer context cleanup/path copy. Linux exposes the principal retry/cancellation/cleanup controls in its native transfer header. Both use the same underlying bounded queue states.

### Coordinated shutdown

0.1.4 hardens the shared shutdown semantics. Closing the application while the queue is paused or active releases dispatch waiters, cancels outstanding work, waits for workers and prevents new dispatch. UI code should treat queue shutdown as terminal rather than attempting to enqueue more jobs.

## Connection Log

The log is a local operational surface, not a credential dump. It may show startup privacy state, profile count, connection/security status, listing/queue transitions and actionable operation errors. Passwords must never be logged.

## Protocol-error UX

0.1.4 rejects malformed FTP reply framing and invalid passive-mode tuples more strictly. User-facing errors should describe the connection/listing failure without reproducing credentials or presenting malformed server input as trusted UI markup.

A valid preliminary `120 -> 220` greeting is handled transparently; a server stuck in repeated preliminary greetings eventually fails within the bounded protocol contract.

## Visual language

`GhostReferencePalette` is canonical. Windows WPF, Windows Setup and Linux remain aligned around dark navy surfaces, restrained blue borders and violet accent. Use surface depth, compact radii, thin borders, typography, focus/selection states and consistent spacing rather than decorative effects.

## Density and accessibility

Ghost FTP is an information-dense utility, but compact controls must retain readable focus state and usable hit targets. Editable controls remain genuinely focusable/editable. Tab order follows the visible workflow. F2/F5/Delete shortcuts map to the current selected context.

## Localization layout

English is primary/default/fallback and the product exposes 29 local selectable languages. Longer translated labels must not overlap controls. Missing technical strings fall back to English; no online localization service is permitted.

## Windows / Linux parity

**Windows / Linux parity** means the same product workflow, security, privacy and transfer semantics, not pixel-identical native widgets.

Both expose saved sites and Quick Connect, FTP / Explicit FTPS / Implicit FTPS, Local/Remote operations, transfer queue/cancellation/retry/pause, diagnostics/logging, keepalive, 29 local languages, local-only profile settings and Demo mode.

The 0.1.4 protocol and shutdown hardening sits below both renderers in Core, so valid/malformed server behavior and lifecycle state are shared rather than reimplemented in UI code.

See `docs/UI-PARITY.md` for the detailed parity contract.

## Windows Setup UX

Setup remains a premium Ghost FTP surface with product identity, canonical palette, step progress, 29-language selector, local-only/privacy messaging, explicit license acceptance, install/update/uninstall summary, transactional rollback messaging, resize support and completion/launch state.

The same installed `GhostFTP-Setup.exe` handles future maintenance/uninstall. Busy install stages block accidental close.

## Error UX

User-facing errors state the operation and useful reason without leaking credentials. Examples include invalid host/port, TLS failure, malformed server protocol response, passive data negotiation failure, unavailable initial path, remote permission failure, local filesystem error, cancellation and package/Setup rollback failure.

The application recovers to a stable Offline/connected state after failure rather than leaving an ambiguous busy state.

## Local Demo regression UX gate

The built-in Demo profile is part of release UX validation. The **Local Demo regression UX gate** verifies a complete local workflow can connect, navigate, list, transfer, rename, create/delete and disconnect without external network activity.

## Protocol/shutdown regression UX gate

The deterministic hardening suite verifies that the UI's underlying shared Core can safely handle concurrent session/queue disposal, reject malformed reply framing and interoperate with a real loopback passive data flow. This prevents visible controls from depending on untested lifecycle assumptions.

## Authentic screenshot gate

The canonical main screenshot is captured from the compiled Windows application at **1914 × 907**. Product documentation refreshes authentic captures after release-quality visual changes rather than editing screenshots manually or substituting conceptual art.

## Performance

UI handlers stay short. Network/transfer work uses async Core paths. Paused workers wait asynchronously rather than polling. Avoid blocking network calls, excessive full-list rebuilds and decorative effects that degrade large-list/concurrent-transfer interaction.

## Definition of done for a UI change

A UI change is complete when the target builds, commands remain reachable at supported sizes, fields do not overlap, target-sensitive actions are not duplicated ambiguously, keyboard/mouse selection maps correctly, destructive actions are explicit, localization fallback works, security/privacy messaging remains accurate, Demo/queue/protocol/UI smoke gates pass, and authentic captures are refreshed when visuals change.
