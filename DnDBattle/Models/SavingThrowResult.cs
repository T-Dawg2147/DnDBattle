namespace DnDBattle.Models
{
    /// <summary>
    /// Result of a single saving throw roll.
    /// </summary>
    public class SavingThrowResult
    {
        public Token Target { get; set; }
        public Ability Ability { get; set; }
        public int DC { get; set; }
        public int D20Roll { get; set; }
        public int Modifier { get; set; }
        public int Total { get; set; }
        public bool Success { get; set; }
        public bool IsNaturalOne { get; set; }
        public bool IsNaturalTwenty { get; set; }
        public bool UsedLegendaryResistance { get; set; }
        public bool AutoFailed { get; set; }

        public override string ToString()
        {
            string targetName = Target?.Name ?? "Target";

            if (AutoFailed)
                return $"{targetName}: Auto-FAIL {Ability} save (condition)";

            string result = $"{targetName}: {D20Roll}+{Modifier}={Total} vs DC {DC}";

            if (IsNaturalTwenty)
                result += " (Nat 20!)";
            else if (IsNaturalOne)
                result += " (Nat 1!)";

            result += Success ? " SUCCESS" : " FAIL";

            if (UsedLegendaryResistance)
                result += " (Legendary Resistance)";

            return result;
        }
    }
}
