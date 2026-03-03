# Visual Refresh Architecture Guide

This document describes the **Visual Refresh** system used throughout the DnDBattle WPF application. Every method that updates UI visuals is tagged with a `// VISUAL REFRESH - [CATEGORY]` comment so developers can search, reason about, and maintain refresh logic efficiently.

---

## 1. Category Reference

| Category Tag | Description | Key Methods |
|---|---|---|
| `TOKEN_RENDERING` | Full token visual rebuild, layout positioning, single-token updates | `RebuildTokenVisuals()`, `LayoutTokens()`, `Token_PropertyChanged()`, `UpdateSingleTokenVisual()`, `CreateElevationBadge()`, `CreateFacingArrow()` |
| `TOKEN_HP` | Lightweight HP bar updates on tokens (no full rebuild) | `UpdateTokenHPBar()`, `CreateTokenHPBar()` |
| `CONDITIONS` | Condition badge creation, condition visual effects on tokens | `CreateConditionBadges()`, `ApplyConditionVisualEffects()`, `RefreshConditionVisuals()` |
| `SELECTED_TOKEN_PANEL` | Right sidebar panel showing selected token details | `UpdateDisplay()`, `UpdateHPColor()`, `UpdateHPBar()`, `UpdateAbilityScores()`, `UpdateConditionsDisplay()`, `UpdateConcentrationDisplay()`, `UpdateDeathSavesDisplay()`, `UpdateLegendaryActionsDisplay()`, `UpdateSpellSlotsDisplay()`, `UpdateNotesDisplay()` |
| `INITIATIVE_TRACKER` | Left sidebar initiative tracker panel | `UpdateDisplay()`, `UpdateCombatButton()`, `SetCurrentTurn()`, `SortByInitiative()` |
| `GRID` | Base battle grid drawing (grid lines, coordinate rulers, cell display) | `UpdateGridVisual()`, `DrawCoordinateRulers()`, `UpdateCurrentCellDisplay()`, `SetGridMaxSize()`, `DrawGridViewport()`, `RefreshMapOverlays()` |
| `LIGHTING` | Light source rendering (point lights, directional lights, shadow cache) | `RedrawLighting()`, `RenderPointLight()`, `RenderDirectionalLight()`, `AddLight()`, `UpdateLight()`, `RemoveLightPublic()` |
| `FOG_OF_WAR` | Fog of war overlay rendering and state changes | `RedrawFog()`, `RenderFogOfWar()`, `RenderFogLayer()`, `SetFogOfWar()`, `RevealAllFog()`, `ResetFog()`, `SetFogEnabled()`, `SetPlayerView()`, `UpdateFogVisibility()`, `UpdateTokenVisibilityForPlayerView()`, `ShowAllTokens()` |
| `VISION` | Token vision overlay and darkvision rendering | `RedrawVisionOverlay()`, `RenderDarkvisionHint()`, `UpdateFogFromTokenVision()`, `ToggleVisionOverlay()` |
| `WALLS` | Wall drawing and rendering | `RedrawWalls()`, `SetWallDrawMode()`, `SetRoomDrawMode()` |
| `MOVEMENT` | Movement range overlay, path preview, measurement, AOO warnings | `RedrawMovementOverlay()`, `RedrawPathVisual()`, `RedrawMovementCostPreview()`, `ComputeAndDrawPathPreview()`, `ClearPathVisual()`, `SetMeasureMode()`, `RedrawMeasureVisual()`, `UpdateMovementCostPreview()`, `ClearMovementCostPreview()`, `DrawAOOWarnings()` |
| `AURAS` | Token aura rendering | `RedrawAuras()`, `RenderAura()`, `InitializePhase5Visuals()` |
| `AREA_EFFECTS` | Area-of-effect spell/ability rendering | `RefreshAreaEffectsDisplay()`, `RedrawAreaEffects()`, `CancelAreaEffectPlacement()`, `UpdateAreaEffectSize()`, `UpdateAreaEffectColor()`, `PlaceAreaEffect()` |
| `STATUS_BAR` | Bottom status bar updates | `UpdateStatus()`, `UpdatePendingSpawnsDisplay()`, `AppendLog()` |
| `COMBAT_AUTOMATION` | Phase 7 combat window displays | `UpdateConcentrationStatus()`, `RefreshSelectedToken()`, `UpdateSlotDisplay()`, `RefreshDisplay()`, `UpdateDisplay()` (MultiAttackPanel) |
| `PAN_ZOOM` | Pan and zoom triggered redraws | `PanBy()` |
| `SHADOW` | Shadow softness and shadow rendering | `UpdateShadowSoftness()` |
| `TILE_MAP` | Tile map loading and rendering on battle grid | `LoadTileMap()`, `RenderTileMapToVisual()`, `DrawTileToVisual()` |
| `TILE_MAP_EDITOR` | Tile map editor control rendering | `RenderMap()`, `DrawGrid()`, `DrawTile()`, `DrawMetadataOverlays()`, etc. |
| `TILE_EFFECTS` | Tile interaction visual effects (hazards, healing, traps) | `ShowHazardEffect()`, `ShowHealingEffect()`, `ShowColoredEffect()`, `ShowTrapTriggerEffect()` |
| `UI_LIST` | List filtering/refresh in dialog windows | `RefreshList()` in various windows |
| `ALL` | Full refresh of everything (expensive) | `RefreshAllVisuals()` |

---

## 2. Refresh Hierarchy

### `RefreshAllVisuals()` — Full Grid Refresh
```
RefreshAllVisuals()
├── UpdateGridVisual()
├── DrawCoordinateRulers()
├── RebuildTokenVisuals()
│   ├── LayoutTokens()
│   ├── UpdateGridVisual()
│   ├── RedrawLighting()
│   └── RedrawAuras()
├── RedrawWalls()
├── RedrawMovementOverlay()
├── RedrawPathVisual()
├── RedrawMovementCostPreview()
├── RedrawFog()
├── RedrawVisionOverlay()
└── RefreshAreaEffectsDisplay()
```

### `RefreshTokenVisuals()` — Token-Only Refresh
```
RefreshTokenVisuals()
└── RebuildTokenVisuals()
    ├── LayoutTokens()
    ├── UpdateGridVisual()
    ├── RedrawLighting()
    └── RedrawAuras()
```

### `RefreshMapOverlays()` — Overlays Without Token Rebuild
```
RefreshMapOverlays()
├── UpdateGridVisual()
├── RedrawLighting()
├── RedrawWalls()
├── RedrawMovementOverlay()
├── RedrawPathVisual()
├── RedrawFog()
├── RedrawVisionOverlay()
└── RefreshAreaEffectsDisplay()
```

---

## 3. When to Use What — Quick Reference

| Scenario | What to Call | Why |
|---|---|---|
| A single token's HP changed | `UpdateTokenHPBar(token)` | Lightweight — only updates one HP bar |
| A token was added or removed | `RebuildTokenVisuals()` | Need to add/remove UI elements |
| A condition was toggled on a token | `UpdateSingleTokenVisual(token)` + `SelectedTokenPanel.UpdateConditionsDisplay()` | Avoids full rebuild; updates badges on one token |
| A wall was added/removed/modified | `RedrawWalls()` + `InvalidateShadowCache()` + `RedrawLighting()` | Walls affect shadows and lighting |
| A door was toggled open/closed | `RedrawWalls()` + `RedrawLighting()` | Same as wall change |
| The grid was resized | `RefreshAllVisuals()` | Everything needs to redraw |
| A light was added/modified/removed | Already handled by `AddLight()` / `UpdateLight()` / `RemoveLightPublic()` | These call `InvalidateShadowCache()` + `RedrawLighting()` |
| Fog of war state changed | `RedrawFog()` | Redraws the fog overlay layer |
| Token vision should update | `RedrawVisionOverlay()` | Redraws the vision dim overlay |
| Area effect placed/modified | `RefreshAreaEffectsDisplay()` | Redraws all area effects |
| Token moved to a new cell | `LayoutTokens()` (auto via `Token_PropertyChanged`) | Position-only update, no rebuild |
| Token's image changed | `RebuildTokenVisuals()` (auto via `Token_PropertyChanged`) | Need to recreate the image element |
| Token's elevation changed | `RebuildTokenVisuals()` (auto via `Token_PropertyChanged`) | Need to recreate elevation badge |
| Token's auras changed | `RedrawAuras()` (auto via `Token_PropertyChanged`) | Only redraws the aura layer |
| Selected token changed | `SelectedTokenPanel.SetToken(token)` → calls `UpdateDisplay()` | Full panel refresh |
| Initiative order changed | `InitiativeTrackerPanel.UpdateDisplay()` | Refreshes the initiative list |
| Pan/zoom occurred | `PanBy()` or zoom methods (already call necessary redraws) | Grid, lighting, movement all update |

---

## 4. Key Trigger Points

### Automatic (via PropertyChanged)
- **`BattleGridControl.TokenRendering.cs` → `Token_PropertyChanged`**: Handles `Conditions`, `IsCurrentTurn`, `IsConcentrating`, `HP/MaxHP/TempHP`, `GridX/GridY`, `Image/DisplayImage`, `Elevation`, `FacingAngle`, `Auras`
- **`SelectedTokenPanel.xaml.cs` → `SelectedToken_PropertyChanged`**: Handles `HP/MaxHP/TempHP`, `Conditions`, `IsConcentrating/ConcentrationSpell`, `DeathSaveSuccesses/DeathSaveFailures`, `LegendaryActionsRemaining`

### Event-Driven
- **`MainViewModel.RequestTokenVisualsRefresh`**: Fired by `RefreshInitiativeOrder()` and `NextTurn()`. Subscribed by `BattleGridControl` → calls `RebuildTokenVisuals()`
- **`Tokens.CollectionChanged`**: Fires `RebuildTokenVisuals()` when tokens added/removed
- **`InitiativeTrackerPanel.TurnChanged`**: Fires `RebuildTokenVisuals()` + `SelectedTokenPanel.UpdateDisplay()` via `MainWindow.SetupInitiativeTracker()`
- **`FogOfWarService.FogChanged`**: Fires `RedrawFog()` via `OnFogChanged()`

---

## 5. How to Add a New Visual Refresh

1. **Tag the method** with `// VISUAL REFRESH - [CATEGORY]` using the appropriate category from the table above. If no category fits, add a new one to this guide.

2. **Choose the right weight**:
   - For a single token change → prefer `UpdateSingleTokenVisual(token)` or a property-specific update
   - For a batch change affecting all tokens → use `RebuildTokenVisuals()`
   - For overlay-only changes → use `RefreshMapOverlays()` or the specific overlay redraw
   - For everything → use `RefreshAllVisuals()` (sparingly!)

3. **Wire up triggers**:
   - If the refresh should fire on a Token property change, add it to `Token_PropertyChanged` in `BattleGridControl.TokenRendering.cs`
   - If the refresh should fire on a SelectedTokenPanel property change, add it to `SelectedToken_PropertyChanged` in `SelectedTokenPanel.xaml.cs`
   - If the refresh should fire on a service event, subscribe in the appropriate initialization method

4. **Thread safety**: All UI updates must happen on the WPF Dispatcher thread. Use `Dispatcher.Invoke()` when calling from background threads.

5. **Performance**: `RebuildTokenVisuals()` is the most expensive operation. It removes and recreates ALL token UI elements. Minimize unnecessary calls. Prefer `UpdateSingleTokenVisual()` or `UpdateTokenHPBar()` when only one token changed.

6. **Update this guide** when adding new categories or significant new refresh methods.

---

## 6. File Locations

| File | What It Contains |
|---|---|
| `Controls/BattleGridControl.xaml.cs` | Orchestration methods, grid visual, pan/zoom, shadow |
| `Controls/BattleGridControl.TokenRendering.cs` | Token rebuild, layout, property change handling, HP bars |
| `Controls/BattleGridControl.Conditions.cs` | Condition badges and visual effects |
| `Controls/BattleGridControl.Lighting.cs` | Light management and rendering |
| `Controls/BattleGridControl.FogOfWar.cs` | Fog of war overlay and control |
| `Controls/BattleGridControl.VisionOverlay.cs` | Vision overlay and darkvision |
| `Controls/BattleGridControl.WallDrawing.cs` | Wall rendering and draw modes |
| `Controls/BattleGridControl.Measurement.cs` | Movement overlay, path preview, measurement, coordinate rulers |
| `Controls/BattleGridControl.Phase5Rendering.cs` | Auras, elevation badges, facing arrows, movement cost preview, AOO warnings |
| `Controls/BattleGridControl.AreaEffects.cs` | Area effect rendering and placement |
| `Controls/BattleGridControl.TileMap.cs` | Tile map loading and rendering |
| `Controls/BattleGridControl.TileInteractions.cs` | Tile interaction visual effects |
| `Controls/BattleGridControl.SaveLoad.cs` | Encounter loading (triggers rebuilds) |
| `Controls/GridVisualHost.cs` | Low-level grid line drawing |
| `Views/Combat/SelectedTokenPanel.xaml.cs` | Selected token detail panel |
| `Views/Combat/InitiativeTrackerPanel.xaml.cs` | Initiative tracker panel |
| `Views/Features/Phase7CombatWindow.xaml.cs` | Combat automation displays |
| `MainWindow.xaml.cs` | Status bar, event wiring |
