namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Describes a link from one map tile (e.g. door/stairs) to a position on another map.
    /// </summary>
    public class MapLink
    {
        public string TargetMapId { get; set; }
        public int TargetX { get; set; }
        public int TargetY { get; set; }
    }
}
