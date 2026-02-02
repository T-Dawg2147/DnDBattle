using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Tiles
{
    public class Tile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Guid InstanceId { get; set; } = Guid.NewGuid();

        public string TileDefinitionId { get; set; }

        private int _gridX;
        public int GridX
        {
            get => _gridX;
            set { _gridX = value; OnPropertyChanged(nameof(GridX)); }
        }

        private int _gridY;
        public int GridY
        {
            get => _gridY;
            set { _gridY = value; OnPropertyChanged(nameof(GridY)); }
        }

        public int Rotation { get; set; } = 0;

        public bool FlipHorizontal { get; set; } = false;

        public bool FlipVertical { get; set; } = false;

        public int? ZIndex { get; set; } = null;

        public string Notes { get; set; }
    }
}
