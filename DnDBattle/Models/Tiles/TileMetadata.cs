using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Tiles
{
    public abstract class TileMetadata : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Guid Id { get; set; } = Guid.NewGuid();

        public abstract TileMetadataType Type { get; }

        public string Name { get; set; }

        public bool IsVisibleToPlayer { get; set; } = false;

        public bool IsTriggered { get; set; } = false;

        public string DMNotes { get; set; }

        public bool IsEnabled { get; set; } = true;
    }
}
