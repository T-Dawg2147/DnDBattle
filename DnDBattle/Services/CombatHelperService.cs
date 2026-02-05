using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Condition = DnDBattle.Models.Condition;

namespace DnDBattle.Services
{
    /// <summary>
    /// Service for combat automation and helper features.
    /// Implements Phase 7 combat automation features.
    /// </summary>
    public class CombatHelperService
    {
        private readonly Random _random = new();

        #region Cover Calculation

        /// <summary>
        /// Calculates the cover type between attacker and target based on obstacles.
        /// </summary>
        public CoverType CalculateCover(Token attacker, Token target, 
            IEnumerable<(Point a, Point b)>? wallSegments = null,
            IEnumerable<Token>? otherTokens = null)
        {
            var devSettings = DevSettings.Instance;
            if (!devSettings.EnableAutoCoverCalculation)
                return CoverType.None;

            Point attackerCenter = new Point(attacker.GridX + 0.5, attacker.GridY + 0.5);
            
            // Get the four corners of the target's space
            var targetCorners = new List<Point>
            {
                new Point(target.GridX, target.GridY),
                new Point(target.GridX + target.SizeInSquares, target.GridY),
                new Point(target.GridX, target.GridY + target.SizeInSquares),
                new Point(target.GridX + target.SizeInSquares, target.GridY + target.SizeInSquares)
            };

            int blockedCorners = 0;

            foreach (var corner in targetCorners)
            {
                bool isBlocked = false;

                // Check wall obstruction
                if (wallSegments != null)
                {
                    foreach (var wall in wallSegments)
                    {
                        if (SegmentsIntersect(attackerCenter, corner, wall.a, wall.b))
                        {
                            isBlocked = true;
                            break;
                        }
                    }
                }

                // Check token obstruction (creatures provide half cover)
                if (!isBlocked && otherTokens != null)
                {
                    foreach (var token in otherTokens)
                    {
                        if (token.Id == attacker.Id || token.Id == target.Id)
                            continue;

                        // Check if line passes through token's space
                        if (LineIntersectsRect(attackerCenter, corner, 
                            token.GridX, token.GridY, 
                            token.SizeInSquares, token.SizeInSquares))
                        {
                            isBlocked = true;
                            break;
                        }
                    }
                }

                if (isBlocked)
                    blockedCorners++;
            }

            // Determine cover type based on blocked corners
            return blockedCorners switch
            {
                4 => CoverType.Full,      // All corners blocked
                3 => CoverType.ThreeQuarters, // 3 corners blocked
                >= 1 => CoverType.Half,   // 1-2 corners blocked
                _ => CoverType.None
            };
        }

        /// <summary>
        /// Gets the AC bonus from cover
        /// </summary>
        public int GetCoverACBonus(CoverType cover)
        {
            return cover switch
            {
                CoverType.Half => 2,
                CoverType.ThreeQuarters => 5,
                CoverType.Full => int.MaxValue, // Can't be targeted
                _ => 0
            };
        }

        /// <summary>
        /// Gets the DEX save bonus from cover
        /// </summary>
        public int GetCoverDexSaveBonus(CoverType cover)
        {
            return cover switch
            {
                CoverType.Half => 2,
                CoverType.ThreeQuarters => 5,
                CoverType.Full => int.MaxValue,
                _ => 0
            };
        }

        #endregion

        #region Flanking Detection

        /// <summary>
        /// Determines if the attacker has flanking advantage against the target.
        /// Flanking: Two allies on opposite sides of an enemy.
        /// </summary>
        public bool HasFlankingAdvantage(Token attacker, Token target, IEnumerable<Token> allTokens)
        {
            var devSettings = DevSettings.Instance;
            if (!devSettings.EnableFlankingIndicators)
                return false;

            // Find allies adjacent to target
            var adjacentAllies = allTokens.Where(t => 
                t.Id != attacker.Id && 
                t.Id != target.Id &&
                t.IsPlayer == attacker.IsPlayer && // Same faction
                !t.IsDead &&
                IsAdjacent(t, target)).ToList();

            if (!IsAdjacent(attacker, target))
                return false;

            // Check if any ally is on the opposite side
            foreach (var ally in adjacentAllies)
            {
                if (AreOnOppositeSides(attacker, ally, target))
                    return true;
            }

            return false;
        }

        private bool IsAdjacent(Token a, Token b)
        {
            int dx = Math.Abs(a.GridX - b.GridX);
            int dy = Math.Abs(a.GridY - b.GridY);
            return dx <= 1 && dy <= 1 && (dx + dy > 0);
        }

        private bool AreOnOppositeSides(Token a, Token b, Token target)
        {
            // Check if a and b are on opposite sides of target (diagonal or straight)
            int dx1 = a.GridX - target.GridX;
            int dy1 = a.GridY - target.GridY;
            int dx2 = b.GridX - target.GridX;
            int dy2 = b.GridY - target.GridY;

            // Opposite sides: opposite directions
            return (dx1 * dx2 < 0 && dy1 == 0 && dy2 == 0) || // Horizontal flanking
                   (dy1 * dy2 < 0 && dx1 == 0 && dx2 == 0) || // Vertical flanking
                   (dx1 * dx2 < 0 && dy1 * dy2 < 0);          // Diagonal flanking
        }

        #endregion

        #region Sneak Attack Eligibility

        /// <summary>
        /// Determines if an attacker is eligible for sneak attack against the target.
        /// </summary>
        public SneakAttackResult CheckSneakAttackEligibility(Token attacker, Token target, 
            IEnumerable<Token> allTokens)
        {
            var devSettings = DevSettings.Instance;
            if (!devSettings.EnableSneakAttackEligibility)
                return new SneakAttackResult { IsEligible = false };

            var result = new SneakAttackResult();

            // Check if attacker has advantage
            if (target.HasCondition(Condition.Blinded) ||
                target.HasCondition(Condition.Paralyzed) ||
                target.HasCondition(Condition.Stunned) ||
                target.HasCondition(Condition.Restrained) ||
                target.HasCondition(Condition.Unconscious))
            {
                result.IsEligible = true;
                result.Reason = "Advantage from target condition";
                return result;
            }

            // Check if attacker is hidden
            if (attacker.HasCondition(Condition.Hidden))
            {
                result.IsEligible = true;
                result.Reason = "Attacker is hidden";
                return result;
            }

            // Check for ally adjacent to target
            var adjacentAlly = allTokens.FirstOrDefault(t =>
                t.Id != attacker.Id &&
                t.Id != target.Id &&
                t.IsPlayer == attacker.IsPlayer &&
                !t.IsDead &&
                !t.HasCondition(Condition.Incapacitated) &&
                IsAdjacent(t, target));

            if (adjacentAlly != null)
            {
                result.IsEligible = true;
                result.Reason = $"Ally ({adjacentAlly.Name}) adjacent to target";
                result.AllyName = adjacentAlly.Name;
                return result;
            }

            return result;
        }

        #endregion

        #region Attack Roll Helpers

        /// <summary>
        /// Determines advantage/disadvantage state for an attack.
        /// </summary>
        public AdvantageState DetermineAdvantageState(Token attacker, Token target, 
            IEnumerable<Token>? allTokens = null, bool isRanged = false)
        {
            var devSettings = DevSettings.Instance;
            if (!devSettings.EnableAdvantageTracking)
                return new AdvantageState { State = RollState.Normal };

            int advantageCount = 0;
            int disadvantageCount = 0;
            var reasons = new List<string>();

            // === ADVANTAGE SOURCES ===

            // Attacker is hidden/invisible
            if (attacker.HasCondition(Condition.Hidden) || attacker.HasCondition(Condition.Invisible))
            {
                advantageCount++;
                reasons.Add("Attacker is hidden/invisible");
            }

            // Target conditions that grant advantage
            if (target.HasCondition(Condition.Blinded))
            {
                advantageCount++;
                reasons.Add("Target is blinded");
            }
            if (target.HasCondition(Condition.Paralyzed))
            {
                advantageCount++;
                reasons.Add("Target is paralyzed");
            }
            if (target.HasCondition(Condition.Petrified))
            {
                advantageCount++;
                reasons.Add("Target is petrified");
            }
            if (target.HasCondition(Condition.Restrained))
            {
                advantageCount++;
                reasons.Add("Target is restrained");
            }
            if (target.HasCondition(Condition.Stunned))
            {
                advantageCount++;
                reasons.Add("Target is stunned");
            }
            if (target.HasCondition(Condition.Unconscious))
            {
                advantageCount++;
                reasons.Add("Target is unconscious");
            }

            // Target is prone and melee attack
            if (!isRanged && target.HasCondition(Condition.Prone))
            {
                advantageCount++;
                reasons.Add("Target is prone (melee)");
            }

            // Flanking (optional rule)
            if (allTokens != null && HasFlankingAdvantage(attacker, target, allTokens))
            {
                advantageCount++;
                reasons.Add("Flanking");
            }

            // === DISADVANTAGE SOURCES ===

            // Attacker conditions
            if (attacker.HasCondition(Condition.Blinded))
            {
                disadvantageCount++;
                reasons.Add("Attacker is blinded");
            }
            if (attacker.HasCondition(Condition.Frightened))
            {
                // Only if target is source of fear (simplified)
                disadvantageCount++;
                reasons.Add("Attacker is frightened");
            }
            if (attacker.HasCondition(Condition.Poisoned))
            {
                disadvantageCount++;
                reasons.Add("Attacker is poisoned");
            }
            if (attacker.HasCondition(Condition.Restrained))
            {
                disadvantageCount++;
                reasons.Add("Attacker is restrained");
            }

            // Target is prone and ranged attack
            if (isRanged && target.HasCondition(Condition.Prone))
            {
                disadvantageCount++;
                reasons.Add("Target is prone (ranged)");
            }

            // Target is dodging
            if (target.HasCondition(Condition.Dodging))
            {
                disadvantageCount++;
                reasons.Add("Target is dodging");
            }

            // Target is invisible (and attacker can't see them)
            if (target.HasCondition(Condition.Invisible) && 
                !attacker.HasTruesight && !attacker.HasBlindsight)
            {
                disadvantageCount++;
                reasons.Add("Target is invisible");
            }

            // Determine final state
            if (advantageCount > 0 && disadvantageCount > 0)
            {
                return new AdvantageState
                {
                    State = RollState.Normal,
                    Reasons = reasons,
                    Note = "Advantage and disadvantage cancel out"
                };
            }
            else if (advantageCount > 0)
            {
                return new AdvantageState
                {
                    State = RollState.Advantage,
                    Reasons = reasons
                };
            }
            else if (disadvantageCount > 0)
            {
                return new AdvantageState
                {
                    State = RollState.Disadvantage,
                    Reasons = reasons
                };
            }

            return new AdvantageState
            {
                State = RollState.Normal,
                Reasons = reasons
            };
        }

        /// <summary>
        /// Rolls an attack with appropriate modifiers and advantage state.
        /// </summary>
        public AttackRollResult RollAttack(int attackBonus, AdvantageState advantageState, int targetAC)
        {
            int roll1 = _random.Next(1, 21);
            int roll2 = advantageState.State != RollState.Normal ? _random.Next(1, 21) : roll1;

            int finalRoll = advantageState.State switch
            {
                RollState.Advantage => Math.Max(roll1, roll2),
                RollState.Disadvantage => Math.Min(roll1, roll2),
                _ => roll1
            };

            int total = finalRoll + attackBonus;
            bool isCritical = finalRoll == 20;
            bool isCriticalMiss = finalRoll == 1;
            bool hits = !isCriticalMiss && (isCritical || total >= targetAC);

            return new AttackRollResult
            {
                NaturalRoll = finalRoll,
                Roll1 = roll1,
                Roll2 = roll2,
                AttackBonus = attackBonus,
                Total = total,
                TargetAC = targetAC,
                Hits = hits,
                IsCriticalHit = isCritical,
                IsCriticalMiss = isCriticalMiss,
                AdvantageState = advantageState
            };
        }

        #endregion

        #region Range Calculations with Elevation

        /// <summary>
        /// Calculates the distance between two tokens considering elevation.
        /// </summary>
        public double CalculateDistance(Token a, Token b, bool includeElevation = true)
        {
            double dx = (b.GridX - a.GridX) * 5; // Convert to feet
            double dy = (b.GridY - a.GridY) * 5;
            
            if (!includeElevation || !DevSettings.Instance.EnableTokenElevation)
            {
                return Math.Sqrt(dx * dx + dy * dy);
            }

            double dz = b.ElevationFeet - a.ElevationFeet;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Checks if a target is within range of an attack/spell.
        /// </summary>
        public bool IsInRange(Token attacker, Token target, int rangeFeet, bool includeElevation = true)
        {
            double distance = CalculateDistance(attacker, target, includeElevation);
            return distance <= rangeFeet;
        }

        #endregion

        #region Helper Methods

        private static bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double d1 = CrossProduct(p4 - p3, p1 - p3);
            double d2 = CrossProduct(p4 - p3, p2 - p3);
            double d3 = CrossProduct(p2 - p1, p3 - p1);
            double d4 = CrossProduct(p2 - p1, p4 - p1);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                return true;

            return false;
        }

        private static double CrossProduct(Vector a, Vector b) => a.X * b.Y - a.Y * b.X;

        private static bool LineIntersectsRect(Point lineStart, Point lineEnd, 
            double rectX, double rectY, double rectW, double rectH)
        {
            // Check if line intersects any edge of the rectangle
            var topLeft = new Point(rectX, rectY);
            var topRight = new Point(rectX + rectW, rectY);
            var bottomLeft = new Point(rectX, rectY + rectH);
            var bottomRight = new Point(rectX + rectW, rectY + rectH);

            return SegmentsIntersect(lineStart, lineEnd, topLeft, topRight) ||
                   SegmentsIntersect(lineStart, lineEnd, topRight, bottomRight) ||
                   SegmentsIntersect(lineStart, lineEnd, bottomRight, bottomLeft) ||
                   SegmentsIntersect(lineStart, lineEnd, bottomLeft, topLeft);
        }

        #endregion
    }

    #region Result Types

    public enum CoverType
    {
        None,
        Half,           // +2 AC, +2 DEX saves
        ThreeQuarters,  // +5 AC, +5 DEX saves
        Full            // Can't be targeted directly
    }

    public enum RollState
    {
        Normal,
        Advantage,
        Disadvantage
    }

    public class SneakAttackResult
    {
        public bool IsEligible { get; set; }
        public string? Reason { get; set; }
        public string? AllyName { get; set; }
    }

    public class AdvantageState
    {
        public RollState State { get; set; } = RollState.Normal;
        public List<string> Reasons { get; set; } = new();
        public string? Note { get; set; }
    }

    public class AttackRollResult
    {
        public int NaturalRoll { get; set; }
        public int Roll1 { get; set; }
        public int Roll2 { get; set; }
        public int AttackBonus { get; set; }
        public int Total { get; set; }
        public int TargetAC { get; set; }
        public bool Hits { get; set; }
        public bool IsCriticalHit { get; set; }
        public bool IsCriticalMiss { get; set; }
        public AdvantageState AdvantageState { get; set; } = new();

        public string GetDescription()
        {
            string advText = AdvantageState.State switch
            {
                RollState.Advantage => $" (Adv: {Roll1}, {Roll2})",
                RollState.Disadvantage => $" (Dis: {Roll1}, {Roll2})",
                _ => ""
            };

            string result = IsCriticalHit ? "CRITICAL HIT!" :
                           IsCriticalMiss ? "CRITICAL MISS!" :
                           Hits ? "HIT" : "MISS";

            return $"Roll: {NaturalRoll}{advText} + {AttackBonus} = {Total} vs AC {TargetAC} - {result}";
        }
    }

    #endregion
}
