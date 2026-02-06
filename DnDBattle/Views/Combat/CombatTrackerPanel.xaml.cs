using DnDBattle.Models;
using DnDBattle.Utils;
using DnDBattle.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using DnDBattle.Views;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Views.Combat;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Views.Combat
{
    /// <summary>
    /// Interaction logic for CombatTrackerPanel.xaml
    /// </summary>
    public partial class CombatTrackerPanel : UserControl
    {
        public CombatTrackerPanel()
        {
            InitializeComponent();
        }

        private void InitiativeItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is Token token)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SelectedToken = token;
                }
            }
        }

    }
}
