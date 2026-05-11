# Spotify Masher — Handover

## What this is
A C# .NET 10 WPF app that sits in the system tray and fires global keyboard shortcuts to control Spotify. Auth is OAuth 2.0 PKCE — after the first browser login, tokens refresh silently forever.

## Current state
**v0.1.0 — fully functional, cleanly building.** All 7 hotkey actions work. The app has been verified to compile with 0 errors/warnings from WSL2.

## What was done across sessions

### Session 1 (initial build)
- Scaffolded WPF solution with NHotkey.Wpf and Hardcodet.NotifyIcon.Wpf
- Implemented PKCE OAuth flow, token refresh, config/token persistence
- Volume adjust via Spotify Web API
- Dark UI with hotkey DataGrid, system tray, single-instance Mutex

### Session 2 (features + polish)
- Added actions: Play/Pause, Next/Prev Track, Seek, Add to Liked, Add to Playlist
- Collapsible hotkeys section (Edit / Save toggle)
- Debug log (Ctrl+Shift+`, or tray menu), hidden by default
- Fixed: DataGrid scrollbar theming (implicit style), column header corner bleed (Transparent bg)
- Auth status/Connect/Disconnect moved to header row
- Custom icon (icon.ico) + logo (fuspotify256.png)
- Midnight purple DWM title bar
- Dark-themed tray context menu; fixed exit delay
- Font changed to Segoe UI Variable

### Session 3 (this session)
- Fixed `Add to Playlist` HTTP 405: playlist ID parser now strips `?si=` and accepts full `https://` share URLs, `spotify:playlist:ID` URIs, and bare IDs
- Auth hint text: `developer.spotify.com/dashboard` is now a clickable Hyperlink (green, underline on hover)
- Help & About dialog (`HelpWindow.xaml/.cs`): dark-themed modal with Getting Started, action reference table, Playlist ID instructions, Tips, and About/GitHub link. Opened via `?` button in the main window footer
- Hyperlink implicit style added to Styles.xaml

## Key files
| File | Purpose |
|---|---|
| `SpotifyMasher/App.xaml.cs` | Startup, single-instance guard, tray init, shared service singletons |
| `SpotifyMasher/MainWindow.xaml/.cs` | Main config UI: auth panel + collapsible hotkey DataGrid |
| `SpotifyMasher/HelpWindow.xaml/.cs` | Help & About modal |
| `SpotifyMasher/DwmHelper.cs` | P/Invoke for midnight purple DWM title bar |
| `SpotifyMasher/Services/SpotifyAuthService.cs` | PKCE flow, token refresh, token persistence |
| `SpotifyMasher/Services/SpotifyApiService.cs` | All 7 Spotify API actions |
| `SpotifyMasher/Services/HotkeyService.cs` | NHotkey registration + action dispatch |
| `SpotifyMasher/Controls/HotkeyBox.cs` | Key-capture TextBox control |
| `SpotifyMasher/Resources/Styles.xaml` | All dark theme styles (implicit + keyed) |

## Next session: Help/About improvements + layout/look polish

The user wants to improve the Help/About screen and adjust the overall layout and look. Possible directions to discuss at the start of the session:

### HelpWindow improvements
- **Tabbed or sectioned layout**: the current window is a long vertical scroll — consider a tab strip (Getting Started / Actions / About) or collapsible accordion sections
- **Action reference table rows**: Next Track and Previous Track are currently merged into one row ("Next / Prev Track") — split them for clarity
- **"Copy" button for Redirect URI**: add a small clipboard button next to the `http://127.0.0.1:5001/callback` redirect URI text so users can copy it without selecting text manually
- **Redirect URI copy button**: in Getting Started step 2, make the redirect URI easily copyable
- **Version number path**: currently uses a relative `../../../..` path from `BaseDirectory` which is fragile in a published build — consider embedding version as an assembly attribute instead
- **Window sizing**: currently `SizeToContent="Height"` with fixed Width=460 and a ScrollViewer — may want a fixed height with always-visible scrollbar instead
- **Visual hierarchy**: the section headings are just coloured TextBlocks — could use a left border accent line or a subtle divider for a more modern feel

### Main window layout/look polish
- The window is fairly minimal — the user may want to discuss spacing, proportions, or any elements that feel off
- Auth form section and hotkey table share similar visual weight — could differentiate more
- Consider whether the HotkeyBox cells look cramped at the current DataGrid row height
- The `?` button is currently a plain ghost-style rounded square — could be styled as a true circle or an icon

### Things to ask the user at session start
- Open the app on Windows and screenshot what looks off — this is the most useful input for layout work
- Which part of the Help dialog feels most in need of improvement: structure, content, or visual design?

## Critical things to know

**Build from WSL2; run from Windows.**
```bash
cd /mnt/c/Users/tamas/Projects/Spotify-Masher/SpotifyMasher
export PATH="$HOME/.dotnet:$PATH" && export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
dotnet build
```
Run from a Windows terminal: `dotnet run` or launch the `.exe` from Windows Explorer.

**`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` must be set** or dotnet crashes (libicu missing in this WSL2 env). Already in `~/.bashrc`.

**Redirect URI must use `127.0.0.1`, not `localhost`.** Spotify has rejected `localhost` since April 2025.

**Services are static singletons in `App`.** `App.AuthService`, `App.ApiService`, `App.HotkeyService`, `App.ConfigService`.
