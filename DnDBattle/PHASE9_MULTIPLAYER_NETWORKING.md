# 🌐 Phase 9: Multiplayer / Networking

## Overview

This document describes the **Multiplayer / Networking System** added to the DnDBattle application. The system adds host/join game sessions, real-time synchronization (token movement, fog of war, chat), DM-authoritative server architecture, player permissions, voice chat integration, and cloud save & sync.

All features can be **enabled/disabled** individually via the **Developer Window** (`Views/DeveloperWindow.xaml`), and configurable parameters are exposed through **Options.cs** for future integration into a user-facing options window.

---

## Features Added

### 9.1 Network Architecture (Client-Server)
**Files:** `Models/Networking.cs`, `Services/GameServer.cs`, `Services/GameClient.cs`

- **DM-Authoritative Server** — the DM runs the server and owns the game state
- **TCP with newline-delimited JSON** for reliable message framing
- **Configurable port** (default 7777) and connection timeout (default 10s)
- **Player management** — track connected players, assign tokens, broadcast updates
- **Packet types** for all game events:
  - `Handshake` — player connection/authentication
  - `FullSync` — initial game state transfer
  - `TokenMove` / `TokenUpdate` — token position changes
  - `FogUpdate` — fog of war changes
  - `ChatMessage` / `DiceRoll` — player communication
  - `AttackRequest` / `AttackResult` — combat actions
  - `PlayerJoined` / `PlayerLeft` — session management
- **Token ownership validation** — server rejects moves for tokens not assigned to the player
- **Concurrent connection handling** — uses `ConcurrentDictionary` for thread safety
- **Clean disconnect** with notification to all players

### 9.2 Player Client Features
**Files:** `Services/GameClient.cs`, `ViewModels/PlayerClientViewModel.cs`

- **Lightweight client** for player connections
- **Client-side prediction** — token moves applied locally immediately, corrected if server rejects
- **Lag compensation** — `MovementPrediction` tracks predictions and reconciles with server confirmations
- **Chat system** — send and receive text messages with timestamps
- **Dice rolling** — request rolls through the server for transparency
- **Attack requests** — players request attacks, DM server resolves
- **Connection management** — connect, disconnect, auto-reconnect awareness
- **MVVM ViewModel** — `PlayerClientViewModel` with `ObservableProperty` and `RelayCommand` for clean WPF binding
- **UI-thread dispatching** — all UI updates marshaled to dispatcher

### 9.3 Synchronized Fog of War
**Files:** `Services/FogSyncService.cs`

- **DM-controlled fog** — DM reveals/hides areas, all players see updates
- **Per-player visibility** — individual players can have unique revealed areas
- **Delta updates** — only changed cells are sent (typically 10–50 bytes per update)
- **RLE compression** — full fog state compressed for new player joins (~200–500 bytes for 50×50 grid)
- **Grid-based fog** — boolean grid with configurable dimensions
- **Radius-based reveal/hide** — circular area operations
- **Full sync for late joiners** — compressed fog state sent on connect

### 9.4 Voice Chat Integration
**Files:** `Services/VoiceChatService.cs`

- **Discord integration** — share Discord invite links with players
- **Open in browser** — one-click to join Discord voice channel
- **Invite message generator** — formatted message with session name and voice link
- **Non-intrusive approach** — uses existing voice chat apps rather than custom voice

### 9.5 Cloud Save & Sync
**Files:** `Services/CloudSaveService.cs`

- **Self-hosted REST API** — save/load encounters to a configurable server
- **HTTP-based** — standard `HttpClient` with JSON content
- **Campaign organization** — encounters organized by campaign ID
- **List encounters** — browse available encounters in a campaign
- **Conflict resolution** — compare local vs. remote timestamps, prefer newer version
- **Graceful fallback** — cloud features disabled by default, optional activation

---

## Options.cs Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `EnableNetworking` | `bool` | `false` | Master toggle for multiplayer networking |
| `NetworkDefaultPort` | `int` | `7777` | Default TCP port for the game server |
| `NetworkConnectionTimeoutSeconds` | `int` | `10` | Connection timeout in seconds |
| `EnableFogSync` | `bool` | `false` | Enable real-time fog synchronization |
| `EnableMultiplayerChat` | `bool` | `false` | Enable the chat system |
| `EnableClientPrediction` | `bool` | `true` | Enable client-side prediction for token movement |
| `EnableVoiceChat` | `bool` | `false` | Enable Discord voice chat integration |
| `EnableCloudSave` | `bool` | `false` | Enable cloud save & sync |
| `CloudSaveServerUrl` | `string` | `""` | URL of the self-hosted cloud save server |

---

## Developer Window Toggles

| Group | Control | Options Property |
|---|---|---|
| 🌐 9.1 Network Architecture | Enable Multiplayer Networking | `EnableNetworking` |
| 🌐 9.1 Network Architecture | Default Server Port (slider) | `NetworkDefaultPort` |
| 🌐 9.1 Network Architecture | Connection Timeout (slider) | `NetworkConnectionTimeoutSeconds` |
| 🌐 9.1 Network Architecture | Enable Client-Side Prediction | `EnableClientPrediction` |
| 👤 9.2 Player Client Features | Enable Multiplayer Chat System | `EnableMultiplayerChat` |
| 🌫️ 9.3 Synchronized Fog of War | Enable Real-Time Fog Sync | `EnableFogSync` |
| 🎤 9.4 Voice Chat Integration | Enable Voice Chat (Discord) | `EnableVoiceChat` |
| ☁️ 9.5 Cloud Save & Sync | Enable Cloud Save & Sync | `EnableCloudSave` |

---

## Architecture

```
DM (GameServer) ←→ Player 1 (GameClient)
       ↕
       └─────→ Player 2 (GameClient)
       ↕
       └─────→ Player 3 (GameClient)
```

- **DM is authoritative** — owns game state, validates all actions
- **Players send inputs** — token moves, chat, dice rolls, attack requests
- **Server broadcasts updates** — confirmed moves, results, state changes
- **Player permissions** — can only control assigned tokens
- **Client-side prediction** — immediate local feedback, server correction if needed

---

## Simple Test Procedure

### ✅ Test 1: Server Start/Stop
1. Open **🌐 Multiplayer → Host Game (DM)...**
2. Verify the default port shows 7777 and timeout shows 10s
3. Start hosting, then stop hosting — ensure the status updates in the window
4. Confirm the **Enable Networking** toggle in the host dialog matches `Options.EnableNetworking`

### ✅ Test 2: Developer Window Toggles
1. Open **Tools → Developer Settings** and scroll to "Phase 9: Multiplayer / Networking"
2. Toggle each checkbox on and off (Client Prediction, Fog Sync, Voice Chat)
3. Adjust the port slider (range: 1024–65535)
4. Adjust the timeout slider (range: 5–60)
5. Click **Reset All to Defaults** — verify Phase 9 options reset to defaults
6. Verify all Phase 9 options are `false` (except `EnableClientPrediction` which defaults to `true`)

### ✅ Test 3: Options Persistence
1. Enable networking features in **Developer Settings** or **🌐 Multiplayer → Host Game (DM)...**
2. Close and reopen the Developer Settings window
3. Verify the toggles reflect the current `Options` state (runtime-only, not persisted to disk)

### ✅ Test 4: Fog Compression
1. The `FogSyncService.CompressFogState` method can be tested by creating a `bool[50,50]` grid
2. Reveal a circular area and compress — verify output is significantly smaller than 2500 bytes
3. Decompress and verify the grid matches the original

### ✅ Test 5: Build Verification
1. Build the solution with `dotnet build DnDBattle.slnx`
2. Verify 0 errors
3. Verify no new warnings were introduced by Phase 9 code
