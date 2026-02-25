using DnDBattle.Models;
using DnDBattle.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DnDBattle.Views;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Views.Combat
{
    public partial class MultiAttackPanel : UserControl
    {
        private Token _token;
        private List<Models.Combat.Action> _multiattackActions = new List<Models.Combat.Action>();

        public event Action<string> LogAction;
        public event Action<Token, Models.Combat.Action> ActionSelected;

        public MultiAttackPanel()
        {
            InitializeComponent();
        }

        public void SetToken(Token token)
        {
            _token = token;
            AnalyzeMultiattack();
            UpdateDisplay();
        }

        private void AnalyzeMultiattack()
        {
            _multiattackActions.Clear();

            if (_token?.Actions == null) return;

            // Look for multiattack action
            var multiattack = _token.Actions.FirstOrDefault(a =>
                a.Name?.ToLower().Contains("multiattack") == true ||
                a.Name?.ToLower().Contains("multi-attack") == true);

            if (multiattack != null)
            {
                TxtMultiattackDescription.Text = multiattack.Description ?? "Makes multiple attacks.";

                // Try to parse which attacks are part of multiattack
                string desc = multiattack.Description?.ToLower() ?? "";

                foreach (var action in _token.Actions)
                {
                    if (action == multiattack) continue;
                    if (action.AttackBonus == null && string.IsNullOrEmpty(action.DamageExpression)) continue;

                    // Check if this action is mentioned in multiattack
                    string actionName = action.Name?.ToLower() ?? "";
                    if (desc.Contains(actionName) || IsLikelyAttack(action))
                    {
                        _multiattackActions.Add(action);
                    }
                }

                // If we couldn't parse, just add all attacks
                if (_multiattackActions.Count == 0)
                {
                    _multiattackActions = _token.Actions
                        .Where(a => a != multiattack && (a.AttackBonus != null || !string.IsNullOrEmpty(a.DamageExpression)))
                        .ToList();
                }
            }
        }

        private bool IsLikelyAttack(Models.Combat.Action action)
        {
            string name = action.Name?.ToLower() ?? "";
            return name.Contains("bite") || name.Contains("claw") || name.Contains("slam") ||
                   name.Contains("sword") || name.Contains("attack") || name.Contains("strike") ||
                   name.Contains("fist") || name.Contains("tail") || name.Contains("gore");
        }

        // VISUAL REFRESH - COMBAT_AUTOMATION
        private void UpdateDisplay()
        {
            AttackButtonsContainer.Children.Clear();

            if (_token == null || _multiattackActions.Count == 0)
            {
                MainBorder.Visibility = Visibility.Collapsed;
                return;
            }

            MainBorder.Visibility = Visibility.Visible;

            // Create button for each attack type
            foreach (var action in _multiattackActions)
            {
                var btn = CreateAttackButton(action);
                AttackButtonsContainer.Children.Add(btn);
            }
        }

        private Button CreateAttackButton(Models.Combat.Action action)
        {
            var btn = new Button
            {
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 4),
                Background = new SolidColorBrush(Color.FromRgb(62, 62, 66)),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = action,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            // Action name
            stack.Children.Add(new TextBlock
            {
                Text = action.Name,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });

            // Attack bonus
            if (action.AttackBonus != null)
            {
                stack.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(8, 0, 0, 0),
                    Child = new TextBlock
                    {
                        Text = $"+{action.AttackBonus}",
                        Foreground = Brushes.White,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold
                    }
                });
            }

            // Damage
            if (!string.IsNullOrEmpty(action.DamageExpression))
            {
                stack.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(4, 0, 0, 0),
                    Child = new TextBlock
                    {
                        Text = action.DamageExpression,
                        Foreground = Brushes.White,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold
                    }
                });
            }

            btn.Content = stack;

            btn.Click += (s, e) =>
            {
                if (s is Button b && b.Tag is Models.Combat.Action a)
                {
                    ActionSelected?.Invoke(_token, a);
                }
            };

            return btn;
        }

        private void BtnRollAll_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null || _multiattackActions.Count == 0) return;

            var result = MessageBox.Show(
                "Roll all multiattack attacks automatically?\n\nThis will roll attacks and damage for all actions.",
                "Roll All Attacks",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var log = new StringBuilder();
            log.AppendLine($"⚔️ {_token.Name} uses Multiattack!");
            log.AppendLine();

            int totalDamage = 0;
            int hits = 0;
            int crits = 0;

            foreach (var action in _multiattackActions)
            {
                var attackResult = RollAttack(action);
                log.AppendLine(attackResult.message);

                if (attackResult.hit)
                {
                    hits++;
                    totalDamage += attackResult.damage;
                    if (attackResult.crit) crits++;
                }
            }

            log.AppendLine();
            log.AppendLine($"📊 Results: {hits}/{_multiattackActions.Count} hits, {totalDamage} total damage" + (crits > 0 ? $", {crits} crits!" : ""));

            LogAction?.Invoke(log.ToString());

            // Show summary
            MessageBox.Show(
                $"Multiattack Results:\n\n" +
                $"Hits: {hits} / {_multiattackActions.Count}\n" +
                $"Total Damage: {totalDamage}\n" +
                (crits > 0 ? $"Critical Hits: {crits}\n" : "") +
                $"\nSee action log for details.",
                "Multiattack Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private (bool hit, bool crit, int damage, string message) RollAttack(Models.Combat.Action action)
        {
            var attackRoll = DiceRoller.RollExpression("1d20");
            int attackTotal = attackRoll.Total + (action.AttackBonus ?? 0);
            bool isCrit = attackRoll.Total == 20;
            bool isCritMiss = attackRoll.Total == 1;

            string msg;

            if (isCritMiss)
            {
                msg = $"  • {action.Name}: {attackRoll.Total}+{action.AttackBonus} = {attackTotal} - 💀 Critical Miss!";
                return (false, false, 0, msg);
            }

            // Assume hit for now (no target AC)
            int damage = 0;
            if (!string.IsNullOrEmpty(action.DamageExpression))
            {
                var damageRoll = DiceRoller.RollExpression(action.DamageExpression);
                damage = damageRoll.Total;

                if (isCrit)
                {
                    var critRoll = DiceRoller.RollExpression(action.DamageExpression);
                    damage += critRoll.Total;
                }
            }

            if (isCrit)
            {
                msg = $"  • {action.Name}: {attackRoll.Total}+{action.AttackBonus} = {attackTotal} - ⚡ CRIT! {damage} damage!";
                return (true, true, damage, msg);
            }
            else
            {
                msg = $"  • {action.Name}: {attackRoll.Total}+{action.AttackBonus} = {attackTotal} → {damage} damage";
                return (true, false, damage, msg);
            }
        }
    }
}