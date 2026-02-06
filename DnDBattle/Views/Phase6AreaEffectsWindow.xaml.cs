using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DnDBattle.Controls;
using DnDBattle.Models;
using DnDBattle.Services;

namespace DnDBattle.Views
{
    /// <summary>
    /// Phase 6: Area Effects Expansion window.
    /// Provides UI for managing spell templates, duration tracking, damage over time,
    /// custom polygon areas, and effect animations.
    /// </summary>
    public partial class Phase6AreaEffectsWindow : Window
    {
        // Reference to the battle grid for placing effects and accessing AreaEffectService
        private readonly BattleGridControl _battleGrid;

        // Duration service for ticking down effect durations each round
        private EffectDurationService _durationService;

        public Phase6AreaEffectsWindow(BattleGridControl battleGrid)
        {
            InitializeComponent();
            _battleGrid = battleGrid;

            // Create duration service wired to the grid's area effect service
            _durationService = new EffectDurationService(_battleGrid.AreaEffectService);

            LoadCurrentValues();
            RefreshActiveEffects();
        }

        /// <summary>
        /// Load current option values into all controls
        /// </summary>
        private void LoadCurrentValues()
        {
            // 6.1 Spell Library
            ChkEnableSpellLibrary.IsChecked = Options.EnableSpellLibrary;

            // 6.2 Duration Tracking
            ChkEnableDurationTracking.IsChecked = Options.EnableDurationTracking;

            // 6.3 Damage Over Time
            ChkEnableDamageOverTime.IsChecked = Options.EnableDamageOverTime;
            ChkAutoApplyDot.IsChecked = Options.AutoApplyDotDamage;

            // 6.4 Custom Polygon Effects
            ChkEnablePolygonEffects.IsChecked = Options.EnablePolygonEffects;

            // 6.5 Effect Animations
            ChkEnableEffectAnimations.IsChecked = Options.EnableEffectAnimations;
            SliderMaxParticles.Value = Options.MaxParticlesPerEffect;
            CmbDefaultAnimation.SelectedIndex = Options.DefaultAnimationType;

            UpdateLabels();
        }

        /// <summary>
        /// Update label text for sliders
        /// </summary>
        private void UpdateLabels()
        {
            if (TxtMaxParticles != null)
                TxtMaxParticles.Text = $"{SliderMaxParticles.Value:F0} particles";
        }

        #region 6.1 Spell Templates Library

        /// <summary>
        /// Opens the SpellLibraryWindow and subscribes to SpellSelected to place the chosen spell
        /// </summary>
        private void OpenSpellLibrary_Click(object sender, RoutedEventArgs e)
        {
            var spellWindow = new SpellLibraryWindow();
            spellWindow.Owner = this;

            // When a spell is selected, start placement on the battle grid
            spellWindow.SpellSelected += (AreaEffect effect) =>
            {
                _battleGrid.StartAreaEffectPlacement(effect);
                RefreshActiveEffects();
            };

            spellWindow.ShowDialog();
        }

        /// <summary>
        /// Quick-place Fireball: 20ft sphere, fire-orange color
        /// </summary>
        private void QuickFireball_Click(object sender, RoutedEventArgs e)
        {
            _battleGrid.StartAreaEffectPlacement(AreaEffectPresets.Fireball);
        }

        /// <summary>
        /// Quick-place Lightning Bolt: 100ft line, electric blue color
        /// </summary>
        private void QuickLightningBolt_Click(object sender, RoutedEventArgs e)
        {
            _battleGrid.StartAreaEffectPlacement(AreaEffectPresets.LightningBolt);
        }

        /// <summary>
        /// Quick-place Cone of Cold: 60ft cone, icy blue color
        /// </summary>
        private void QuickConeOfCold_Click(object sender, RoutedEventArgs e)
        {
            _battleGrid.StartAreaEffectPlacement(AreaEffectPresets.ConeOfCold);
        }

        #endregion

        #region 6.2 Duration Tracking

        /// <summary>
        /// Manually advance one round, ticking down all effect durations and removing expired ones
        /// </summary>
        private void AdvanceRound_Click(object sender, RoutedEventArgs e)
        {
            var expired = _durationService.OnRoundEnd();

            if (expired.Count > 0)
            {
                TxtDurationStatus.Text = $"Expired: {string.Join(", ", expired)}";
            }
            else
            {
                TxtDurationStatus.Text = "Round advanced. No effects expired.";
            }

            RefreshActiveEffects();
        }

        #endregion

        #region Active Effects List

        /// <summary>
        /// Refresh the active effects list from the AreaEffectService
        /// </summary>
        private void RefreshActiveEffects()
        {
            var effects = _battleGrid.AreaEffectService.ActiveEffects.ToList();
            LstActiveEffects.ItemsSource = effects;

            // Update the duration status with a summary of active timed effects
            var timedEffects = effects.Where(e => e.DurationRounds > 0).ToList();
            if (timedEffects.Count > 0)
            {
                TxtDurationStatus.Text = $"{timedEffects.Count} effect(s) with duration tracking: " +
                    string.Join(", ", timedEffects.Select(e => $"{e.Name} ({e.RoundsRemaining} rds)"));
            }
            else if (effects.Count > 0)
            {
                TxtDurationStatus.Text = $"{effects.Count} active effect(s), none with duration tracking.";
            }
            else
            {
                TxtDurationStatus.Text = "No active area effects.";
            }
        }

        /// <summary>
        /// Remove a single effect by its Id (from the remove button in the list)
        /// </summary>
        private void RemoveEffect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid effectId)
            {
                _battleGrid.AreaEffectService.RemoveEffect(effectId);
                RefreshActiveEffects();
            }
        }

        /// <summary>
        /// Clear all active area effects from the battlefield
        /// </summary>
        private void ClearAllEffects_Click(object sender, RoutedEventArgs e)
        {
            _battleGrid.AreaEffectService.ClearAllEffects();
            RefreshActiveEffects();
        }

        /// <summary>
        /// Manually refresh the active effects list
        /// </summary>
        private void RefreshEffects_Click(object sender, RoutedEventArgs e)
        {
            RefreshActiveEffects();
        }

        #endregion

        #region Feature Toggles - Sync checkboxes to Options

        /// <summary>
        /// Handle feature toggle checkbox changes - sync all checkboxes to Options
        /// </summary>
        private void OnFeatureToggled(object sender, RoutedEventArgs e)
        {
            // 6.1 Spell Library
            Options.EnableSpellLibrary = ChkEnableSpellLibrary.IsChecked == true;

            // 6.2 Duration Tracking
            Options.EnableDurationTracking = ChkEnableDurationTracking.IsChecked == true;

            // 6.3 Damage Over Time
            Options.EnableDamageOverTime = ChkEnableDamageOverTime.IsChecked == true;
            Options.AutoApplyDotDamage = ChkAutoApplyDot.IsChecked == true;

            // 6.4 Custom Polygon Effects
            Options.EnablePolygonEffects = ChkEnablePolygonEffects.IsChecked == true;

            // 6.5 Effect Animations
            Options.EnableEffectAnimations = ChkEnableEffectAnimations.IsChecked == true;
        }

        /// <summary>
        /// Handle slider value changes - sync to Options and update labels
        /// </summary>
        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Options.MaxParticlesPerEffect = (int)SliderMaxParticles.Value;
            UpdateLabels();
        }

        /// <summary>
        /// Handle default animation type combo box change
        /// </summary>
        private void CmbDefaultAnimation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbDefaultAnimation.SelectedIndex >= 0)
            {
                Options.DefaultAnimationType = CmbDefaultAnimation.SelectedIndex;
            }
        }

        #endregion
    }
}
