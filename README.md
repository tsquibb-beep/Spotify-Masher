<p align="center">
  <img src="fuspotify256.png" width="256" alt="Spotify Masher logo">
</p>

<h1 align="center">Spotify Masher</h1>

<p align="center">
  Control Spotify from anywhere with global keyboard shortcuts — no clicking required.
</p>

<p align="center">
  <a href="https://github.com/tsquibb-beep/Spotify-Masher/releases/latest"><img src="https://img.shields.io/github/v/release/tsquibb-beep/Spotify-Masher?style=flat-square" alt="Latest Release"></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-blue?style=flat-square" alt="Platform">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square" alt=".NET 10">
</p>

---

## Overview

Spotify Masher is a Windows system tray app. Set up hotkeys once, and control Spotify from any app, any window — no switching focus, no clicking.

After a one-time login it runs silently in the background, refreshing its Spotify connection automatically.

---

## What's it look like?

![Spotify Masher main window](screenshots/main.png?v=3)

![Spotify Masher hotkey bindings](screenshots/hotkeys.png?v=1)

![Spotify Masher notification settings](screenshots/notifications.png?v=1)

---

## Features

- **Global hotkeys** — fire from any window, even fullscreen games
- **Play / Pause** the current track
- **Next / Previous** track — toast shows the new track name and artist
- **Seek** forward or backward by a configurable number of seconds
- **Volume** up or down
- **Like** the current track (adds to Liked Songs)
- **Add to Playlist** — paste any Spotify playlist URL, URI, or ID
- **Show Current Track** — toast with track name, artist, album, and cover art
- **Toast notifications** — customisable position, duration, and per-app overrides
- Starts silently in the system tray on launch
- Silent token refresh — no re-login after the first time
- Optional **Launch at Startup** from the tray menu

---

## Download

Go to the [Releases page](https://github.com/tsquibb-beep/Spotify-Masher/releases/latest) and grab the version that suits you:

| File | Size | Requirement |
|---|---|---|
| `SpotifyMasher.exe` | ~170 MB | Nothing — just run it |
| `SpotifyMasher-slim.exe` | ~3 MB | [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) |

No installation required — run from wherever you like.

> **Which should I pick?**  
> If you're unsure, grab `SpotifyMasher.exe` — it works on any Windows 10/11 machine with no prerequisites. The slim version is for people who already have .NET 10 installed and want a smaller file. Make sure you have the **Desktop Runtime** (not just the base .NET Runtime) — it's the middle column on the download page.

---

## Getting Started

### 1. Create a Spotify Developer app (one-time)

1. Go to [developer.spotify.com/dashboard](https://developer.spotify.com/dashboard) and create a new app
2. Under **Redirect URIs**, add exactly: `http://127.0.0.1:5001/callback`
3. Copy your **Client ID**

### 2. Connect Spotify Masher

1. Launch `SpotifyMasher.exe`
2. Paste your Client ID and click **Connect** — your browser will open for a one-time login
3. After authorising, the browser closes and the app is ready

### 3. Set up your hotkeys

1. Open the app from the system tray
2. Click **Edit Hotkeys**, add your hotkeys, and click **Save**

---

## Requirements

- Windows 10 or 11
- A Spotify account (Free or Premium)
- Nothing else — the runtime is bundled

---

## License

MIT
