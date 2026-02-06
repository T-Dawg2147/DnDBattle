using DnDBattle.Services;
using System;
using System.Linq;
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
using DnDBattle.Views.Dice;
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
    public partial class CombatStatisticsPanel : UserControl
    {
        private CombatStatisticsService _statsService;

        public CombatStatisticsPanel()
        {
            InitializeComponent();
        }

        public void SetStatsService(CombatStatisticsService service)
        {
            _statsService = service;
            _statsService.StatsUpdated += RefreshDisplay;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (_statsService == null) return;

            Dispatcher.Invoke(() =>
            {
                // Update header
                TxtRound.Text = _statsService.TotalRounds.ToString();
                TxtDuration.Text = _statsService.CombatDuration.ToString(@"m\:ss");

                // Clear and rebuild stats
                StatsContainer.Children.Clear();

                var allStats = _statsService.GetAllStats().ToList();

                if (allStats.Count == 0)
                {
                    StatsContainer.Children.Add(new TextBlock
                    {
                        Text = "No combat data yet",
                        Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                        FontStyle = FontStyles.Italic,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 0)
                    });
                    return;
                }

                foreach (var stats in allStats)
                {
                    StatsContainer.Children.Add(CreateStatCard(stats));
                }

                // Update summary
                var summary = _statsService.GetCombatSummary();
                TxtTotalDamage.Text = summary.TotalDamageDealt.ToString("N0");
                TxtTotalHealing.Text = summary.TotalHealing.ToString("N0");
                TxtTotalKills.Text = summary.TotalKills.ToString();
                TxtTotalCrits.Text = allStats.Sum(s => s.CriticalHits).ToString();
            });
        }

        private Border CreateStatCard(TokenCombatStats stats)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(37, 37, 38)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var mainStack = new StackPanel();

            // Name row
            var nameRow = new Grid();
            nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameTxt = new TextBlock
            {
                Text = stats.TokenName,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            };
            Grid.SetColumn(nameTxt, 0);
            nameRow.Children.Add(nameTxt);

            // Damage badge
            var damageBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 2, 8, 2)
            };
            damageBadge.Child = new TextBlock
            {
                Text = $"{stats.TotalDamageDealt} dmg",
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(damageBadge, 1);
            nameRow.Children.Add(damageBadge);

            mainStack.Children.Add(nameRow);

            // Stats row
            var statsRow = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };

            // Attacks
            if (stats.TotalAttacks > 0)
            {
                statsRow.Children.Add(CreateMiniStat("Attacks", $"{stats.TotalHits}/{stats.TotalAttacks}", Color.FromRgb(100, 181, 246)));
                statsRow.Children.Add(CreateMiniStat("Hit %", $"{stats.HitPercentage:F0}%", Color.FromRgb(76, 175, 80)));
            }

            // Crits
            if (stats.CriticalHits > 0)
            {
                statsRow.Children.Add(CreateMiniStat("Crits", stats.CriticalHits.ToString(), Color.FromRgb(255, 215, 0)));
            }

            // Crit fails
            if (stats.CriticalMisses > 0)
            {
                statsRow.Children.Add(CreateMiniStat("Nat 1s", stats.CriticalMisses.ToString(), Color.FromRgb(156, 39, 176)));
            }

            // Kills
            if (stats.Kills > 0)
            {
                statsRow.Children.Add(CreateMiniStat("Kills", stats.Kills.ToString(), Color.FromRgb(255, 152, 0)));
            }

            // Healing
            if (stats.TotalHealingDone > 0)
            {
                statsRow.Children.Add(CreateMiniStat("Healing", stats.TotalHealingDone.ToString(), Color.FromRgb(76, 175, 80)));
            }

            // Damage taken
            if (stats.TotalDamageTaken > 0)
            {
                statsRow.Children.Add(CreateMiniStat("Taken", stats.TotalDamageTaken.ToString(), Color.FromRgb(183, 28, 28)));
            }

            mainStack.Children.Add(statsRow);

            // Highest hit
            if (stats.HighestSingleHit > 0)
            {
                mainStack.Children.Add(new TextBlock
                {
                    Text = $"Best hit: {stats.HighestSingleHit} vs {stats.HighestHitTarget}",
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    FontSize = 10,
                    Margin = new Thickness(0, 5, 0, 0)
                });
            }

            card.Child = mainStack;
            return card;
        }

        private Border CreateMiniStat(string label, string value, Color color)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 0, 6, 0)
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock
            {
                Text = $"{label}: ",
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontSize = 10
            });
            stack.Children.Add(new TextBlock
            {
                Text = value,
                Foreground = new SolidColorBrush(color),
                FontSize = 10,
                FontWeight = FontWeights.Bold
            });

            border.Child = stack;
            return border;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDisplay();
        }
    }
}