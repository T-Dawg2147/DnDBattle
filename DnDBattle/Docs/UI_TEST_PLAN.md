# DnD Battle Builder — UI Test Plan

> **Format guide**  
> Each test step has a **Status** cell and a **Notes / Comments** cell.  
> Fill the Status column using the legend below after you run each test.  
> Use the Notes column for anything you need to flag — partial failures, workarounds, environment quirks, etc.

| Symbol | Meaning |
|--------|---------|
| ✅ | Pass — works exactly as expected |
| ❌ | Fail — does not work |
| ⚠️ | Partial — works but with an issue worth noting |
| ⏭️ | Skipped — deliberately not tested this session |
| 🔲 | Not yet tested |

---

## Recommended Testing Path

Run sections in this order to build up state progressively (later sections depend on earlier ones):

1. [Section 1 — App Launch & Main Window](#section-1--app-launch--main-window)
2. [Section 2 — Menu Bar](#section-2--menu-bar)
3. [Section 3 — Toolbar](#section-3--toolbar)
4. [Section 4 — Battle Map (Centre Panel)](#section-4--battle-map-centre-panel)
5. [Section 5 — Left Sidebar: Initiative Tracker](#section-5--left-sidebar-initiative-tracker)
6. [Section 6 — Left Sidebar: Action Log](#section-6--left-sidebar-action-log)
7. [Section 7 — Right Sidebar: Selected Token Panel](#section-7--right-sidebar-selected-token-panel)
8. [Section 8 — Combat Bar (Bottom) & Status Bar](#section-8--combat-bar-bottom--status-bar)
9. [Section 9 — Creature Browser Window](#section-9--creature-browser-window)
10. [Section 10 — Creature Editor Window](#section-10--creature-editor-window)
11. [Section 11 — Token Editor & Notes](#section-11--token-editor--notes)
12. [Section 12 — Encounter Builder & Templates](#section-12--encounter-builder--templates)
13. [Section 13 — Database Import & Management](#section-13--database-import--management)
14. [Section 14 — Options & Settings Windows](#section-14--options--settings-windows)
15. [Section 15 — Wall Drawing Tools](#section-15--wall-drawing-tools)
16. [Section 16 — Fog of War](#section-16--fog-of-war)
17. [Section 17 — Tile Map Editor](#section-17--tile-map-editor)
18. [Section 18 — Phase 4: Lighting & Vision](#section-18--phase-4-lighting--vision)
19. [Section 19 — Phase 5: Advanced Token Features](#section-19--phase-5-advanced-token-features)
20. [Section 20 — Phase 6: Area Effects & Spells](#section-20--phase-6-area-effects--spells)
21. [Section 21 — Phase 7: Combat Automation](#section-21--phase-7-combat-automation)
22. [Section 22 — Phase 8: Advanced Map Features](#section-22--phase-8-advanced-map-features)
23. [Section 23 — Multiplayer / Networking](#section-23--multiplayer--networking)
24. [Section 24 — Experimental Features](#section-24--experimental-features)
25. [Section 25 — Dice, Timers & Statistics Panels](#section-25--dice-timers--statistics-panels)
26. [Section 26 — Combat Dialogs](#section-26--combat-dialogs)
27. [Section 27 — Map Object Editors](#section-27--map-object-editors)
28. [Section 28 — Keyboard Shortcuts](#section-28--keyboard-shortcuts)

---

## Section 1 — App Launch & Main Window

**Goal:** Verify the application opens correctly and the main layout is intact.

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 1.1 | Launch `DnDBattle.exe` | Application opens without errors or crash dialogs | 🔲 | |
| 1.2 | Verify window title | Title bar reads **"DnD Battle Builder"** | 🔲 | |
| 1.3 | Verify default window size | Window opens at approximately 1720 × 950 px and fits on screen | 🔲 | |
| 1.4 | Verify menu bar is visible | A menu bar is visible at the top of the window containing File, Edit, Map, Encounter, Database, View, Walls, Tools, Phase 4–8, Multiplayer, Experimental | 🔲 | |
| 1.5 | Verify toolbar is visible | A toolbar row appears below the menu bar with buttons: ➕ Add Creature, 🗺️ Load Map, Undo (↩️), Redo (↪️), 💡 Light, Snap to Grid, 📏, Grid toggle, Coords toggle | 🔲 | |
| 1.6 | Verify left sidebar is visible | Left sidebar (Initiative Tracker + Action Log) is displayed | 🔲 | |
| 1.7 | Verify centre panel is visible | Battle map area fills the centre of the window | 🔲 | |
| 1.8 | Verify right sidebar is visible | Right sidebar (Selected Token Panel) is displayed; shows "Select a token on the map" placeholder | 🔲 | |
| 1.9 | Verify status bar is visible | Status bar appears at bottom of window; shows "Cell: -,-" and "Mode: Normal" | 🔲 | |
| 1.10 | Resize window | Window can be resized; layout adjusts gracefully; minimum size 1000 × 700 is respected | 🔲 | |
| 1.11 | Drag left splitter | Dragging the vertical splitter between left sidebar and map resizes both panels | 🔲 | |
| 1.12 | Drag right splitter | Dragging the vertical splitter between map and right sidebar resizes both panels | 🔲 | |
| 1.13 | Drag left-sidebar horizontal splitter | Dragging the horizontal splitter inside the left sidebar resizes Initiative Tracker vs Action Log | 🔲 | |

---

## Section 2 — Menu Bar

### 2a — File Menu

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.1 | Click **File** | File menu drops down showing: Save Encounter…, Load Encounter…, *(separator)*, Import SRD Pack…, *(separator)*, Settings…, *(separator)*, Exit | 🔲 | |
| 2.2 | Click **File → Save Encounter…** | Save file dialog opens | 🔲 | |
| 2.3 | Click **File → Load Encounter…** | Open file dialog opens | 🔲 | |
| 2.4 | Click **File → Import SRD Pack…** | Import dialog or file picker opens | 🔲 | |
| 2.5 | Click **File → Settings…** | Options window opens | 🔲 | |
| 2.6 | Click **File → Exit** | Application closes cleanly | 🔲 | |

### 2b — Edit Menu

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.7 | Click **Edit** | Edit menu drops down showing: Undo (Ctrl+Z), Redo (Ctrl+Y) | 🔲 | |
| 2.8 | Click **Edit → Undo** (with nothing to undo) | Either nothing happens or a status message indicates nothing to undo | 🔲 | |
| 2.9 | Click **Edit → Redo** (with nothing to redo) | Either nothing happens or a status message indicates nothing to redo | 🔲 | |

### 2c — Map Menu

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.10 | Click **Map** | Menu drops down showing: Load Tile Map…, Clear Tile Map, *(separator)*, Open Tile Map Editor… | 🔲 | |
| 2.11 | Click **Map → Load Tile Map…** | File picker opens to select a tile map | 🔲 | |
| 2.12 | Click **Map → Clear Tile Map** | Tile map is cleared from the battle grid | 🔲 | |
| 2.13 | Click **Map → Open Tile Map Editor…** | Tile Map Editor window opens | 🔲 | |

### 2d — Encounter Menu

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.14 | Click **Encounter** | Menu drops down showing: 📋 Encounter Builder button, *(separator)*, Encounter Templates… | 🔲 | |
| 2.15 | Click **Encounter → Encounter Builder** | Encounter Builder window/panel opens | 🔲 | |
| 2.16 | Click **Encounter → Encounter Templates…** | Encounter Templates window opens | 🔲 | |

### 2e — Database Menu

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.17 | Click **Database** | Menu drops down showing: Import JSON to Database…, Reload from Database, *(separator)*, Database Statistics | 🔲 | |
| 2.18 | Click **Database → Import JSON to Database…** | Database Import window opens | 🔲 | |
| 2.19 | Click **Database → Reload from Database** | Database reloads (status bar or log updates) | 🔲 | |
| 2.20 | Click **Database → Database Statistics** | Statistics message or window appears | 🔲 | |

### 2f — View Menu

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.21 | Click **View** | Menu drops down | 🔲 | |
| 2.22 | Click **View → Toggle Left Sidebar** | Left sidebar collapses/expands (also via F9) | 🔲 | |
| 2.23 | Click **View → Toggle Right Sidebar** | Right sidebar collapses/expands (also via F10) | 🔲 | |
| 2.24 | Click **View → Grid Settings…** | Grid settings dialog opens | 🔲 | |
| 2.25 | Click **View → Fog of War → Enable Fog of War** | Checkmark appears; fog overlay is applied to the battle map | 🔲 | |
| 2.26 | Click **View → Fog of War → Fog Mode → Exploration** | Exploration mode is selected (checkmark); fog stays revealed after tokens move | 🔲 | |
| 2.27 | Click **View → Fog of War → Fog Mode → Dynamic** | Dynamic mode is selected; fog reverts after tokens move | 🔲 | |
| 2.28 | Click **View → Fog of War → Reveal All (DM)** | All fog is cleared on the map | 🔲 | |
| 2.29 | Click **View → Fog of War → Reset Fog** | Fog is re-applied across the entire map | 🔲 | |

### 2g — Walls Menu

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.30 | Click **🧱 Walls** | Menu drops down with all wall options listed | 🔲 | |
| 2.31 | Click **Walls → Draw Solid Wall** | Mode changes to solid wall drawing; status bar updates | 🔲 | |
| 2.32 | Click **Walls → Draw Door** | Mode changes to door drawing | 🔲 | |
| 2.33 | Click **Walls → Draw Window** | Mode changes to window drawing | 🔲 | |
| 2.34 | Click **Walls → Draw Half Wall** | Mode changes to half-wall drawing | 🔲 | |
| 2.35 | Click **Walls → 🏠 Draw Room (Solid)** | Room drawing mode active | 🔲 | |
| 2.36 | Click **Walls → 🚪 Draw Room (with Doors)** | Room-with-doors drawing mode active | 🔲 | |
| 2.37 | Click **Walls → Stop Drawing** | Returns to normal mode | 🔲 | |
| 2.38 | Click **Walls → Clear All Walls** | All walls are removed from the map | 🔲 | |

### 2h — Tools Menu

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.39 | Click **Tools → Tile Map Builder** | Tile map builder opens | 🔲 | |
| 2.40 | Click **Tools → Developer Window…** | Developer Window opens | 🔲 | |

### 2i — Phase 4 Menu (Lighting & Vision)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.41 | Click **💡 Phase 4 → Lighting & Vision Panel…** | Phase 4 panel opens | 🔲 | |
| 2.42 | Click **Phase 4 → Quick: Add Point Light** | A point light is added at default location; map updates | 🔲 | |
| 2.43 | Click **Phase 4 → Quick: Add Directional Light** | A directional light is added; map updates | 🔲 | |
| 2.44 | Click **Phase 4 → Toggle Vision Overlay** | Checkmark toggles; vision overlay appears/disappears on map | 🔲 | |
| 2.45 | Click **Phase 4 → Update Fog from Vision** | Fog updates to reflect current token vision | 🔲 | |
| 2.46 | Click **Phase 4 → Clear Shadow Cache** | Shadow cache cleared; map re-renders | 🔲 | |

### 2j — Phase 5 Menu (Advanced Token Features)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.47 | Click **🎭 Phase 5 → Advanced Token Features Panel…** | Phase 5 panel opens | 🔲 | |
| 2.48 | With a token selected, click **Phase 5 → Quick: Add Paladin Aura to Selected** | Paladin aura is added to the selected token; visible on map | 🔲 | |
| 2.49 | With a token selected, click **Phase 5 → Quick: Add Spirit Guardians to Selected** | Spirit Guardians aura appears on selected token | 🔲 | |
| 2.50 | With a token selected, click **Phase 5 → Set Elevation → 10 ft** | Token elevation changes to 10 ft; displayed on token | 🔲 | |
| 2.51 | Click **Phase 5 → Refresh Token Visuals** | All token visuals redraw correctly | 🔲 | |

### 2k — Phase 6 Menu (Area Effects Expansion)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.52 | Click **🔥 Phase 6 → Area Effects Panel…** | Phase 6 panel opens | 🔲 | |
| 2.53 | Click **Phase 6 → Open Spell Library…** | Spell Library window opens | 🔲 | |
| 2.54 | Click **Phase 6 → Quick: Place Fireball (20ft)** | Fireball area effect appears on map (20 ft radius) | 🔲 | |
| 2.55 | Click **Phase 6 → Quick: Place Darkness (15ft)** | Darkness effect appears | 🔲 | |
| 2.56 | Click **Phase 6 → Quick: Place Fog Cloud (20ft)** | Fog Cloud effect appears | 🔲 | |
| 2.57 | Click **Phase 6 → Advance Round (Tick Durations)** | Effect durations decrease by 1 round; expired effects are removed | 🔲 | |
| 2.58 | Click **Phase 6 → Clear All Effects** | All area effects are removed from the map | 🔲 | |

### 2l — Phase 7 Menu (Combat Automation)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.59 | Click **⚔️ Phase 7 → Combat Automation Panel…** | Phase 7 panel opens | 🔲 | |
| 2.60 | With attacker and target present, click **Phase 7 → Quick: Roll Attack on Target** | Attack roll is resolved and logged in the Action Log | 🔲 | |
| 2.61 | Click **Phase 7 → Quick: Roll Saving Throw** | Saving throw dialog appears or result is logged | 🔲 | |
| 2.62 | Click **Phase 7 → Spell Slots → Long Rest (Restore All)** | All spell slots restored; Action Log updated | 🔲 | |
| 2.63 | Click **Phase 7 → Spell Slots → Short Rest** | Short rest effects applied | 🔲 | |
| 2.64 | Click **Phase 7 → Check Concentration** | Concentration check runs; result shown in log | 🔲 | |

### 2m — Phase 8 Menu (Advanced Map Features)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.65 | Click **🗺️ Phase 8 → Map Features Panel…** | Phase 8 panel opens | 🔲 | |
| 2.66 | Click **Phase 8 → Grid Type → Hex Grid (Flat Top)** | Map switches to flat-top hex grid | 🔲 | |
| 2.67 | Click **Phase 8 → Grid Type → Hex Grid (Pointy Top)** | Map switches to pointy-top hex grid | 🔲 | |
| 2.68 | Click **Phase 8 → Grid Type → Square Grid (Default)** | Map returns to standard square grid | 🔲 | |
| 2.69 | Click **Phase 8 → Toggle Gridless Mode** | Checkmark toggles; grid lines disappear/reappear | 🔲 | |
| 2.70 | Click **Phase 8 → Add Map Note at Center…** | Dialog prompts for note text; note appears on map after confirming | 🔲 | |
| 2.71 | Click **Phase 8 → Open Map Library…** | Map library window opens | 🔲 | |

### 2n — Multiplayer Menu

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.72 | Click **🌐 Multiplayer → Host Game (DM)…** | Host Game window opens | 🔲 | |
| 2.73 | Click **Multiplayer → Join Game (Player)…** | Join Game window opens | 🔲 | |
| 2.74 | Click **Multiplayer → Voice Chat (Discord)…** | Voice Chat window opens | 🔲 | |
| 2.75 | Click **Multiplayer → Cloud Save & Sync…** | Cloud Save window opens | 🔲 | |

### 2o — Experimental Menu

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 2.76 | Click **🧪 Experimental → Experimental Features Panel…** | Experimental features window opens | 🔲 | |
| 2.77 | Click **Experimental → 🌧️ Weather → Rain** | Rain weather effect applied to map | 🔲 | |
| 2.78 | Click **Experimental → 🌧️ Weather → Snow** | Snow effect applied | 🔲 | |
| 2.79 | Click **Experimental → 🌧️ Weather → Fog** | Fog effect applied | 🔲 | |
| 2.80 | Click **Experimental → 🌧️ Weather → Storm** | Storm effect applied | 🔲 | |
| 2.81 | Click **Experimental → 🌧️ Weather → Clear Weather** | Weather effect removed | 🔲 | |
| 2.82 | Click **Experimental → 🕐 Time of Day → Dawn** | Time-of-day lighting shifts to dawn | 🔲 | |
| 2.83 | Click **Experimental → 🕐 Time of Day → Day** | Lighting shifts to daytime | 🔲 | |
| 2.84 | Click **Experimental → 🕐 Time of Day → Dusk** | Lighting shifts to dusk | 🔲 | |
| 2.85 | Click **Experimental → 🕐 Time of Day → Night** | Lighting shifts to night | 🔲 | |
| 2.86 | Click **Experimental → 🎲 Quick Dice Roll → Roll d20** | A d20 result appears in the Action Log | 🔲 | |
| 2.87 | Click **Experimental → 🎲 Quick Dice Roll → Roll d6** | A d6 result appears in the Action Log | 🔲 | |
| 2.88 | Click **Experimental → 🎲 Generate Procedural Map…** | Procedural map generation dialog/window opens | 🔲 | |
| 2.89 | Click **Experimental → 📏 Add Distance Measurement…** | Measurement tool activates or dialog opens | 🔲 | |
| 2.90 | Click **Experimental → ♿ Accessibility Settings…** | Accessibility settings window opens | 🔲 | |

---

## Section 3 — Toolbar

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 3.1 | Click **➕ Add Creature** button | Creature Browser window opens | 🔲 | |
| 3.2 | Click **🗺️ Load Map** button | File picker opens to select a map image | 🔲 | |
| 3.3 | Click **↩️** (Undo) button | Undo executes (same as Ctrl+Z); status bar or log reflects action | 🔲 | |
| 3.4 | Click **↪️** (Redo) button | Redo executes (same as Ctrl+Y) | 🔲 | |
| 3.5 | Hover **↩️** button | Tooltip reads "Undo (Ctrl+Z)" | 🔲 | |
| 3.6 | Hover **↪️** button | Tooltip reads "Redo (Ctrl+Y)" | 🔲 | |
| 3.7 | Click **💡 Light** button | Light source is added or light tool activates | 🔲 | |
| 3.8 | Toggle **🔒 Snap to Grid** checkbox | Tokens snap to grid cells when checked; move freely when unchecked | 🔲 | |
| 3.9 | Click **📏** (Measure) button | Measurement mode activates; cursor changes | 🔲 | |
| 3.10 | Hover **📏** button | Tooltip reads "Measure Distance" | 🔲 | |
| 3.11 | Toggle **Grid** checkbox | Grid lines appear/disappear on battle map | 🔲 | |
| 3.12 | Hover **Grid** checkbox | Tooltip reads "Show/Hide Grid Lines" | 🔲 | |
| 3.13 | Toggle **Coords** checkbox | Coordinate labels appear/disappear on map | 🔲 | |
| 3.14 | Hover **Coords** checkbox | Tooltip reads "Show/Hide Coordinate Labels" | 🔲 | |
| 3.15 | Start combat then check toolbar centre | Combat status badge "⚔️ COMBAT – Round X" appears in the centre of the toolbar | 🔲 | |

---

## Section 4 — Battle Map (Centre Panel)

### 4a — General Rendering

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 4.1 | Launch app, observe centre panel | Empty battle map renders with a grid | 🔲 | |
| 4.2 | Load a map image (via 🗺️ Load Map) | Background image appears under the grid | 🔲 | |
| 4.3 | Move mouse over map | Status bar "Cell: X,Y" updates to reflect the hovered grid cell | 🔲 | |

### 4b — Token Placement & Interaction

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 4.4 | Add a creature from browser and place it | Token appears on the map at the target cell | 🔲 | |
| 4.5 | Left-click an existing token | Token is selected; right sidebar updates to show token details | 🔲 | |
| 4.6 | Drag a token to a new cell | Token moves to the new position; grid snapping applies when enabled | 🔲 | |
| 4.7 | Drag a token outside the map boundary | Token stops at the map edge; does not disappear | 🔲 | |
| 4.8 | Left-click empty map area | Any previously selected token is deselected; right sidebar reverts to placeholder | 🔲 | |

### 4c — Right-Click Context Menus

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 4.9 | Right-click an empty cell on the map | Context menu appears (should include options like Add Light Source, map note, etc.) | 🔲 | |
| 4.10 | Right-click a token | Context menu appears with token-specific options (Edit, Remove, Conditions, etc.) | 🔲 | |
| 4.11 | Click **Add Light Source** from right-click menu | Light source sub-menu appears (Point, Directional, etc.) | 🔲 | |

### 4d — Area Effect & Fog Toolbars (on map)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 4.12 | Observe Area Effect Toolbar at top of map | Toolbar is visible; buttons for AoE shapes are shown | 🔲 | |
| 4.13 | Observe Fog of War Toolbar at top of map | Toolbar is visible next to the AoE toolbar | 🔲 | |
| 4.14 | Click an area effect shape in the AoE Toolbar | Shape appears as a template to place on the map | 🔲 | |

### 4e — Measurement Tool

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 4.15 | Activate measurement (📏 toolbar or menu), click a start cell, click an end cell | A measurement line appears; distance is shown in feet/squares | 🔲 | |
| 4.16 | Activate measurement, drag across multiple cells | Measurement updates in real-time as you drag | 🔲 | |

---

## Section 5 — Left Sidebar: Initiative Tracker

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 5.1 | Observe the left sidebar with no tokens | Initiative tracker is empty or shows placeholder text | 🔲 | |
| 5.2 | Add 2+ creatures to the map | Each creature appears as an entry in the initiative list | 🔲 | |
| 5.3 | Click **🎲 Reroll All Initiative** | All initiative values are re-randomised; list reorders | 🔲 | |
| 5.4 | Click an entry in the initiative list | The corresponding token is selected on the battle map | 🔲 | |
| 5.5 | Start combat (see Section 8), then observe initiative list | The active combatant's entry is highlighted in green with a ▶ indicator | 🔲 | |
| 5.6 | During combat, advance turn | Highlight moves to next entry | 🔲 | |
| 5.7 | Verify creature count at bottom of panel | Footer shows correct count of creatures in combat | 🔲 | |
| 5.8 | Hover over an initiative list entry | Background lightens (hover effect) | 🔲 | |

---

## Section 6 — Left Sidebar: Action Log

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 6.1 | Observe Action Log at app launch | Log is empty or shows a startup entry | 🔲 | |
| 6.2 | Perform any action (e.g., roll initiative) | New entry appears in the log with a timestamp `[HH:mm:ss]`, a source label, and a message | 🔲 | |
| 6.3 | Perform several actions to fill the log | Scroll bar appears when log overflows; scrolling works | 🔲 | |
| 6.4 | Hover a log entry | Background lightens (hover effect) | 🔲 | |
| 6.5 | Verify log font | Log text uses Consolas monospace font at 10pt | 🔲 | |

---

## Section 7 — Right Sidebar: Selected Token Panel

### 7a — No Token Selected

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 7.1 | Launch app / deselect all tokens | Right sidebar shows "Select a token on the map" placeholder text | 🔲 | |

### 7b — Token Selected (Basic Stats)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 7.2 | Select a token | Placeholder disappears; token detail scroll panel becomes visible | 🔲 | |
| 7.3 | Verify token name in header | Correct token name shown in large bold text at the top of the panel | 🔲 | |
| 7.4 | Verify subtitle line | Type, size, or alignment shown below the name in smaller hint text | 🔲 | |
| 7.5 | Verify token thumbnail | Token image (40×40) is shown in the top-right corner of the header | 🔲 | |
| 7.6 | Verify HP display | HP and max HP are shown and correct | 🔲 | |
| 7.7 | Verify AC display | Armour Class value is shown | 🔲 | |

### 7c — Conditions Bar

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 7.8 | Apply a condition to a token (via right-click context menu) | Conditions bar appears; condition icon is shown | 🔲 | |
| 7.9 | Remove the condition | Conditions bar hides when no conditions remain | 🔲 | |

### 7d — Concentration Indicator

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 7.10 | Assign a concentration spell to a token | Concentration bar appears showing "🎯 Concentrating" and the spell name | 🔲 | |
| 7.11 | Click **✕** (break concentration) button | Concentration bar hides; spell is removed | 🔲 | |

### 7e — Death Saves Panel

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 7.12 | Reduce a token's HP to 0 | Death Saves panel appears showing 3 success circles and 3 failure circles | 🔲 | |
| 7.13 | Record a death save success | One success circle fills | 🔲 | |
| 7.14 | Record a death save failure | One failure circle fills | 🔲 | |
| 7.15 | Record 3 successes | Token stabilises; status text updates accordingly | 🔲 | |
| 7.16 | Record 3 failures | Token is marked as dead; status text updates | 🔲 | |

### 7f — Targeting Mode

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 7.17 | Trigger an attack or action that requires targeting | Targeting mode indicator appears in the panel (green bar with instruction text) | 🔲 | |
| 7.18 | Click **✕ Cancel** in the targeting indicator | Targeting mode ends; indicator disappears | 🔲 | |

---

## Section 8 — Combat Bar (Bottom) & Status Bar

### 8a — Combat Tracker Panel (Left Sidebar, Combat Mode)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 8.1 | Observe the Combat Tracker header | "⚔️ Combat Tracker" heading is visible; round indicator is hidden when not in combat | 🔲 | |
| 8.2 | Click **Start Combat** button | Button turns red labelled "End Combat"; round counter appears; initiative order activates | 🔲 | |
| 8.3 | Observe Turn Controls row (only visible in combat) | "◀ Prev" and "Next ▶" buttons + current creature name appear | 🔲 | |
| 8.4 | Click **Next ▶** | Advances to next combatant; initiative highlight moves | 🔲 | |
| 8.5 | Click **◀ Prev** | Goes back to previous combatant | 🔲 | |
| 8.6 | Let round complete | Round counter increments by 1 | 🔲 | |
| 8.7 | Click **End Combat** | Combat ends; combat controls hide; round display resets | 🔲 | |

### 8b — Initiative Bar (Bottom of Window, During Combat)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 8.8 | Start combat and observe bottom of window | Initiative bar appears showing round counter + horizontal list of combatants + Prev/Next buttons | 🔲 | |
| 8.9 | Verify current turn highlight in initiative bar | The active combatant card uses accent colour background | 🔲 | |
| 8.10 | Scroll horizontal initiative list | List scrolls when there are many combatants | 🔲 | |
| 8.11 | End combat | Initiative bar disappears | 🔲 | |

### 8c — Status Bar

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 8.12 | Move mouse over map | "Cell: X,Y" updates in real-time | 🔲 | |
| 8.13 | Change modes (e.g., wall drawing) | "Mode: …" text in status bar reflects current mode | 🔲 | |

---

## Section 9 — Creature Browser Window

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 9.1 | Open via ➕ Add Creature or File menu | Creature Browser Window opens as a separate window | 🔲 | |
| 9.2 | Verify creature list displays | Creatures from the database are listed | 🔲 | |
| 9.3 | Search/filter creatures | Filtering the list narrows results correctly | 🔲 | |
| 9.4 | Select a creature and click Add / double-click | Creature is placed as a token on the battle map | 🔲 | |
| 9.5 | Click Edit on an existing creature | Creature Editor window opens with that creature's stats | 🔲 | |
| 9.6 | Click New / Create Creature | Creature Editor opens with blank fields | 🔲 | |
| 9.7 | Close the browser | Window closes cleanly; map focus returns | 🔲 | |

---

## Section 10 — Creature Editor Window

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 10.1 | Open a creature for editing | Creature Editor opens (700 × 600 px); all fields are populated | 🔲 | |
| 10.2 | Verify Basic Info section | Name, Type (dropdown), Size (dropdown) fields are present and pre-filled | 🔲 | |
| 10.3 | Change Type to each creature type | All 14 types available: Aberration, Beast, Celestial, Construct, Dragon, Elemental, Fey, Fiend, Giant, Humanoid, Monstrosity, Ooze, Plant, Undead | 🔲 | |
| 10.4 | Change Size dropdown | All 6 sizes available: Tiny, Small, Medium, Large, Huge, Gargantuan | 🔲 | |
| 10.5 | Edit the Name field | Name text updates | 🔲 | |
| 10.6 | Scroll the editor | Scroll bar works when content exceeds window height | 🔲 | |
| 10.7 | Click **Save** (or OK) | Changes are saved; window closes | 🔲 | |
| 10.8 | Click **Cancel** | No changes are applied; window closes | 🔲 | |

---

## Section 11 — Token Editor & Notes

### 11a — Token Editor View

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 11.1 | Open Token Editor for a placed token | Token Editor view opens showing HP, AC, name, image, and stats fields | 🔲 | |
| 11.2 | Edit HP field | HP updates and is reflected in the map token | 🔲 | |
| 11.3 | Edit name | Token name updates on map and in initiative list | 🔲 | |
| 11.4 | Assign or change token image | Image updates in editor preview and on map | 🔲 | |
| 11.5 | Save changes | Changes persist after closing and re-selecting the token | 🔲 | |

### 11b — Token Notes Panel

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 11.6 | Open Token Notes for a token | Notes panel appears | 🔲 | |
| 11.7 | Type a note | Text is accepted | 🔲 | |
| 11.8 | Save and re-open notes | Previously entered note is still present | 🔲 | |

### 11c — Tag Input Dialog

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 11.9 | Open Tag Input Dialog (via creature/token editor) | Dialog opens with a text input for tag entry | 🔲 | |
| 11.10 | Enter a tag and confirm | Tag appears on the creature/token | 🔲 | |
| 11.11 | Cancel dialog | No tag is added | 🔲 | |

---

## Section 12 — Encounter Builder & Templates

### 12a — Encounter Builder

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 12.1 | Open the Encounter Builder | Encounter Builder window/panel opens | 🔲 | |
| 12.2 | Verify party setup section | Fields for number of players and average player level are present | 🔲 | |
| 12.3 | Set party size and level | XP thresholds update | 🔲 | |
| 12.4 | Add monsters to the encounter | Monsters appear in the encounter list | 🔲 | |
| 12.5 | Check difficulty rating | Difficulty indicator (Easy/Medium/Hard/Deadly) updates based on monster XP | 🔲 | |
| 12.6 | Load encounter to map | All encounter tokens placed on map | 🔲 | |
| 12.7 | Clear encounter | Encounter list empties | 🔲 | |

### 12b — Encounter Templates

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 12.8 | Open the Encounter Templates window | Window opens listing saved templates | 🔲 | |
| 12.9 | Save current encounter as a template | Template saved and appears in the list | 🔲 | |
| 12.10 | Load a template | Encounter is recreated from the template | 🔲 | |
| 12.11 | Delete a template | Template removed from list | 🔲 | |

---

## Section 13 — Database Import & Management

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 13.1 | Open Database Import Window (**Database → Import JSON to Database…**) | Window opens with an import interface | 🔲 | |
| 13.2 | Import a valid JSON creature file | Creatures are imported; success message shown | 🔲 | |
| 13.3 | Import an invalid/malformed JSON file | Error message is displayed; app does not crash | 🔲 | |
| 13.4 | Click **Database → Reload from Database** | Creature list refreshes | 🔲 | |
| 13.5 | Click **Database → Database Statistics** | Statistics (creature count, etc.) are shown | 🔲 | |

---

## Section 14 — Options & Settings Windows

### 14a — Options Window (File → Settings)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 14.1 | Open the Options window | Window (420 × 420 px) opens; "Application Options" heading visible | 🔲 | |
| 14.2 | Verify Grid group | "Cell size (px)", "Max width (tiles)", "Max height (tiles)" fields present and pre-filled | 🔲 | |
| 14.3 | Change cell size | Value updates in field | 🔲 | |
| 14.4 | Verify Gameplay group | "Auto-resolve Attacks of Opportunity" and "Live Mode" checkboxes visible | 🔲 | |
| 14.5 | Toggle Live Mode | Checkbox changes state | 🔲 | |
| 14.6 | Verify Performance/Visuals group | "Shadow softness (px)" and "Path speed (squares/sec)" fields present | 🔲 | |
| 14.7 | Click **Save** | Changes are applied; window closes | 🔲 | |
| 14.8 | Click **Cancel** | Window closes; settings unchanged | 🔲 | |

### 14b — Developer Window

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 14.9 | Open Developer Window (**Tools → Developer Window…**) | Developer Window opens | 🔲 | |
| 14.10 | Verify feature toggles are displayed | Individual feature flags are listed and toggleable | 🔲 | |
| 14.11 | Toggle a feature flag | Feature state changes on the map/UI | 🔲 | |
| 14.12 | Close window | Window closes | 🔲 | |

### 14c — Quick Actions Settings Window

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 14.13 | Open Quick Actions Settings window | Window opens | 🔲 | |
| 14.14 | Add a custom quick action | Action appears in the list | 🔲 | |
| 14.15 | Remove a quick action | Action is removed | 🔲 | |
| 14.16 | Save settings | Settings persist after window close | 🔲 | |

### 14d — Cloud Save Window

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 14.17 | Open Cloud Save Window (**Multiplayer → Cloud Save & Sync…**) | Window opens | 🔲 | |
| 14.18 | Interact with save/load controls | UI responds (even if cloud service is unavailable) | 🔲 | |
| 14.19 | Close window | Window closes cleanly | 🔲 | |

---

## Section 15 — Wall Drawing Tools

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 15.1 | Select **Walls → Draw Solid Wall** | Status bar shows wall drawing mode | 🔲 | |
| 15.2 | Click two cells to draw a wall segment | Wall line appears on the map between the two points | 🔲 | |
| 15.3 | Draw a door segment | Door icon/indicator appears on the wall | 🔲 | |
| 15.4 | Draw a window segment | Window indicator appears | 🔲 | |
| 15.5 | Draw a half wall | Half-wall rendered at reduced height/opacity | 🔲 | |
| 15.6 | Use **Draw Room (Solid)** — click and drag a rectangle | A complete solid room outline is drawn | 🔲 | |
| 15.7 | Use **Draw Room (with Doors)** | Room outline drawn with doors on the sides | 🔲 | |
| 15.8 | Click **Stop Drawing** | Returns to normal cursor/mode | 🔲 | |
| 15.9 | Verify walls block token movement/line of sight | Placed token cannot be dragged through a wall (or LOS blocked visually) | 🔲 | |
| 15.10 | Click **Clear All Walls** | All walls removed from map | 🔲 | |

---

## Section 16 — Fog of War

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 16.1 | Enable Fog of War via **View → Fog of War → Enable Fog of War** | Dark fog overlay covers the entire map | 🔲 | |
| 16.2 | Move a token with vision | Fog lifts in the area the token can see | 🔲 | |
| 16.3 | Switch to Exploration mode | Revealed areas stay revealed even when token moves away | 🔲 | |
| 16.4 | Switch to Dynamic mode | Revealed areas re-fog when token moves away | 🔲 | |
| 16.5 | Click **Reveal All (DM)** | All fog is instantly removed | 🔲 | |
| 16.6 | Click **Reset Fog** | Fog re-covers the entire map | 🔲 | |
| 16.7 | Use the Fog of War Toolbar on the map to paint/erase fog | Brush tool paints or erases fog on the selected cells | 🔲 | |
| 16.8 | Disable Fog of War | Fog overlay disappears; map is fully visible | 🔲 | |

---

## Section 17 — Tile Map Editor

### 17a — Tile Map Editor Window

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 17.1 | Open Tile Map Editor (**Map → Open Tile Map Editor…**) | Tile Map Editor window opens | 🔲 | |
| 17.2 | Verify tile palette panel | Left panel shows available tile categories and tiles | 🔲 | |
| 17.3 | Select a tile from the palette | Tile is highlighted/selected | 🔲 | |
| 17.4 | Paint tiles onto the map canvas | Selected tile appears at clicked cells | 🔲 | |
| 17.5 | Erase tiles | Tile removed from cell | 🔲 | |
| 17.6 | Open Tile Properties Panel | Properties for the selected tile type are shown | 🔲 | |
| 17.7 | Resize the map (**Map Resize Dialog**) | Dialog opens; new dimensions are accepted; map canvas updates | 🔲 | |
| 17.8 | Add a trap (Trap Editor Dialog) | Trap Editor opens; trap saved to map | 🔲 | |
| 17.9 | Open Minimap Control | Minimap appears showing the full tile map at small scale | 🔲 | |
| 17.10 | Open Tile Category Dialog | Dialog allows creating or renaming tile categories | 🔲 | |
| 17.11 | Save the tile map | Map saved to file | 🔲 | |
| 17.12 | Load a saved tile map back into the main map | Tile map appears on battle grid | 🔲 | |
| 17.13 | Close Tile Map Editor | Window closes; changes remain on map | 🔲 | |

### 17b — Manual Roll Dialog

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 17.14 | Open Manual Roll Dialog (when prompted by the system for a manual dice roll) | Dialog appears with dice type and result input | 🔲 | |
| 17.15 | Enter a valid number and confirm | Roll is accepted and logged | 🔲 | |
| 17.16 | Enter an invalid value | Error or validation message appears | 🔲 | |

---

## Section 18 — Phase 4: Lighting & Vision

*(Open via **💡 Phase 4 → Lighting & Vision Panel…**)*

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 18.1 | Open the Lighting & Vision Panel | Window opens with light source list and controls | 🔲 | |
| 18.2 | Add a Point light source | Light appears on map; radial gradient rendered with bright/dim radius | 🔲 | |
| 18.3 | Add a Directional light | Directional light cone appears; shadows cast in correct direction | 🔲 | |
| 18.4 | Add an Ambient light | Ambient glow applied to map | 🔲 | |
| 18.5 | Change light colour (warm yellow preset) | Light gradient changes to warm yellow | 🔲 | |
| 18.6 | Change light colour (cool blue preset) | Light gradient changes to cool blue | 🔲 | |
| 18.7 | Change light colour (red preset) | Light gradient changes to red | 🔲 | |
| 18.8 | Adjust bright radius slider | Bright zone radius on map updates | 🔲 | |
| 18.9 | Adjust dim radius slider | Dim zone radius on map updates | 🔲 | |
| 18.10 | Adjust intensity slider (0.0 → 1.0) | Light intensity fades/brightens accordingly | 🔲 | |
| 18.11 | Disable a light source via its toggle | Light disappears from map | 🔲 | |
| 18.12 | Re-enable the light | Light reappears | 🔲 | |
| 18.13 | Delete a light source | Light removed; map re-renders | 🔲 | |
| 18.14 | Add a wall, then check shadow casting | Shadow appears behind the wall relative to the light | 🔲 | |
| 18.15 | Verify shadow cache invalidates on wall change | Shadows update correctly after adding/removing a wall | 🔲 | |
| 18.16 | Toggle Vision Overlay (**Phase 4 → Toggle Vision Overlay**) | Overlay shows which cells are visible to selected token | 🔲 | |
| 18.17 | Select a token with Darkvision, check overlay | Darkvision range is shown differently to normal vision | 🔲 | |
| 18.18 | Click **Update Fog from Vision** | Fog of war updates to match the currently computed vision | 🔲 | |
| 18.19 | Save and reload the encounter | Light sources are restored from the saved file | 🔲 | |

---

## Section 19 — Phase 5: Advanced Token Features

*(Open via **🎭 Phase 5 → Advanced Token Features Panel…**)*

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 19.1 | Open Phase 5 Token Features Window | Window opens with aura, elevation, and visual controls | 🔲 | |
| 19.2 | Select a token; add a Paladin Aura | Aura ring renders around the token on the map | 🔲 | |
| 19.3 | Select a token; add Spirit Guardians | Spirit Guardians area renders around the token | 🔲 | |
| 19.4 | Set token elevation to 10 ft | Elevation badge/indicator shown on the token | 🔲 | |
| 19.5 | Set elevation to 0 ft (ground) | Elevation indicator removed or shows "Ground" | 🔲 | |
| 19.6 | Token with higher elevation — compare line of sight to one at ground level | Elevation is factored into LOS calculation | 🔲 | |
| 19.7 | Click **Refresh Token Visuals** | All tokens redraw with current visual state | 🔲 | |
| 19.8 | Close the Phase 5 window | Window closes; auras remain on map | 🔲 | |

---

## Section 20 — Phase 6: Area Effects & Spells

### 20a — Area Effects Panel

*(Open via **🔥 Phase 6 → Area Effects Panel…**)*

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 20.1 | Open Phase 6 Area Effects Window | Window opens | 🔲 | |
| 20.2 | Add a custom area effect (name, radius, colour) | Effect placed on map correctly | 🔲 | |
| 20.3 | Verify effect overlaps tokens and highlights them | Tokens within the effect area are visually indicated | 🔲 | |
| 20.4 | Advance round — effect with 1 round left expires | Effect disappears from map after round advance | 🔲 | |
| 20.5 | Clear all effects | All area effects removed | 🔲 | |

### 20b — Area Effect Toolbar (on map)

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 20.6 | Select a sphere/cylinder shape from AoE Toolbar | Template preview follows mouse cursor on map | 🔲 | |
| 20.7 | Select a cone shape | Cone preview rotates toward cursor | 🔲 | |
| 20.8 | Select a line shape | Line preview shown | 🔲 | |
| 20.9 | Click to place selected shape | Effect is permanently placed at that position | 🔲 | |

### 20c — Spell Library Window

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 20.10 | Open the Spell Library (**Phase 6 → Open Spell Library…**) | Spell Library window opens | 🔲 | |
| 20.11 | Browse/search spells | Spell list filters as you type | 🔲 | |
| 20.12 | Select a spell with an area effect and place it | Effect appears on map at target location | 🔲 | |
| 20.13 | Close library | Window closes | 🔲 | |

### 20d — Spell Slots Display

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 20.14 | Select a spellcasting token | Spell Slots Display shows available slots by level | 🔲 | |
| 20.15 | Cast a spell (expend a slot) | Slot count decreases | 🔲 | |
| 20.16 | Long Rest | All slots restored | 🔲 | |

---

## Section 21 — Phase 7: Combat Automation

*(Open via **⚔️ Phase 7 → Combat Automation Panel…**)*

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 21.1 | Open Phase 7 Combat Window | Window opens with attack, save, and action resolution controls | 🔲 | |
| 21.2 | Select attacker and target; click **Roll Attack** | Attack roll computed; hit/miss shown; damage applied if hit | 🔲 | |
| 21.3 | Action Resolution Dialog — verify it opens for complex actions | Dialog presents action choices | 🔲 | |
| 21.4 | Multi-attack Panel — trigger a multi-attack | Multiple attack rolls resolved in sequence | 🔲 | |
| 21.5 | Concentration Check Dialog | Dialog appears when concentrating spellcaster takes damage | 🔲 | |
| 21.6 | Death Save Dialog | Dialog appears when a creature at 0 HP must roll | 🔲 | |
| 21.7 | Legendary Actions Display | Legendary action count shown for applicable creatures; decrements on use | 🔲 | |
| 21.8 | Turn Timer Display — verify it appears | Timer shows elapsed time for the current turn | 🔲 | |
| 21.9 | Turn Timer Settings Dialog — open and set a timer limit | Custom timer limit is applied | 🔲 | |
| 21.10 | Token Details Window — open full details for a token | Full stat block window opens | 🔲 | |

---

## Section 22 — Phase 8: Advanced Map Features

*(Open via **🗺️ Phase 8 → Map Features Panel…**)*

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 22.1 | Open Phase 8 Map Features Window | Window opens | 🔲 | |
| 22.2 | Switch to Hex grid (Flat Top) | Grid redraws as hexagons; token movement adapts | 🔲 | |
| 22.3 | Switch to Hex grid (Pointy Top) | Grid redraws in pointy-top orientation | 🔲 | |
| 22.4 | Return to Square grid | Grid returns to default | 🔲 | |
| 22.5 | Toggle Gridless Mode | Grid lines disappear entirely | 🔲 | |
| 22.6 | Add a Map Note via dialogue | Note marker appears at map centre; text is retrievable by clicking | 🔲 | |
| 22.7 | Open Map Library | Window lists saved or importable maps | 🔲 | |

---

## Section 23 — Multiplayer / Networking

### 23a — Host Game Window

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 23.1 | Open Host Game Window | Window (850 × 700 px) opens with Server Controls and Token Assignment sections | 🔲 | |
| 23.2 | Verify Port field default | Port field shows **7777** by default | 🔲 | |
| 23.3 | Change port to a valid number | Port field accepts numeric input | 🔲 | |
| 23.4 | Click **Start Server** | Button text changes to "Stop Server"; status shows "Running" | 🔲 | |
| 23.5 | Verify Connected Players list | List is visible (empty until players connect) | 🔲 | |
| 23.6 | Assign a token to a player (if player connected) | Assignment reflects in Token Assignment section | 🔲 | |
| 23.7 | Click **Stop Server** | Server stops; status shows "Stopped" | 🔲 | |
| 23.8 | Close the window | Window closes cleanly; server stops if running | 🔲 | |

### 23b — Join Game Window

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 23.9 | Open Join Game Window | Window opens with host IP/port fields | 🔲 | |
| 23.10 | Enter a host IP and port | Fields accept the values | 🔲 | |
| 23.11 | Click **Connect** (no server running) | Error or timeout message displayed; app does not crash | 🔲 | |
| 23.12 | Close window | Window closes cleanly | 🔲 | |

### 23c — Voice Chat Window

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 23.13 | Open Voice Chat Window | Window opens | 🔲 | |
| 23.14 | Verify Discord-related controls visible | UI elements for Discord voice integration present | 🔲 | |
| 23.15 | Close window | Window closes | 🔲 | |

---

## Section 24 — Experimental Features

*(Open via **🧪 Experimental → Experimental Features Panel…**)*

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 24.1 | Open Experimental Features Window | Window opens listing all experimental features | 🔲 | |
| 24.2 | Enable Rain weather effect | Particle/overlay effect visible on map | 🔲 | |
| 24.3 | Enable Snow weather effect | Snow particles/overlay visible | 🔲 | |
| 24.4 | Enable Fog weather effect | Fog particles/overlay visible | 🔲 | |
| 24.5 | Enable Storm weather effect | Storm effect visible | 🔲 | |
| 24.6 | Clear weather | All weather effects removed | 🔲 | |
| 24.7 | Set Time of Day to Night | Lighting shifts to night ambience | 🔲 | |
| 24.8 | Set Time of Day to Dawn | Lighting shifts appropriately | 🔲 | |
| 24.9 | Generate a procedural map | Map is generated and displayed | 🔲 | |
| 24.10 | Add a distance measurement | Measurement appears on map | 🔲 | |
| 24.11 | Open Accessibility Settings | Accessibility options (font size, contrast, etc.) visible | 🔲 | |

---

## Section 25 — Dice, Timers & Statistics Panels

### 25a — Dice History Panel

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 25.1 | Perform several dice rolls | Each roll appears as an entry in the Dice History panel | 🔲 | |
| 25.2 | Verify roll entries show die type, result, and purpose | All three pieces of information visible per entry | 🔲 | |
| 25.3 | Clear dice history | History panel empties | 🔲 | |

### 25b — Combat Statistics Panel

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 25.4 | Run a combat session | Statistics panel updates with damage dealt, healed, kills, etc. | 🔲 | |
| 25.5 | Check per-creature stats | Each combatant's contributions are shown | 🔲 | |

### 25c — Turn Timer Display & Settings

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 25.6 | Observe Turn Timer during combat | Timer shows elapsed seconds for the current turn | 🔲 | |
| 25.7 | Open Turn Timer Settings Dialog | Dialog opens with timer duration and warning options | 🔲 | |
| 25.8 | Set a 60-second turn limit | Timer starts counting; warning triggers at configured threshold | 🔲 | |
| 25.9 | Let a turn exceed the limit | Warning indicator appears | 🔲 | |

---

## Section 26 — Combat Dialogs

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 26.1 | Trigger **Action Resolution Dialog** | Dialog shows available actions/options; correct choice applies the action | 🔲 | |
| 26.2 | Trigger **Concentration Check Dialog** | Dialog prompts for a Constitution save; result determines concentration | 🔲 | |
| 26.3 | Trigger **Death Save Dialog** | Dialog shows pass/fail options for a death saving throw | 🔲 | |
| 26.4 | Open **Initiative View** window | Shows initiative order in a standalone window | 🔲 | |
| 26.5 | Use **Legendary Actions Display** for a creature with legendary actions | Action count shown; clicking an action decrements the counter | 🔲 | |
| 26.6 | Trigger **Multi-Attack Panel** | Panel shows all attacks; each one can be rolled separately | 🔲 | |
| 26.7 | Open **Token Details Window** | Full stat block and action list for the selected token shown | 🔲 | |

---

## Section 27 — Map Object Editors

All editors are accessible via the right-click context menu on map cells or from the Phase 8 / Tile Map Editor windows.

| ID | Test Step | Expected Result | Status | Notes / Comments |
|----|-----------|-----------------|--------|------------------|
| 27.1 | Open **Trap Editor Dialog** | Dialog opens; fields for name, trigger, damage, save DC present | 🔲 | |
| 27.2 | Save a trap | Trap appears on map at selected cell | 🔲 | |
| 27.3 | Open **Hazard Editor Dialog** | Dialog opens with hazard configuration fields | 🔲 | |
| 27.4 | Save a hazard | Hazard marker appears on map | 🔲 | |
| 27.5 | Open **Healing Zone Editor Dialog** | Dialog opens; healing amount and trigger fields present | 🔲 | |
| 27.6 | Save a healing zone | Healing zone marker appears on map | 🔲 | |
| 27.7 | Open **Interactive Editor Dialog** | Dialog opens for an interactive object (door, lever, etc.) | 🔲 | |
| 27.8 | Save an interactive object | Object appears on map; clicking it triggers interaction | 🔲 | |
| 27.9 | Open **Secret Editor Dialog** | Dialog opens for hidden/secret content | 🔲 | |
| 27.10 | Save a secret | Secret marker placed; hidden unless revealed | 🔲 | |
| 27.11 | Open **Spawn Editor Dialog** | Dialog opens with spawn point configuration | 🔲 | |
| 27.12 | Save a spawn point | Spawn point appears on map; pending spawns counter updates | 🔲 | |
| 27.13 | Open **Teleporter Editor Dialog** | Dialog opens with source/destination tile fields | 🔲 | |
| 27.14 | Save a teleporter | Teleporter pair appears on map; moving a token to the source teleports it to destination | 🔲 | |

---

## Section 28 — Keyboard Shortcuts

| ID | Shortcut | Expected Action | Status | Notes / Comments |
|----|----------|-----------------|--------|------------------|
| 28.1 | **Ctrl+S** | Save Encounter dialog opens | 🔲 | |
| 28.2 | **Ctrl+O** | Load Encounter dialog opens | 🔲 | |
| 28.3 | **Ctrl+Z** | Undo last action | 🔲 | |
| 28.4 | **Ctrl+Y** | Redo last undone action | 🔲 | |
| 28.5 | **F9** | Toggle left sidebar | 🔲 | |
| 28.6 | **F10** | Toggle right sidebar | 🔲 | |
| 28.7 | **Alt+F4** | Exit application | 🔲 | |

---

## Test Session Log

Use this table to record each testing session. Add a new row per session.

| Session # | Date | Tester | Sections Covered | Overall Result | Session Notes |
|-----------|------|--------|-----------------|----------------|---------------|
| 1 | | | | | |
| 2 | | | | | |
| 3 | | | | | |

---

## Known Issues / Outstanding Items

Record issues discovered during testing here. Each issue found during a test step can be referenced by its test ID (e.g., `4.6`).

| Issue # | Test ID | Description | Severity (Low/Medium/High/Critical) | Status (Open/Fixed/Won't Fix) | Notes |
|---------|---------|-------------|--------------------------------------|-------------------------------|-------|
| 1 | | | | | |
| 2 | | | | | |
| 3 | | | | | |
