# 📊 Phase 4: Lighting & Vision System

## Overview

This document describes the **Lighting & Vision System** added to the DnDBattle application. The system adds realistic D&D 5e-compliant light sources, shadow casting, token vision ranges, vision mode rendering, automatic fog reveal, and directional lights.

All features can be **enabled/disabled** individually via the **Developer Window** (`Views/DeveloperWindow.xaml`), and configurable parameters are exposed through **Options.cs** for future integration into a user-facing options window.

---

## Features Added

### 4.1 Basic Light Sources
**Files:** `Models/LightSource.cs`, `Controls/BattleGridControl.Lighting.cs`, `Controls/BattleGridControl.ContextMenus.cs`

- **Enhanced `LightSource` model** with:
  - `BrightRadius` / `DimRadius` (in grid squares, matches D&D 5e bright/dim light)
  - `LightColor` (WPF `Color`) with presets: warm yellow, cool blue, red, green, white, purple
  - `Intensity` (0.0–1.0 slider)
  - `IsEnabled` toggle per light
  - `LightType` enum: `Point`, `Directional`, `Ambient`
  - `Label` for identification
- **Right-click context menu** on empty cells: "Add Light Source" with sub-options
- **Color-aware radial gradient** rendering with proper bright/dim falloff
- **Save/Load** via updated `LightDto` (preserves color, type, direction, cone width)

### 4.2 Shadow Casting
**Files:** `Services/WallService.cs`, `Controls/BattleGridControl.Lighting.cs`, `Options.cs`

- **Raycasting-based shadows** from wall service (`ComputeLitPolygon`)
- **Shadow polygon caching** — shadows are only recalculated when lights or walls change, not every frame
- **Configurable ray count** (36–720, default 180) for quality vs. performance trade-off
- **Configurable shadow softness** via blur effect (0–20 px)
- Cache is invalidated automatically when walls are added/removed

### 4.3 Token Vision Ranges
**Files:** `Models/TokenVision.cs`, `Models/Token.cs`, `Services/VisionService.cs`

- **`TokenVision` model** with D&D 5e vision types:
  - `NormalRange` (default 60 ft = 12 squares in bright light)
  - `DarkvisionRange` (sees in darkness as dim light)
  - `BlindsightRange` (ignores walls/darkness)
  - `TruesightRange` (sees invisible, illusions)
- **Vision cone support** (`HasVisionCone`, `VisionConeAngle`, `FacingAngle`)
- **Automatic parsing** from `Token.Senses` string (e.g., "Darkvision 60 ft" → 12 squares)
- **`VisionService`** calculates visible cells considering:
  - Distance and range per vision type
  - Wall line-of-sight checks
  - Lighting state per cell
  - Vision cone geometry
- **Position-based caching** — vision is recalculated only when token moves

### 4.4 Vision Mode Rendering
**Files:** `Controls/BattleGridControl.VisionOverlay.cs`

- **Vision overlay layer** (Z-index 90) shows what player tokens can see
- **Darkvision grayscale hint** — cells seen through darkvision in darkness get a subtle blue-gray tint
- **Non-visible cells** rendered with dim overlay
- Toggle via `ToggleVisionOverlay(bool)` method

### 4.5 Automatic Fog Reveal
**Files:** `Controls/BattleGridControl.VisionOverlay.cs`, `Services/FogOfWarService.cs`

- **Fog automatically reveals** based on aggregated player token vision
- **Three modes** (configurable in Options/DeveloperWindow):
  - **Exploration** (default): Once revealed, stays revealed forever
  - **Dynamic**: Only currently visible cells are revealed
  - **Hybrid**: Revealed areas persist with dimmer overlay
- Called via `UpdateFogFromTokenVision()` — wire to token movement events

### 4.6 Directional Lights
**Files:** `Models/LightSource.cs`, `Controls/BattleGridControl.Lighting.cs`

- **`LightType.Directional`** with cone geometry rendering
- `Direction` (angle in degrees) and `ConeWidth` (default 60°)
- **Cone clipping** — light gradient is clipped to a pie-slice shape
- Available via right-click context menu: "Add Directional Light (Spotlight)"
- `IsPointInCone()` method on `LightSource` for directional vision checks

---

## Developer Window

**File:** `Views/DeveloperWindow.xaml` / `Views/DeveloperWindow.xaml.cs`

A dark-themed settings window where each Phase 4 feature can be toggled individually:

| Feature | Toggle | Options |
|---------|--------|---------|
| 4.1 Basic Lighting | ✅ Enable/Disable | Bright radius, Dim radius sliders |
| 4.2 Shadow Casting | ✅ Enable/Disable | Ray count, Shadow softness sliders |
| 4.3 Token Vision | ✅ Enable/Disable | Default vision range slider |
| 4.4 Vision Rendering | ✅ Enable/Disable | — |
| 4.5 Auto Fog Reveal | ✅ Enable/Disable | Fog reveal mode dropdown |
| 4.6 Directional Lights | ✅ Enable/Disable | — |

All settings write directly to `Options.cs` static properties. A "Reset All to Defaults" button restores factory settings.

---

## Options.cs Additions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableLighting` | bool | true | Master toggle for lighting system |
| `EnableShadowCasting` | bool | true | Shadow casting from walls |
| `EnableTokenVision` | bool | true | Per-token vision calculation |
| `EnableVisionModeRendering` | bool | true | Darkvision/blindsight visual effects |
| `EnableAutoFogReveal` | bool | true | Auto-reveal fog from token vision |
| `EnableDirectionalLights` | bool | true | Directional/cone light support |
| `DefaultBrightLightRadius` | double | 4.0 | Default bright radius (squares) |
| `DefaultDimLightRadius` | double | 8.0 | Default dim radius (squares) |
| `ShadowCastRayCount` | int | 180 | Rays per light for shadow quality |
| `ShadowQualityTier` | int | 1 | 0=Low, 1=Medium, 2=High, 3=Ultra |
| `FogRevealMode` | int | 0 | 0=Exploration, 1=Dynamic, 2=Hybrid |
| `DefaultTokenVisionRange` | int | 12 | Default vision range (squares) |

---

## Simple Test Procedure

### Test 1: Add a Point Light
1. Launch the application and open/create an encounter
2. Use **💡 Phase 4 → Quick: Add Point Light** from the top menu
4. ✅ **Expected:** A warm yellow radial glow appears at the cell with an orange indicator dot

### Test 2: Add a Colored Light
1. Open **💡 Phase 4 → Lighting & Vision Panel...**
2. Add a **🎨 Colored Light → 🔵 Cool Blue (Moonlight)** from the panel
3. ✅ **Expected:** A blue-tinted radial glow appears

### Test 3: Add a Directional Light
1. Use **💡 Phase 4 → Quick: Add Directional Light** from the top menu
3. ✅ **Expected:** A 60° cone of light appears pointing to the right (0°)

### Test 4: Remove a Light
1. Right-click a cell where a light exists
2. Select **"🗑️ Remove Light" → select the light**
3. ✅ **Expected:** The light disappears from the map

### Test 5: Shadow Casting
1. Draw a wall segment near a light source
2. ✅ **Expected:** The light is clipped by the wall, creating a shadow on the other side

### Test 6: Phase 4 Panel / Developer Settings
1. Open **💡 Phase 4 → Lighting & Vision Panel...** (or Tools → Developer Settings → Phase 4)
2. Uncheck "Enable Lighting System"
3. ✅ **Expected:** All lights disappear from the map
4. Re-check "Enable Lighting System"
5. ✅ **Expected:** All lights reappear

### Test 7: Token Vision
1. Place a player token on the map
2. Set its Senses to "Darkvision 60 ft"
3. Toggle vision overlay on via **💡 Phase 4 → Toggle Vision Overlay**
4. ✅ **Expected:** Cells within 12 squares are visible; cells in darkness seen through darkvision have a slight blue tint

### Test 8: Save/Load
1. Add several lights of different types and colors
2. Save the encounter
3. Close and reload the encounter
4. ✅ **Expected:** All lights restore with correct colors, types, and positions
