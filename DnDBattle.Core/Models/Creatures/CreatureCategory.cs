using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Models.Creatures
{
    public class CreatureCategory : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Id { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value;OnPropertyChanged(nameof(Name)); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string ParentId { get; set; }

        private string _icon = "📁";
        public string Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(nameof(Icon)); OnPropertyChanged(nameof(DisplayName)); }
        }

        public int SortOrder { get; set; }
        public bool IsSystem { get; set; }

        private int _creatureCount;
        public int CreatureCount
        {
            get => _creatureCount;
            set { _creatureCount = value; OnPropertyChanged(nameof(CreatureCount)); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string DisplayName => $"{Icon} {Name} ({CreatureCount})";
    }
}
