# Primal Meter

[🇧🇷 Português](README.md) · 🇺🇸 English

A combat meter for Final Fantasy XIV, ACT-style, with a modern interface and **no plugins at all**.
It's a single `PrimalMeter.exe`: open it, it sits on top of the game and starts measuring. No XIVLauncher, Dalamud, IINACT or ACT needed.

**[⬇ Download the latest version](https://github.com/saantanavitor/primal-meter/releases/latest)**

- **DPS, healing and damage taken** for the whole party, updated every second
- **Per-player details**: critical, direct hit, max hit, overheal and **damage by ability**
- **History** of the last 20 fights
- **Lock mode**: the mouse goes straight through to the game
- **Replay log**: review past fights outside the game
- **Demo** with a simulated fight
- In **Portuguese or English** (Portuguese is the default; switch in the ⋯ menu)
- Uses the **same parsing engine as ACT**, so the numbers match ACT and IINACT

---

## Installation

1. On the [Releases](https://github.com/saantanavitor/primal-meter/releases/latest) page, download `PrimalMeter.exe`.
2. Put it wherever you like, for example `Documents\PrimalMeter`. There's no installer, and .NET doesn't need to be installed.
3. The first time you open it, Windows may show a blue warning. See [the Windows warning](#the-windows-warning-is-not-a-virus) below.
4. In the game, under **System Configuration → Display Settings**, set the screen mode to **Borderless Windowed**. In exclusive full screen, no program can appear on top of the game.

On the first run, the meter downloads the ACT parsing engine from its author's official page (about 4 MB) and shows "Preparing…" meanwhile. After that, it only checks whether a new version is out.

To use it in English, open the **⋯** menu and click **"Idioma: Português · switch to English"**. The choice is saved.

---

## How to use

1. Open FFXIV the way you already do: Steam, the official launcher or XIVLauncher, it doesn't matter.
2. Open `PrimalMeter.exe`, before or after the game. The meter appears on the right side of the screen.
3. Check the dot next to the title:
   - **yellow, "Waiting for the game…"**: FFXIV isn't open yet;
   - **green, "Live"**: it's measuring.
4. Adjust the position: **drag the top** to move it and **pull the bottom-right corner** to resize it. Position and size are saved.
5. **Lock it before playing** with **Ctrl+Alt+L**. When locked, the meter ignores the mouse and clicks go to the game. Use the same shortcut to unlock.

| Shortcut | What it does |
| --- | --- |
| **Ctrl+Alt+L** | Lock / unlock (clicks go through to the game) |
| **Ctrl+Alt+H** | Show / hide the meter |

Both shortcuts can be changed in the **⋯ menu → "Lock shortcut"** and **"Show/hide shortcut"**: click and press the new combination (with Ctrl or Alt). If another program already uses it, the meter tells you. Up to version 0.4.1 the shortcuts were Ctrl+Shift+L and Ctrl+Shift+H, but Ctrl+Shift+L conflicts with Radeon Software.

**In the meter**

- **DPS, Heal and Tank tabs**: each one sorts the party by a different number.
- **Click a player** to see the details and the damage (or healing) of each ability.
- **"Fight" selector**: opens previous fights. The last 20 are saved.
- **⋯ menu**:
  - demo and log replay;
  - how DPS is calculated (by fight duration, like ACT, or by each player's active time);
  - background opacity;
  - data source (built-in engine or IINACT);
  - language (Portuguese or English);
  - clear history and quit.

**Tray icon** (next to the Windows clock): click it to show or hide the meter. Right-click opens the same options as the menu.

**Replay log**: the meter saves one log per day in `Documents\PrimalMeter`, in the same network log format as ACT and IINACT. In the ⋯ menu, choose "Replay log…" and open one of those files. Use ▶/❚❚ to pause, 1×–64× to change the speed and ⏭ to process everything at once.

**Already using IINACT?** In the ⋯ menu, switch the source to "IINACT". The meter then reads its data, like in earlier versions (`ws://127.0.0.1:10501/ws`).

---

## The Windows warning is not a virus

The first time you open `PrimalMeter.exe`, Windows may show:

> **Windows protected your PC**
> Microsoft Defender SmartScreen prevented an unrecognized app from starting.

To open it, click **More info** and then **Run anyway**. The warning won't show again for that file.

### Why the warning shows up

SmartScreen **didn't find any virus**. It warns about **every program it doesn't know yet**, and a program only becomes known in two ways:

1. **A code-signing certificate.** It's paid, costs hundreds of dollars a year, and is issued to verified companies or people. Primal Meter is a free personal project and doesn't have one yet.
2. **Reputation.** After many people download the same file without problems, the warning goes away by itself.

Many FFXIV community programs, including plugins, tools and ACT itself, show the same warning for the same reason. The warning says Windows **doesn't know** the program, not that it's dangerous.

Some antivirus programs may be suspicious of Primal Meter for a more specific reason: it **injects a DLL into the game** to read combat (see below). It's the same technique ACT uses, which is why ACT is also often flagged by stricter antivirus programs. If yours blocks it, add an exception for `PrimalMeter.exe` and for the `C:\ProgramData\PrimalMeter` folder.

### What the program does on your computer

To avoid depending on plugins, Primal Meter does exactly what ACT does:

- **Downloads the ACT parsing engine** (FFXIV_ACT_Plugin, by Ravahn) from its [official releases page](https://github.com/ravahn/FFXIV_ACT_Plugin/releases) and keeps it in `%LOCALAPPDATA%\PrimalMeter\engine`. Every time it opens, it checks that page for a new version.
- **Reads the game's memory** to know names, jobs, party and zone. It only reads, never writes.
- **Injects [Deucalion](https://github.com/ff14wed/deucalion) into the game**, a small open-source DLL that ships with the ACT engine. It passes the combat packets the game receives on to the meter. The copy in use lives in `C:\ProgramData\PrimalMeter\deucalion`.
- **When the meter closes, Deucalion turns itself off**, leaving the game as it was before.

And what it does **not** do:

- it doesn't ask for administrator rights;
- it doesn't change game files;
- it doesn't send your data or game data to the internet. Communication with Deucalion is local, through a *named pipe*, and the only internet access is checking for the ACT engine on GitHub and loading the interface fonts (Google Fonts);
- it doesn't automate anything in the game.

### Why you can trust it

- **The code is open.** Everything the program does is in this repository: the window and engine in [`src`](src), and the interface and calculations in [`web`](web). Anyone can read it.
- **You can build the `.exe` yourself** from the source, with the command in the [Development](#development) section. That way you don't have to trust the file I published.
- **You can verify the file.** Each release lists the **SHA-256** of the `.exe`. In PowerShell, run the command below and compare it with the release:

  ```powershell
  Get-FileHash .\PrimalMeter.exe -Algorithm SHA256
  ```

  If they match, the file is exactly what was published here, with no changes along the way.
- **You can get a second opinion.** Upload the file to [VirusTotal](https://www.virustotal.com/), which checks it with dozens of antivirus engines at once. Because of the DLL injection, it's normal for one or two of them to flag something generic, as happens with ACT.

> General rule for any program: download Primal Meter **only** from this repository's [Releases](https://github.com/saantanavitor/primal-meter/releases) page. Copies shared by other people or other sites may have been tampered with.

---

## Common problems

| Problem | What to do |
| --- | --- |
| Stuck on "Waiting for the game…" with the game open | Close and reopen the meter. If your antivirus blocked Deucalion, add an exception for `C:\ProgramData\PrimalMeter`. |
| "Engine error" | The error appears in the yellow message. Details are in `%APPDATA%\PrimalMeter\motor.log`. Right after a major game patch, the ACT engine may take a few hours to be updated by its author; reopen the meter later. |
| IINACT stopped capturing after I closed Primal Meter | Disable and enable IINACT in `/xlplugins`. To avoid it, turn IINACT on before opening Primal Meter. |
| "Preparing…" never finishes | The first run needs internet to download the engine. Check your connection and reopen. |
| The meter doesn't appear over the game | Set the game to **Borderless Windowed**. |
| The shortcut doesn't work, or opens another program (Radeon, Discord, NVIDIA…) | Another program uses the same combination. Change the shortcut in the meter's ⋯ menu, or use the tray icon. |
| I can't click the meter | It's locked. Press **Ctrl+Alt+L**. |
| The meter disappeared | Press **Ctrl+Alt+H** or click the tray icon. |
| "Needs the WebView2 Runtime" | This only happens on old Windows 10 installs. Install the [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) and open it again. |

---

## How it works

```
FFXIV ◄── Deucalion (injected) ──pipe──┐
  │                                    ▼
  └── memory (read only) ──► ACT engine (FFXIV_ACT_Plugin) ──► NotACT ──► interface (WebView2)
                                                                  │
                                             Documents\PrimalMeter\Network_*.log
```

- **`src/PrimalMeter`**: WPF app (.NET 10). Transparent always-on-top window, click-through when locked, global shortcuts and tray icon.
  - `Engine/`: hosts the ACT engine outside the game and turns Deucalion off on exit.
  - `IinactClient.cs`: IINACT WebSocket client, for those who choose that source.
- **`src/NotACT`**: a layer that imitates Advanced Combat Tracker for the engine. It comes from [IINACT](https://github.com/marzent/IINACT).
- **`src/EngineSetup`**: downloads the engine from the official release, extracts the embedded DLLs and applies the fixes for modern .NET. The engine is **never redistributed**: each person downloads it on their own machine.
- **`web/index.html`**: the interface. It's embedded in the `.exe` and also works on its own in a browser or in Browsingway (`index.html?OVERLAY_WS=ws://127.0.0.1:10501/ws`).
- **Live totals**: come from ACT itself (NotACT), with its rules: shields count as healing, Limit Break shows up separately and each fight ends 5 seconds after the game leaves combat (or on a wipe), like IINACT.
- **`web/engine.js`**: calculates damage by ability and, in the demo and in log replay, the totals too, from ACT log lines (types 21/22 actions, 24 DoT/HoT, 25 deaths, 03/261 combatants, 260 combat state).

## Development

Requirements: .NET SDK 10 and Node.js (only for tests and for editing the interface).

Before building for the first time, download the ACT engine into `external_dependencies/` (it's only used to compile and never goes into the repository):

```bash
powershell -ExecutionPolicy Bypass -File tools/fetch-engine.ps1
```

```bash
dotnet run --project src/PrimalMeter
```

The development build uses its own settings (`%APPDATA%\PrimalMeter-dev`) and can run next to the installed meter.

```bash
node tests/replay.js "%USERPROFILE%\Documents\PrimalMeter\Network_xxxxx.log"
```

```bash
node tools/serve.js
```

The last one serves the interface at http://localhost:5173/ with the demo running, to work on the look without opening the app.

To build the distribution executable (`dist/PrimalMeter.exe`, about 67 MB, no .NET installation needed):

```bash
powershell -ExecutionPolicy Bypass -File tools/publish.ps1
```

## Known limitations

- Right after a major game patch, the meter only works again once the ACT engine's author publishes the update, which usually takes a few hours.
- Clicking the meter while unlocked takes focus away from the game. Lock it before playing.
- **Using it together with IINACT** (for example, to compare numbers): turn IINACT on **before** opening Primal Meter. If IINACT is turned on afterwards, closing Primal Meter undoes IINACT's capture until it is disabled and enabled again in `/xlplugins`. The same happens with ACT and IINACT together.

## Former name

Up to version 0.2.0, the project was called **Rhalgr Meter**. The first time Primal Meter opens, it copies the settings, fight history and the already-downloaded engine from Rhalgr Meter. After that, the old `RhalgrMeter.exe` can be deleted. Logs saved before the switch stay in `Documents\RhalgrMeter` and can be opened with "Replay log…".

## Credits and license

Primal Meter is free software, released under the [GNU GPL v3](LICENSE).

- **[FFXIV_ACT_Plugin](https://github.com/ravahn/FFXIV_ACT_Plugin)**, by Ravahn: the parsing engine. It's downloaded from the official release on each user's machine and isn't part of this repository.
- **[IINACT](https://github.com/marzent/IINACT)**, by marzent (GPL-3): the NotACT layer and the way to prepare the engine to run outside ACT.
- **[Deucalion](https://github.com/ff14wed/deucalion)**, by ff14wed: capturing the game's network packets.
- **[Machina](https://github.com/ravahn/machina)**, by Ravahn: packet reading, used by the engine.

## Disclaimer

Third-party tools violate Square Enix's terms of service. Use it for yourself and don't comment on other people's numbers in game.
