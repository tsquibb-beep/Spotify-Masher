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
- **Update `version.txt` at the end of every session** before committing HANDOVER.md
- The csproj reads `version.txt` at build time via MSBuild inline task and sets `<Version>`
- `HelpWindow` reads the version at runtime from `AssemblyInformationalVersionAttribute` — no file path dependency
- Follows SemVer: `MAJOR.MINOR.PATCH`
  - MAJOR: breaking changes / major rewrites
  - MINOR: new user-visible features
  - PATCH: bug fixes, UI polish, under-the-hood improvements
- To release: edit `version.txt`, commit, then `git tag vX.X.X && git push --tags`
- Then go to GitHub → Releases → create a new release from the tag, attach the published exe

## Session Handover
- At the end of every session, **replace** `HANDOVER.md` with a fresh document covering current state and what's next
- HANDOVER.md is not a history log — it is a handover brief for the next session only
- Permanent lessons and gotchas belong in CLAUDE.md, not HANDOVER.md

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

### Publish (single-file self-contained exe for distribution)
```powershell
cd C:\Users\tamas\Projects\Spotify-Masher\SpotifyMasher
dotnet publish -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```
Output: `publish\SpotifyMasher.exe` (~170MB — expected, the .NET runtime is bundled).

**Why self-contained only:** Framework-dependent single-file is similarly large for WPF because WPF's native rendering components are bundled regardless. Self-contained is the correct distribution choice.

The csproj automatically applies `IncludeNativeLibrariesForSelfExtract=true` and `DebugType=None` when `PublishSingleFile=true`, producing a true single exe with no companion files.

---

## Project Overview

A C# WPF application that:
1. Authenticates with Spotify via OAuth 2.0 PKCE — **no re-authorisation after first login**
2. Sits silently in the system tray
3. Listens for configurable global keyboard shortcuts
4. Executes Spotify API actions: Play/Pause, Volume, Next/Prev Track, Seek, Like, Add to Playlist

**User:** Tom Squibb (tsquibb@gmail.com, GitHub: tsquibb-beep)
**Repo:** https://github.com/tsquibb-beep/Spotify-Masher (public)

---

## Folder Structure

```
Spotify-Masher/
├── SpotifyMasher.slnx              ← Solution file (.NET 10 new format)
├── SpotifyMasher/
│   ├── SpotifyMasher.csproj        ← net10.0-windows, UseWPF, EnableWindowsTargeting
│   ├── App.xaml / App.xaml.cs      ← Startup, single-instance Mutex, tray icon init, startup registry
│   ├── MainWindow.xaml / .cs       ← Config UI: auth panel + hotkey DataGrid
│   ├── HelpWindow.xaml / .cs       ← Help & About modal (opened via '?' footer button)
│   ├── DwmHelper.cs                ← P/Invoke DWMWA_CAPTION_COLOR — midnight purple title bar
│   ├── Models/
│   │   ├── HotkeyBinding.cs        ← One hotkey row: KeysDisplay, Modifiers, Key, Action, Parameter
│   │   ├── AppConfig.cs            ← Serialisable config: ClientId + List<HotkeyBinding>
│   │   └── TokenData.cs            ← Stored OAuth tokens: AccessToken, RefreshToken, ExpiresAt
│   ├── Services/
│   │   ├── SpotifyAuthService.cs   ← PKCE OAuth flow, token refresh, token persistence
│   │   ├── SpotifyApiService.cs    ← All Spotify Web API actions (see below)
│   │   ├── HotkeyService.cs        ← NHotkey registration/unregistration + action dispatch
│   │   └── ConfigService.cs        ← Load/save AppConfig JSON from %AppData%\SpotifyMasher\
│   ├── Controls/
│   │   └── HotkeyBox.cs            ← Custom read-only TextBox that captures key combos
│   └── Resources/
│       ├── Styles.xaml             ← Dark modern theme (all implicit + keyed styles)
│       ├── icon.ico                ← App/tray icon
│       └── fuspotify256.png        ← Logo image shown in MainWindow header
├── screenshots/                    ← Screenshots for README.md (commit PNGs here)
├── README.md                       ← GitHub front page: overview, features, install, getting started
├── version.txt                     ← SemVer single source of truth
├── CLAUDE.md                       ← This file
├── HANDOVER.md                     ← Fresh session-end brief (replaced each session)
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
   - Starts `HttpListener` on `http://127.0.0.1:5001/callback/`
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

### Launch at Startup
- Tray menu has a checkable "Launch at Startup" item
- On enable: writes `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\SpotifyMasher` = `"<path-to-exe>"`
- On disable: deletes the registry value
- On app start: reads registry to sync the menu item's checked state
- **Portable-safe:** stores the exe path at the time of enabling — if the user moves the exe, they toggle it off and back on from the new location
- The `_startupMenuItem` field in `App.xaml.cs` holds the reference — `x:Name` does not work for elements inside `Application.Resources`, so it is found by iterating `ContextMenu.Items` at startup

### Single-instance guard
Named `Mutex("SpotifyMasherSingleInstance")` in `App.OnStartup`. If a second instance starts, it shows a MessageBox and calls `Shutdown()`.

### DWM title bar theming
- `DwmHelper.SetGreenTitleBar(window)` sets a midnight purple (#2D1B69) caption bar via `DwmSetWindowAttribute`
- COLORREF format is `0x00BBGGRR` (not RGB): `#2D1B69` → `0x00691B2D`
- Called in `OnSourceInitialized` (not constructor — HWND must exist first)
- Applied to both `MainWindow` and `HelpWindow`

### Spotify API actions (SpotifyApiService.cs)
| Method | API call | Notes |
|---|---|---|
| `AdjustVolumeAsync(delta)` | GET + PUT `/me/player/volume` | Clamps 0–100 |
| `PlayPauseAsync()` | GET + PUT `/me/player/play` or `pause` | Reads `is_playing` first |
| `NextTrackAsync()` | POST `/me/player/next` | |
| `PreviousTrackAsync()` | POST `/me/player/previous` | |
| `SeekAsync(deltaSeconds)` | GET + PUT `/me/player/seek` | Reads `progress_ms`, clamps to duration |
| `LikeCurrentTrackAsync()` | GET `/me/player/currently-playing` + PUT `/me/tracks` | |
| `AddToPlaylistAsync(param)` | GET currently-playing + POST `/playlists/{id}/tracks` | Accepts bare ID, `spotify:playlist:ID` URI, or full `https://open.spotify.com/playlist/...?si=...` URL — strips `?si=` automatically |

### Help & About window
- `HelpWindow.xaml/.cs` — modal dialog, `WindowStartupLocation="CenterOwner"`, `ResizeMode="NoResize"`
- Shows: Getting Started steps, action reference table, Playlist ID instructions, Tips, About/GitHub link
- Version read from `AssemblyInformationalVersionAttribute` at runtime (set at build time from `version.txt` via csproj MSBuild task)
- DWM purple title bar applied via same `DwmHelper.SetGreenTitleBar`

---

## Hard-Won Gotchas

| Symptom | Root cause | Fix |
|---|---|---|
| `dotnet --version` crashes with ICU error in WSL2 | Ubuntu 26.04 WSL2 missing libicu; can't sudo to install | Set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` (in ~/.bashrc); build still works fine |
| NETSDK1100 "To build targeting Windows…" | WPF is Windows-only; cross-compile from Linux needs a flag | Add `<EnableWindowsTargeting>true</EnableWindowsTargeting>` to csproj |
| `.slnx` not `.sln` | .NET 10 defaults to new solution format | Use `SpotifyMasher.slnx` in all `dotnet sln` commands |
| `HttpListener` needs exact prefix match | Spotify redirects to `http://127.0.0.1:5001/callback` (no trailing slash), but `HttpListener` requires a trailing slash on the prefix | Register prefix as `http://127.0.0.1:5001/callback/` — HttpListener still matches the exact URL |
| Refresh token rotation | Spotify sometimes returns a new refresh_token in the refresh response | Always check for and persist `refresh_token` in the refresh response, not just `access_token` |
| WPF DataGrid cells not editable | Default DataGrid cell edit mode requires a double-click | Template columns with embedded controls (TextBox, ComboBox) are always interactive — no double-click needed |
| Implicit vs keyed ScrollBar styles | Keyed styles don't reach inside DataGrid's own control template | Use an implicit `<Style TargetType="ScrollBar">` (no `x:Key`) in App.xaml merged resources — applies app-wide including DataGrid internals |
| DataGrid column header corner bleed | Coloured header background shows square corners through rounded outer Border | Set DataGridColumnHeader `Background=Transparent` — the outer Border's background shows through with correct rounded corners |
| Spotify playlist share URL has `?si=` suffix | Pasting a Spotify share link like `...?si=abc123` passes an invalid playlist ID to the API → HTTP 405 | `AddToPlaylistAsync` strips `?si=` and handles full URLs, `spotify:playlist:ID` URIs, and bare IDs |
| Debug toggle key not firing when child has focus | `OnPreviewKeyDown` override doesn't fire if a child (TextBox, DataGrid) marks the event handled | Use `AddHandler(PreviewKeyDownEvent, handler, handledEventsToo: true)` in the Window constructor |
| Window focus after tray restore | `Activate()` alone doesn't guarantee keyboard focus on Windows 11 | Call `window.Focus()` after `Activate()` in `ShowMainWindow()` |
| WPF Hyperlink in TextBlock | A Hyperlink is an inline element inside a `<TextBlock>`, not a standalone control | Use `<Run Text="..." /><Hyperlink NavigateUri="..." RequestNavigate="Handler">text</Hyperlink>` inside TextBlock; handler calls `Process.Start` with `UseShellExecute = true` |
| ComboBox bound to strings via ItemsSource | Using `<ComboBoxItem>` elements with `SelectedItem="{Binding}"` produces "System.Windows.Controls.ComboBoxItem: ..." strings | Use `ItemsSource="{x:Static local:MainWindow.AvailableActions}"` with a `IReadOnlyList<string>` static property |
| `x:Name` on elements inside `Application.Resources` | Named elements in a ResourceDictionary don't generate code-behind fields on the App class | Store a reference in a field; find the element by iterating the parent collection at runtime (e.g. `ContextMenu.Items.OfType<MenuItem>()`) |
| WPF single-file framework-dependent is still large | WPF's native rendering DLLs are bundled even without self-contained, making the size similar | Use self-contained only; framework-dependent offers no meaningful size saving for WPF |

---

## Spotify Developer App Setup (one-time)
1. Go to https://developer.spotify.com/dashboard
2. Create a new app (any name/description)
3. Add Redirect URI: `http://127.0.0.1:5001/callback` (Spotify requires the explicit IP — `localhost` is rejected since April 2025)
4. Copy the **Client ID** — paste it into Spotify Masher's auth panel
5. No Client Secret is needed (PKCE flow)

---

## Current Status

**v1.0.0 — publicly released on GitHub**
- Compiles cleanly from WSL2 (`dotnet build`, 0 errors, 0 warnings)
- Auth: Client ID input → PKCE browser flow → seamless silent token refresh on subsequent launches
- Hotkey table: collapsible, add/delete rows, HotkeyBox captures key combos, save re-registers all hotkeys
- All 7 actions implemented: Play/Pause, Change Volume, Next Track, Previous Track, Seek, Add to Liked, Add to Playlist
- System tray: starts hidden, dark-themed context menu (Show / Debug Log / Launch at Startup / Exit)
- Launch at Startup: registry-based toggle in tray menu, portable-safe
- Debug log: toggled via Ctrl+Shift+` or tray menu, hidden by default
- Help & About dialog: opened via `?` footer button, covers all actions + playlist ID instructions
- Auth hint text contains a clickable hyperlink to developer.spotify.com/dashboard
- Custom icon (`icon.ico`) and logo (`fuspotify256.png`) integrated
- Midnight purple DWM title bar on MainWindow and HelpWindow
- Config persisted to `%AppData%\SpotifyMasher\config.json`
- README.md on GitHub with badges, features, getting started instructions
- `screenshots/` folder ready for screenshots (referenced in README)
