namespace DnDBattle.Models
{
    /// <summary>
    /// Types of vision that tokens can have
    /// </summary>
    public enum VisionType
    {
        /// <summary>Normal vision - requires light to see</summary>
        Normal,

        /// <summary>Low-light vision - can see in dim light as if bright</summary>
        LowLight,

        /// <summary>Darkvision - grayscale vision in darkness</summary>
        Darkvision,

        /// <summary>Blindsight - can sense without seeing</summary>
        Blindsight,

        /// <summary>Truesight - can see through illusions, invisibility, etc.</summary>
        Truesight,

        /// <summary>Tremorsense - can detect vibrations in the ground</summary>
        Tremorsense,

        /// <summary>Blind - cannot see at all</summary>
        Blind
    }

    /// <summary>
    /// Extension methods for VisionType
    /// </summary>
    public static class VisionTypeExtensions
    {
        public static string GetDisplayName(this VisionType visionType)
        {
            return visionType switch
            {
                VisionType.Normal => "Normal Vision",
                VisionType.LowLight => "Low-Light Vision",
                VisionType.Darkvision => "Darkvision",
                VisionType.Blindsight => "Blindsight",
                VisionType.Truesight => "Truesight",
                VisionType.Tremorsense => "Tremorsense",
                VisionType.Blind => "Blind",
                _ => "Unknown"
            };
        }

        public static string GetDescription(this VisionType visionType)
        {
            return visionType switch
            {
                VisionType.Normal => "Requires light to see. Dim light causes disadvantage on Perception checks.",
                VisionType.LowLight => "Can see in dim light as if it were bright light.",
                VisionType.Darkvision => "Can see in darkness as if it were dim light (grayscale only).",
                VisionType.Blindsight => "Can perceive surroundings without relying on sight.",
                VisionType.Truesight => "Can see through illusions, invisibility, and into the Ethereal Plane.",
                VisionType.Tremorsense => "Can detect creatures touching the ground within range.",
                VisionType.Blind => "Cannot see. Automatically fails checks requiring sight.",
                _ => ""
            };
        }

        public static string GetIcon(this VisionType visionType)
        {
            return visionType switch
            {
                VisionType.Normal => "👁️",
                VisionType.LowLight => "🌙",
                VisionType.Darkvision => "😈",
                VisionType.Blindsight => "🦇",
                VisionType.Truesight => "✨",
                VisionType.Tremorsense => "🌍",
                VisionType.Blind => "🕶️",
                _ => "❓"
            };
        }
    }
}
