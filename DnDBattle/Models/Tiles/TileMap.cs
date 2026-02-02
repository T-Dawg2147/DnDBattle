using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
