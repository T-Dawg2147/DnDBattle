using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DnDBattle.Controls;
using DnDBattle.Models;
using DnDBattle.ViewModels;

namespace DnDBattle.Views
{
    /// <summary>
    /// Phase 5: Advanced Token Features window.
    /// Provides UI for managing A* pathfinding, movement cost preview, path animation,
    /// AOO detection, token auras, elevation, and facing/flanking.
    /// </summary>
    public partial class Phase5TokenFeaturesWindow : Window
    {
        // Reference to the battle grid for visual updates
        private readonly BattleGridControl _battleGrid;

        // Reference to the main view model for token access
        private readonly MainViewModel _viewModel;

        public Phase5TokenFeaturesWindow(BattleGridControl battleGrid, MainViewModel viewModel)
        {
            InitializeComponent();
            _battleGrid = battleGrid;
            _viewModel = viewModel;

            // Subscribe to selected token changes to keep UI in sync
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            LoadCurrentValues();
            RefreshSelectedTokenDisplay();
        }

        // Load current option values into all controls
        private void LoadCurrentValues()
        {
            // 5.1 Pathfinding
            ChkEnablePathfinding.IsChecked = Options.EnablePathfinding;
            ChkAllowDiagonal.IsChecked = Options.AllowDiagonalMovement;
            SliderMaxDepth.Value = Options.PathfindingMaxDepth;

            // 5.2 Movement Cost Preview
            ChkEnableMovementPreview.IsChecked = Options.EnableMovementCostPreview;

            // 5.3 Path Animation
            ChkEnablePathAnimation.IsChecked = Options.EnablePathAnimation;
            SliderAnimationSpeed.Value = Options.PathAnimationSecondsPerSquare;

            // 5.4 AOO Detection
            ChkEnableAOO.IsChecked = Options.EnableAOODetection;

            // 5.5 Token Auras
            ChkEnableAuras.IsChecked = Options.EnableTokenAuras;

            // 5.6 Token Elevation
            ChkEnableElevation.IsChecked = Options.EnableTokenElevation;

            // 5.7 Token Facing
            ChkEnableFacing.IsChecked = Options.EnableTokenFacing;
            ChkAutoFace.IsChecked = Options.AutoFaceMovementDirection;
            ChkEnableFlanking.IsChecked = Options.EnableFlankingDetection;
            SliderFacingAngle.Value = 0;

            UpdateLabels();
        }

        // React to SelectedToken property changes on the view model
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedToken))
            {
                RefreshSelectedTokenDisplay();
            }
        }

        // Update the selected token name label and refresh aura list
        private void RefreshSelectedTokenDisplay()
        {
            var token = _viewModel?.SelectedToken;
            if (token != null)
            {
                TxtSelectedToken.Text = token.Name ?? "(unnamed)";
                AurasList.ItemsSource = token.Auras;

                // Sync elevation combo to token's current elevation
                int elev = token.Elevation;
                CmbElevation.SelectedIndex = elev switch
                {
                    0 => 0,
                    5 => 1,
                    10 => 2,
                    15 => 3,
                    20 => 4,
                    30 => 5,
                    50 => 6,
                    100 => 7,
                    _ => 0
                };

                // Sync facing slider to token's current angle
                SliderFacingAngle.Value = token.FacingAngle;
            }
            else
            {
                TxtSelectedToken.Text = "(none)";
                AurasList.ItemsSource = null;
            }
        }

        // Update label text for all sliders
        private void UpdateLabels()
        {
            if (TxtMaxDepth != null) TxtMaxDepth.Text = $"{SliderMaxDepth.Value:F0} nodes";
            if (TxtAnimationSpeed != null) TxtAnimationSpeed.Text = $"{SliderAnimationSpeed.Value:F1} sec/sq";
            if (TxtFacingAngle != null) TxtFacingAngle.Text = $"{SliderFacingAngle.Value:F0}°";
        }

        #region 5.5 Token Auras - Add/Remove preset auras to selected token

        // Add a Paladin Aura (10 sq radius holy glow) to the selected token
        private void AddPaladinAura_Click(object sender, RoutedEventArgs e)
        {
            AddAuraToSelectedToken(TokenAura.PaladinAura());
        }

        // Add Spirit Guardians aura to the selected token
        private void AddSpiritGuardians_Click(object sender, RoutedEventArgs e)
        {
            AddAuraToSelectedToken(TokenAura.SpiritGuardians());
        }

        // Add Rage aura to the selected token
        private void AddRage_Click(object sender, RoutedEventArgs e)
        {
            AddAuraToSelectedToken(TokenAura.Rage());
        }

        // Add Bless aura to the selected token
        private void AddBless_Click(object sender, RoutedEventArgs e)
        {
            AddAuraToSelectedToken(TokenAura.Bless());
        }

        // Shared helper to add an aura to the selected token and refresh visuals
        private void AddAuraToSelectedToken(TokenAura aura)
        {
            var token = _viewModel?.SelectedToken;
            if (token == null)
            {
                MessageBox.Show("Please select a token first.", "No Token Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            token.Auras.Add(aura);
            _battleGrid.RedrawAuras();
            RefreshSelectedTokenDisplay();
        }

        // Remove the selected aura from the token's aura list
        private void RemoveAura_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel?.SelectedToken;
            if (token == null)
            {
                MessageBox.Show("Please select a token first.", "No Token Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (AurasList.SelectedItem is TokenAura selectedAura)
            {
                token.Auras.Remove(selectedAura);
                _battleGrid.RedrawAuras();
                RefreshSelectedTokenDisplay();
            }
            else
            {
                MessageBox.Show("Please select an aura to remove.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region 5.6 Token Elevation - Set elevation from combo preset

        // Apply the selected elevation preset to the currently selected token
        private void ApplyElevation_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel?.SelectedToken;
            if (token == null)
            {
                MessageBox.Show("Please select a token first.", "No Token Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Map combo index to elevation value in feet
            int elevation = CmbElevation.SelectedIndex switch
            {
                0 => 0,
                1 => 5,
                2 => 10,
                3 => 15,
                4 => 20,
                5 => 30,
                6 => 50,
                7 => 100,
                _ => 0
            };

            token.Elevation = elevation;
            _battleGrid.InitializePhase5Visuals();
        }

        #endregion

        #region 5.7 Token Facing - Set facing angle from slider

        // Apply the facing angle from the slider to the currently selected token
        private void ApplyFacing_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel?.SelectedToken;
            if (token == null)
            {
                MessageBox.Show("Please select a token first.", "No Token Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            token.FacingAngle = SliderFacingAngle.Value;
            _battleGrid.InitializePhase5Visuals();
        }

        #endregion

        // Handle feature toggle checkbox changes - sync all checkboxes to Options
        private void OnFeatureToggled(object sender, RoutedEventArgs e)
        {
            // 5.1 Pathfinding options
            Options.EnablePathfinding = ChkEnablePathfinding.IsChecked == true;
            Options.AllowDiagonalMovement = ChkAllowDiagonal.IsChecked == true;

            // 5.2 Movement cost preview
            Options.EnableMovementCostPreview = ChkEnableMovementPreview.IsChecked == true;

            // 5.3 Path animation
            Options.EnablePathAnimation = ChkEnablePathAnimation.IsChecked == true;

            // 5.4 AOO detection
            Options.EnableAOODetection = ChkEnableAOO.IsChecked == true;

            // 5.5 Token auras
            Options.EnableTokenAuras = ChkEnableAuras.IsChecked == true;

            // 5.6 Token elevation
            Options.EnableTokenElevation = ChkEnableElevation.IsChecked == true;

            // 5.7 Token facing and flanking
            Options.EnableTokenFacing = ChkEnableFacing.IsChecked == true;
            Options.AutoFaceMovementDirection = ChkAutoFace.IsChecked == true;
            Options.EnableFlankingDetection = ChkEnableFlanking.IsChecked == true;
        }

        // Handle slider value changes - sync to Options and update labels
        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 5.1 Pathfinding max depth
            Options.PathfindingMaxDepth = (int)SliderMaxDepth.Value;

            // 5.3 Path animation speed
            Options.PathAnimationSecondsPerSquare = SliderAnimationSpeed.Value;

            UpdateLabels();
        }

        // Unsubscribe from view model events when closing
        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            base.OnClosed(e);
        }
    }
}
