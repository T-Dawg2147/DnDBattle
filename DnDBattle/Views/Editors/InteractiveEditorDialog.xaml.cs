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
