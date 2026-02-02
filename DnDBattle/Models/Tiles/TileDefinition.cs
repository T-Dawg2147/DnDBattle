using System;
using System.ComponentModel;
using System.Windows.Media;

namespace DnDBattle.Models.Tiles
{
    public class TileDefinition : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ImagePath { get; set; }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(nameof(DisplayName)); }
        }

        public string Category { get; set; } = "General";

        public string Description { get; set; }

        public bool BlocksMovement { get; set; } = false;

        public bool BlocksSight { get; set; } = false;

        public bool BlocksLight { get; set; } = false;

        public Color? TintColor { get; set; } = null;

        public int ZIndex { get; set; } = 0;

        public bool IsEnabled { get; set; } = true;

        public override string ToString() => DisplayName ?? ImagePath ?? "Unamed Tile";
    }
}
