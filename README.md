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

![Spotify Masher main window](screenshots/main.png)

![Spotify Masher key bindings](screenshots/keybinds.png)

---

## Features

- **Global hotkeys** — fire from any window, even fullscreen games
- **Play / Pause** the current track
- **Next / Previous** track
- **Seek** forward or backward by a configurable number of seconds
- **Volume** up or down
- **Like** the current track (adds to Liked Songs)
- **Add to Playlist** — paste any Spotify playlist URL, URI, or ID
- Starts silently in the system tray on launch
- Silent token refresh — no re-login after the first time
- Optional **Launch at Startup** from the tray menu

---

## Download

Go to the [Releases page](https://github.com/tsquibb-beep/Spotify-Masher/releases/latest) and download `SpotifyMasher.exe`.

No installation required — just run it from wherever you like.

> **Why is it ~170 MB?**  
> The .NET 10 runtime is bundled inside the exe so you don't need to install anything else. This is expected and normal for self-contained .NET apps.

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
2. Click **Edit**, add your hotkeys, and click **Save**

---

## Requirements

- Windows 10 or 11
- A Spotify account (Free or Premium)
- Nothing else — the runtime is bundled

---

## License

MIT
