namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Defines the type of grid used on the map.
    /// </summary>
    public enum GridType
    {
        /// <summary>Standard square grid (default D&amp;D).</summary>
        Square,

        /// <summary>Hexagonal grid with flat top orientation.</summary>
        HexFlatTop,

        /// <summary>Hexagonal grid with pointy top orientation.</summary>
        HexPointyTop
    }
}
