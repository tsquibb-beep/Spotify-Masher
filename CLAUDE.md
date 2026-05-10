# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

# Spotify Masher

## Git Workflow
- After every meaningful change, commit with a clean descriptive message and push to GitHub
- Commit message format: imperative mood, subject line under 72 characters
- Remote: `origin` → `git@github.com:tsquibb-beep/Spotify-Masher.git` (SSH — not HTTPS)
- Git identity set locally: `user.name = Tom Squibb`, `user.email = tsquibb@gmail.com`
- SSH via Windows OpenSSH: `core.sshCommand = /mnt/c/Windows/System32/OpenSSH/ssh.exe`
- Always push immediately after committing — no unpushed local commits

## Versioning
- Version is stored in `version.txt` (project root) — single source of truth
- Follows SemVer: `MAJOR.MINOR.PATCH`
  - MAJOR: breaking changes / major rewrites
  - MINOR: new user-visible features
  - PATCH: bug fixes, UI polish, under-the-hood improvements
- To release: edit `version.txt`, commit, then `git tag v0.x.x && git push --tags`

## Session Handover
- At the end of every session, update `HANDOVER.md` with current state, what was done, and what's next

---

## Commands

### Environment (WSL2)
```bash
# .NET 10 is installed in ~/.dotnet — set these before running any dotnet command:
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
```
These are already added to `~/.bashrc` so new shells pick them up automatically.

### Build
```bash
cd /mnt/c/Users/tamas/Projects/Spotify-Masher/SpotifyMasher
dotnet build
```

### Run (must be run from Windows — WPF requires Windows)
Open a Windows terminal (PowerShell or CMD) and run:
```powershell
cd C:\Users\tamas\Projects\Spotify-Masher\SpotifyMasher
dotnet run
```
Or build from WSL2 then double-click `bin\Debug\net10.0-windows\SpotifyMasher.exe` from Windows Explorer.

### Publish (single-file exe for distribution)
```powershell
dotnet publish -r win-x64 --self-contained false -o publish/
```

---

## Project Overview

A C# WPF application that:
1. Authenticates with Spotify via OAuth 2.0 PKCE — **no re-authorisation after first login**
2. Sits silently in the system tray
3. Listens for configurable global keyboard shortcuts
4. Executes Spotify API actions (initially: volume adjustment)

**User:** Tom Squibb (tsquibb@gmail.com, GitHub: tsquibb-beep)

---

## Folder Structure

```
Spotify-Masher/
├── SpotifyMasher.slnx              ← Solution file (.NET 10 new format)
├── SpotifyMasher/
│   ├── SpotifyMasher.csproj        ← net10.0-windows, UseWPF, EnableWindowsTargeting
│   ├── App.xaml / App.xaml.cs      ← Startup, single-instance Mutex, tray icon init
│   ├── MainWindow.xaml / .cs       ← Config UI: auth panel + hotkey DataGrid
│   ├── Models/
│   │   ├── HotkeyBinding.cs        ← One hotkey row: KeysDisplay, Modifiers, Key, Action, Parameter
│   │   ├── AppConfig.cs            ← Serialisable config: ClientId + List<HotkeyBinding>
│   │   └── TokenData.cs            ← Stored OAuth tokens: AccessToken, RefreshToken, ExpiresAt
│   ├── Services/
│   │   ├── SpotifyAuthService.cs   ← PKCE OAuth flow, token refresh, token persistence
│   │   ├── SpotifyApiService.cs    ← GET /me/player (volume) + PUT /me/player/volume
│   │   ├── HotkeyService.cs        ← NHotkey registration/unregistration + action dispatch
│   │   └── ConfigService.cs        ← Load/save AppConfig JSON from %AppData%\SpotifyMasher\
│   ├── Controls/
│   │   └── HotkeyBox.cs            ← Custom read-only TextBox that captures key combos
│   └── Resources/
│       ├── Styles.xaml             ← Dark modern theme (Spotify green accent)
│       └── tray.ico                ← Placeholder icon (green circle on dark bg)
├── version.txt                     ← SemVer single source of truth
├── CLAUDE.md                       ← This file
├── HANDOVER.md                     ← Session-end summary
└── .gitignore
```

---

## Tech Stack

| Concern | Choice |
|---|---|
| Framework | .NET 10, WPF (net10.0-windows) |
| Language | C# 13 |
| Global hotkeys | NHotkey.Wpf 4.0.0 |
| System tray | Hardcodet.NotifyIcon.Wpf 2.0.1 |
| Spotify auth | PKCE OAuth 2.0 — manual HttpClient, no wrapper library |
| Config + tokens | JSON in `%AppData%\SpotifyMasher\` |
| Spotify API | Direct HttpClient calls |

---

## Config & Token Storage

Both files live in `%AppData%\SpotifyMasher\` (outside the repo — never committed):

- `config.json` — `{ "ClientId": "...", "Bindings": [...] }`
- `tokens.json` — `{ "AccessToken": "...", "RefreshToken": "...", "ExpiresAt": "..." }`

---

## Architecture Notes

### OAuth PKCE flow
1. `SpotifyAuthService.StartAuthAsync(clientId)`:
   - Generates `code_verifier` (random 64 bytes, base64url) and `code_challenge` (SHA256 → base64url)
   - Opens browser to Spotify auth URL
   - Starts `HttpListener` on `http://localhost:5001/callback/`
   - Waits up to 5 minutes for the redirect
   - Exchanges code → `access_token` + `refresh_token` via POST to `/api/token`
   - Persists tokens to `tokens.json`
2. `GetValidTokenAsync()`: checks `ExpiresAt`; if within 30 seconds of expiry, silently calls refresh endpoint with `grant_type=refresh_token`
3. **After first auth, the user never sees the browser again.** Refresh tokens do not expire unless the user explicitly disconnects or revokes access on Spotify.

### Hotkey registration
- `HotkeyService.RegisterAll()` calls `NHotkeyManager.Current.AddOrReplace()` for each `HotkeyBinding` where `Key != Key.None`
- Called once on startup (from saved config) and again after every "Save" in the UI
- `UnregisterAll()` is called before re-registering (on Save) and on app shutdown

### HotkeyBox control
- Inherits `TextBox`, `IsReadOnly = true`
- `OnPreviewKeyDown`: captures `Keyboard.Modifiers` + `e.Key`, builds display string ("Ctrl+F8"), updates bound `HotkeyBinding`
- Ignores lone modifier presses (Ctrl, Alt, Shift, Win alone)

### System tray
- `Hardcodet.NotifyIcon.Wpf.TaskbarIcon` defined in `App.xaml` resources (not a window, lives in the resource dictionary)
- App starts with no window visible; `MainWindow` is instantiated but `Show()` is not called
- Double-clicking tray icon / "Show" context menu item calls `MainWindow.Show()` + `Activate()`
- `MainWindow.OnClosing` cancels close and calls `Hide()` instead — app never truly closes via the X button
- "Exit" in tray context menu: unregisters hotkeys → disposes tray → releases Mutex → `Shutdown()`

### Single-instance guard
Named `Mutex("SpotifyMasherSingleInstance")` in `App.OnStartup`. If a second instance starts, it shows a MessageBox and calls `Shutdown()`.

### Spotify volume API
- GET `https://api.spotify.com/v1/me/player` → `device.volume_percent` (int 0–100)
- PUT `https://api.spotify.com/v1/me/player/volume?volume_percent={n}`
- `AdjustVolumeAsync(delta)` = get current → clamp(current + delta, 0, 100) → set

---

## Hard-Won Gotchas

| Symptom | Root cause | Fix |
|---|---|---|
| `dotnet --version` crashes with ICU error in WSL2 | Ubuntu 26.04 WSL2 missing libicu; can't sudo to install | Set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` (in ~/.bashrc); build still works fine |
| NETSDK1100 "To build targeting Windows…" | WPF is Windows-only; cross-compile from Linux needs a flag | Add `<EnableWindowsTargeting>true</EnableWindowsTargeting>` to csproj |
| `.slnx` not `.sln` | .NET 10 defaults to new solution format | Use `SpotifyMasher.slnx` in all `dotnet sln` commands |
| `HttpListener` needs exact prefix match | Spotify redirects to `http://localhost:5001/callback` (no trailing slash), but `HttpListener` requires a trailing slash on the prefix | Register prefix as `http://localhost:5001/callback/` — HttpListener still matches the exact URL |
| Refresh token rotation | Spotify sometimes returns a new refresh_token in the refresh response | Always check for and persist `refresh_token` in the refresh response, not just `access_token` |
| WPF DataGrid cells not editable | Default DataGrid cell edit mode requires a double-click | Template columns with embedded controls (TextBox, ComboBox) are always interactive — no double-click needed |

---

## Spotify Developer App Setup (one-time)
1. Go to https://developer.spotify.com/dashboard
2. Create a new app (any name/description)
3. Add Redirect URI: `http://localhost:5001/callback`
4. Copy the **Client ID** — paste it into Spotify Masher's auth panel
5. No Client Secret is needed (PKCE flow)

---

## Current Status

**v0.1.0 — initial build**
- WPF app compiles and builds cleanly (`dotnet build`, 0 errors, 0 warnings)
- Auth panel: Client ID input + "Authorise with Spotify" button → PKCE flow → seamless refresh
- Hotkey table: Keybinding | Action | Parameter — add/delete rows, HotkeyBox captures key combos
- System tray: app starts hidden, tray icon with Show/Exit menu
- Config persisted to `%AppData%\SpotifyMasher\config.json`
- Volume adjust: `AdjustVolumeAsync(delta)` clamps 0–100
- **Not yet run on Windows** — build verified from WSL2, runtime test pending
