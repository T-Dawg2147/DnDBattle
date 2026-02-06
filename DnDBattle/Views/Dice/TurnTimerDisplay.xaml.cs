using DnDBattle.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    public partial class TurnTimerDisplay : UserControl
    {
        private TurnTimerService _timerService;
        private double _totalWidth;

        public event Action<string> LogMessage;
        public event Action TimerExpired;

        public TurnTimerDisplay()
        {
            InitializeComponent();

            _timerService = new TurnTimerService();
            _timerService.TimerTick += OnTimerTick;
            _timerService.TimerExpired += OnTimerExpired;
            _timerService.TurnEnded += OnTurnEnded;

            Loaded += (s, e) => _totalWidth = TimerProgressBar.ActualWidth > 0 ? ActualWidth - 80 : 150;
            SizeChanged += (s, e) => _totalWidth = ActualWidth - 80;
        }

        public TurnTimerService TimerService => _timerService;

        public bool IsEnabled
        {
            get => _timerService.IsEnabled;
            set
            {
                _timerService.IsEnabled = value;
                TimerBorder.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void SetTimeLimit(int seconds)
        {
            _timerService.SetTimeLimit(seconds);
        }

        public void StartTurn()
        {
            if (!_timerService.IsEnabled) return;

            _timerService.Start();
            UpdateDisplay(_timerService.TimeLimit);
        }

        public void EndTurn()
        {
            _timerService.Stop();
        }

        public void PauseTurn()
        {
            _timerService.Pause();
            TxtTimerIcon.Text = "⏸️";
        }

        public void ResumeTurn()
        {
            _timerService.Resume();
            TxtTimerIcon.Text = "⏱️";
        }

        private void OnTimerTick(TimeSpan remaining)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateDisplay(remaining);

                // Warning at 30 seconds
                if (remaining.TotalSeconds <= 30 && remaining.TotalSeconds > 29)
                {
                    LogMessage?.Invoke("⏰ 30 seconds remaining!");
                }

                // Warning at 10 seconds
                if (remaining.TotalSeconds <= 10 && remaining.TotalSeconds > 9)
                {
                    LogMessage?.Invoke("⏰ 10 seconds remaining!");
                }
            });
        }

        private void UpdateDisplay(TimeSpan remaining)
        {
            // Update time text
            if (remaining.TotalHours >= 1)
            {
                TxtTimeRemaining.Text = remaining.ToString(@"h\:mm\:ss");
            }
            else
            {
                TxtTimeRemaining.Text = remaining.ToString(@"m\:ss");
            }

            // Update progress bar
            double percent = remaining.TotalSeconds / _timerService.TimeLimit.TotalSeconds;
            TimerProgressBar.Width = Math.Max(0, _totalWidth * percent);

            // Update colors based on time remaining
            if (remaining.TotalSeconds <= 10)
            {
                TxtTimeRemaining.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                TimerProgressBar.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                TxtTimerIcon.Text = "🔴";
            }
            else if (remaining.TotalSeconds <= 30)
            {
                TxtTimeRemaining.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                TimerProgressBar.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                TxtTimerIcon.Text = "🟡";
            }
            else
            {
                TxtTimeRemaining.Foreground = Brushes.White;
                TimerProgressBar.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                TxtTimerIcon.Text = "⏱️";
            }
        }

        private void OnTimerExpired()
        {
            Dispatcher.Invoke(() =>
            {
                TxtTimeRemaining.Text = "TIME!";
                TxtTimeRemaining.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                TxtTimerIcon.Text = "⏰";
                TimerProgressBar.Width = 0;

                LogMessage?.Invoke("⏰ Turn timer expired!");
                TimerExpired?.Invoke();
            });
        }

        private void OnTurnEnded(TimeSpan elapsed)
        {
            // Could log turn duration
            System.Diagnostics.Debug.WriteLine($"Turn lasted: {elapsed:m\\:ss}");
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TurnTimerSettingsDialog(_timerService)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                IsEnabled = dialog.TimerEnabled;

                if (dialog.TimerEnabled)
                {
                    TimerBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    TimerBorder.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}