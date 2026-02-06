using System;
using System.Collections.Generic;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Lightweight reference to a saved TileMap for the map library.
    /// </summary>
    public class TileMapReference
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Untitled Map";
        public string FilePath { get; set; }
        public string ThumbnailPath { get; set; }
        public DateTime LastUsed { get; set; } = DateTime.Now;
        public List<string> Tags { get; set; } = new List<string>();
    }
}
