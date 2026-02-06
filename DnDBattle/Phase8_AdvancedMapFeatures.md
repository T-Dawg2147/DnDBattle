# Phase 8 – Advanced Map Features

This document describes the features added in **Phase 8** and explains how to
test each one. Use the top menu **🗺️ Phase 8** → **Map Features Panel...** (or `🔧 Tools → Developer Settings → Phase 8`) to toggle features and run quick actions like setting grid type, enabling gridless mode, adding map notes, or opening the Map Library.

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

1. Open **🗺️ Phase 8 → Map Features Panel...** and ensure *Enable Multi-Map Management* is checked.
2. Open **🗺️ Phase 8 → Open Map Library...**, add two maps, and switch between them — verify the map changes and recent list updates.
3. Use the search box in the Map Library for "tavern" and verify filtering by name/tag works.
4. Adjust the *Max Recent Maps* slider in the panel and confirm the Recent Maps list respects the limit.

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

1. Enable *Background Image Layers* in the **Map Features Panel...**.
2. In the panel, add a background image layer to the current map, setting image path, opacity, and alignment.
3. Save the map, reload it, and verify the layer persists with the configured opacity.

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

1. In **🗺️ Phase 8**, choose **Grid Type → Hex Grid (Flat Top)** or **Hex Grid (Pointy Top)**.
2. Place a token and confirm grid snapping follows hex coordinates; switch back to **Square Grid** and verify snapping changes.
3. Use the Map Features Panel to switch grid types again and confirm distances and movement highlights update accordingly.

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

1. Toggle **🗺️ Phase 8 → Toggle Gridless Mode** on.
2. Place tokens and verify they are not snapped to grid coordinates.
3. Toggle **Show Grid Overlay** in the Map Features Panel to show/hide the dotted grid.

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

1. In the Map Features Panel, enable *Custom Grid Sizes*.
2. Set **Default Feet/Square** to 5 and verify a 30 ft move previews as 6 squares.
3. Change the slider to 10 ft/square and verify movement previews shrink accordingly.

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

1. Enable *Map Notes & Labels* in the Map Features Panel.
2. Use **🗺️ Phase 8 → Add Map Note at Center...** to place a note; verify it appears on the map.
3. Toggle *Show DM-Only Notes* in the panel and confirm DM-only notes hide/show.
4. Adjust the *Default Font Size* slider and add another note — verify it uses the new size.

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
