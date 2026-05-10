# Spotify Masher — Handover

## What this is
A C# WPF app that sits in the system tray and fires global keyboard shortcuts to control Spotify volume (more actions to come). Auth is OAuth 2.0 PKCE — after the first browser login the user never sees the auth page again; tokens refresh silently.

## State
**v0.1.0 — initial build.** Compiles cleanly from WSL2 (`dotnet build`, 0 errors). Not yet run on Windows — that is the first thing to do next session.

## What was done this session
- Installed .NET 10.0.203 in WSL2 via `dotnet-install.sh` (into `~/.dotnet`); set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` in `~/.bashrc`
- Scaffolded WPF solution with `NHotkey.Wpf` and `Hardcodet.NotifyIcon.Wpf`
- Implemented all models, services, controls, and UI:
  - PKCE OAuth flow with seamless silent token refresh
  - Volume adjust via Spotify Web API
  - Global hotkey registration via NHotkey
  - Dark modern UI (Spotify green accent) with hotkey DataGrid
  - System tray with Show/Exit menu, single-instance Mutex
- Created CLAUDE.md, HANDOVER.md, version.txt, .gitignore
- Pushed initial commit to `git@github.com:tsquibb-beep/Spotify-Masher.git`

## Key files
| File | Purpose |
|---|---|
| `SpotifyMasher/App.xaml.cs` | Startup, single-instance guard, tray init, shared service singletons |
| `SpotifyMasher/MainWindow.xaml` | Full config UI XAML |
| `SpotifyMasher/MainWindow.xaml.cs` | Auth flow wiring, hotkey grid, save/load |
| `SpotifyMasher/Services/SpotifyAuthService.cs` | PKCE flow, token refresh, token persistence |
| `SpotifyMasher/Services/SpotifyApiService.cs` | GET+PUT volume API calls |
| `SpotifyMasher/Services/HotkeyService.cs` | NHotkey registration + action dispatch |
| `SpotifyMasher/Controls/HotkeyBox.cs` | Key-capture TextBox control |
| `SpotifyMasher/Resources/Styles.xaml` | Dark theme styles |

## Critical things to know

**Build from WSL2 only; run from Windows.**
`dotnet build` works in WSL2 with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`. But WPF apps only run on Windows — use `dotnet run` from a Windows terminal or launch the compiled `.exe` from Windows Explorer.

**`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` must be set** or `dotnet` crashes at startup in this WSL2 environment (libicu not installed, can't sudo). It's in `~/.bashrc` so it persists.

**Redirect URI must match exactly.** Spotify Dashboard redirect URI must be `http://localhost:5001/callback` (no trailing slash from Spotify's side). The `HttpListener` prefix is registered with a trailing slash — this is intentional; HttpListener requires it.

**Refresh token does not expire** unless the user disconnects in the UI or revokes access at spotify.com/account. After first auth the app silently refreshes via the stored refresh token every ~hour.

**Services are static singletons in `App`.** `App.AuthService`, `App.ApiService`, `App.HotkeyService`, `App.ConfigService` — these are the single instances used everywhere.

## Likely next steps
- **First priority:** Run on Windows and test the auth flow end-to-end
- Fix any runtime issues discovered on Windows
- Add placeholder text (watermark) to the `ClientIdBox` (currently uses `Tag` which doesn't auto-display)
- Test hotkey registration and volume change with Spotify playing
- Polish: show current volume in the status bar when connected
- Future actions: Play/Pause, Next/Previous track (add to the Action ComboBox)
