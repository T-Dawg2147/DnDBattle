using DnDBattle.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DnDBattle.Views
{
    public partial class FogOfWarToolbar : UserControl
    {
        // Events
        public event Action<bool> FogEnabledChanged;
        public event Action<FogBrushMode> BrushModeChanged;
        public event Action<int> BrushSizeChanged;
        public event Action<bool> PlayerViewChanged;
        public event Action RevealPlayersRequested;
        public event Action RevealAllRequested;
        public event Action HideAllRequested;
        public event Action<FogShapeTool> ShapeToolSelected;

        public FogOfWarToolbar()
        {
            InitializeComponent();
        }

        public bool IsFogEnabled => BtnEnableFog.IsChecked ?? false;
        public bool IsPlayerView => BtnPlayerView.IsChecked ?? false;
        public FogBrushMode CurrentBrushMode => RdoReveal.IsChecked == true ? FogBrushMode.Reveal : FogBrushMode.Hide;

        public int CurrentBrushSize
        {
            get
            {
                if (CmbBrushSize.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    return int.Parse(item.Tag.ToString());
                }
                return 3;
            }
        }

        private void BtnEnableFog_Checked(object sender, RoutedEventArgs e)
        {
            FogEnabledChanged?.Invoke(true);
        }

        private void BtnEnableFog_Unchecked(object sender, RoutedEventArgs e)
        {
            FogEnabledChanged?.Invoke(false);
        }

        private void BrushMode_Changed(object sender, RoutedEventArgs e)
        {
            var mode = RdoReveal.IsChecked == true ? FogBrushMode.Reveal : FogBrushMode.Hide;
            BrushModeChanged?.Invoke(mode);
        }

        private void CmbBrushSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BrushSizeChanged?.Invoke(CurrentBrushSize);
        }

        private void BtnRevealRect_Click(object sender, RoutedEventArgs e)
        {
            ShapeToolSelected?.Invoke(FogShapeTool.Rectangle);
        }

        private void BtnRevealCircle_Click(object sender, RoutedEventArgs e)
        {
            ShapeToolSelected?.Invoke(FogShapeTool.Circle);
        }

        private void BtnRevealPlayers_Click(object sender, RoutedEventArgs e)
        {
            RevealPlayersRequested?.Invoke();
        }

        private void BtnRevealAll_Click(object sender, RoutedEventArgs e)
        {
            RevealAllRequested?.Invoke();
        }

        private void BtnHideAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to hide the entire map?",
                "Reset Fog",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                HideAllRequested?.Invoke();
            }
        }

        private void BtnPlayerView_Changed(object sender, RoutedEventArgs e)
        {
            PlayerViewChanged?.Invoke(BtnPlayerView.IsChecked ?? false);
        }

        /// <summary>
        /// Sets the fog enabled state (for loading saved state)
        /// </summary>
        public void SetFogEnabled(bool enabled)
        {
            BtnEnableFog.IsChecked = enabled;
        }
    }

    public enum FogShapeTool
    {
        None,
        Rectangle,
        Circle
    }
}