using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Views.Editors;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Views.Editors
{
    public partial class InteractiveEditorDialog : Window
    {
        public InteractiveMetadata Interactive { get; private set; }

        public InteractiveEditorDialog(InteractiveMetadata existing = null)
        {
            InitializeComponent();
            Interactive = existing ?? new InteractiveMetadata();

            if (existing != null)
            {
                LoadData();
            }
        }

        private void LoadData()
        {
            TxtName.Text = Interactive.Name;
            CmbObjectType.SelectedIndex = (int)Interactive.ObjectType;
            TxtExamine.Text = Interactive.ExamineDescription;
            TxtEffect.Text = Interactive.ActivationEffect;
            ChkRequiresCheck.IsChecked = Interactive.RequiresCheck;
            TxtDC.Text = Interactive.CheckDC.ToString();
            ChkSingleUse.IsChecked = Interactive.SingleUse;
            ChkLocked.IsChecked = Interactive.IsLocked;
            TxtGold.Text = Interactive.GoldPieces.ToString();
            TxtItems.Text = Interactive.ContainedItems;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Interactive.Name = TxtName.Text;
            Interactive.ObjectType = (InteractiveType)CmbObjectType.SelectedIndex;
            Interactive.ExamineDescription = TxtExamine.Text;
            Interactive.ActivationEffect = TxtEffect.Text;
            Interactive.RequiresCheck = ChkRequiresCheck.IsChecked ?? false;
            Interactive.CheckDC = int.TryParse(TxtDC.Text, out int dc) ? dc : 15;
            Interactive.SingleUse = ChkSingleUse.IsChecked ?? false;
            Interactive.IsLocked = ChkLocked.IsChecked ?? false;
            Interactive.GoldPieces = int.TryParse(TxtGold.Text, out int gold) ? gold : 0;
            Interactive.ContainedItems = TxtItems.Text;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
