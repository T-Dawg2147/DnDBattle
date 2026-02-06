using System.Collections.Generic;
using System.Windows.Media;

namespace DnDBattle.Models
{
    /// <summary>
    /// Pre-built library of D&D spell templates for quick area effect placement
    /// </summary>
    public static class SpellLibrary
    {
        public static List<SpellTemplate> GetDefaultSpells()
        {
            return new List<SpellTemplate>
            {
                // ── Cantrips ──
                new SpellTemplate { Name = "Acid Splash", Level = 0, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 5, DamageType = DamageType.Acid, Color = Color.FromArgb(120, 76, 175, 80), Description = "1d6 acid damage, Dex save", DamageExpression = "1d6" },
                new SpellTemplate { Name = "Poison Spray", Level = 0, School = "Conjuration", Shape = AreaEffectShape.Cone, Size = 10, DamageType = DamageType.Poison, Color = Color.FromArgb(120, 156, 39, 176), Description = "1d12 poison damage, Con save", DamageExpression = "1d12" },
                new SpellTemplate { Name = "Thunderclap", Level = 0, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 5, DamageType = DamageType.Thunder, Color = Color.FromArgb(120, 171, 71, 188), Description = "1d6 thunder damage, Con save", DamageExpression = "1d6" },
                new SpellTemplate { Name = "Word of Radiance", Level = 0, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 5, DamageType = DamageType.Radiant, Color = Color.FromArgb(100, 255, 241, 118), Description = "1d6 radiant damage, Con save", DamageExpression = "1d6" },
                new SpellTemplate { Name = "Sword Burst", Level = 0, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 5, DamageType = DamageType.Force, Color = Color.FromArgb(120, 100, 181, 246), Description = "1d6 force damage, Dex save", DamageExpression = "1d6" },

                // ── 1st Level ──
                new SpellTemplate { Name = "Burning Hands", Level = 1, School = "Evocation", Shape = AreaEffectShape.Cone, Size = 15, DamageType = DamageType.Fire, Color = Color.FromArgb(120, 255, 140, 0), Description = "3d6 fire damage, Dex save half", DamageExpression = "3d6" },
                new SpellTemplate { Name = "Thunderwave", Level = 1, School = "Evocation", Shape = AreaEffectShape.Cube, Size = 15, DamageType = DamageType.Thunder, Color = Color.FromArgb(120, 100, 149, 237), Description = "2d8 thunder damage, Con save half", DamageExpression = "2d8" },
                new SpellTemplate { Name = "Entangle", Level = 1, School = "Conjuration", Shape = AreaEffectShape.Square, Size = 20, DamageType = DamageType.None, Color = Color.FromArgb(100, 34, 139, 34), Description = "Restrained, Str save", Duration = 10, RequiresConcentration = true },
                new SpellTemplate { Name = "Fog Cloud", Level = 1, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.None, Color = Color.FromArgb(100, 200, 200, 200), Description = "Heavily obscured area", Duration = 60, RequiresConcentration = true },
                new SpellTemplate { Name = "Grease", Level = 1, School = "Conjuration", Shape = AreaEffectShape.Square, Size = 10, DamageType = DamageType.None, Color = Color.FromArgb(100, 139, 119, 101), Description = "Dex save or fall prone", Duration = 10 },
                new SpellTemplate { Name = "Faerie Fire", Level = 1, School = "Evocation", Shape = AreaEffectShape.Cube, Size = 20, DamageType = DamageType.None, Color = Color.FromArgb(100, 255, 105, 180), Description = "Advantage on attacks, Dex save", Duration = 10, RequiresConcentration = true },
                new SpellTemplate { Name = "Sleep", Level = 1, School = "Enchantment", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.None, Color = Color.FromArgb(80, 147, 112, 219), Description = "5d8 HP of creatures fall asleep" },
                new SpellTemplate { Name = "Color Spray", Level = 1, School = "Illusion", Shape = AreaEffectShape.Cone, Size = 15, DamageType = DamageType.None, Color = Color.FromArgb(100, 255, 215, 0), Description = "6d10 HP of creatures blinded" },

                // ── 2nd Level ──
                new SpellTemplate { Name = "Shatter", Level = 2, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 10, DamageType = DamageType.Thunder, Color = Color.FromArgb(120, 186, 85, 211), Description = "3d8 thunder damage, Con save half", DamageExpression = "3d8" },
                new SpellTemplate { Name = "Cloud of Daggers", Level = 2, School = "Conjuration", Shape = AreaEffectShape.Cube, Size = 5, DamageType = DamageType.Slashing, Color = Color.FromArgb(120, 192, 192, 192), Description = "4d4 slashing damage (no save)", DamageExpression = "4d4", Duration = 10, RequiresConcentration = true, DamageTiming = DamageTiming.StartOfTurn },
                new SpellTemplate { Name = "Moonbeam", Level = 2, School = "Evocation", Shape = AreaEffectShape.Cylinder, Size = 5, DamageType = DamageType.Radiant, Color = Color.FromArgb(100, 230, 230, 250), Description = "2d10 radiant damage, Con save half", DamageExpression = "2d10", Duration = 10, RequiresConcentration = true, DamageTiming = DamageTiming.StartOfTurn },
                new SpellTemplate { Name = "Flaming Sphere", Level = 2, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 5, DamageType = DamageType.Fire, Color = Color.FromArgb(120, 255, 87, 34), Description = "2d6 fire damage, Dex save half", DamageExpression = "2d6", Duration = 10, RequiresConcentration = true, DamageTiming = DamageTiming.EndOfTurn },
                new SpellTemplate { Name = "Darkness", Level = 2, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 15, DamageType = DamageType.None, Color = Color.FromArgb(150, 20, 20, 40), Description = "Magical darkness, non-magical light fails", Duration = 100, RequiresConcentration = true },
                new SpellTemplate { Name = "Silence", Level = 2, School = "Illusion", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.None, Color = Color.FromArgb(80, 75, 0, 130), Description = "No sound, immune to thunder, can't cast verbal", Duration = 100, RequiresConcentration = true },
                new SpellTemplate { Name = "Web", Level = 2, School = "Conjuration", Shape = AreaEffectShape.Cube, Size = 20, DamageType = DamageType.None, Color = Color.FromArgb(100, 220, 220, 220), Description = "Restrained, Dex save; flammable", Duration = 60, RequiresConcentration = true },
                new SpellTemplate { Name = "Spike Growth", Level = 2, School = "Transmutation", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.Piercing, Color = Color.FromArgb(100, 107, 142, 35), Description = "2d4 piercing per 5ft moved", DamageExpression = "2d4", Duration = 100, RequiresConcentration = true, DamageTiming = DamageTiming.OnEnter },
                new SpellTemplate { Name = "Scorching Ray", Level = 2, School = "Evocation", Shape = AreaEffectShape.Line, Size = 30, Width = 5, DamageType = DamageType.Fire, Color = Color.FromArgb(120, 255, 69, 0), Description = "3 rays, 2d6 fire each, ranged attack", DamageExpression = "2d6" },

                // ── 3rd Level ──
                new SpellTemplate { Name = "Fireball", Level = 3, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.Fire, Color = Color.FromArgb(120, 255, 69, 0), Description = "8d6 fire damage, Dex save half", DamageExpression = "8d6" },
                new SpellTemplate { Name = "Lightning Bolt", Level = 3, School = "Evocation", Shape = AreaEffectShape.Line, Size = 100, Width = 5, DamageType = DamageType.Lightning, Color = Color.FromArgb(120, 135, 206, 250), Description = "8d6 lightning damage, Dex save half", DamageExpression = "8d6" },
                new SpellTemplate { Name = "Spirit Guardians", Level = 3, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 15, DamageType = DamageType.Radiant, Color = Color.FromArgb(100, 255, 215, 0), Description = "3d8 radiant damage, Wis save half", DamageExpression = "3d8", Duration = 100, RequiresConcentration = true, DamageTiming = DamageTiming.StartOfTurn },
                new SpellTemplate { Name = "Stinking Cloud", Level = 3, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.Poison, Color = Color.FromArgb(100, 154, 205, 50), Description = "Poisoned, Con save; heavily obscured", Duration = 10, RequiresConcentration = true },
                new SpellTemplate { Name = "Sleet Storm", Level = 3, School = "Conjuration", Shape = AreaEffectShape.Cylinder, Size = 40, DamageType = DamageType.Cold, Color = Color.FromArgb(100, 176, 224, 230), Description = "Difficult terrain, Con save or fall prone", Duration = 10, RequiresConcentration = true },
                new SpellTemplate { Name = "Hunger of Hadar", Level = 3, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.Cold, Color = Color.FromArgb(150, 25, 25, 50), Description = "2d6 cold (start), 2d6 acid (end)", DamageExpression = "2d6", Duration = 10, RequiresConcentration = true, DamageTiming = DamageTiming.StartOfTurn },
                new SpellTemplate { Name = "Call Lightning", Level = 3, School = "Conjuration", Shape = AreaEffectShape.Cylinder, Size = 60, DamageType = DamageType.Lightning, Color = Color.FromArgb(100, 135, 206, 250), Description = "3d10 lightning, Dex save half", DamageExpression = "3d10", Duration = 100, RequiresConcentration = true },
                new SpellTemplate { Name = "Hypnotic Pattern", Level = 3, School = "Illusion", Shape = AreaEffectShape.Cube, Size = 30, DamageType = DamageType.None, Color = Color.FromArgb(80, 186, 85, 211), Description = "Charmed + incapacitated, Wis save", Duration = 10, RequiresConcentration = true },
                new SpellTemplate { Name = "Fear", Level = 3, School = "Illusion", Shape = AreaEffectShape.Cone, Size = 30, DamageType = DamageType.None, Color = Color.FromArgb(100, 128, 0, 128), Description = "Frightened, Wis save; must Dash", Duration = 10, RequiresConcentration = true },

                // ── 4th Level ──
                new SpellTemplate { Name = "Ice Storm", Level = 4, School = "Evocation", Shape = AreaEffectShape.Cylinder, Size = 20, DamageType = DamageType.Cold, Color = Color.FromArgb(120, 173, 216, 230), Description = "2d8 bludgeoning + 4d6 cold, Dex save half", DamageExpression = "4d6" },
                new SpellTemplate { Name = "Wall of Fire", Level = 4, School = "Evocation", Shape = AreaEffectShape.Line, Size = 60, Width = 5, DamageType = DamageType.Fire, Color = Color.FromArgb(150, 255, 69, 0), Description = "5d8 fire damage within 10ft, Dex save half", DamageExpression = "5d8", Duration = 10, RequiresConcentration = true, DamageTiming = DamageTiming.EndOfTurn },
                new SpellTemplate { Name = "Blight", Level = 4, School = "Necromancy", Shape = AreaEffectShape.Sphere, Size = 5, DamageType = DamageType.Necrotic, Color = Color.FromArgb(120, 66, 66, 66), Description = "8d8 necrotic damage, Con save half", DamageExpression = "8d8" },
                new SpellTemplate { Name = "Sickening Radiance", Level = 4, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 30, DamageType = DamageType.Radiant, Color = Color.FromArgb(100, 255, 241, 118), Description = "4d10 radiant, Con save; exhaustion", DamageExpression = "4d10", Duration = 100, RequiresConcentration = true, DamageTiming = DamageTiming.StartOfTurn },
                new SpellTemplate { Name = "Vitriolic Sphere", Level = 4, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.Acid, Color = Color.FromArgb(120, 76, 175, 80), Description = "10d4 acid + 5d4 (failed save), Dex save half", DamageExpression = "10d4" },

                // ── 5th Level ──
                new SpellTemplate { Name = "Cone of Cold", Level = 5, School = "Evocation", Shape = AreaEffectShape.Cone, Size = 60, DamageType = DamageType.Cold, Color = Color.FromArgb(120, 173, 216, 230), Description = "8d8 cold damage, Con save half", DamageExpression = "8d8" },
                new SpellTemplate { Name = "Cloudkill", Level = 5, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.Poison, Color = Color.FromArgb(120, 154, 205, 50), Description = "5d8 poison damage, Con save half", DamageExpression = "5d8", Duration = 100, RequiresConcentration = true, DamageTiming = DamageTiming.StartOfTurn },
                new SpellTemplate { Name = "Insect Plague", Level = 5, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.Piercing, Color = Color.FromArgb(100, 139, 119, 101), Description = "4d10 piercing damage, Con save half", DamageExpression = "4d10", Duration = 100, RequiresConcentration = true, DamageTiming = DamageTiming.StartOfTurn },
                new SpellTemplate { Name = "Synaptic Static", Level = 5, School = "Enchantment", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.Psychic, Color = Color.FromArgb(120, 236, 64, 122), Description = "8d6 psychic damage, Int save half", DamageExpression = "8d6" },
                new SpellTemplate { Name = "Wall of Force", Level = 5, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 10, DamageType = DamageType.None, Color = Color.FromArgb(80, 100, 181, 246), Description = "Impassable wall, 10 min", Duration = 100, RequiresConcentration = true },
                new SpellTemplate { Name = "Destructive Wave", Level = 5, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 30, DamageType = DamageType.Thunder, Color = Color.FromArgb(120, 171, 71, 188), Description = "5d6 thunder + 5d6 radiant/necrotic, Con save half", DamageExpression = "5d6" },

                // ── 6th Level ──
                new SpellTemplate { Name = "Chain Lightning", Level = 6, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 15, DamageType = DamageType.Lightning, Color = Color.FromArgb(120, 255, 235, 59), Description = "10d8 lightning, Dex save half", DamageExpression = "10d8" },
                new SpellTemplate { Name = "Sunbeam", Level = 6, School = "Evocation", Shape = AreaEffectShape.Line, Size = 60, Width = 5, DamageType = DamageType.Radiant, Color = Color.FromArgb(120, 255, 241, 118), Description = "6d8 radiant, Con save half; blinded", DamageExpression = "6d8", Duration = 10, RequiresConcentration = true },
                new SpellTemplate { Name = "Blade Barrier", Level = 6, School = "Evocation", Shape = AreaEffectShape.Line, Size = 100, Width = 5, DamageType = DamageType.Slashing, Color = Color.FromArgb(120, 192, 192, 192), Description = "6d10 slashing, Dex save half", DamageExpression = "6d10", Duration = 100, RequiresConcentration = true, DamageTiming = DamageTiming.OnEnter },

                // ── 7th Level ──
                new SpellTemplate { Name = "Fire Storm", Level = 7, School = "Evocation", Shape = AreaEffectShape.Cube, Size = 40, DamageType = DamageType.Fire, Color = Color.FromArgb(120, 255, 69, 0), Description = "7d10 fire damage, Dex save half", DamageExpression = "7d10" },
                new SpellTemplate { Name = "Prismatic Spray", Level = 7, School = "Evocation", Shape = AreaEffectShape.Cone, Size = 60, DamageType = DamageType.Force, Color = Color.FromArgb(100, 255, 105, 180), Description = "10d6 (varies by color), Dex save", DamageExpression = "10d6" },

                // ── 8th Level ──
                new SpellTemplate { Name = "Incendiary Cloud", Level = 8, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 20, DamageType = DamageType.Fire, Color = Color.FromArgb(120, 255, 140, 0), Description = "10d8 fire damage, Dex save half", DamageExpression = "10d8", Duration = 10, RequiresConcentration = true, DamageTiming = DamageTiming.StartOfTurn },
                new SpellTemplate { Name = "Sunburst", Level = 8, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 60, DamageType = DamageType.Radiant, Color = Color.FromArgb(120, 255, 241, 118), Description = "12d6 radiant damage, Con save half", DamageExpression = "12d6" },

                // ── 9th Level ──
                new SpellTemplate { Name = "Meteor Swarm", Level = 9, School = "Evocation", Shape = AreaEffectShape.Sphere, Size = 40, DamageType = DamageType.Fire, Color = Color.FromArgb(150, 255, 69, 0), Description = "20d6 fire + 20d6 bludgeoning, Dex save half", DamageExpression = "20d6" },
                new SpellTemplate { Name = "Storm of Vengeance", Level = 9, School = "Conjuration", Shape = AreaEffectShape.Sphere, Size = 360, DamageType = DamageType.Thunder, Color = Color.FromArgb(100, 47, 79, 79), Description = "2d6 thunder, Con save; multi-round", DamageExpression = "2d6", Duration = 10, RequiresConcentration = true, DamageTiming = DamageTiming.StartOfTurn },

                // ── Breath Weapons ──
                new SpellTemplate { Name = "Dragon Breath (Cone)", Level = 0, School = "Breath", Shape = AreaEffectShape.Cone, Size = 30, DamageType = DamageType.Fire, Color = Color.FromArgb(120, 255, 69, 0), Description = "Varies by dragon type", DamageExpression = "8d6" },
                new SpellTemplate { Name = "Dragon Breath (Line)", Level = 0, School = "Breath", Shape = AreaEffectShape.Line, Size = 60, Width = 5, DamageType = DamageType.Lightning, Color = Color.FromArgb(120, 135, 206, 250), Description = "Varies by dragon type", DamageExpression = "8d6" },
            };
        }
    }
}
