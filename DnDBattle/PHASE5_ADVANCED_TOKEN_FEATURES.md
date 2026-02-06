# 📊 Phase 5: Advanced Token Features

## Overview

Phase 5 adds seven advanced token features to enhance tactical combat on the battle map. All features can be enabled/disabled from the **Developer Settings** window (`🔧 Developer Settings - Features`).

---

## ⭐ Features Added

### 5.1 🧭 Enhanced A* Pathfinding

**What it does:** Improved pathfinding that supports diagonal movement, difficult terrain costs, and configurable search depth limits.

- **Diagonal movement** costs 1.5 squares per step (D&D 5-10-5 rule approximation)
- **Search depth** capped at configurable max (default 60 squares = 300ft) to prevent runaway searches
- **Node limit** enforced via `Options.MaxAStarNodes` (default 20,000) for performance safety
- Path uses **Chebyshev heuristic** when diagonals are enabled, **Manhattan** otherwise

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `EnablePathfinding` | `true` | Master toggle for enhanced pathfinding |
| `AllowDiagonalMovement` | `true` | Allow diagonal steps (1.5x cost) |
| `PathfindingMaxDepth` | `60` | Max search depth in squares |

**Simple Test:** Select a token, hold Ctrl and click a destination. The path preview should show diagonal steps where appropriate. Try placing walls to verify the path routes around them.

---

### 5.2 📊 Movement Cost Preview

**What it does:** When hovering over a cell with a token selected, displays a color-coded movement cost indicator.

- **Green** — Can reach within normal movement
- **Yellow** — Close to movement limit (within dash range)
- **Red** — Too far to reach

The cost indicator shows `used/total` squares (e.g. `4/6`).

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `EnableMovementCostPreview` | `true` | Show cost overlay on hover |

**Simple Test:** Select a token on the map, then hover your mouse over nearby cells. You should see a colored rectangle with movement cost text appear at each cell. Green cells are reachable, red are too far.

---

### 5.3 🎬 Path Animation

**What it does:** Configurable smooth animation speed for tokens moving along paths.

- Adjustable speed from 0.1 to 2.0 seconds per square
- Works with the existing path animation system (`Options.PathSpeedSquaresPerSecond`)

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `EnablePathAnimation` | `true` | Enable smooth token movement |
| `PathAnimationSecondsPerSquare` | `0.3` | Animation speed (seconds per grid square) |

**Simple Test:** Open Developer Settings, adjust the speed slider. Move a token along a path to see the animation speed change.

---

### 5.4 ⚔️ Attack of Opportunity Detection

**What it does:** Enhanced AOO detection during path preview with visual warnings.

- **Red circles** around enemies that would get an Attack of Opportunity
- **"AOO" label** above threatening enemies
- Detects when a token leaves an enemy's reach (adjacent → non-adjacent)
- Considers diagonal adjacency for 8-directional reach
- Auto-resolve toggle (dice rolls vs. DM approval)

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `EnableAOODetection` | `true` | Show AOO warnings on path preview |
| `AutoResolveAOOs` | `true` | Auto-roll AOO attacks |

**Simple Test:** Place a player token adjacent to an enemy token. Select the player, Ctrl+click a destination that moves away from the enemy. You should see a red AOO warning circle around the enemy and "AOO" text label.

---

### 5.5 🔮 Token Auras

**What it does:** Visual aura rings around tokens for spells and abilities.

- **Radial gradient** fill with configurable radius, color, and opacity
- **Dashed border** ring at the aura edge
- **Name label** above the aura
- Multiple auras per token
- Toggle visibility per aura
- Pre-built templates: Paladin Aura, Spirit Guardians, Rage, Bless

**Pre-built Auras:**
| Aura | Radius | Color | Use Case |
|------|--------|-------|----------|
| Paladin Aura | 10ft (2 sq) | Gold | Aura of Protection |
| Spirit Guardians | 15ft (3 sq) | Blue | Spirit Guardians spell |
| Rage | 5ft (1 sq) | Red | Barbarian Rage |
| Bless | 30ft (6 sq) | Green | Bless spell area |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `EnableTokenAuras` | `true` | Render auras on map |

**How to use:** Right-click a token → 🔮 Auras → ➕ Add Pre-built Aura → select an aura type. Toggle visibility or remove via the same menu.

**Simple Test:** Right-click a token, select Auras → Add Pre-built Aura → Paladin Aura. A gold ring should appear around the token. Toggle visibility on/off from the aura submenu.

---

### 5.6 🏔️ Token Elevation

**What it does:** Tracks and displays token elevation for 3D tactical positioning.

- **Blue badge** in top-right corner showing elevation in feet
- Preset elevation values: 0 (ground), 5, 10, 15, 20, 30, 50, 100ft
- Elevation-adjusted distance calculation for spell ranges
- Flying tokens can be positioned above ground level

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `EnableTokenElevation` | `true` | Show elevation badges |

**How to use:** Right-click a token → 🏔️ Elevation → select a height.

**Simple Test:** Right-click a token and set elevation to 20ft. A blue badge showing "20ft" should appear in the top-right corner of the token. Set back to ground (0ft) and the badge should disappear.

---

### 5.7 🧭 Token Facing / Direction

**What it does:** Shows which direction a token is facing with a yellow arrow indicator.

- **Yellow arrow** extends from token center in facing direction
- **Arrowhead** for clear direction indication
- Auto-face movement direction option
- **Flanking detection** — checks if allies are positioned on opposite sides of a target (135°+ separation)

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `EnableTokenFacing` | `true` | Show facing arrow |
| `AutoFaceMovementDirection` | `true` | Auto-update facing when moving |
| `EnableFlankingDetection` | `true` | Check for flanking bonus |

**Simple Test:** Enable token facing in Developer Settings. Tokens should display a yellow arrow pointing in their facing direction. The default direction is right (0°).

---

## 🔧 Developer Window

All Phase 5 features are accessible from the Developer Settings window:

1. Open the app
2. Navigate to **Tools** → **Developer Settings** (or however the developer window is opened)
3. Scroll down to the **"Phase 5: Advanced Token Features"** section
4. Toggle features on/off using checkboxes
5. Adjust sliders for fine-tuning (path depth, animation speed)
6. Click **"Reset All to Defaults"** to restore all settings

---

## 📁 Files Modified / Created

### New Files
| File | Description |
|------|-------------|
| `Models/TokenAura.cs` | Aura model with pre-built templates |
| `Controls/BattleGridControl.Phase5Rendering.cs` | Phase 5 visual rendering (auras, elevation, facing, cost preview, AOO) |
| `PHASE5_ADVANCED_TOKEN_FEATURES.md` | This documentation |

### Modified Files
| File | Changes |
|------|---------|
| `Options.cs` | Added 12 Phase 5 configuration options |
| `Models/Token.cs` | Added `Elevation`, `FacingAngle`, `Auras` properties |
| `Services/MovementService.cs` | Enhanced with diagonal support, cost maps, AOO detection, flanking, elevation distance |
| `Controls/BattleGridControl.xaml.cs` | Initialize Phase 5 visuals |
| `Controls/BattleGridControl.xaml` | Added MouseLeave event |
| `Controls/BattleGridControl.TokenRendering.cs` | Render elevation badges and facing arrows |
| `Controls/BattleGridControl.MouseHandlers.cs` | Movement cost preview on hover |
| `Controls/BattleGridControl.Measurement.cs` | AOO warnings in path preview |
| `Controls/BattleGridControl.ContextMenus.cs` | Aura and Elevation context menus |
| `Views/DeveloperWindow.xaml` | Phase 5 feature toggles UI |
| `Views/DeveloperWindow.xaml.cs` | Phase 5 settings read/write logic |

---

## 🧪 Quick Validation Test

To validate all Phase 5 features are working:

1. **Build** the project — should compile with 0 errors
2. **Open Developer Settings** — scroll to Phase 5 section, verify all toggles are visible and default to enabled
3. **Select a token** — hover over cells to see movement cost preview (green/yellow/red)
4. **Ctrl+click** a destination — path preview should show diagonal steps and AOO warnings
5. **Right-click token** → Auras → add a Paladin Aura — gold ring should appear
6. **Right-click token** → Elevation → set to 20ft — blue badge should show
7. **Toggle features off** in Developer Settings — visual elements should disappear
8. **Reset to defaults** — all features re-enabled
