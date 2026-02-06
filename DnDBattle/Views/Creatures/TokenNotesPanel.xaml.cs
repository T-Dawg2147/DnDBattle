using DnDBattle.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
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

namespace DnDBattle.Views.Creatures
{
    public partial class TokenNotesPanel : UserControl
    {
        private Token _token;

        public event Action<string> LogAction;

        public TokenNotesPanel()
        {
            InitializeComponent();
        }

        public void SetToken(Token token)
        {
            _token = token;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            NotesContainer.Children.Clear();

            if (_token == null)
            {
                MainBorder.Visibility = Visibility.Collapsed;
                return;
            }

            MainBorder.Visibility = Visibility.Visible;

            if (_token.CombatNotes == null || _token.CombatNotes.Count == 0)
            {
                NotesContainer.Children.Add(new TextBlock
                {
                    Text = "No notes",
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    FontStyle = FontStyles.Italic,
                    FontSize = 10
                });
                return;
            }

            foreach (var note in _token.CombatNotes)
            {
                NotesContainer.Children.Add(CreateNoteItem(note));
            }
        }

        private Border CreateNoteItem(TokenNote note)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            var icon = new TextBlock
            {
                Text = note.Type.GetIcon(),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            // Text
            var text = new TextBlock
            {
                Text = note.Text,
                Foreground = Brushes.White,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            // Delete button
            var deleteBtn = new Button
            {
                Content = "×",
                Width = 18,
                Height = 18,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Tag = note.Id,
                Margin = new Thickness(5, 0, 0, 0)
            };
            deleteBtn.Click += (s, e) =>
            {
                if (s is Button btn && btn.Tag is string noteId)
                {
                    _token.RemoveNote(noteId);
                    UpdateDisplay();
                }
            };
            Grid.SetColumn(deleteBtn, 2);
            grid.Children.Add(deleteBtn);

            // Expiry indicator
            if (note.ExpiresOnRound.HasValue)
            {
                border.ToolTip = $"Expires after round {note.ExpiresOnRound}";
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                border.BorderThickness = new Thickness(1);
            }

            border.Child = grid;
            return border;
        }

        private void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            QuickAddPanel.Visibility = Visibility.Visible;
            TxtQuickNote.Focus();
        }

        private void BtnConfirmNote_Click(object sender, RoutedEventArgs e)
        {
            AddNoteFromInput();
        }

        private void TxtQuickNote_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddNoteFromInput();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                QuickAddPanel.Visibility = Visibility.Collapsed;
                TxtQuickNote.Text = "";
                e.Handled = true;
            }
        }

        private void AddNoteFromInput()
        {
            string text = TxtQuickNote.Text?.Trim();
            if (string.IsNullOrEmpty(text) || _token == null) return;

            // Detect note type from keywords
            NoteType type = DetectNoteType(text);

            _token.AddNote(text, type);
            LogAction?.Invoke($"📝 Added note to {_token.Name}: {text}");

            TxtQuickNote.Text = "";
            QuickAddPanel.Visibility = Visibility.Collapsed;
            UpdateDisplay();
        }

        private NoteType DetectNoteType(string text)
        {
            string lower = text.ToLower();

            if (lower.Contains("shield") || lower.Contains("reaction") || lower.Contains("used"))
                return NoteType.Combat;
            if (lower.Contains("advantage") || lower.Contains("disadvantage") || lower.Contains("reminder"))
                return NoteType.Reminder;
            if (lower.Contains("until") || lower.Contains("end of") || lower.Contains("turn"))
                return NoteType.Condition;
            if (lower.Contains("remaining") || lower.Contains("/") || lower.Contains("left"))
                return NoteType.Tracking;

            return NoteType.General;
        }
    }
}