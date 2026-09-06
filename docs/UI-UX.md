# Ghost FTP UI / UX Guidelines

This document defines the desktop interaction rules for Ghost FTP **0.1.6 Beta** and the Windows Setup experience. The goal is a clean, information-dense professional FTP workstation rendered in Ghost FTP's own modern visual language.

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
9. **Failure state must be deterministic.** Protocol/lifecycle/integrity failure returns the UI to a stable state.
10. **Transfer integrity outranks convenience.** Ghost FTP restarts or aborts an unverified partial instead of pretending stale bytes are safe to resume.

## Workstation hierarchy

The approved desktop information architecture is saved-server sidebar, menu/primary toolbar, Connection Log + Quick Connect, Local + Remote panes, Transfers queue and status area. This hierarchy is the visual contract on Windows and Linux even though WPF and X11 use different native primitives.

## Clean toolbar rule

The global toolbar is reserved for application-level Connect, Disconnect, Upload, Download, Refresh, Site Manager, Settings and Diagnostics where space permits. Contextual New folder, Rename and Delete actions stay inside Local/Remote panes.

## Quick Connect

Host receives flexible width; Port stays compact; Security remains readable; Username/Password stay editable at the supported minimum size. Connect/Disconnect and session-only controls remain secondary to credential entry. **Keep in this tab** is session-only and does not imply persistent password storage. Plain FTP approval is explicit for real non-Demo servers.

## Resizing model

Windows supports native resize/minimize/maximize/restore plus draggable sidebar, Connection Log / Quick Connect, Local/Remote and Transfers splitters. Window size/state, sidebar width, connection-panel height, Local/Remote ratio and Transfers height are persisted within bounded settings.

Linux reacts to X11/XWayland resize and condenses secondary toolbar/queue controls before hiding primary file-transfer operations.

## Local / Remote panes

Each pane shows title, current path, navigation, contextual actions, filter and file list. Local exposes Upload; Remote exposes Download. Both expose Refresh, New folder, Rename and Delete. Destructive actions use the danger treatment; primary transfers use the accent treatment; disabled commands visibly disable.

Server-provided listing text is never treated as trusted UI markup. The shared parser bounds lines/facts before entries are exposed to either renderer.

## Transfers queue

The queue communicates direction, state, progress, bytes, speed, ETA, retry count, source and destination.

### Pause / resume

**Pause queue** gates queued/retrying dispatch. Running transfers continue. **Resume queue** releases the dispatch gate. This behavior is identical across renderers because both use the same Core queue.

### Safe resume UX contract

0.1.6 safe resume is a Core integrity feature, not a new pause button or a promise that an active FTP stream can be frozen. An interrupted download may reuse a local partial only when the active endpoint and remote `SIZE`/`MDTM` identity match its bounded local resume metadata.

A legacy, corrupt or stale partial must restart from byte zero. If untrusted staged bytes cannot be removed, the transfer must fail closed before REST/RETR rather than silently reusing them. If the server cannot supply enough identity information, Ghost FTP favors a fresh transfer over unverifiable resume.

Downloaded bytes remain staged as `.ghostftp.part` until applicable post-transfer remote-revision validation succeeds. Any pre-existing destination remains untouched until that final commit. If the remote file changes while bytes are in flight, staged bytes are discarded and the previous destination is preserved.

Resume sidecars are implementation state and should not be presented as normal Ghost FTP user content. User-facing failure text should explain the integrity problem without exposing sidecar contents or credentials.

### Retry, cancellation and cleanup

Windows exposes selected/failed retry plus selected/all cancellation and richer context cleanup/path copy. Linux exposes principal retry/cancellation/cleanup controls in its native transfer header. Both use the same bounded Core queue states.

### Progress cadence and completion refresh

Transfer progress remains responsive without turning every data-buffer operation into a render operation. Core uses a bounded UI cadence and terminal state remains immediate. Windows coalesces burst completion refreshes to avoid one remote LIST per completed item.

### Coordinated shutdown

Closing while the queue is paused or active releases dispatch waiters, cancels outstanding work, waits for workers and prevents new dispatch. UI code treats queue shutdown as terminal.

## Connection Log

The log is a local operational surface, not a credential dump. It may show startup privacy state, profile count, connection/security status, listing/queue transitions and actionable errors. Passwords and resume sidecar contents must never be logged.

## Protocol and integrity error UX

Malformed FTP reply framing, invalid passive tuples, pathological listing input and remote-revision mismatch fail with actionable error state rather than partially trusted data.

For an in-flight remote-file change, user-facing text may explain that the server-side file changed during download and staged bytes were discarded while an existing destination was preserved. It must not imply a cryptographic checksum was performed when the decision was based on FTP `SIZE`/`MDTM` metadata.

## Visual language

`GhostReferencePalette` is canonical. Windows WPF, Windows Setup and Linux remain aligned around dark navy surfaces, restrained blue borders and violet accent. Use surface depth, compact radii, thin borders, typography, focus/selection states and consistent spacing rather than decorative effects.

## Density and accessibility

Ghost FTP is an information-dense utility, but compact controls retain readable focus state and usable hit targets. Editable controls remain genuinely focusable/editable. Tab order follows the visible workflow. F2/F5/Delete shortcuts map to the current selected context.

## Localization layout

English is primary/default/fallback and the product exposes 29 local selectable languages. Longer translated labels must not overlap controls. Missing technical strings fall back to English; no online localization service is permitted.

0.1.6 does not add a new visible settings screen or required localized command; its resume-integrity logic remains below the renderer. Existing English technical fallback rules apply to integrity failures without changing the enforcement decision.

## Windows / Linux parity

Windows/Linux parity means the same product workflow, security, privacy and transfer semantics, not pixel-identical native widgets. Both renderers share the same Core parser, transfer queue, pooled-buffer behavior and staged safe download-resume integrity rules.

See `docs/UI-PARITY.md` for the detailed parity contract.

## Windows Setup UX

Setup remains a premium Ghost FTP surface with product identity, canonical palette, step progress, 29-language selector, local-only/privacy messaging, explicit license acceptance, install/update/uninstall summary, transactional rollback messaging, resize support and completion/launch state.

The same installed `GhostFTP-Setup.exe` handles maintenance/uninstall. Busy install stages block accidental close.

## Regression UX gates

The local Demo regression validates the complete no-network Demo workflow. The general hardening suite validates protocol/parser/settings/lifecycle behavior. `GhostFTP.ResumeSelfTest` independently validates exact REST offset reuse, stale-identity restart, in-flight remote mutation rejection, preservation of an existing destination and fail-closed stale-partial cleanup on both Windows and Linux.

## Authentic screenshot gate

The canonical main screenshot is captured from the compiled Windows application at **1914 × 907**. Product documentation refreshes authentic captures after release-quality visual changes rather than editing screenshots manually or substituting conceptual art.

## Performance

UI handlers stay short. Network/transfer work uses async Core paths. Paused workers wait asynchronously rather than polling. Avoid blocking network calls, excessive full-list rebuilds, repeated post-transfer LIST requests and decorative effects that degrade large-list/concurrent-transfer interaction.

## Definition of done for a UI change

A UI change is complete when targets build, commands remain reachable, fields do not overlap, contextual actions are unambiguous, selection/keyboard behavior is correct, destructive actions are explicit, localization fallback works, security/privacy/integrity messaging is accurate, Demo/queue/protocol/resume/UI gates pass, and authentic captures are refreshed/inspected when visuals change.
