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

        public ObservableCollection<TileMetadata> Metadata { get; set; } = new ObservableCollection<TileMetadata>();

        public bool HasMetadata => Metadata != null && Metadata.Count > 0;

        public bool HasMetadataType(TileMetadataType type)
        {
            foreach (var meta in Metadata)
            {
                if (meta.Type == type)
                    return true;
            }
            return false;
        }

        public List<TileMetadata> GetMetadata(TileMetadataType type)
        {
            var result = new List<TileMetadata>();
            foreach (var meta in Metadata)
            {
                if (meta.Type == type)
                    result.Add(meta);
            }
            return result;
        }

        public int GetEffectiveZIndex(TileDefinition tileDef)
        {
            if (ZIndex.HasValue)
                return ZIndex.Value;

            return tileDef?.Layer.GetDefaultZIndex() ?? 0;
        }
    }
}
