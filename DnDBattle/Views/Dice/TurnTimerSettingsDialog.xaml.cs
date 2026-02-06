using DnDBattle.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;

namespace DnDBattle.Views.Dice
{
    public partial class TurnTimerSettingsDialog : Window
    {
        private TurnTimerService _timerService;

        public bool TimerEnabled { get; private set; }
        public int TimeLimitSeconds { get; private set; }
        public bool PlaySound { get; private set; }

        public TurnTimerSettingsDialog(TurnTimerService timerService)
        {
            InitializeComponent();

            _timerService = timerService;

            // Load current settings
            ChkEnabled.IsChecked = timerService.IsEnabled;
            SliderTime.Value = timerService.TimeLimit.TotalSeconds;
            UpdateTimeDisplay();
        }

        private void SliderTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            if (TxtTimeValue == null) return;

            int seconds = (int)SliderTime.Value;
            var time = TimeSpan.FromSeconds(seconds);
            TxtTimeValue.Text = time.ToString(@"m\:ss");
        }

        private void BtnPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string secondsStr)
            {
                if (int.TryParse(secondsStr, out int seconds))
                {
                    SliderTime.Value = seconds;
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            TimerEnabled = ChkEnabled.IsChecked ?? false;
            TimeLimitSeconds = (int)SliderTime.Value;
            PlaySound = ChkSound.IsChecked ?? true;

            // Apply settings
            _timerService.IsEnabled = TimerEnabled;
            _timerService.SetTimeLimit(TimeLimitSeconds);

            DialogResult = true;
            Close();
        }
    }
}