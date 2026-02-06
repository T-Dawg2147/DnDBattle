using DnDBattle.Models;
using System.Collections.Generic;
using DnDBattle.Services;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Services.Combat;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Combat
{
    public static class ConditionEffectsService
    {
        /// <summary>
        /// Gets the mechanical effects of a condition for display/reminders
        /// </summary>
        public static ConditionEffects GetEffects(Condition condition)
        {
            return condition switch
            {
                Condition.Blinded => new ConditionEffects
                {
                    AttackDisadvantage = true,
                    AttacksAgainstHaveAdvantage = true,
                    AutoFailSightChecks = true,
                    Description = "Can't see. Auto-fail checks requiring sight. Attack rolls have disadvantage. Attacks against have advantage."
                },
                Condition.Charmed => new ConditionEffects
                {
                    CantAttackCharmer = true,
                    CharmerHasAdvantageOnSocial = true,
                    Description = "Can't attack the charmer. Charmer has advantage on social checks."
                },
                Condition.Deafened => new ConditionEffects
                {
                    AutoFailHearingChecks = true,
                    Description = "Can't hear. Auto-fail checks requiring hearing."
                },
                Condition.Frightened => new ConditionEffects
                {
                    DisadvantageWhileSourceVisible = true,
                    CantMoveCloserToSource = true,
                    Description = "Disadvantage on ability checks and attacks while source is visible. Can't willingly move closer to source."
                },
                Condition.Grappled => new ConditionEffects
                {
                    SpeedZero = true,
                    Description = "Speed becomes 0. Can't benefit from speed bonuses."
                },
                Condition.Incapacitated => new ConditionEffects
                {
                    CantTakeActions = true,
                    CantTakeReactions = true,
                    Description = "Can't take actions or reactions."
                },
                Condition.Invisible => new ConditionEffects
                {
                    AttackAdvantage = true,
                    AttacksAgainstHaveDisadvantage = true,
                    Description = "Impossible to see without special means. Attacks have advantage. Attacks against have disadvantage."
                },
                Condition.Paralyzed => new ConditionEffects
                {
                    Incapacitated = true,
                    CantMove = true,
                    CantSpeak = true,
                    AutoFailStrDexSaves = true,
                    AttacksAgainstHaveAdvantage = true,
                    MeleeHitsAreCrits = true,
                    Description = "Incapacitated. Can't move or speak. Auto-fail STR/DEX saves. Attacks against have advantage. Melee hits are crits."
                },
                Condition.Petrified => new ConditionEffects
                {
                    Incapacitated = true,
                    CantMove = true,
                    CantSpeak = true,
                    Unaware = true,
                    AutoFailStrDexSaves = true,
                    AttacksAgainstHaveAdvantage = true,
                    ResistAllDamage = true,
                    ImmuneToPoison = true,
                    Description = "Transformed to inanimate substance. Incapacitated, unaware. Resist all damage. Immune to poison/disease."
                },
                Condition.Poisoned => new ConditionEffects
                {
                    AttackDisadvantage = true,
                    AbilityCheckDisadvantage = true,
                    Description = "Disadvantage on attack rolls and ability checks."
                },
                Condition.Prone => new ConditionEffects
                {
                    AttackDisadvantage = true,
                    MeleeAttacksAgainstHaveAdvantage = true,
                    RangedAttacksAgainstHaveDisadvantage = true,
                    MustCrawl = true,
                    Description = "Disadvantage on attacks. Melee attacks against have advantage. Ranged attacks against have disadvantage."
                },
                Condition.Restrained => new ConditionEffects
                {
                    SpeedZero = true,
                    AttackDisadvantage = true,
                    AttacksAgainstHaveAdvantage = true,
                    DexSaveDisadvantage = true,
                    Description = "Speed 0. Attack disadvantage. Attacks against have advantage. DEX save disadvantage."
                },
                Condition.Stunned => new ConditionEffects
                {
                    Incapacitated = true,
                    CantMove = true,
                    SpeakFalteringly = true,
                    AutoFailStrDexSaves = true,
                    AttacksAgainstHaveAdvantage = true,
                    Description = "Incapacitated. Can't move. Speak falteringly. Auto-fail STR/DEX saves. Attacks against have advantage."
                },
                Condition.Unconscious => new ConditionEffects
                {
                    Incapacitated = true,
                    CantMove = true,
                    CantSpeak = true,
                    Unaware = true,
                    DropItems = true,
                    FallProne = true,
                    AutoFailStrDexSaves = true,
                    AttacksAgainstHaveAdvantage = true,
                    MeleeHitsAreCrits = true,
                    Description = "Incapacitated, can't move or speak, unaware. Drop items, fall prone. Auto-fail STR/DEX saves. Attacks have advantage, melee crits."
                },
                Condition.Exhaustion1 => new ConditionEffects
                {
                    HasLevels = true,
                    Description = "Cumulative effects based on level (1-6). Level 6 = death."
                },
                _ => new ConditionEffects { Description = "No special effects." }
            };
        }

        /// <summary>
        /// Checks if an attack should have advantage based on conditions
        /// </summary>
        public static bool ShouldHaveAdvantage(Token attacker, Token target)
        {
            // Attacker advantages
            if (attacker.HasCondition(Condition.Invisible))
                return true;

            // Target conditions that grant advantage
            if (target.HasCondition(Condition.Blinded) ||
                target.HasCondition(Condition.Paralyzed) ||
                target.HasCondition(Condition.Petrified) ||
                target.HasCondition(Condition.Restrained) ||
                target.HasCondition(Condition.Stunned) ||
                target.HasCondition(Condition.Unconscious))
                return true;

            // Prone - melee only
            if (target.HasCondition(Condition.Prone))
            {
                // Would need to check if melee attack
                return true; // Simplified
            }

            return false;
        }

        /// <summary>
        /// Checks if an attack should have disadvantage based on conditions
        /// </summary>
        public static bool ShouldHaveDisadvantage(Token attacker, Token target)
        {
            // Attacker disadvantages
            if (attacker.HasCondition(Condition.Blinded) ||
                attacker.HasCondition(Condition.Frightened) ||
                attacker.HasCondition(Condition.Poisoned) ||
                attacker.HasCondition(Condition.Prone) ||
                attacker.HasCondition(Condition.Restrained))
                return true;

            // Target conditions that impose disadvantage
            if (target.HasCondition(Condition.Invisible))
                return true;

            return false;
        }

        /// <summary>
        /// Gets reminder text for a token's active conditions
        /// </summary>
        public static List<string> GetConditionReminders(Token token)
        {
            var reminders = new List<string>();

            if (token.HasCondition(Condition.Poisoned))
                reminders.Add("⚠️ Poisoned: Disadvantage on attacks and ability checks");

            if (token.HasCondition(Condition.Frightened))
                reminders.Add("⚠️ Frightened: Disadvantage while source visible, can't approach");

            if (token.HasCondition(Condition.Blinded))
                reminders.Add("⚠️ Blinded: Disadvantage on attacks, attacks against have advantage");

            if (token.HasCondition(Condition.Prone))
                reminders.Add("⚠️ Prone: Disadvantage on attacks, melee against has advantage");

            if (token.HasCondition(Condition.Restrained))
                reminders.Add("⚠️ Restrained: Speed 0, attack disadvantage, DEX save disadvantage");

            if (token.HasCondition(Condition.Incapacitated))
                reminders.Add("⚠️ Incapacitated: Can't take actions or reactions");

            if (token.HasCondition(Condition.Stunned))
                reminders.Add("⚠️ Stunned: Incapacitated, auto-fail STR/DEX saves");

            if (token.HasCondition(Condition.Paralyzed))
                reminders.Add("⚠️ Paralyzed: Auto-fail STR/DEX saves, melee hits are crits");

            if (token.HasCondition(Condition.Unconscious))
                reminders.Add("⚠️ Unconscious: Auto-fail STR/DEX saves, melee hits are crits");

            if (token.IsConcentrating)
                reminders.Add($"🎯 Concentrating on: {token.ConcentrationSpell}");

            return reminders;
        }
    }

    public class ConditionEffects
    {
        public bool AttackAdvantage { get; set; }
        public bool AttackDisadvantage { get; set; }
        public bool AttacksAgainstHaveAdvantage { get; set; }
        public bool AttacksAgainstHaveDisadvantage { get; set; }
        public bool MeleeAttacksAgainstHaveAdvantage { get; set; }
        public bool RangedAttacksAgainstHaveDisadvantage { get; set; }
        public bool MeleeHitsAreCrits { get; set; }
        public bool AbilityCheckDisadvantage { get; set; }
        public bool DisadvantageWhileSourceVisible { get; set; }
        public bool DexSaveDisadvantage { get; set; }
        public bool AutoFailStrDexSaves { get; set; }
        public bool AutoFailSightChecks { get; set; }
        public bool AutoFailHearingChecks { get; set; }
        public bool SpeedZero { get; set; }
        public bool CantMove { get; set; }
        public bool MustCrawl { get; set; }
        public bool CantSpeak { get; set; }
        public bool SpeakFalteringly { get; set; }
        public bool CantTakeActions { get; set; }
        public bool CantTakeReactions { get; set; }
        public bool CantAttackCharmer { get; set; }
        public bool CharmerHasAdvantageOnSocial { get; set; }
        public bool CantMoveCloserToSource { get; set; }
        public bool Incapacitated { get; set; }
        public bool Unaware { get; set; }
        public bool DropItems { get; set; }
        public bool FallProne { get; set; }
        public bool ResistAllDamage { get; set; }
        public bool ImmuneToPoison { get; set; }
        public bool HasLevels { get; set; }
        public string Description { get; set; }
    }
}