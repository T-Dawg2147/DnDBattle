using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public class TileMap : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Guid Id { get; set; } = Guid.NewGuid();

        private string _name = "Untitled Map";
        public string Name
        {
            get => _name;
            set { _name = value;OnPropertyChanged(nameof(Name)); }
        }

        public int Width { get; set; } = 50;

        public int Height { get; set; } = 50;

        public double CellSize { get; set; } = 48.0;

        public ObservableCollection<Tile> PlacedTiles { get; set; } = new ObservableCollection<Tile>();

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public string BackgroundColor { get; set; } = "#FF1A1A1A";

        public bool ShowGrid { get; set; } = true;

        // ── Phase 8: Advanced Map Features ──

        /// <summary>Grid type: Square, HexFlatTop, or HexPointyTop.</summary>
        public GridType GridType { get; set; } = GridType.Square;

        /// <summary>When true, tokens are placed at exact pixel positions instead of snapping to the grid.</summary>
        public bool GridlessMode { get; set; }

        /// <summary>In gridless mode, optionally show a subtle reference grid overlay.</summary>
        public bool ShowGridOverlay { get; set; }

        /// <summary>Number of in-game feet each grid square represents (default 5 ft D&amp;D standard).</summary>
        public int FeetPerSquare { get; set; } = 5;

        /// <summary>Background image layers rendered beneath the tile grid.</summary>
        public List<BackgroundLayer> BackgroundLayers { get; set; } = new List<BackgroundLayer>();

        /// <summary>Map notes and labels pinned to grid positions.</summary>
        public List<MapNote> Notes { get; set; } = new List<MapNote>();

        // ── Queries ──

        public IEnumerable<Tile> GetTilesAt(int x, int y) =>
            PlacedTiles.Where(t => t.GridX == x && t.GridY == y);

        public void ClearTilesAt(int x, int y)
        {
            var tilesToRemove = GetTilesAt(x, y).ToList();
            foreach (var tile in tilesToRemove)
            {
                PlacedTiles.Remove(tile);
            }
        }

        public void AddTile(Tile tile)
        {
            if (tile.GridX < 0 || tile.GridX >= Width || tile.GridY < 0 || tile.GridY >= Height)
                return;

            PlacedTiles.Add(tile);
            ModifiedDate = DateTime.Now;
        }

        public void RemoveTile(Tile tile)
        {
            PlacedTiles.Remove(tile);
            ModifiedDate = DateTime.Now;
        }

        // ── Phase 8 helpers ──

        /// <summary>Convert a speed string like "30 ft" to squares based on the current FeetPerSquare.</summary>
        public int GetSpeedInSquares(string speedString)
        {
            if (string.IsNullOrWhiteSpace(speedString)) return 6; // default 30ft / 5ft
            var match = System.Text.RegularExpressions.Regex.Match(speedString, @"(\d+)");
            if (!match.Success) return 6;
            int feet = int.Parse(match.Groups[1].Value);
            return FeetPerSquare > 0 ? feet / FeetPerSquare : feet;
        }

        /// <summary>Rescale the map when changing feet-per-square. Adjusts tile positions and dimensions.</summary>
        public void ChangeGridScale(int newFeetPerSquare)
        {
            if (newFeetPerSquare <= 0 || newFeetPerSquare == FeetPerSquare) return;

            double scaleFactor = (double)FeetPerSquare / newFeetPerSquare;

            foreach (var tile in PlacedTiles)
            {
                tile.GridX = (int)(tile.GridX * scaleFactor);
                tile.GridY = (int)(tile.GridY * scaleFactor);
            }

            Width = Math.Max(1, (int)(Width * scaleFactor));
            Height = Math.Max(1, (int)(Height * scaleFactor));

            FeetPerSquare = newFeetPerSquare;
            ModifiedDate = DateTime.Now;
        }

        /// <summary>Add a note to the map.</summary>
        public void AddNote(MapNote note)
        {
            if (note == null) return;
            Notes.Add(note);
            ModifiedDate = DateTime.Now;
        }

        /// <summary>Remove a note by id.</summary>
        public bool RemoveNote(Guid noteId)
        {
            var note = Notes.FirstOrDefault(n => n.Id == noteId);
            if (note == null) return false;
            Notes.Remove(note);
            ModifiedDate = DateTime.Now;
            return true;
        }
    }
}
