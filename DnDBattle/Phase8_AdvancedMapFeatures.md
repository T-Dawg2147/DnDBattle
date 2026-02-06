# Phase 8 – Advanced Map Features

This document describes the features added in **Phase 8** and explains how to
test each one.  Every feature can be toggled on/off in the **Developer Settings**
window (🔧 menu) so they can be enabled or disabled at runtime.

---

## 8.1 Multi-Map Management

| Detail | Value |
|--------|-------|
| Difficulty | 🟡 Medium |
| Impact | ⭐⭐⭐ Important |

### What was added

| File | Purpose |
|------|---------|
| `Models/Tiles/TileMapReference.cs` | Lightweight reference (ID, name, file path, tags, last-used timestamp) stored in the map library index. |
| `Models/Tiles/MapLink.cs` | Describes a link from a tile (door / stairs) to a position on another map. |
| `Services/MapLibraryService.cs` | Full map library: add / remove / search maps, quick-switch between maps with state persistence, save/load library index. |
| `Options.cs` | `EnableMultiMapManagement`, `MapLibraryMaxRecent` |

### How to test

1. Open **Developer Settings → Phase 8** and ensure *Enable Multi-Map Management* is checked.
2. In code (or a future Map Library window) create two `TileMapReference` objects and add them to a `MapLibraryService` instance.
3. Call `LoadMap(id)` to switch maps – verify the `MapChanged` event fires.
4. Call `SearchMaps("tavern")` and verify filtering by name/tag works.
5. Adjust the *Max Recent Maps* slider in Developer Settings and verify `RecentMaps` returns the correct count.

---

## 8.2 Background Image Layers

| Detail | Value |
|--------|-------|
| Difficulty | 🟢 Easy |
| Impact | ⭐⭐⭐ Important |

### What was added

| File | Purpose |
|------|---------|
| `Models/Tiles/BackgroundLayer.cs` | Model with image path, opacity, visibility, z-order, and grid-alignment corners (top-left / bottom-right). |
| `Models/Tiles/TileMap.cs` | New `BackgroundLayers` list property. |
| `Models/Tiles/TileMapDto.cs` | `BackgroundLayerDto` for JSON serialization. |
| `Options.cs` | `EnableBackgroundLayers` |

### How to test

1. Enable *Background Image Layers* in Developer Settings.
2. Add a `BackgroundLayer` to a `TileMap.BackgroundLayers` list, setting `ImagePath`, `Opacity`, and alignment coordinates.
3. Serialize the `TileMap` to JSON and verify the background layers are included.
4. Deserialize and confirm the layer data round-trips correctly.

---

## 8.3 Hexagonal Grids

| Detail | Value |
|--------|-------|
| Difficulty | 🟠 Hard |
| Impact | ⭐⭐ Useful |

### What was added

| File | Purpose |
|------|---------|
| `Models/Tiles/GridType.cs` | Enum: `Square`, `HexFlatTop`, `HexPointyTop`. |
| `Models/Tiles/HexCoord.cs` | Axial coordinate struct with pixel conversion, distance, neighbors, and cube-coordinate rounding. |
| `Services/HexGridService.cs` | Hex vertex computation, A\* pathfinding, and radius-area helpers. |
| `Models/Tiles/TileMap.cs` | New `GridType` property (default `Square`). |
| `Options.cs` | `EnableHexGrid` |

### How to test

1. Enable *Hexagonal Grid Support* in Developer Settings.
2. Create a `HexCoord(3, 4)` and verify `DistanceTo(new HexCoord(0, 0))` returns `7`.
3. Convert to pixel with `ToPixel()`, then back with `FromPixel()` – the coordinates should match.
4. Call `HexGridService.GetHexesInRadius(center, 2)` and confirm 19 hexes are returned.
5. Call `HexGridService.FindPath(start, goal, isBlocked, 100)` with no blocked cells and verify a path is returned.

---

## 8.4 Gridless Mode

| Detail | Value |
|--------|-------|
| Difficulty | 🟢 Easy |
| Impact | ⭐⭐ Useful |

### What was added

| File | Purpose |
|------|---------|
| `Models/Tiles/TileMap.cs` | `GridlessMode` and `ShowGridOverlay` boolean properties. |
| `Options.cs` | `EnableGridlessMode` |

### How to test

1. Enable *Gridless Mode* in Developer Settings.
2. Set `TileMap.GridlessMode = true` and verify tokens are no longer snapped to grid coordinates when placed.
3. Toggle `ShowGridOverlay` and confirm a subtle dotted grid overlay appears or disappears.

---

## 8.5 Custom Grid Sizes

| Detail | Value |
|--------|-------|
| Difficulty | 🟢 Easy |
| Impact | ⭐⭐⭐ Important |

### What was added

| File | Purpose |
|------|---------|
| `Models/Tiles/TileMap.cs` | `FeetPerSquare` property (default 5), `GetSpeedInSquares()`, and `ChangeGridScale()`. |
| `Options.cs` | `EnableCustomGridSizes`, `DefaultFeetPerSquare` |

### How to test

1. Enable *Custom Grid Sizes* in Developer Settings.
2. Create a `TileMap` with `FeetPerSquare = 5` and call `GetSpeedInSquares("30 ft")` – result should be `6`.
3. Call `ChangeGridScale(10)` and verify:
   - `FeetPerSquare` is now `10`.
   - Tile positions and map dimensions are halved (scaled by 5/10 = 0.5).
4. Adjust the *Default Feet/Square* slider in Developer Settings.

---

## 8.6 Map Notes & Labels

| Detail | Value |
|--------|-------|
| Difficulty | 🟢 Easy |
| Impact | ⭐⭐⭐ Important |

### What was added

| File | Purpose |
|------|---------|
| `Models/Tiles/MapNote.cs` | `MapNote` model (text, position, category, visibility, styling) and `NoteCategory` enum. |
| `Models/Tiles/TileMap.cs` | `Notes` list, `AddNote()`, `RemoveNote()` helpers. |
| `Models/Tiles/TileMapDto.cs` | `MapNoteDto` for JSON serialization. |
| `Options.cs` | `EnableMapNotes`, `ShowDMOnlyNotes`, `MapNoteDefaultFontSize` |

### How to test

1. Enable *Map Notes & Labels* in Developer Settings.
2. Add a `MapNote` to a `TileMap` via `AddNote()` – verify `Notes.Count` increases.
3. Set `IsPlayerVisible = false` on a note and toggle *Show DM-Only Notes* in Developer Settings.
4. Call `RemoveNote(noteId)` and verify the note is removed.
5. Adjust the *Default Font Size* slider and create a new note – verify it uses the slider value.

---

## Developer Window

All Phase 8 features appear under the **"⚙️ Phase 8: Advanced Map Features"**
section in the Developer Settings window.  Each feature has a checkbox toggle and,
where applicable, sliders for numeric parameters.

| Control | Maps to |
|---------|---------|
| Enable Multi-Map Management | `Options.EnableMultiMapManagement` |
| Max Recent Maps slider | `Options.MapLibraryMaxRecent` |
| Enable Background Image Layers | `Options.EnableBackgroundLayers` |
| Enable Hexagonal Grid Support | `Options.EnableHexGrid` |
| Enable Gridless Mode | `Options.EnableGridlessMode` |
| Enable Custom Grid Sizes | `Options.EnableCustomGridSizes` |
| Default Feet/Square slider | `Options.DefaultFeetPerSquare` |
| Enable Map Notes & Labels | `Options.EnableMapNotes` |
| Show DM-Only Notes | `Options.ShowDMOnlyNotes` |
| Default Font Size slider | `Options.MapNoteDefaultFontSize` |

---

## Options.cs Summary (Phase 8 additions)

```
EnableMultiMapManagement    (bool, default true)
MapLibraryMaxRecent         (int,  default 10)
EnableBackgroundLayers      (bool, default true)
EnableHexGrid               (bool, default true)
EnableGridlessMode          (bool, default true)
EnableCustomGridSizes       (bool, default true)
DefaultFeetPerSquare        (int,  default 5)
EnableMapNotes              (bool, default true)
MapNoteDefaultFontSize      (double, default 12)
ShowDMOnlyNotes             (bool, default true)
```
