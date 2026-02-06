using System.Windows;
using DnDBattle.ViewModels;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Views.Multiplayer;

namespace DnDBattle.Views.Multiplayer
{
    public partial class JoinGameWindow : Window
    {
        public JoinGameWindow()
        {
            InitializeComponent();
            DataContext = new PlayerClientViewModel();
        }
    }
}
