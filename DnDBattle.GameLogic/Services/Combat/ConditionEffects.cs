using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using Condition = DnDBattle.Models.Effects.Condition;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Combat
{
    /// <summary>
    /// Provides static helpers that apply D&amp;D 5e condition mechanical effects
    /// to attack rolls, saving throws, and movement.
    /// </summary>
    public static class CombatConditionHelper
    {
        /// <summary>
        /// Returns whether the token can move (not stunned, paralyzed, restrained, unconscious, or petrified).
        /// </summary>
        public static bool CanMove(Token token)
        {
            return !token.HasCondition(Condition.Stunned) &&
                   !token.HasCondition(Condition.Paralyzed) &&
                   !token.HasCondition(Condition.Restrained) &&
                   !token.HasCondition(Condition.Unconscious) &&
                   !token.HasCondition(Condition.Petrified);
        }

        /// <summary>
        /// Determines the attack modifier for the attacker based on their conditions.
        /// </summary>
        public static AttackMode GetAttackModifier(Token attacker, Token defender)
        {
            // Advantage sources
            if (attacker.HasCondition(Condition.Invisible))
                return AttackMode.Advantage;
            if (attacker.HasCondition(Condition.Hidden))
                return AttackMode.Advantage;

            // Disadvantage sources
            if (attacker.HasCondition(Condition.Blinded))
                return AttackMode.Disadvantage;
            if (attacker.HasCondition(Condition.Frightened))
                return AttackMode.Disadvantage;
            if (attacker.HasCondition(Condition.Poisoned))
                return AttackMode.Disadvantage;
            if (attacker.HasCondition(Condition.Prone))
                return AttackMode.Disadvantage;
            if (attacker.HasCondition(Condition.Restrained))
                return AttackMode.Disadvantage;

            return AttackMode.Normal;
        }

        /// <summary>
        /// Determines the defense modifier for the defender based on their conditions.
        /// Returns Advantage if attacks against the defender have advantage, etc.
        /// </summary>
        public static AttackMode GetDefenseModifier(Token defender)
        {
            // Attacks against have advantage
            if (defender.HasCondition(Condition.Paralyzed) ||
                defender.HasCondition(Condition.Unconscious) ||
                defender.HasCondition(Condition.Stunned) ||
                defender.HasCondition(Condition.Restrained) ||
                defender.HasCondition(Condition.Prone))
                return AttackMode.Advantage;

            // Attacks against have disadvantage
            if (defender.HasCondition(Condition.Invisible) ||
                defender.HasCondition(Condition.Dodging))
                return AttackMode.Disadvantage;

            return AttackMode.Normal;
        }

        /// <summary>
        /// Checks if the token auto-fails a saving throw of the given ability due to conditions.
        /// Stunned and Paralyzed cause auto-fail on STR/DEX saves.
        /// </summary>
        public static bool AutoFailSave(Token token, Ability ability)
        {
            if (ability == Ability.Strength || ability == Ability.Dexterity)
            {
                if (token.HasCondition(Condition.Stunned) ||
                    token.HasCondition(Condition.Paralyzed) ||
                    token.HasCondition(Condition.Unconscious))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if an attack is an automatic critical hit (e.g., hitting paralyzed/unconscious within 5ft).
        /// Uses grid distance of 1 square = 5ft.
        /// </summary>
        public static bool IsAutoCrit(Token attacker, Token defender)
        {
            if (defender.HasCondition(Condition.Paralyzed) ||
                defender.HasCondition(Condition.Unconscious))
            {
                int distance = CalculateDistance(attacker, defender);
                return distance <= 1; // Within 5ft (1 square)
            }
            return false;
        }

        /// <summary>
        /// Calculates Chebyshev distance in grid squares between two tokens.
        /// </summary>
        public static int CalculateDistance(Token a, Token b)
        {
            return Math.Max(Math.Abs(a.GridX - b.GridX), Math.Abs(a.GridY - b.GridY));
        }

        /// <summary>
        /// Checks whether a token can take actions (not incapacitated, stunned, paralyzed, unconscious, or petrified).
        /// </summary>
        public static bool CanTakeActions(Token token)
        {
            return !token.HasCondition(Condition.Incapacitated) &&
                   !token.HasCondition(Condition.Stunned) &&
                   !token.HasCondition(Condition.Paralyzed) &&
                   !token.HasCondition(Condition.Unconscious) &&
                   !token.HasCondition(Condition.Petrified);
        }

        /// <summary>
        /// Checks whether a token can take reactions.
        /// </summary>
        public static bool CanTakeReactions(Token token)
        {
            return CanTakeActions(token) &&
                   !token.HasCondition(Condition.Stunned);
        }

        /// <summary>
        /// Gets the cover AC bonus for a given cover level.
        /// </summary>
        public static int GetCoverACBonus(CoverLevel cover)
        {
            return cover switch
            {
                CoverLevel.Half => 2,
                CoverLevel.ThreeQuarters => 5,
                _ => 0
            };
        }

        /// <summary>
        /// Gets the cover DEX save bonus for a given cover level.
        /// </summary>
        public static int GetCoverDexSaveBonus(CoverLevel cover)
        {
            return cover switch
            {
                CoverLevel.Half => 2,
                CoverLevel.ThreeQuarters => 5,
                _ => 0
            };
        }
    }
}
