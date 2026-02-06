# 🧪 Undecided Features (Experimental) – Feature Documentation

This document describes the **Undecided Features** added to DnDBattle. These are
experimental features that can be individually toggled on/off via the **Developer Settings**
window (`🔧 Developer Settings - Features` → Undecided Features section).
All undecided features default to **disabled** (except Weather which defaults to enabled)
and must be explicitly enabled before use.

---

## ⭐ Features Added

### UD.1 🌧️ Dynamic Weather & Environment

**Service:** `WeatherService` (`Services/WeatherService.cs`)
**Models:** `WeatherType` (`Models/WeatherType.cs`), `TimeOfDay` (`Models/TimeOfDay.cs`), `Season` (`Models/Season.cs`)

#### What It Does
- Renders **dynamic weather particles** on the battle map: Rain, Snow, Fog, Storm, Sandstorm.
- **Particle pooling** — all particles are pre-allocated at startup (default 500) and recycled. No allocations during gameplay, zero GC pressure.
- **Viewport culling** — particles outside the visible viewport are immediately recycled and skipped during rendering.
- **Day/night cycle** — lighting overlay dims the map based on time of day (Dawn 0.15, Day 0.0, Dusk 0.25, Night 0.55 opacity). Storm adds +0.15 darkening (capped at 0.7).
- **Wind system** — configurable wind angle (degrees) and strength. All particles are pushed by wind in addition to their base velocity.
- **Season support** — `Season` enum (Spring, Summer, Autumn, Winter) for thematic environment rendering.
- **Vision range modifier** — weather reduces vision range (Fog → 0.3×, Storm → 0.5×, Rain → 0.8×, Snow → 0.6×, Sandstorm → 0.4×).
- **Lighting cache** — day/night overlay opacity is cached and only recomputed every 2 seconds to avoid redundant calculations.

#### Weather Types

| Type | Particle Count | VelocityY (px/s) | VelocityX (px/s) | Particle Size |
|------|---------------|-------------------|-------------------|---------------|
| Rain | 100% of max | 200–300 | ±10 | 1.5–3.0 |
| Snow | 60% of max | 40–70 | ±15 | 3.0–7.0 |
| Fog | 30% of max | ±1.5 | ±2.5 | 20–50 |
| Storm | 100% of max | 250–400 | ±30 | 2.0–4.0 |
| Sandstorm | 80% of max | ±10 | 40–80 | 2.0–5.0 |

#### Options (`Options.cs`)

| Option | Default | Description |
|--------|---------|-------------|
| `EnableWeatherEffects` | `true` | Master toggle for weather particle effects |
| `EnableDayNightCycle` | `true` | Enable day/night lighting overlay |
| `WeatherMaxParticles` | `500` | Maximum weather particles (50–1000, slider) |

#### Performance Optimizations
- **Pre-allocated pool** — `WeatherParticle[]` array allocated once in constructor, never resized.
- **Swap-and-compact** — dead particles are swapped with the last active particle to keep the active region contiguous.
- **`ReadOnlySpan<T>` rendering** — `GetActiveParticles()` returns a slice of the pool array with zero allocation.
- **Viewport culling** — particles outside `ViewportBounds` are immediately deactivated.
- **Cached lighting** — overlay opacity recalculated at most once every 2 seconds.

#### Simple Test
1. Open Developer Settings → Undecided Features and ensure *Enable Weather Effects* is checked.
2. In code, create a `WeatherService` and call `SetWeather(WeatherType.Rain, 0.8)`.
3. Call `Update(0.016)` each frame (60 fps) and verify `GetActiveParticles()` returns particles with changing positions.
4. Call `SetTimeOfDay(TimeOfDay.Night)` and verify `GetLightingOverlayOpacity()` returns `0.55`.
5. Set `WindAngleDegrees = 45` and `WindStrength = 2.0`, verify particles drift diagonally.

---

### UD.2 🏔️ 2.5D Token Elevation

**Service:** `ElevationService` (`Services/ElevationService.cs`)

#### What It Does
- Manages **terrain elevation** on a per-cell basis using a **flat array** (`int[]`) for O(1) lookups.
- Manages **per-token elevation** overrides for flying, climbing, and levitating tokens (stored in a `Dictionary<string, int>`).
- **3D distance calculation** — `Calculate3DDistance()` uses Pythagorean theorem: `sqrt(dx² + dy² + dz²)` where `dz` is the elevation difference in grid squares.
- **Falling damage** — `CalculateFallingDamageDice()` computes D&D 5e falling damage: **1d6 per 10 feet, maximum 20d6** (200 ft).
- **Elevation line of sight** — `HasElevationLineOfSight()` checks whether a higher token can see over intervening walls based on relative elevation.
- **Configurable step size** — `ElevationStepFeet` (default 10 ft) controls the raise/lower increment.
- **Maximum elevation** — capped at 300 ft by default.
- **Layer rendering** — `GetDistinctElevationLevels()` returns all unique elevation values on the map for layer-based rendering.

#### Options (`Options.cs`)

| Option | Default | Description |
|--------|---------|-------------|
| `EnableElevationSystem` | `false` | Master toggle for 2.5D terrain elevation |

#### Performance Optimizations
- **Flat array storage** — terrain elevations stored as `int[width * height]` for cache-friendly sequential access.
- **O(1) lookups** — `GetTerrainElevation(x, y)` uses `array[y * width + x]` index calculation.
- **No allocations** — raise/lower operations modify the array in-place.

#### Simple Test
1. Enable *2.5D Terrain Elevation Layers* in Developer Settings.
2. Create an `ElevationService` and call `Initialize(20, 20)`.
3. Call `SetTerrainElevation(5, 5, 30)` and verify `GetTerrainElevation(5, 5)` returns `30`.
4. Call `SetTokenElevation("token1", 20)` and verify `GetTotalHeight("token1", 5, 5)` returns `50`.
5. Call `CalculateFallingDamageDice(60)` — should return `6` (6d6).
6. Call `CalculateFallingDamageDice(250)` — should return `20` (capped at 20d6).
7. Verify `Calculate3DDistance(0, 0, 0, 3, 4, 25, 5)` returns the Pythagorean distance with elevation factored in.

---

### UD.3 🎞️ Animated Tiles

**Service:** `AnimatedTileService` (`Services/AnimatedTileService.cs`)
**Model:** `AnimatedTileDefinition` (`Models/AnimatedTileDefinition.cs`)

#### What It Does
- Renders **spritesheet-based tile animations** on the battle grid (water flow, fire, torches, magic circles, etc.).
- **Spritesheet UV animation** — each tile definition references a single spritesheet image. Frames are extracted via UV coordinate calculation (`GetCurrentFrameRect()`), avoiding per-frame image loading.
- **Viewport culling** — only tiles within the visible `ViewportBounds` are updated each frame.
- **LOD (Level of Detail)** — animations stop entirely when zoomed below 0.3× (`MinZoomForAnimation`).
- **Frame skipping** — when more than **50 visible animated tiles**, FPS is capped at **15 fps** (`ReducedFps`) to maintain performance.
- **Random start frames** — tiles with `RandomStartFrame = true` begin on a random frame to prevent synchronized animation across identical tiles.
- **10 built-in definitions** included for immediate use.

#### Built-In Animated Tile Definitions

| ID | Display Name | Frames | FPS | Category | Looping |
|----|-------------|--------|-----|----------|---------|
| `water_flow` | Flowing Water | 4 | 6 | Water | ✅ |
| `water_ripple` | Water Ripple | 4 | 4 | Water | ✅ |
| `fire_small` | Small Fire | 6 | 10 | Fire | ✅ |
| `fire_large` | Large Fire | 6 | 12 | Fire | ✅ |
| `torch_flicker` | Torch Flicker | 4 | 8 | Fire | ✅ |
| `magic_circle` | Magic Circle | 8 | 4 | Magic | ✅ |
| `lava_bubble` | Lava Bubbling | 6 | 5 | Hazard | ✅ |
| `blood_splatter` | Blood Splatter | 3 | 8 | Hazard | ❌ |
| `leaves_falling` | Falling Leaves | 6 | 4 | Nature | ✅ |
| `banner_wave` | Waving Banner | 4 | 6 | Atmospheric | ✅ |

#### Options (`Options.cs`)

| Option | Default | Description |
|--------|---------|-------------|
| `EnableAnimatedTiles` | `false` | Master toggle for animated tile rendering |

#### Performance Optimizations
- **Packed dictionary key** — grid position `(x, y)` packed into a single `long` via `PackKey()` to avoid tuple/struct allocation for dictionary keys.
- **Viewport culling** — tiles outside `ViewportBounds` are skipped during `Update()` and marked `IsVisible = false`.
- **LOD cutoff** — all animation stops when `ZoomLevel < 0.3`, saving CPU at low zoom levels.
- **Adaptive FPS** — at 50+ visible tiles, effective FPS drops to 15 to maintain smooth rendering of the rest of the scene.
- **Pre-allocated state** — `TileAnimState` objects are created once per grid cell and reused via `Reset()`.

#### Simple Test
1. Enable *Animated Tiles* in Developer Settings.
2. Create an `AnimatedTileService`, register a built-in definition: `service.RegisterDefinition(AnimatedTileService.GetBuiltInDefinitions()[0])`.
3. Place a tile: `service.PlaceAnimatedTile("water_flow", 5, 5)`.
4. Call `service.Update(0.016)` repeatedly and verify `GetCurrentFrameRect(5, 5)` returns changing frame coordinates.
5. Set `service.ZoomLevel = 0.2` and call `Update()` — verify no frames advance (LOD cutoff).
6. Place 60 tiles and verify `GetPerformanceStats()` shows effective FPS ≤ 15.

---

### UD.4 🎲 Procedural Map Generation

**Service:** `ProceduralMapService` (`Services/ProceduralMapService.cs`)
**Model:** `ProceduralMapConfig` (`Models/ProceduralMapConfig.cs`)

#### What It Does
- Generates **procedural battle maps** using three algorithms:
  - **BSP Dungeon** — Binary Space Partitioning creates rooms connected by L-shaped corridors with optional doors.
  - **Cellular Automata Caves** — random fill + iterative smoothing produces natural cave systems.
  - **Arena** — simple open arena surrounded by walls.
- **Seeded RNG** — optional `Seed` property on `ProceduralMapConfig` for reproducible generation. Same seed → same map every time.
- **Flat byte array output** — generated maps stored as `byte[width * height]` with cell types: 0=empty, 1=floor, 2=wall, 3=corridor, 4=door.
- **Room metadata** — dungeon generation returns a list of `RoomInfo` objects with position, size, and center coordinates.
- **Configurable parameters** — room size range, corridor width, cave fill probability, iteration count, tile IDs for rendering.

#### Generation Algorithms

| Algorithm | Config Properties | Output |
|-----------|-------------------|--------|
| **BSP Dungeon** | `MinRoomSize`, `MaxRoomSize`, `CorridorWidth`, `AddDoors` | Rooms + corridors + doors |
| **Cellular Automata Cave** | `CaveFillProbability` (0.0–1.0), `CaveIterations` | Organic cave shapes |
| **Arena** | `Width`, `Height` | Open floor with wall border |

#### Options (`Options.cs`)

| Option | Default | Description |
|--------|---------|-------------|
| `EnableProceduralMapGeneration` | `false` | Master toggle for procedural map generation |

#### Performance Optimizations
- **Flat byte arrays** — all map data stored as `byte[]` for cache-friendly iteration.
- **Double-buffered automata** — cave generation uses two buffers and swaps references each iteration (no allocation per pass).
- **O(1) cell lookup** — `ProceduralMapResult.GetCell(x, y)` uses direct array indexing.

#### Simple Test
1. Enable *Procedural Map Generation* in Developer Settings.
2. Create a `ProceduralMapService` and a config: `new ProceduralMapConfig { Type = MapGenerationType.Dungeon, Width = 50, Height = 50, Seed = 12345 }`.
3. Call `Generate(config)` and verify `result.Grid` is non-null and length = 2500.
4. Verify `result.Rooms.Count > 0` and each room has valid position/size.
5. Generate again with the same seed and verify the grids are identical.
6. Change `Type` to `MapGenerationType.Cave` and verify the output is a cave (no rooms, organic shapes).
7. Change `Type` to `MapGenerationType.Arena` and verify a single room spanning the map.

---

### UD.5 🎨 Advanced Token Customization

**Service:** `TokenCustomizationService` (`Services/TokenCustomizationService.cs`)
**Model:** `TokenCustomization` (`Models/TokenCustomization.cs`)

#### What It Does
- Per-token **visual customization** with persistent settings stored by token ID.
- **6 token shapes** with WPF clip geometries:

| Shape | Geometry Type | Description |
|-------|--------------|-------------|
| Circle | `EllipseGeometry` | Default round token |
| Square | `RectangleGeometry` | Sharp-cornered square |
| RoundedSquare | `RectangleGeometry` (15% corner radius) | Rounded corners |
| Hexagon | `PathGeometry` (6-point) | Hex-shaped token |
| Diamond | `PathGeometry` (4-point rotated square) | Diamond/rhombus |
| Star | `PathGeometry` (10-point, 40% inner radius) | 5-pointed star |

- **Borders** — configurable color (hex string), thickness, and style (None, Solid, Dashed, Double, Glow).
- **Glow effects** — optional glow halo around the token border with configurable color and radius (default 8px).
- **Name plates** — token name labels with configurable position (Above, Below, Inside, Left, Right), background color, text color, font size, and always-visible toggle.
- **Condition overlays** — show condition icons as overlay badges on the token (max 4 before collapsing).

#### Options (`Options.cs`)

| Option | Default | Description |
|--------|---------|-------------|
| `EnableTokenCustomization` | `false` | Master toggle for advanced token customization |

#### Performance Optimizations
- **O(1) lookup** — customizations stored in `Dictionary<string, TokenCustomization>` keyed by token ID.
- **Frozen geometries** — all `PathGeometry` shapes are frozen after creation for thread-safety and rendering performance.
- **Frozen brushes** — `CreateFrozenBrush()` creates and freezes `SolidColorBrush` instances for reuse.

#### Simple Test
1. Enable *Token Customization* in Developer Settings.
2. Create a `TokenCustomizationService` and call `GetCustomization("token1")` — verify default values.
3. Modify the customization: set `Shape = TokenShape.Hexagon`, `BorderColorHex = "#FF0000"`, `ShowGlow = true`.
4. Call `SetCustomization(custom)` and verify `GetCustomization("token1")` returns the updated values.
5. Call `TokenCustomizationService.GetClipGeometry(TokenShape.Star, 48)` and verify a non-null `Geometry` is returned.
6. Verify `GetNamePlateOffset(NamePlatePosition.Above, 48, 60, 16)` returns a position above the token.

---

### UD.6 📏 Persistent Measurement Templates

**Service:** `MeasurementService` (`Services/MeasurementService.cs`)
**Model:** `Measurement` (`Models/Measurement.cs`)

#### What It Does
- **Persistent measurements** on the battle map that survive between turns and sessions.
- **D&D grid distance** — diagonal movement costs **1.5× squares** (5e variant rule): `straight + diagonal × 1.5`.
- **Shoelace polygon area** — calculates area of arbitrary polygon measurements using the Shoelace formula.
- **5 measurement types**: Distance, Path (multi-point), Radius, Area (rectangle), Polygon.
- **6 purpose categories** with default color-coding:

| Purpose | Default Color | Use Case |
|---------|--------------|----------|
| Info | `#4FC3F7` (Light Blue) | General information |
| Danger | `#F44336` (Red) | Hazard zones, enemy ranges |
| Safe | `#4CAF50` (Green) | Safe zones, cover areas |
| Spell | `#BA68C8` (Purple) | Spell ranges and areas |
| Movement | `#FFB74D` (Orange) | Movement ranges, dash distance |
| Custom | `#FFFFFF` (White) | User-defined |

- **Visibility toggle** — measurements can be shown/hidden individually.
- **Labels** — optional distance/area labels rendered on the map.

#### Options (`Options.cs`)

| Option | Default | Description |
|--------|---------|-------------|
| `EnableMeasurements` | `false` | Master toggle for persistent measurement templates |

#### Performance Optimizations
- **O(1) lookup** — measurements stored in `Dictionary<string, Measurement>` keyed by ID.
- **Lazy rendering** — `GetVisibleMeasurements()` filters with LINQ only when called.

#### Simple Test
1. Enable *Persistent Measurement Templates* in Developer Settings.
2. Create a `MeasurementService` and call `AddDistanceMeasurement("Range", 0, 0, 3, 4)`.
3. Call `CalculateDistanceFeet(measurement)` — verify D&D diagonal distance is returned (not Euclidean).
4. Add a radius measurement: `AddRadiusMeasurement("Fireball", 10, 10, 4)`.
5. Call `CalculateAreaSqFeet()` on the radius measurement — verify area = π × (4 × 5)² ≈ 1257 sq ft.
6. Toggle visibility: `ToggleVisibility(id)` — verify `GetVisibleMeasurements()` excludes it.
7. Verify `GetPurposeColor(MeasurementPurpose.Danger)` returns `"#F44336"`.

---

### UD.8 🎲 3D Dice Roller

**Services:**
- `DicePhysicsService` (`Services/DicePhysicsService.cs`)
- `DiceStatisticsService` (`Services/DiceStatisticsService.cs`)

**Models:** `DiceType` (`Models/DiceType.cs`)

#### What It Does

**Dice Physics (`DicePhysicsService`):**
- **Pre-determined results** — dice values are rolled via RNG at roll time, then physics simulation animates dice to "land" on the correct face. This ensures fair results while looking realistic.
- **Euler integration** — simplified physics using position += velocity × dt, with gravity at 500 px/s².
- **Bouncing** — ground bounce at `bounceFactor = 0.4`, wall bounces keep dice within a 600×600 px area.
- **Settling detection** — dice are marked settled when total speed < 5 px/s and within 1 px of ground.
- **Friction** — velocity and angular velocity multiplied by 0.92 on each bounce.
- **Angular rotation** — 3-axis rotation (X, Y, Z) with independent angular velocities up to ±720°/s.
- **Supports all D&D dice**: d4, d6, d8, d10, d12, d20, d100.
- **Max 20 dice per roll** — hard cap to prevent performance issues.
- **Skip animation** — `SkipAnimation()` immediately settles all dice.

**Dice Statistics (`DiceStatisticsService`):**
- **Circular buffer history** — roll records stored in a fixed-size array (default 500 entries). Old records are overwritten when the buffer is full.
- **Running averages** — per-dice-type averages maintained incrementally via `RunningAverage` class. `GetAverageRoll()` is O(1).
- **Luck score** — ratio of actual d20 average to expected average (10.5). Score > 1.0 = lucky, < 1.0 = unlucky.
- **Critical hit/fail counts** — tracks natural 20s and natural 1s on d20 rolls.
- **Roll distribution** — `GetDistribution(sides)` returns a histogram of how many times each value was rolled.
- **Recent rolls** — `GetRecentRolls(count)` returns the most recent N rolls, newest first.

#### Options (`Options.cs`)

| Option | Default | Description |
|--------|---------|-------------|
| `EnableDiceRoller3D` | `false` | Master toggle for 3D dice roller |
| `EnableDiceStatistics` | `false` | Enable dice roll statistics tracking |
| `DiceHistoryMaxSize` | `500` | Maximum roll records in the circular buffer |

#### Performance Optimizations
- **Pre-determined results** — avoids expensive real-time collision detection to determine face values.
- **Circular buffer** — fixed-size array prevents unbounded memory growth for roll history.
- **Running averages** — O(1) average queries without re-scanning history.
- **Max dice cap** — hard limit of 20 dice per roll prevents runaway simulation.

#### Simple Test
1. Enable *3D Dice Roller* and *Dice Roll Statistics* in Developer Settings.
2. Create a `DicePhysicsService` and call `Roll(DiceType.D20, 2)`.
3. Verify `IsRolling == true` and `GetDice().Count == 2`.
4. Call `Update(0.016)` repeatedly until `IsRolling == false` — verify all dice have `IsSettled == true`.
5. Verify `GetTotal()` returns a value between 2 and 40.
6. Create a `DiceStatisticsService` and call `RecordRoll(20, 20)` — verify `CriticalHitCount == 1`.
7. Call `RecordRoll(1, 20)` — verify `CriticalFailCount == 1`.
8. Call `GetAverageRoll(20)` — verify it returns `10.5` (average of 20 and 1).
9. Verify `LuckScore` is calculated as average / 10.5.

---

### UD.9 ♿ Accessibility Suite

**Service:** `AccessibilityService` (`Services/AccessibilityService.cs`)

#### What It Does
- **4 colorblind palettes** — pre-computed color replacements for Protanopia (red-blind), Deuteranopia (green-blind), and Tritanopia (blue-blind), plus normal vision.

| Mode | Danger | Safe | Warning | Info | Accent | Highlight |
|------|--------|------|---------|------|--------|-----------|
| Normal | Red `#F44336` | Green `#4CAF50` | Orange `#FFB74D` | Blue `#4FC3F7` | Purple `#BA68C8` | Gold `#FFD700` |
| Protanopia | Yellow-orange `#D4A017` | Steel blue `#4682B4` | Gold `#FFD700` | Light blue `#87CEEB` | Medium purple `#9370DB` | Orange `#FFA500` |
| Deuteranopia | Dark orange `#CC6600` | Bright blue `#3399FF` | Yellow `#FFCC00` | Light blue `#66B2FF` | Light purple `#CC66FF` | Orange `#FF9933` |
| Tritanopia | Red `#FF4444` | Pink `#FF8888` | Pinkish red `#FF6666` | Gray `#CCCCCC` | Magenta `#FF44FF` | Light red `#FFAAAA` |

- **High contrast mode** — maps all colors to either pure black or pure white based on luminance threshold (128). Background → black, foreground → white.
- **UI scaling** — `AccessibilityUIScale` controls global UI scale from **100% to 300%** (slider in 25% increments). `GetUIScaleTransform()` returns a `ScaleTransform` for WPF layout.
- **Dyslexia-friendly fonts** — when enabled, switches to `OpenDyslexic` font family with `Verdana` and `Segoe UI` fallbacks.
- **Screen reader text generation** — static helper methods produce accessible descriptions:
  - `DescribeToken(name, hp, maxHp, gridX, gridY, conditions)` → `"Fighter, 45 of 50 hit points, at grid position 3,7, conditions: Poisoned, Prone"`
  - `DescribeRoll(expression, result, isCritical)` → `"Rolled 2d6+3, result: 11"`

#### Options (`Options.cs`)

| Option | Default | Description |
|--------|---------|-------------|
| `EnableHighContrast` | `false` | Enable high contrast mode |
| `EnableColorblindMode` | `false` | Enable colorblind-friendly palettes |
| `AccessibilityUIScale` | `100` | UI scale percentage (100–300) |
| `EnableDyslexiaFont` | `false` | Enable dyslexia-friendly font |
| `EnableKeyboardNavigation` | `false` | Enable full keyboard navigation mode |

#### Performance Optimizations
- **Pre-computed palettes** — all colorblind palettes are defined as static `Dictionary` entries, computed once at class load.
- **Frozen brushes** — `ColorPalette.ToBrush()` creates and freezes WPF brushes for thread-safe reuse.
- **Luminance-based contrast** — high contrast calculation uses a simple weighted sum (0.299R + 0.587G + 0.114B), no color space conversion.

#### Simple Test
1. Enable *High Contrast Mode*, *Colorblind-Friendly Palettes*, and *Dyslexia-Friendly Font* in Developer Settings.
2. Create an `AccessibilityService` and call `SetColorblindMode(ColorblindMode.Protanopia)`.
3. Verify `GetCurrentPalette().Danger` returns `"#D4A017"` (yellow-orange instead of red).
4. Verify `GetHighContrastForeground()` returns `Colors.White` when high contrast is enabled.
5. Set `AccessibilityUIScale = 200` in Options and verify `UIScaleFactor == 2.0`.
6. Verify `GetAccessibleFontFamily().Source` contains `"OpenDyslexic"`.
7. Call `DescribeToken("Goblin", 7, 7, 3, 4, new[] { "Frightened" })` and verify it returns a readable string.
8. Adjust the UI Scale slider in Developer Settings from 100% to 200% and verify the UI visually scales.

---

## 🔧 Developer Window Integration

All Undecided Features appear in the **Developer Settings** window under the
**"⚙️ Undecided Features (Experimental)"** heading (displayed in orange `#FFFF8844`
to distinguish from stable phase features). Each feature has a checkbox toggle
and, where applicable, sliders for numeric parameters.

### How to Access
1. Open the app.
2. Navigate to **Tools** → **Developer Settings** (or use the 🔧 menu).
3. Scroll down past Phase 4–8 sections to the **Undecided Features** section.
4. Toggle features on/off using checkboxes.
5. Adjust sliders for fine-tuning (particle counts, UI scale).
6. Click **"Reset All to Defaults"** to restore all settings.

### Developer Window Controls

| Control | Maps to | Type |
|---------|---------|------|
| Enable Weather Effects | `Options.EnableWeatherEffects` | Checkbox |
| Enable Day/Night Cycle | `Options.EnableDayNightCycle` | Checkbox |
| Max Weather Particles | `Options.WeatherMaxParticles` | Slider (50–1000) |
| Enable 2.5D Terrain Elevation Layers | `Options.EnableElevationSystem` | Checkbox |
| Enable Animated Tiles | `Options.EnableAnimatedTiles` | Checkbox |
| Enable Procedural Map Generation | `Options.EnableProceduralMapGeneration` | Checkbox |
| Enable Token Customization | `Options.EnableTokenCustomization` | Checkbox |
| Enable Persistent Measurement Templates | `Options.EnableMeasurements` | Checkbox |
| Enable 3D Dice Roller | `Options.EnableDiceRoller3D` | Checkbox |
| Enable Dice Roll Statistics | `Options.EnableDiceStatistics` | Checkbox |
| Enable High Contrast Mode | `Options.EnableHighContrast` | Checkbox |
| Enable Colorblind-Friendly Palettes | `Options.EnableColorblindMode` | Checkbox |
| Enable Dyslexia-Friendly Font | `Options.EnableDyslexiaFont` | Checkbox |
| Enable Full Keyboard Navigation | `Options.EnableKeyboardNavigation` | Checkbox |
| UI Scale | `Options.AccessibilityUIScale` | Slider (100–300) |

---

## 📁 Files Added

### Models
| File | Description |
|------|-------------|
| `Models/WeatherType.cs` | Enum: None, Rain, Snow, Fog, Storm, Sandstorm |
| `Models/TimeOfDay.cs` | Enum: Dawn, Day, Dusk, Night |
| `Models/Season.cs` | Enum: Spring, Summer, Autumn, Winter |
| `Models/AnimatedTileDefinition.cs` | Spritesheet animation definition with frame count, FPS, layout, category |
| `Models/ProceduralMapConfig.cs` | Config for map generation: type, dimensions, room sizes, seed |
| `Models/TokenCustomization.cs` | Per-token visual settings: shape, border, glow, name plate, overlays |
| `Models/Measurement.cs` | Measurement model with points, type, purpose, label, color |
| `Models/DiceType.cs` | Enum: D4, D6, D8, D10, D12, D20, D100 |

### Services
| File | Description |
|------|-------------|
| `Services/WeatherService.cs` | Particle pooling weather system with day/night cycle and wind |
| `Services/ElevationService.cs` | 2.5D terrain elevation with 3D distance and falling damage |
| `Services/AnimatedTileService.cs` | Spritesheet tile animation with viewport culling and LOD |
| `Services/ProceduralMapService.cs` | BSP dungeon, cellular automata cave, and arena generation |
| `Services/TokenCustomizationService.cs` | Token shape clipping, border rendering, name plate positioning |
| `Services/MeasurementService.cs` | Persistent distance/area measurements with D&D grid math |
| `Services/DicePhysicsService.cs` | Euler-integration dice physics with gravity, bouncing, settling |
| `Services/DiceStatisticsService.cs` | Circular buffer history, running averages, luck score, crit tracking |
| `Services/AccessibilityService.cs` | Colorblind palettes, high contrast, UI scaling, dyslexia fonts |

### Modified Files
| File | Changes |
|------|---------|
| `Options.cs` | Added 16 Undecided Feature options (see summary below) |
| `Views/DeveloperWindow.xaml` | Added Undecided Features toggle section with checkboxes and sliders |
| `Views/DeveloperWindow.xaml.cs` | Load/save/reset logic for Undecided Feature options |

---

## Options.cs Summary (Undecided Feature additions)

```
EnableWeatherEffects          (bool,   default true)
EnableDayNightCycle           (bool,   default true)
WeatherMaxParticles           (int,    default 500)
EnableElevationSystem         (bool,   default false)
EnableAnimatedTiles           (bool,   default false)
EnableProceduralMapGeneration (bool,   default false)
EnableTokenCustomization      (bool,   default false)
EnableMeasurements            (bool,   default false)
EnableDiceRoller3D            (bool,   default false)
EnableDiceStatistics          (bool,   default false)
DiceHistoryMaxSize            (int,    default 500)
EnableHighContrast            (bool,   default false)
EnableColorblindMode          (bool,   default false)
AccessibilityUIScale          (int,    default 100)
EnableDyslexiaFont            (bool,   default false)
EnableKeyboardNavigation      (bool,   default false)
```

---

## 🧪 Quick Validation Test

To validate all Undecided Features are working:

1. **Build** the project — should compile with 0 errors.
2. **Open Developer Settings** — scroll to the Undecided Features section (orange header), verify all toggles are visible.
3. **UD.1 Weather** — enable weather, create a `WeatherService`, call `SetWeather(WeatherType.Rain)`, verify particles are active after `Update()`.
4. **UD.2 Elevation** — enable elevation, create an `ElevationService`, initialize a grid, set/get terrain elevation, verify `CalculateFallingDamageDice(100) == 10`.
5. **UD.3 Animated Tiles** — enable animated tiles, register a built-in definition, place a tile, call `Update()` multiple times, verify frame index changes.
6. **UD.4 Procedural Maps** — enable procedural generation, generate a dungeon with seed `42`, verify rooms are created and grid is filled.
7. **UD.5 Token Customization** — enable customization, get a customization for a token, change shape to `TokenShape.Star`, verify clip geometry is returned.
8. **UD.6 Measurements** — enable measurements, add a distance measurement, verify D&D diagonal distance is calculated correctly.
9. **UD.8 Dice Roller** — enable dice roller, roll 3d6, call `Update()` until settled, verify `GetTotal()` is between 3 and 18.
10. **UD.9 Accessibility** — enable colorblind mode, set to Protanopia, verify danger color is yellow-orange instead of red. Adjust UI scale slider and verify `UIScaleFactor` changes.
11. **Toggle all features off** in Developer Settings — verify all services gracefully return defaults/no-ops.
12. **Reset to defaults** — click "Reset All to Defaults" and verify all Undecided Features return to their default states.
