# Phase 7: Combat Automation – Feature Documentation

This document describes the **Phase 7 Combat Automation** features added to DnDBattle.
Use the top menu **⚔️ Phase 7** → **Combat Automation Panel...** (or `🔧 Developer Settings → Phase 7`) to toggle features and trigger quick actions (Quick Attack, Quick Save, Short/Long Rest, Check Concentration).

---

## 7.1 Attack Roll System

**Service:** `AttackRollSystem` (`Services/AttackRollSystem.cs`)  
**Models:** `AttackResult`, `AttackMode` (`Models/AttackResult.cs`, `Models/AttackMode.cs`)

### What It Does
- Rolls **d20 + attack bonus** against a target's **Armor Class** (AC).
- Supports **Normal**, **Advantage** (roll 2d20, take highest), and **Disadvantage** (roll 2d20, take lowest).
- **Critical Hit** on natural 20 — doubles all damage dice.
- **Critical Fumble** on natural 1 — always misses regardless of modifiers.
- Automatically applies **damage** to the target (including resistances/vulnerabilities/immunities).
- Integrates with **Condition Automation** (conditions modify advantage/disadvantage).
- Integrates with **Cover System** (cover adds to effective AC).

### Options (`Options.cs`)
| Option | Default | Description |
|---|---|---|
| `EnableAttackRollSystem` | `true` | Master toggle for attack roll automation |
| `AutoApplyDamage` | `true` | Auto-apply damage to targets on a successful hit |

### Simple Test
1. Place two tokens on the map: "Fighter" (AttackBonus=5) and "Goblin" (AC=15, HP=7).
2. Select Fighter, then use **⚔️ Phase 7 → Quick: Roll Attack on Target**.
3. Verify the combat log shows the d20 roll +5 vs AC 15 and applies damage on hit.

---

## 7.2 Saving Throw Automation

**Service:** `SavingThrowSystem` (`Services/SavingThrowSystem.cs`)  
**Model:** `SavingThrowResult` (`Models/SavingThrowResult.cs`)

### What It Does
- Rolls **d20 + ability modifier** vs a **DC** (difficulty class).
- Supports all six abilities: STR, DEX, CON, INT, WIS, CHA.
- **Natural 20** always succeeds; **Natural 1** always fails.
- Batch saves for **area effects** (e.g., Fireball) — rolls once for all affected tokens.
- Half-damage on success (for Dex saves like Fireball) or no damage on success.
- **Legendary Resistance** — automatically used by monsters to turn a failure into success.
- Integrates with **Condition Automation** (stunned/paralyzed auto-fail STR/DEX saves).

### Options (`Options.cs`)
| Option | Default | Description |
|---|---|---|
| `EnableSavingThrowAutomation` | `true` | Master toggle for saving throw automation |
| `AutoRollMonsterSaves` | `true` | Auto-roll saves for monster tokens (no manual prompt) |

### Simple Test
1. Select a token "Wizard" with WIS=16 (modifier +3).
2. Use **⚔️ Phase 7 → Quick: Roll Saving Throw** and choose Wisdom, DC 15.
3. Verify the result equals d20+3 and the success flag matches the DC check.

---

## 7.3 Spell Slot Tracking

**Service:** `SpellCastingService` (`Services/SpellCastingService.cs`)  
**Model:** `SpellSlots` (`Models/SpellSlots.cs`) — *already existed, now integrated with casting*

### What It Does
- Tracks spell slots per token (levels 1–9) with max and current values.
- Auto-deducts a slot when a spell is cast via `SpellCastingService.CastSpell()`.
- Integrates with **Concentration Tracking** — automatically starts concentration if the spell requires it.
- Short rest / long rest recovery supported via `SpellSlots.ShortRest()` / `SpellSlots.LongRest()`.
- Full caster, half caster, and third caster level progressions built-in.

### Options (`Options.cs`)
| Option | Default | Description |
|---|---|---|
| `EnableSpellSlotTracking` | `true` | Enable spell slot consumption on cast |

### Simple Test
1. Open **⚔️ Phase 7 → Combat Automation Panel...** and set a caster token to level 5 (4/3/2 slots).
2. Cast a level 3 spell from the panel.
3. Verify level 3 slots decrement from 2 to 1; use **Spell Slots → Short Rest/Long Rest** menu items to restore.

---

## 7.4 Concentration Tracking

**Service:** `ConcentrationService` (`Services/ConcentrationService.cs`)

### What It Does
- Tracks which spell a token is concentrating on.
- On damage, auto-rolls a **Constitution saving throw** (DC = max(10, damage/2)).
- On failure, concentration is **automatically broken**.
- Casting a new concentration spell drops the previous one first.
- Visual indicator via existing `IsConcentrating` / `ConcentrationSpell` properties on Token.

### Options (`Options.cs`)
| Option | Default | Description |
|---|---|---|
| `EnableConcentrationTracking` | `true` | Master toggle for concentration |
| `AutoPromptConcentrationCheck` | `true` | Auto-prompt concentration check when damaged |

### Simple Test
1. Select a token with CON=14 (modifier +2).
2. In the Combat Automation Panel, start concentration on "Hold Person".
3. Use **⚔️ Phase 7 → Check Concentration** after taking 22 damage (DC 11).
4. Verify the save uses d20+2 vs DC 11 and breaks concentration on failure.

---

## 7.5 Condition Automation

**Service:** `CombatConditionHelper` (static class, `Services/ConditionEffects.cs`)  
**Model:** `ConditionInstance` (`Models/ConditionInstance.cs`)

### What It Does
Applies D&D 5e mechanical effects based on active conditions:

| Condition | Movement | Attacks | Defense | Saves |
|---|---|---|---|---|
| **Blinded** | — | Disadvantage | Advantage to be hit | — |
| **Frightened** | — | Disadvantage | — | — |
| **Invisible** | — | Advantage | Disadvantage to be hit | — |
| **Paralyzed** | Blocked | — | Advantage to be hit, auto-crit ≤5ft | Auto-fail STR/DEX |
| **Prone** | — | Disadvantage | Advantage (melee) | — |
| **Restrained** | Blocked | Disadvantage | Advantage to be hit | Disadvantage DEX |
| **Stunned** | Blocked | — | Advantage to be hit | Auto-fail STR/DEX |
| **Unconscious** | Blocked | — | Advantage, auto-crit ≤5ft | Auto-fail STR/DEX |
| **Dodging** | — | — | Disadvantage to be hit | — |
| **Hidden** | — | Advantage | — | — |

- Integrated into `AttackRollSystem` — advantage/disadvantage combine per 5e rules.
- `ConditionInstance` supports **duration tracking** (rounds remaining) and **save-to-end** (e.g., WIS save DC 15 at end of turn).

### Options (`Options.cs`)
| Option | Default | Description |
|---|---|---|
| `EnableConditionAutomation` | `true` | Enable condition mechanical effects |

### Simple Test
1. In the Combat Automation Panel, apply the **Stunned** condition to a token.
2. Verify the token cannot move and attackers roll with advantage (see log/overlay).
3. Trigger a Dexterity save via **Quick: Roll Saving Throw** to confirm auto-fail.

---

## 7.6 Cover System

**Service:** `CoverSystem` (`Services/CoverSystem.cs`)  
**Model:** `CoverLevel` (`Models/CoverLevel.cs`)

### What It Does
- Traces a **Bresenham line** between attacker and defender to detect obstacles.
- **Half cover** (+2 AC, +2 DEX saves) — 1 cover-providing obstacle.
- **Three-quarters cover** (+5 AC, +5 DEX saves) — 2+ cover-providing obstacles.
- **Full cover** — blocking obstacle (wall) → target cannot be attacked.
- Integrated into `AttackRollSystem` — cover bonus added to effective AC.

### Options (`Options.cs`)
| Option | Default | Description |
|---|---|---|
| `EnableCoverSystem` | `true` | Enable cover detection and AC bonuses |

### Simple Test
1. Create two tokens at positions (0,0) and (5,5).
2. Call `CoverSystem.CalculateCover(attacker, defender, isBlocked, providesCover)`.
3. With `providesCover` returning true for cell (3,3): result should be `CoverLevel.Half`.
4. With `isBlocked` returning true for cell (3,3): result should be `CoverLevel.Full`.

---

## Developer Window Integration

All Phase 7 features appear in the **Developer Settings** window under the
"⚙️ Phase 7: Combat Automation" heading. Each feature has a checkbox toggle
that reads/writes the corresponding `Options` static property in real-time.

### How to Access
Open the Developer Settings window from the application menu/toolbar.
Scroll down to the Phase 7 section to enable/disable individual combat automation features.

---

## New Files Added

### Models
| File | Description |
|---|---|
| `Models/Ability.cs` | Enum for the six D&D ability scores |
| `Models/AttackMode.cs` | Enum: Normal, Advantage, Disadvantage |
| `Models/AttackResult.cs` | Full attack roll result with damage |
| `Models/CoverLevel.cs` | Enum: None, Half, ThreeQuarters, Full |
| `Models/SavingThrowResult.cs` | Saving throw result with legendary resistance |
| `Models/ConditionInstance.cs` | Duration-tracked condition with save-to-end |

### Services
| File | Description |
|---|---|
| `Services/AttackRollSystem.cs` | Core attack roll logic |
| `Services/SavingThrowSystem.cs` | Saving throw logic with batch support |
| `Services/ConcentrationService.cs` | Concentration check and management |
| `Services/ConditionEffects.cs` | Static helpers for condition mechanics |
| `Services/CoverSystem.cs` | Cover detection using Bresenham line |
| `Services/SpellCastingService.cs` | Spell slot consumption workflow |

### Modified Files
| File | Changes |
|---|---|
| `Options.cs` | Added 9 Phase 7 options |
| `Views/DeveloperWindow.xaml` | Added Phase 7 toggle section |
| `Views/DeveloperWindow.xaml.cs` | Load/save/reset Phase 7 options |
| `DnDBattle.csproj` | Added `EnableWindowsTargeting` for cross-platform build |
