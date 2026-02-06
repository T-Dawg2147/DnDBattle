# Phase 6: Area Effects Expansion – Feature Documentation

This document describes the **Phase 6** features added to DnDBattle, covering area effect enhancements for the battle grid system.

---

## 📖 6.1 Spell Templates Library

### What It Does
A searchable library of **50+ D&D spell templates** that can be placed directly on the battle grid as area effects.

### Features
- **Search by name**, school, or description
- **Filter by spell level** (Cantrip through 9th)
- **Favorites** — star spells you use often and filter to show only favorites
- **Quick-place** — double-click or click "Place Spell" to start placement
- **Spell details** — selected spell shows level, school, shape, size, duration, damage, and description

### How to Use
1. Open the Spell Library via the menu or `Ctrl+L`
2. Search or browse for a spell
3. Select a spell to view its details in the info bar
4. Click **Place Spell** (or double-click) to begin placement on the map
5. Click on the grid to place the effect; cones/lines require a second click for direction

### Data Structure
Each spell is a `SpellTemplate` with: Name, Level, School, Shape, Size (feet), Width (lines), DamageType, Color, Description, Duration (rounds), Concentration flag, DamageExpression, and DamageTiming.

Spells automatically populate AreaEffect properties including duration tracking and damage-over-time fields.

---

## ⏱️ 6.2 Duration Tracking

### What It Does
Area effects with a duration automatically count down each round and expire when their time is up.

### Features
- **Timer badge** — a round indicator badge appears on effects showing remaining rounds
- **Color-coded** — badge turns **red** when only 1 round remains
- **Expiry warning** — log message when an effect is about to expire
- **Auto-remove** — effects are automatically removed when their duration reaches 0
- **Concentration indicator** — effects requiring concentration show "(C)" in their label

### How It Works
- When placing an effect with `DurationRounds > 0`, the `RoundsRemaining` counter starts at that value
- Each time a combat round ends (via the initiative tracker), `EffectDurationService.OnRoundEnd()` decrements all durations
- At 1 round remaining, a warning is logged
- At 0 remaining, the effect is removed and a message is logged

### Developer Setting
Toggle: **Enable Duration Tracking** in the Developer Window (Phase 6 section)

---

## 🔥 6.3 Damage Over Time

### What It Does
Area effects can automatically deal damage to tokens that start or end their turn inside the effect area.

### Features
- **Automatic damage rolls** using the spell's `DamageExpression` (e.g. "3d8")
- **Three timing modes**: `OnEnter`, `StartOfTurn`, `EndOfTurn`
- **Resistance/immunity** checks via the token's existing damage resistance system
- **Temp HP** absorption is automatically handled
- **Logging** — all damage is logged with full details (roll, type, resistances, remaining HP)

### Damage Timing
| Timing | When Applied |
|--------|-------------|
| `OnEnter` | When a token moves into the effect area |
| `StartOfTurn` | At the beginning of a token's turn (if in the area) |
| `EndOfTurn` | At the end of a token's turn (if in the area) |

### Developer Settings
- **Enable Damage Over Time** — master toggle
- **Auto-Apply DoT Damage** — when unchecked, only reminder messages appear (DM applies manually)

---

## 📐 6.4 Custom Polygon Areas

### What It Does
Draw irregular area shapes by clicking to place vertices, creating custom polygons for effects that don't fit standard shapes.

### Features
- **Click to place vertices** on the grid
- **Ray-casting** point-in-polygon test for accurate containment checks
- **Arbitrary shapes** — not limited to circles, cones, or rectangles

### Use Cases
- Irregularly shaped rooms or corridors
- Custom terrain effects
- Wall of Fire following a custom path
- Plant Growth in organic shapes

### Data Structure
`PolygonAreaEffect` extends `AreaEffect` with a `List<Point> Vertices` property and provides:
- `GetPolygonGeometry(cellSize)` — returns WPF `PathGeometry` for rendering
- `ContainsGridPoint(x, y)` — optimized ray-casting containment test

### Developer Setting
Toggle: **Enable Custom Polygon Area Effects** in the Developer Window

---

## ✨ 6.5 Effect Animations

### What It Does
Visual animations for active area effects to make the battle grid more dynamic and immersive.

### Animation Types
| Type | Description |
|------|-------------|
| **None** | Static effect (no animation) |
| **Pulse** | Border thickness and opacity oscillate (breathing effect) |
| **Particle** | Particles spawn inside the effect area, drift upward, and fade out |
| **Rotate** | The effect geometry rotates around its center point |

### Performance Tuning
- **Max Particles Per Effect** — slider in Developer Window (default: 50, range: 10-200)
- **Default Animation Type** — dropdown to set which animation new effects get by default
- Delta time is clamped to prevent jumps if the window was hidden
- Stale particle data is cleaned up when effects are removed

### Developer Settings
- **Enable Effect Animations** — master toggle (disables all animations for low-end systems)
- **Max Particles/Effect** — performance slider
- **Default Animation** — dropdown (None/Pulse/Particle/Rotate)

---

## 🔧 Developer Window

All Phase 6 features have been added to the **Developer Settings** window with individual toggles and configuration options. Open it from the Developer menu to enable/disable features at runtime.

### Phase 6 Controls
| Control | Option |
|---------|--------|
| ☑ Enable Spell Templates Library | `Options.EnableSpellLibrary` |
| ☑ Enable Duration Tracking | `Options.EnableDurationTracking` |
| ☑ Enable Damage Over Time | `Options.EnableDamageOverTime` |
| ☑ Auto-Apply DoT Damage | `Options.AutoApplyDotDamage` |
| ☑ Enable Custom Polygon Areas | `Options.EnablePolygonEffects` |
| ☑ Enable Effect Animations | `Options.EnableEffectAnimations` |
| 🎚 Max Particles/Effect | `Options.MaxParticlesPerEffect` |
| 📋 Default Animation | `Options.DefaultAnimationType` |

---

## 🧪 Simple Test Procedure

### Test 1: Spell Library
1. Open the Spell Library (menu or Ctrl+L)
2. Type "fire" in the search box → verify Fireball and other fire spells appear
3. Click on Fireball → verify details show "3rd Evocation · Sphere · 20ft · 8d6 Fire"
4. Star a spell as favorite, check "Favorites Only" → verify only starred spells show
5. Double-click Fireball → verify placement cursor appears on the grid

### Test 2: Duration Tracking
1. Place Spirit Guardians (has 100-round duration) on the grid
2. Verify a blue badge with "100" appears on the effect
3. Advance rounds in the initiative tracker
4. Verify the badge number decrements each round
5. When it reaches 1, verify the badge turns red and a warning appears in the log

### Test 3: Damage Over Time
1. Place Cloud of Daggers (StartOfTurn, 4d4 slashing) on the grid
2. Place a token inside the effect area
3. Start the token's turn in initiative
4. Verify damage is rolled and logged: "{Name} takes X slashing damage from Cloud of Daggers"
5. Verify the token's HP is reduced

### Test 4: Polygon Effects
1. Verify `PolygonAreaEffect` can be instantiated with vertices
2. Add 4 vertices forming a square and verify `ContainsGridPoint` returns true for interior points
3. Verify `ContainsGridPoint` returns false for exterior points

### Test 5: Effect Animations
1. Enable effect animations in Developer Window
2. Set Default Animation to "Pulse"
3. Place a Fireball on the grid
4. Verify the border thickness oscillates (pulsing effect)
5. Change animation to "Rotate" and verify the effect rotates

### Test 6: Developer Window
1. Open Developer Window
2. Scroll to Phase 6 section
3. Toggle each checkbox and verify the corresponding `Options` property changes
4. Click "Reset All to Defaults" and verify all Phase 6 options reset
