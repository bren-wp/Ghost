# Ghost FTP live server smoke test

Ghost FTP contains an **opt-in, non-destructive** live FTP/FTPS smoke harness for validating a real server without committing credentials to source control.

## Security rule

Never place a real FTP username, password, server secret or private key in:

- source files;
- README examples;
- workflow YAML;
- issue or pull-request comments;
- command-line arguments that can appear in process listings;
- screenshots or CI logs.

The live harness reads connection data from process environment variables. The GitHub workflow maps those variables from repository Actions secrets.

## Required GitHub Actions secrets

Configure these repository secrets before manually running **Live FTP smoke test**:

```text
GHOSTFTP_LIVE_HOST
GHOSTFTP_LIVE_USERNAME
GHOSTFTP_LIVE_PASSWORD
```

Optional secrets:

```text
GHOSTFTP_LIVE_PORT          default 21 for explicit/plain, 990 for implicit
GHOSTFTP_LIVE_SECURITY      explicit (default), implicit, or plain
GHOSTFTP_LIVE_PATH          default /
GHOSTFTP_LIVE_ALLOW_PLAIN   set to 1 only when plain FTP is intentionally required
```

Do not use workflow-dispatch text inputs for passwords. Repository secrets are masked by GitHub and are not stored in the repository history.

## What the smoke test does

The harness performs only this sequence:

1. validate local configuration;
2. create `FtpSession` using the selected FTP/FTPS mode;
3. connect and authenticate;
4. verify TLS state when FTPS is requested;
5. issue `PWD`;
6. optionally change to the configured read-only test path;
7. issue a directory listing (`MLSD` or `LIST` through normal capability logic);
8. issue the same server-only `NOOP` keepalive used by the application;
9. disconnect cleanly.

It does **not** upload, download, rename, delete or create remote data.

## Local execution

Use environment variables rather than putting secrets on the command line. Example names only:

```powershell
$env:GHOSTFTP_LIVE_HOST = '<server>'
$env:GHOSTFTP_LIVE_PORT = '21'
$env:GHOSTFTP_LIVE_USERNAME = '<username>'
$env:GHOSTFTP_LIVE_PASSWORD = '<password>'
$env:GHOSTFTP_LIVE_SECURITY = 'explicit'
$env:GHOSTFTP_LIVE_PATH = '/'

dotnet run --project tests/GhostFTP.LiveSmoke/GhostFTP.LiveSmoke.csproj -c Release
```

Clear the environment variables when finished.

## Plain FTP

Plain FTP transmits credentials and content without TLS. The harness rejects plain FTP unless `GHOSTFTP_LIVE_ALLOW_PLAIN=1` is explicitly set. This mirrors the desktop product policy that plain FTP must never be an accidental downgrade.

Prefer explicit FTPS on port 21 when the server supports it.

## Logs and redaction

The harness never intentionally prints the configured password, username or host. If an exception message happens to contain one of those values, the harness replaces it with `[redacted]` before writing the failure message.

The normal deterministic CI does not have access to live credentials and therefore does not run this workflow automatically.
