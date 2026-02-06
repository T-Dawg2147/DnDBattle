using System;
using System.Windows;
using System.Globalization;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Views.Settings;

namespace DnDBattle.Views.Settings
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();

            TxtCellSize.Text = Options.DefaultGridCellSize.ToString(CultureInfo.InvariantCulture);
            TxtMaxWidth.Text = Options.GridMaxWidth.ToString();
            TxtMaxHeight.Text = Options.GridMaxHeight.ToString();
            TxtShadowSoftness.Text = Options.ShadowSoftnessPx.ToString(CultureInfo.InvariantCulture);
            TxtPathSpeed.Text = Options.PathSpeedSquaresPerSecond.ToString(CultureInfo.InvariantCulture);
            ChkAutoAOO.IsChecked = Options.AutoResolveAOOs;
            ChkLiveMode.IsChecked = Options.LiveMode;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TxtCellSize.Text, out double cs)) Options.DefaultGridCellSize = Math.Max(8, cs);
            if (int.TryParse(TxtMaxWidth.Text, out int mw)) Options.GridMaxWidth = Math.Max(1, mw);
            if (int.TryParse(TxtMaxHeight.Text, out int mh)) Options.GridMaxHeight = Math.Max(1, mh);
            if (double.TryParse(TxtShadowSoftness.Text, out double ss)) Options.ShadowSoftnessPx = Math.Max(0, ss);
            if (double.TryParse(TxtPathSpeed.Text, out double ps)) Options.PathSpeedSquaresPerSecond = Math.Max(0.1, ps);

            Options.AutoResolveAOOs = ChkAutoAOO.IsChecked == true;
            Options.LiveMode = ChkLiveMode.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
