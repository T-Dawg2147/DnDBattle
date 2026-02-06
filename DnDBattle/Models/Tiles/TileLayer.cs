using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Models.Tiles
{
    public enum TileLayer
    {
        Floor = 0,
        Terrain = 10,
        Wall = 20,
        Door = 30,
        Furniture = 40,
        Props = 50,
        Effects = 60,
        Roof = 70
    }

    public static class TileLayerExtensions
    {
        public static string GetDisplayName(this TileLayer layer)
        {
            return layer switch
            {
                TileLayer.Floor => "Floor",
                TileLayer.Terrain => "Terrain",
                TileLayer.Wall => "Walls",
                TileLayer.Door => "Doors",
                TileLayer.Furniture => "Furniture",
                TileLayer.Props => "Props",
                TileLayer.Effects => "Effects",
                TileLayer.Roof => "Roof",
                _ => layer.ToString()
            };
        }

        public static string GetIcon(this TileLayer layer)
        {
            return layer switch
            {
                TileLayer.Floor => "🟫",
                TileLayer.Terrain => "🏔️",
                TileLayer.Wall => "🧱",
                TileLayer.Door => "🚪",
                TileLayer.Furniture => "🪑",
                TileLayer.Props => "📦",
                TileLayer.Effects => "✨",
                TileLayer.Roof => "🏠",
                _ => "📍"
            };
        }

        public static int GetDefaultZIndex(this TileLayer layer)
        {
            return (int)layer;
        }
    }
}
